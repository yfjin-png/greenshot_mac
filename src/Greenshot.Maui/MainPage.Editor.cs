using System.IO;
using Greenshot.Maui.Core.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Greenshot.Maui;

public partial class MainPage
{
	private const double EditorClickToEditDistance = 8d;
	private readonly ImageEditorSession _imageEditorSession = new();
	private Guid? _inlineTextEditingAnnotationId;
	private bool _isUpdatingInlineTextEditor;
	private Guid? _pendingInlineTextEditAnnotationId;
	private Point? _pendingInlineTextEditPoint;

	private async Task EnterImageEditorModeAsync(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		var documentInfo = await _imageEditorService.LoadAsync(filePath);
		_imageEditorSession.BeginEditing(filePath, documentInfo);
		RefreshWorkspaceState("Editing image");
	}

	private void ExitImageEditorMode(string? statusText = null)
	{
		SetSelectionPointerCursor(false);
		ClearEditorPointerIndicator();
		HideInlineTextEditor();
		_imageEditorSession.ExitEditing();
		RefreshWorkspaceState(statusText);
	}

	private void OnRectangleToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Rectangle);

	private void OnPencilToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Pencil);

	private void OnArrowToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Arrow);

	private void OnLineToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Line);

	private void OnHighlightToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Highlight);

	private void OnTextToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.Text);

	private void OnCalloutToolClicked(object? sender, EventArgs e) => SetEditorTool(ImageEditorTool.SpeechBubble);

	private void OnUndoAnnotationClicked(object? sender, EventArgs e)
	{
		if (_imageEditorSession.UndoLastAnnotation())
		{
			UpdateEditorVisual();
			UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
		}
	}

	private async void OnSaveEditedCopyClicked(object? sender, EventArgs e)
		=> await SaveEditedCopyAsync();

	private async Task SaveEditedCopyAsync()
	{
		if (!_imageEditorSession.IsEditing || string.IsNullOrWhiteSpace(_imageEditorSession.SourceImagePath))
		{
			return;
		}

		if (_imageEditorSession.Annotations.Count == 0)
		{
			await DisplayAlertAsync("Nothing to save", "Add at least one annotation before saving a copy.", "OK");
			return;
		}

		try
		{
			var outputPath = await _imageEditorService.SaveAnnotatedCopyAsync(
				_imageEditorSession.SourceImagePath,
				_imageEditorSession.Annotations);
			string? clipboardFailure = null;

			try
			{
				await CopyImageToClipboardAsync(outputPath);
			}
			catch (Exception ex)
			{
				clipboardFailure = ex.Message;
			}

			_imageEditorSession.ExitEditing();
			_workspaceSession.LoadImage(outputPath);
			RefreshWorkspaceState(clipboardFailure is null ? "Edited copy saved" : "Edited copy saved; clipboard copy failed");

			if (clipboardFailure is not null)
			{
				await DisplayAlertAsync("Clipboard copy failed", clipboardFailure, "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Save failed", ex.Message, "OK");
		}
	}

	private void OnCloseEditorClicked(object? sender, EventArgs e)
		=> CloseEditor();

	private void CloseEditor()
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		ExitImageEditorMode("Image loaded");
	}

	private void OnEditorPointerEntered(object? sender, PointerEventArgs e)
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		SetSelectionPointerCursor(true);
		UpdateEditorPointerIndicator(e.GetPosition(EditorInputOverlay));
	}

	private void OnEditorPointerExited(object? sender, PointerEventArgs e)
	{
		SetSelectionPointerCursor(false);
		ClearEditorPointerIndicator();
	}

	private async void OnEditorPointerPressed(object? sender, PointerEventArgs e)
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		if (e.Button == ButtonsMask.Secondary)
		{
			await ShowWorkspaceMenuAsync();
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(EditorInputOverlay);
		if (point is null || !TryMapEditorPoint(point.Value, out var imagePoint))
		{
			return;
		}

		UpdateEditorPointerIndicator(point);
		if (_inlineTextEditingAnnotationId.HasValue)
		{
			HideInlineTextEditor();
		}

		var selectedAnnotationBeforeInteraction = _imageEditorSession.SelectedAnnotation;
		if (_imageEditorSession.TryBeginInteraction(imagePoint))
		{
			var selectedAnnotation = _imageEditorSession.SelectedAnnotation;
			var interactionHandle = _imageEditorSession.ActiveInteractionHandle;
			var shouldEditText = selectedAnnotation is { } annotation &&
				selectedAnnotationBeforeInteraction is { } previouslySelectedAnnotation &&
				previouslySelectedAnnotation.Id == annotation.Id &&
				IsTextEditableAnnotation(annotation.Tool) &&
				interactionHandle == ImageEditorSelectionHandle.Body;

			UpdateEditorVisual();
			UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));

			if (selectedAnnotation is { } clickedAnnotation && shouldEditText)
			{
				TrackPendingInlineTextEdit(clickedAnnotation.Id, point.Value);
			}
			else
			{
				ClearPendingInlineTextEdit();
			}

			return;
		}

		ClearPendingInlineTextEdit();
		_imageEditorSession.StartDraft(imagePoint);
		UpdateEditorVisual();
	}

	private void OnEditorPointerMoved(object? sender, PointerEventArgs e)
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(EditorInputOverlay);
		UpdateEditorPointerIndicator(point);

		if (_imageEditorSession.IsManipulatingSelection)
		{
			if (_pendingInlineTextEditAnnotationId.HasValue &&
				(point is null || !IsWithinInlineTextEditDistance(point.Value)))
			{
				ClearPendingInlineTextEdit();
			}

			if (point is null || !TryMapEditorPoint(point.Value, out var interactionPoint))
			{
				return;
			}

			_imageEditorSession.UpdateInteraction(interactionPoint);
			UpdateEditorVisual();
			return;
		}

		if (!_imageEditorSession.IsDrawingDraft || point is null || !TryMapEditorPoint(point.Value, out var imagePoint))
		{
			return;
		}

		ClearPendingInlineTextEdit();
		_imageEditorSession.UpdateDraft(imagePoint);
		UpdateEditorVisual();
	}

	private void OnEditorPointerReleased(object? sender, PointerEventArgs e)
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(EditorInputOverlay);
		UpdateEditorPointerIndicator(point);

		if (_imageEditorSession.IsManipulatingSelection)
		{
			var shouldBeginInlineTextEditing =
				_pendingInlineTextEditAnnotationId.HasValue &&
				point is not null &&
				IsWithinInlineTextEditDistance(point.Value) &&
				_imageEditorSession.SelectedAnnotation is { } selectedAnnotation &&
				selectedAnnotation.Id == _pendingInlineTextEditAnnotationId.Value &&
				IsTextEditableAnnotation(selectedAnnotation.Tool);

			if (point is not null && TryMapEditorPoint(point.Value, out var interactionPoint))
			{
				_imageEditorSession.CompleteInteraction(interactionPoint);
			}
			else
			{
				_imageEditorSession.CompleteInteraction();
			}

			ClearPendingInlineTextEdit();
			if (shouldBeginInlineTextEditing)
			{
				BeginInlineTextEditing();
				return;
			}

			UpdateEditorVisual();
			UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
			return;
		}

		if (!_imageEditorSession.IsDrawingDraft)
		{
			return;
		}

		if (point is not null && TryMapEditorPoint(point.Value, out var imagePoint))
		{
			_imageEditorSession.CompleteDraft(imagePoint);
		}
		else
		{
			_imageEditorSession.CompleteDraft();
		}

		ClearPendingInlineTextEdit();
		UpdateEditorVisual();
		UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
	}

	private void SetEditorTool(ImageEditorTool tool)
	{
		if (!_imageEditorSession.IsEditing)
		{
			return;
		}

		ClearPendingInlineTextEdit();
		HideInlineTextEditor();
		_imageEditorSession.SetTool(tool);
		UpdateEditorVisual();
	}

	private static bool IsTextEditableAnnotation(ImageEditorTool tool) =>
		tool is ImageEditorTool.Text or ImageEditorTool.SpeechBubble;

	private void TrackPendingInlineTextEdit(Guid annotationId, Point point)
	{
		_pendingInlineTextEditAnnotationId = annotationId;
		_pendingInlineTextEditPoint = point;
	}

	private bool IsWithinInlineTextEditDistance(Point point)
	{
		if (!_pendingInlineTextEditPoint.HasValue)
		{
			return false;
		}

		var deltaX = point.X - _pendingInlineTextEditPoint.Value.X;
		var deltaY = point.Y - _pendingInlineTextEditPoint.Value.Y;
		return (deltaX * deltaX) + (deltaY * deltaY) <= EditorClickToEditDistance * EditorClickToEditDistance;
	}

	private void ClearPendingInlineTextEdit()
	{
		_pendingInlineTextEditAnnotationId = null;
		_pendingInlineTextEditPoint = null;
	}

	private void BeginInlineTextEditing()
	{
		if (_imageEditorSession.SelectedAnnotation is not { } annotation || !IsTextEditableAnnotation(annotation.Tool))
		{
			return;
		}

		_inlineTextEditingAnnotationId = annotation.Id;
		UpdateEditorVisual();
		UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
		ClearPendingInlineTextEdit();

		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1), () =>
		{
			InlineTextEditor.Focus();
			var text = InlineTextEditor.Text ?? string.Empty;
			InlineTextEditor.CursorPosition = 0;
			InlineTextEditor.SelectionLength = text.Length;
		});
	}

	private void OnInlineTextEditorTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_isUpdatingInlineTextEditor ||
			!_inlineTextEditingAnnotationId.HasValue ||
			!_imageEditorSession.IsEditing ||
			_imageEditorSession.SelectedAnnotation is not { } annotation ||
			annotation.Id != _inlineTextEditingAnnotationId.Value)
		{
			return;
		}

		_imageEditorSession.SetText(e.NewTextValue ?? string.Empty);
		UpdateEditorPropertyPanel();
	}

	private void OnInlineTextEditorUnfocused(object? sender, FocusEventArgs e)
	{
		if (_inlineTextEditingAnnotationId.HasValue)
		{
			HideInlineTextEditor();
		}
	}

	private void UpdateEditorVisual()
	{
		UpdateEditorToolSelection();
		UpdateEditorPropertyPanel();

		if (!_imageEditorSession.IsEditing || !_imageEditorSession.DocumentInfo.IsValid)
		{
			EditorAnnotationLayer.IsVisible = false;
			EditorAnnotationLayer.Children.Clear();
			EditorInputOverlay.InputTransparent = false;
			HideInlineTextEditor(updateVisual: false);
			return;
		}

		var displayRect = GetEditorDisplayRect();
		EditorInputOverlay.InputTransparent = _inlineTextEditingAnnotationId.HasValue;
		EditorAnnotationLayer.IsVisible = displayRect.Width > 0d && displayRect.Height > 0d;
		RenderEditorAnnotations(displayRect);
		UpdateInlineTextEditor(displayRect);
	}

	private void UpdateEditorToolSelection()
	{
		SetEditorToolButtonState(EditorPencilButton, _imageEditorSession.ActiveTool == ImageEditorTool.Pencil);
		SetEditorToolButtonState(EditorRectangleButton, _imageEditorSession.ActiveTool == ImageEditorTool.Rectangle);
		SetEditorToolButtonState(EditorArrowButton, _imageEditorSession.ActiveTool == ImageEditorTool.Arrow);
		SetEditorToolButtonState(EditorLineButton, _imageEditorSession.ActiveTool == ImageEditorTool.Line);
		SetEditorToolButtonState(EditorHighlightButton, _imageEditorSession.ActiveTool == ImageEditorTool.Highlight);
		SetEditorToolButtonState(EditorTextButton, _imageEditorSession.ActiveTool == ImageEditorTool.Text);
		SetEditorToolButtonState(EditorCalloutButton, _imageEditorSession.ActiveTool == ImageEditorTool.SpeechBubble);
	}

	private static void SetEditorToolButtonState(Button button, bool isActive)
	{
		button.BackgroundColor = isActive
			? Color.FromArgb("#15875C")
			: Color.FromArgb("#EAF8F1");
		button.TextColor = isActive
			? Colors.White
			: Color.FromArgb("#173525");
	}

	private bool TryMapEditorPoint(Point point, out ImageEditorPoint imagePoint)
	{
		imagePoint = default;
		if (!_imageEditorSession.IsEditing || !_imageEditorSession.DocumentInfo.IsValid)
		{
			return false;
		}

		var displayRect = GetEditorDisplayRect();
		if (displayRect.Width <= 0d || displayRect.Height <= 0d)
		{
			return false;
		}

		var clampedX = Math.Clamp(point.X, displayRect.X, displayRect.X + displayRect.Width);
		var clampedY = Math.Clamp(point.Y, displayRect.Y, displayRect.Y + displayRect.Height);
		var relativeX = (clampedX - displayRect.X) / displayRect.Width;
		var relativeY = (clampedY - displayRect.Y) / displayRect.Height;

		imagePoint = new ImageEditorPoint(
			relativeX * _imageEditorSession.DocumentInfo.PixelWidth,
			relativeY * _imageEditorSession.DocumentInfo.PixelHeight);
		return true;
	}

	private SelectionCanvasRect GetEditorDisplayRect()
	{
		var stageWidth = EditorInputOverlay.Width > 0d ? EditorInputOverlay.Width : PreviewStage.Width;
		var stageHeight = EditorInputOverlay.Height > 0d ? EditorInputOverlay.Height : PreviewStage.Height;

		return ScreenshotSelectionMapper.GetDisplayedImageRect(
			stageWidth,
			stageHeight,
			_imageEditorSession.DocumentInfo.PixelWidth,
			_imageEditorSession.DocumentInfo.PixelHeight);
	}

	private void UpdateEditorPointerIndicator(Point? point)
	{
		if (!_imageEditorSession.IsEditing || point is null)
		{
			ClearEditorPointerIndicator();
			return;
		}

		var displayRect = GetEditorDisplayRect();
		var clampedX = Math.Clamp(point.Value.X, displayRect.X, displayRect.X + displayRect.Width);
		var clampedY = Math.Clamp(point.Value.Y, displayRect.Y, displayRect.Y + displayRect.Height);

		EditorCursorIndicator.IsVisible = true;
		AbsoluteLayout.SetLayoutBounds(
			EditorCursorIndicator,
			new Rect(
				clampedX - (SelectionPointerIndicatorSize / 2d),
				clampedY - (SelectionPointerIndicatorSize / 2d),
				SelectionPointerIndicatorSize,
				SelectionPointerIndicatorSize));
	}

	private void ClearEditorPointerIndicator()
	{
		EditorCursorIndicator.IsVisible = false;
		AbsoluteLayout.SetLayoutBounds(
			EditorCursorIndicator,
			new Rect(0d, 0d, SelectionPointerIndicatorSize, SelectionPointerIndicatorSize));
	}

	private void RenderEditorAnnotations(SelectionCanvasRect displayRect)
	{
		EditorAnnotationLayer.Children.Clear();
		if (!_imageEditorSession.IsEditing || !_imageEditorSession.DocumentInfo.IsValid)
		{
			return;
		}

		foreach (var annotation in _imageEditorSession.Annotations)
		{
			AddEditorAnnotationViews(annotation, 1f, displayRect);
		}

		if (_imageEditorSession.DraftAnnotation is { } draftAnnotation)
		{
			AddEditorAnnotationViews(draftAnnotation, 0.72f, displayRect);
		}

		if (_imageEditorSession.SelectedAnnotation is { } selectedAnnotation)
		{
			AddEditorSelectionChrome(selectedAnnotation, displayRect);
		}
	}

	private void AddEditorAnnotationViews(
		ImageEditorAnnotation annotation,
		float opacity,
		SelectionCanvasRect displayRect)
	{
		var suppressText = ShouldSuppressAnnotationText(annotation);
		switch (annotation.Tool)
		{
			case ImageEditorTool.Image:
				if (CreateImageAnnotationView(annotation, opacity, displayRect) is { } imageView)
				{
					EditorAnnotationLayer.Children.Add(imageView);
				}
				break;
			case ImageEditorTool.Pencil:
				foreach (var view in CreatePencilViews(annotation, opacity, displayRect))
				{
					EditorAnnotationLayer.Children.Add(view);
				}
				break;
			case ImageEditorTool.Rectangle:
			case ImageEditorTool.Highlight:
				EditorAnnotationLayer.Children.Add(
					CreateAnnotationBorderView(annotation.Bounds, annotation.Style, opacity, displayRect));
				break;
			case ImageEditorTool.Text:
				EditorAnnotationLayer.Children.Add(
					CreateAnnotationBorderView(annotation.Bounds, annotation.Style, opacity, displayRect, suppressText ? null : annotation.Text));
				break;
			case ImageEditorTool.SpeechBubble:
				foreach (var view in CreateCalloutViews(annotation, opacity, displayRect, suppressText ? null : annotation.Text))
				{
					EditorAnnotationLayer.Children.Add(view);
				}
				break;
			case ImageEditorTool.Arrow:
				foreach (var view in CreateLineViews(annotation, opacity, displayRect, includeArrowHead: true))
				{
					EditorAnnotationLayer.Children.Add(view);
				}
				break;
			case ImageEditorTool.Line:
				foreach (var view in CreateLineViews(annotation, opacity, displayRect, includeArrowHead: false))
				{
					EditorAnnotationLayer.Children.Add(view);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(annotation.Tool), annotation.Tool, "Unsupported image editor tool.");
		}
	}

	private View? CreateImageAnnotationView(
		ImageEditorAnnotation annotation,
		float opacity,
		SelectionCanvasRect displayRect)
	{
		if (string.IsNullOrWhiteSpace(annotation.AssetPath) || !File.Exists(annotation.AssetPath))
		{
			return null;
		}

		var displayBounds = ToDisplayRect(annotation.Bounds, displayRect);
		var view = new Border
		{
			InputTransparent = true,
			StrokeThickness = 0d,
			Background = Brush.Transparent,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
			Content = new Image
			{
				InputTransparent = true,
				Source = ImageSource.FromFile(annotation.AssetPath),
				Aspect = Aspect.Fill,
				Opacity = opacity
			}
		};

		AbsoluteLayout.SetLayoutBounds(
			view,
			new Rect(displayBounds.X, displayBounds.Y, displayBounds.Width, displayBounds.Height));

		return view;
	}

	private Border CreateAnnotationBorderView(
		ImageEditorRect bounds,
		ImageEditorAnnotationStyle style,
		float opacity,
		SelectionCanvasRect displayRect,
		string? text = null)
	{
		var displayBounds = ToDisplayRect(bounds, displayRect);
		var border = new Border
		{
			InputTransparent = true,
			Stroke = ToSolidColorBrush(style.StrokeColor, opacity),
			StrokeThickness = GetStrokeThickness(style, displayRect),
			Background = ToSolidColorBrush(style.FillColor, opacity),
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
		};

		if (GetStrokeDashArray(style, displayRect) is { } dashArray)
		{
			border.StrokeDashArray = dashArray;
		}

		if (!string.IsNullOrWhiteSpace(text))
		{
			border.Padding = new Thickness(12, 8);
			border.Content = new Label
			{
				InputTransparent = true,
				Text = text,
				LineBreakMode = LineBreakMode.WordWrap,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				FontSize = GetTextFontSize(style, displayRect),
				TextColor = ToColor(style.TextColor, opacity)
			};
		}

		AbsoluteLayout.SetLayoutBounds(
			border,
			new Rect(displayBounds.X, displayBounds.Y, displayBounds.Width, displayBounds.Height));

		return border;
	}

	private IReadOnlyList<View> CreateCalloutViews(
		ImageEditorAnnotation annotation,
		float opacity,
		SelectionCanvasRect displayRect,
		string? text)
	{
		var displayBounds = ToDisplayRect(annotation.Bounds, displayRect);
		var border = CreateAnnotationBorderView(annotation.Bounds, annotation.Style, opacity, displayRect, text);
		var fillColor = ToColor(annotation.Style.FillColor, opacity);
		var strokeColor = ToColor(annotation.Style.StrokeColor, opacity);
		var tailBaseCenterX = displayBounds.X + Math.Max(displayBounds.Width * 0.28d, 18d);
		var tailBaseY = displayBounds.Bottom;
		var tailHalfWidth = Math.Min(Math.Max(displayBounds.Width * 0.08d, 10d), 24d);
		var tailTip = new Point(
			displayBounds.X + Math.Max(displayBounds.Width * 0.14d, 12d),
			displayBounds.Bottom + Math.Max(displayBounds.Height * 0.18d, 18d));
		var tailLeft = new Point(tailBaseCenterX - tailHalfWidth, tailBaseY);
		var tailRight = new Point(tailBaseCenterX + tailHalfWidth, tailBaseY);
		var views = new List<View> { border };

		if (fillColor.Alpha > 0f)
		{
			var polygon = new Polygon
			{
				InputTransparent = true,
				StrokeThickness = 0d,
				Fill = new SolidColorBrush(fillColor),
				Points = [tailLeft, tailTip, tailRight]
			};
			AbsoluteLayout.SetLayoutBounds(polygon, new Rect(0d, 0d, 1d, 1d));
			AbsoluteLayout.SetLayoutFlags(polygon, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
			views.Add(polygon);
		}

		views.Add(CreateStyledLine(tailLeft, tailTip, strokeColor, annotation.Style, displayRect));
		views.Add(CreateStyledLine(tailTip, tailRight, strokeColor, annotation.Style, displayRect));
		return views;
	}

	private IReadOnlyList<View> CreatePencilViews(
		ImageEditorAnnotation annotation,
		float opacity,
		SelectionCanvasRect displayRect)
	{
		var points = annotation.PathPoints is { Count: > 1 }
			? annotation.PathPoints
			: [annotation.StartPoint, annotation.EndPoint];
		var strokeColor = ToColor(annotation.Style.StrokeColor, opacity);
		var views = new List<View>();

		for (var index = 1; index < points.Count; index++)
		{
			views.Add(CreateStyledLine(
				ToDisplayPoint(points[index - 1], displayRect),
				ToDisplayPoint(points[index], displayRect),
				strokeColor,
				annotation.Style,
				displayRect));
		}

		return views;
	}

	private IReadOnlyList<View> CreateLineViews(
		ImageEditorAnnotation annotation,
		float opacity,
		SelectionCanvasRect displayRect,
		bool includeArrowHead)
	{
		var style = annotation.Style;
		var strokeColor = ToColor(style.StrokeColor, opacity);
		var strokeThickness = GetStrokeThickness(style, displayRect);
		var start = ToDisplayPoint(annotation.StartPoint, displayRect);
		var end = ToDisplayPoint(annotation.EndPoint, displayRect);
		var deltaX = end.X - start.X;
		var deltaY = end.Y - start.Y;
		var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
		if (length <= double.Epsilon)
		{
			return Array.Empty<View>();
		}

		var line = CreateStyledLine(start, end, strokeColor, style, displayRect);
		if (!includeArrowHead)
		{
			return [line];
		}

		var directionX = deltaX / length;
		var directionY = deltaY / length;
		var headLength = Math.Max(16d, strokeThickness * 3.5d);
		var headWidth = Math.Max(12d, strokeThickness * 2.4d);
		var baseX = end.X - (directionX * headLength);
		var baseY = end.Y - (directionY * headLength);
		var perpendicularX = -directionY;
		var perpendicularY = directionX;

		var polygon = new Polygon
		{
			InputTransparent = true,
			StrokeThickness = 0d,
			Fill = new SolidColorBrush(strokeColor),
			Points =
			[
				new Point(end.X, end.Y),
				new Point(baseX + (perpendicularX * headWidth / 2d), baseY + (perpendicularY * headWidth / 2d)),
				new Point(baseX - (perpendicularX * headWidth / 2d), baseY - (perpendicularY * headWidth / 2d))
			]
		};
		AbsoluteLayout.SetLayoutBounds(polygon, new Rect(0d, 0d, 1d, 1d));
		AbsoluteLayout.SetLayoutFlags(polygon, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);

		return [line, polygon];
	}

	private Line CreateStyledLine(
		Point start,
		Point end,
		Microsoft.Maui.Graphics.Color strokeColor,
		ImageEditorAnnotationStyle style,
		SelectionCanvasRect displayRect)
	{
		var line = new Line
		{
			InputTransparent = true,
			Stroke = new SolidColorBrush(strokeColor),
			StrokeThickness = GetStrokeThickness(style, displayRect),
			StrokeLineCap = PenLineCap.Round,
			X1 = start.X,
			Y1 = start.Y,
			X2 = end.X,
			Y2 = end.Y
		};

		if (GetStrokeDashArray(style, displayRect) is { } dashArray)
		{
			line.StrokeDashArray = dashArray;
		}

		AbsoluteLayout.SetLayoutBounds(line, new Rect(0d, 0d, 1d, 1d));
		AbsoluteLayout.SetLayoutFlags(line, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
		return line;
	}

	private Rect ToDisplayRect(ImageEditorRect bounds, SelectionCanvasRect displayRect)
	{
		var left = displayRect.X + ((bounds.X / _imageEditorSession.DocumentInfo.PixelWidth) * displayRect.Width);
		var top = displayRect.Y + ((bounds.Y / _imageEditorSession.DocumentInfo.PixelHeight) * displayRect.Height);
		var right = displayRect.X + ((bounds.Right / _imageEditorSession.DocumentInfo.PixelWidth) * displayRect.Width);
		var bottom = displayRect.Y + ((bounds.Bottom / _imageEditorSession.DocumentInfo.PixelHeight) * displayRect.Height);

		return new Rect(
			Math.Min(left, right),
			Math.Min(top, bottom),
			Math.Abs(right - left),
			Math.Abs(bottom - top));
	}

	private Point ToDisplayPoint(ImageEditorPoint point, SelectionCanvasRect displayRect) =>
		new(
			displayRect.X + ((point.X / _imageEditorSession.DocumentInfo.PixelWidth) * displayRect.Width),
			displayRect.Y + ((point.Y / _imageEditorSession.DocumentInfo.PixelHeight) * displayRect.Height));

	private double GetStrokeThickness(ImageEditorAnnotationStyle style, SelectionCanvasRect displayRect)
	{
		var scaleX = displayRect.Width / _imageEditorSession.DocumentInfo.PixelWidth;
		var scaleY = displayRect.Height / _imageEditorSession.DocumentInfo.PixelHeight;
		return Math.Max(Math.Min(scaleX, scaleY) * style.StrokeThickness, 2d);
	}

	private double GetTextFontSize(ImageEditorAnnotationStyle style, SelectionCanvasRect displayRect)
	{
		var scaleX = displayRect.Width / _imageEditorSession.DocumentInfo.PixelWidth;
		var scaleY = displayRect.Height / _imageEditorSession.DocumentInfo.PixelHeight;
		return Math.Max(Math.Min(scaleX, scaleY) * style.TextSize, 12d);
	}

	private static DoubleCollection? GetStrokeDashArray(ImageEditorAnnotationStyle style, SelectionCanvasRect displayRect)
	{
		if (style.StrokeStyle != ImageEditorStrokeStyle.Dashed)
		{
			return null;
		}

		var widthScale = Math.Max(Math.Min(displayRect.Width, displayRect.Height) / 240d, 1d);
		return [8d * widthScale, 5d * widthScale];
	}

	private static Brush ToSolidColorBrush(ImageEditorColor color, float opacity) =>
		new SolidColorBrush(ToColor(color, opacity));

	private static Microsoft.Maui.Graphics.Color ToColor(ImageEditorColor color, float opacity) =>
		Microsoft.Maui.Graphics.Color.FromRgba(
			color.R / 255f,
			color.G / 255f,
			color.B / 255f,
			(color.A / 255f) * opacity);

	private bool ShouldSuppressAnnotationText(ImageEditorAnnotation annotation) =>
		_inlineTextEditingAnnotationId.HasValue &&
		annotation.Id == _inlineTextEditingAnnotationId.Value &&
		IsTextEditableAnnotation(annotation.Tool);

	private void UpdateInlineTextEditor(SelectionCanvasRect displayRect)
	{
		if (_imageEditorSession.SelectedAnnotation is not { } annotation ||
			!_inlineTextEditingAnnotationId.HasValue ||
			annotation.Id != _inlineTextEditingAnnotationId.Value ||
			!IsTextEditableAnnotation(annotation.Tool) ||
			displayRect.Width <= 0d ||
			displayRect.Height <= 0d)
		{
			HideInlineTextEditor(updateVisual: false);
			return;
		}

		var displayBounds = ToDisplayRect(annotation.Bounds, displayRect);
		_isUpdatingInlineTextEditor = true;
		try
		{
			InlineTextEditorOverlay.IsVisible = true;
			InlineTextEditorHost.Padding = annotation.Tool == ImageEditorTool.SpeechBubble
				? new Thickness(14d, 8d)
				: new Thickness(12d, 8d);
			AbsoluteLayout.SetLayoutBounds(
				InlineTextEditorHost,
				new Rect(
					displayBounds.X,
					displayBounds.Y,
					Math.Max(displayBounds.Width, 72d),
					Math.Max(displayBounds.Height, 48d)));
			InlineTextEditor.FontSize = GetTextFontSize(annotation.Style, displayRect);
			InlineTextEditor.TextColor = ToColor(annotation.Style.TextColor, 1f);
			InlineTextEditor.HorizontalTextAlignment = TextAlignment.Center;
			if (!string.Equals(InlineTextEditor.Text, annotation.Text, StringComparison.Ordinal))
			{
				InlineTextEditor.Text = annotation.Text;
			}

			InlineTextEditorHost.IsVisible = true;
		}
		finally
		{
			_isUpdatingInlineTextEditor = false;
		}
	}

	private void HideInlineTextEditor(bool updateVisual = true)
	{
		if (!_inlineTextEditingAnnotationId.HasValue && !InlineTextEditorHost.IsVisible)
		{
			return;
		}

		ClearPendingInlineTextEdit();
		_inlineTextEditingAnnotationId = null;
		InlineTextEditorOverlay.IsVisible = false;
		InlineTextEditorHost.IsVisible = false;
		EditorInputOverlay.InputTransparent = false;
		AbsoluteLayout.SetLayoutBounds(InlineTextEditorHost, new Rect(0d, 0d, 0d, 0d));

		if (updateVisual && _imageEditorSession.IsEditing)
		{
			UpdateEditorVisual();
		}
	}

	private void AddEditorSelectionChrome(ImageEditorAnnotation annotation, SelectionCanvasRect displayRect)
	{
		switch (annotation.Tool)
		{
			case ImageEditorTool.Image:
			case ImageEditorTool.Rectangle:
			case ImageEditorTool.Highlight:
			case ImageEditorTool.Text:
			case ImageEditorTool.SpeechBubble:
				AddBoxSelectionChrome(annotation.Bounds, displayRect);
				break;
			case ImageEditorTool.Pencil:
				AddPencilSelectionChrome(annotation, displayRect);
				break;
			case ImageEditorTool.Arrow:
			case ImageEditorTool.Line:
				AddLineSelectionChrome(annotation, displayRect);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(annotation.Tool), annotation.Tool, "Unsupported image editor tool.");
		}
	}

	private void AddBoxSelectionChrome(ImageEditorRect bounds, SelectionCanvasRect displayRect)
	{
		var displayBounds = ToDisplayRect(bounds, displayRect);
		var selectionBorder = new Border
		{
			InputTransparent = true,
			Stroke = new SolidColorBrush(Color.FromArgb("#0F6B47")),
			StrokeThickness = 2d,
			Background = Brush.Transparent,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
		};
		AbsoluteLayout.SetLayoutBounds(
			selectionBorder,
			new Rect(displayBounds.X - 4d, displayBounds.Y - 4d, displayBounds.Width + 8d, displayBounds.Height + 8d));
		EditorAnnotationLayer.Children.Add(selectionBorder);

		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(new Point(displayBounds.X, displayBounds.Y)));
		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(new Point(displayBounds.X + displayBounds.Width, displayBounds.Y)));
		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(new Point(displayBounds.X, displayBounds.Y + displayBounds.Height)));
		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(new Point(displayBounds.X + displayBounds.Width, displayBounds.Y + displayBounds.Height)));
	}

	private void AddLineSelectionChrome(ImageEditorAnnotation annotation, SelectionCanvasRect displayRect)
	{
		var start = ToDisplayPoint(annotation.StartPoint, displayRect);
		var end = ToDisplayPoint(annotation.EndPoint, displayRect);

		var line = new Line
		{
			InputTransparent = true,
			Stroke = new SolidColorBrush(Color.FromArgb("#0F6B47")),
			StrokeThickness = 3d,
			StrokeLineCap = PenLineCap.Round,
			X1 = start.X,
			Y1 = start.Y,
			X2 = end.X,
			Y2 = end.Y
		};
		AbsoluteLayout.SetLayoutBounds(line, new Rect(0d, 0d, 1d, 1d));
		AbsoluteLayout.SetLayoutFlags(line, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
		EditorAnnotationLayer.Children.Add(line);

		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(start));
		EditorAnnotationLayer.Children.Add(CreateSelectionHandle(end));
	}

	private void AddPencilSelectionChrome(ImageEditorAnnotation annotation, SelectionCanvasRect displayRect)
	{
		var displayBounds = ToDisplayRect(annotation.Bounds, displayRect);
		var selectionBorder = new Border
		{
			InputTransparent = true,
			Stroke = new SolidColorBrush(Color.FromArgb("#0F6B47")),
			StrokeThickness = 2d,
			Background = Brush.Transparent,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) }
		};

		AbsoluteLayout.SetLayoutBounds(
			selectionBorder,
			new Rect(
				displayBounds.X - 4d,
				displayBounds.Y - 4d,
				Math.Max(displayBounds.Width + 8d, 12d),
				Math.Max(displayBounds.Height + 8d, 12d)));
		EditorAnnotationLayer.Children.Add(selectionBorder);
	}

	private Border CreateSelectionHandle(Point center)
	{
		var handle = new Border
		{
			InputTransparent = true,
			Background = new SolidColorBrush(Color.FromArgb("#FFFFFF")),
			Stroke = new SolidColorBrush(Color.FromArgb("#0F6B47")),
			StrokeThickness = 2d,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) }
		};
		const double handleSize = 14d;
		AbsoluteLayout.SetLayoutBounds(
			handle,
			new Rect(center.X - (handleSize / 2d), center.Y - (handleSize / 2d), handleSize, handleSize));
		return handle;
	}
}

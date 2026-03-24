namespace Greenshot.Maui.Core.Services;

public sealed class ImageEditorSession
{
	private sealed record InteractionState(
		Guid AnnotationId,
		ImageEditorSelectionHandle Handle,
		ImageEditorPoint PointerStart,
		ImageEditorAnnotation OriginalAnnotation);

	private const double AnnotationHitTolerance = 12d;
	private const double MinimumAnnotationPixels = 6d;
	private readonly List<ImageEditorAnnotation> _annotations = [];
	private readonly Dictionary<ImageEditorTool, ImageEditorToolPreset> _toolPresets = Enum
		.GetValues<ImageEditorTool>()
		.ToDictionary(tool => tool, ImageEditorToolDefaults.Get);
	private InteractionState? _interactionState;

	public string? SourceImagePath { get; private set; }

	public ImageEditorDocumentInfo DocumentInfo { get; private set; }

	public ImageEditorTool ActiveTool { get; private set; } = ImageEditorTool.Rectangle;

	public bool IsEditing => !string.IsNullOrWhiteSpace(SourceImagePath) && DocumentInfo.IsValid;

	public bool IsDrawingDraft => DraftStartPoint.HasValue && DraftCurrentPoint.HasValue;

	public bool IsManipulatingSelection => _interactionState is not null;

	public ImageEditorPoint? DraftStartPoint { get; private set; }

	public ImageEditorPoint? DraftCurrentPoint { get; private set; }

	public Guid? SelectedAnnotationId { get; private set; }

	public IReadOnlyList<ImageEditorAnnotation> Annotations => _annotations;

	public ImageEditorAnnotation? SelectedAnnotation =>
		SelectedAnnotationId.HasValue
			? _annotations.FirstOrDefault(annotation => annotation.Id == SelectedAnnotationId.Value)
			: null;

	public ImageEditorAnnotationStyle CurrentStyle =>
		SelectedAnnotation?.Style ?? _toolPresets[ActiveTool].Style;

	public string CurrentText =>
		SelectedAnnotation?.Text ?? _toolPresets[ActiveTool].Text;

	public ImageEditorAnnotation? DraftAnnotation =>
		IsDrawingDraft
			? new ImageEditorAnnotation(
				Guid.Empty,
				ActiveTool,
				DraftStartPoint!.Value,
				DraftCurrentPoint!.Value,
				_toolPresets[ActiveTool].Style,
				_toolPresets[ActiveTool].Text)
			: null;

	public void BeginEditing(string filePath, ImageEditorDocumentInfo documentInfo)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		if (!documentInfo.IsValid)
		{
			throw new ArgumentOutOfRangeException(nameof(documentInfo), "Image dimensions must be greater than zero.");
		}

		SourceImagePath = filePath;
		DocumentInfo = documentInfo;
		ActiveTool = ImageEditorTool.Rectangle;
		_annotations.Clear();
		ResetToolPresets();
		ClearSelection();
		CancelDraft();
	}

	public void ExitEditing()
	{
		SourceImagePath = null;
		DocumentInfo = default;
		ActiveTool = ImageEditorTool.Rectangle;
		_annotations.Clear();
		ResetToolPresets();
		ClearSelection();
		CancelDraft();
	}

	public void SetTool(ImageEditorTool tool)
	{
		ActiveTool = tool;
		ClearSelection();
		CancelDraft();
	}

	public void StartDraft(ImageEditorPoint point)
	{
		if (!IsEditing)
		{
			return;
		}

		var clampedPoint = ClampPoint(point);
		DraftStartPoint = clampedPoint;
		DraftCurrentPoint = clampedPoint;
	}

	public void UpdateDraft(ImageEditorPoint point)
	{
		if (!IsEditing || !DraftStartPoint.HasValue)
		{
			return;
		}

		DraftCurrentPoint = ClampPoint(point);
	}

	public ImageEditorAnnotation? CompleteDraft(ImageEditorPoint? point = null)
	{
		if (!IsEditing || !DraftStartPoint.HasValue || !DraftCurrentPoint.HasValue)
		{
			return null;
		}

		if (point.HasValue)
		{
			DraftCurrentPoint = ClampPoint(point.Value);
		}

		var preset = _toolPresets[ActiveTool];
		var annotation = new ImageEditorAnnotation(
			Guid.NewGuid(),
			ActiveTool,
			DraftStartPoint.Value,
			DraftCurrentPoint.Value,
			preset.Style,
			preset.Text);

		CancelDraft();
		if (!annotation.IsMeaningful())
		{
			return null;
		}

		_annotations.Add(annotation);
		SelectedAnnotationId = annotation.Id;
		return annotation;
	}

	public bool UndoLastAnnotation()
	{
		if (_annotations.Count == 0)
		{
			return false;
		}

		var removed = _annotations[^1];
		_annotations.RemoveAt(_annotations.Count - 1);
		if (SelectedAnnotationId == removed.Id)
		{
			ClearSelection();
		}

		return true;
	}

	public void ClearSelection()
	{
		SelectedAnnotationId = null;
		_interactionState = null;
	}

	public void SetStrokeColor(ImageEditorColor color)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { StrokeColor = color } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { StrokeColor = color } });
	}

	public void SetFillColor(ImageEditorColor color)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { FillColor = color } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { FillColor = color } });
	}

	public void SetTextColor(ImageEditorColor color)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { TextColor = color } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { TextColor = color } });
	}

	public void SetStrokeThickness(float strokeThickness)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { StrokeThickness = strokeThickness } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { StrokeThickness = strokeThickness } });
	}

	public void SetStrokeStyle(ImageEditorStrokeStyle strokeStyle)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { StrokeStyle = strokeStyle } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { StrokeStyle = strokeStyle } });
	}

	public void SetTextSize(float textSize)
	{
		UpdateCurrentPreset(preset => preset with { Style = preset.Style with { TextSize = textSize } });
		ApplyToSelectedAnnotation(annotation => annotation with { Style = annotation.Style with { TextSize = textSize } });
	}

	public void SetText(string text)
	{
		var safeText = text ?? string.Empty;
		UpdateCurrentPreset(preset => preset with { Text = safeText });
		ApplyToSelectedAnnotation(annotation => annotation with { Text = safeText });
	}

	public bool TryBeginInteraction(ImageEditorPoint point)
	{
		if (!IsEditing)
		{
			return false;
		}

		var clampedPoint = ClampPoint(point);
		var hitTarget = FindHitTarget(clampedPoint);
		if (hitTarget is null)
		{
			ClearSelection();
			return false;
		}

		var annotation = GetAnnotation(hitTarget.Value.AnnotationId);
		if (annotation is null)
		{
			ClearSelection();
			return false;
		}

		SelectedAnnotationId = annotation.Id;
		ActiveTool = annotation.Tool;
		CancelDraft();
		_interactionState = new InteractionState(
			annotation.Id,
			hitTarget.Value.Handle,
			clampedPoint,
			annotation);
		return true;
	}

	public bool UpdateInteraction(ImageEditorPoint point)
	{
		if (_interactionState is null)
		{
			return false;
		}

		var updatedAnnotation = ApplyInteraction(_interactionState, ClampPoint(point));
		ReplaceAnnotation(updatedAnnotation);
		return true;
	}

	public bool CompleteInteraction(ImageEditorPoint? point = null)
	{
		if (_interactionState is null)
		{
			return false;
		}

		if (point.HasValue)
		{
			UpdateInteraction(point.Value);
		}

		_interactionState = null;
		return true;
	}

	public void CancelDraft()
	{
		DraftStartPoint = null;
		DraftCurrentPoint = null;
	}

	private ImageEditorHitTarget? FindHitTarget(ImageEditorPoint point)
	{
		var selectedAnnotation = SelectedAnnotation;
		if (selectedAnnotation is not null && selectedAnnotation.TryHitTest(point, AnnotationHitTolerance, out var selectedHandle))
		{
			return new ImageEditorHitTarget(selectedAnnotation.Id, selectedHandle);
		}

		for (var index = _annotations.Count - 1; index >= 0; index--)
		{
			var annotation = _annotations[index];
			if (annotation.TryHitTest(point, AnnotationHitTolerance, out var handle))
			{
				return new ImageEditorHitTarget(annotation.Id, handle);
			}
		}

		return null;
	}

	private ImageEditorAnnotation? GetAnnotation(Guid annotationId) =>
		_annotations.FirstOrDefault(annotation => annotation.Id == annotationId);

	private void ReplaceAnnotation(ImageEditorAnnotation annotation)
	{
		for (var index = 0; index < _annotations.Count; index++)
		{
			if (_annotations[index].Id != annotation.Id)
			{
				continue;
			}

			_annotations[index] = annotation;
			SelectedAnnotationId = annotation.Id;
			return;
		}
	}

	private ImageEditorAnnotation ApplyInteraction(InteractionState state, ImageEditorPoint currentPoint)
	{
		return state.Handle switch
		{
			ImageEditorSelectionHandle.Body => MoveAnnotation(state.OriginalAnnotation, state.PointerStart, currentPoint),
			ImageEditorSelectionHandle.TopLeft => ResizeBoxAnnotation(state.OriginalAnnotation, currentPoint, new ImageEditorPoint(state.OriginalAnnotation.Bounds.Right, state.OriginalAnnotation.Bounds.Bottom)),
			ImageEditorSelectionHandle.TopRight => ResizeBoxAnnotation(state.OriginalAnnotation, currentPoint, new ImageEditorPoint(state.OriginalAnnotation.Bounds.X, state.OriginalAnnotation.Bounds.Bottom)),
			ImageEditorSelectionHandle.BottomLeft => ResizeBoxAnnotation(state.OriginalAnnotation, currentPoint, new ImageEditorPoint(state.OriginalAnnotation.Bounds.Right, state.OriginalAnnotation.Bounds.Y)),
			ImageEditorSelectionHandle.BottomRight => ResizeBoxAnnotation(state.OriginalAnnotation, currentPoint, new ImageEditorPoint(state.OriginalAnnotation.Bounds.X, state.OriginalAnnotation.Bounds.Y)),
			ImageEditorSelectionHandle.StartPoint => UpdateLineEndpoint(state.OriginalAnnotation, currentPoint, updateStart: true),
			ImageEditorSelectionHandle.EndPoint => UpdateLineEndpoint(state.OriginalAnnotation, currentPoint, updateStart: false),
			_ => state.OriginalAnnotation
		};
	}

	private ImageEditorAnnotation MoveAnnotation(
		ImageEditorAnnotation annotation,
		ImageEditorPoint pointerStart,
		ImageEditorPoint currentPoint)
	{
		var bounds = annotation.Bounds;
		var deltaX = currentPoint.X - pointerStart.X;
		var deltaY = currentPoint.Y - pointerStart.Y;
		var allowedDeltaX = Math.Clamp(deltaX, -bounds.X, DocumentInfo.PixelWidth - bounds.Right);
		var allowedDeltaY = Math.Clamp(deltaY, -bounds.Y, DocumentInfo.PixelHeight - bounds.Bottom);

		return annotation.MoveBy(allowedDeltaX, allowedDeltaY);
	}

	private ImageEditorAnnotation ResizeBoxAnnotation(
		ImageEditorAnnotation annotation,
		ImageEditorPoint movingCorner,
		ImageEditorPoint fixedCorner)
	{
		var adjustedMovingCorner = EnsureMinimumCornerDistance(fixedCorner, movingCorner);
		var newBounds = ImageEditorRect.FromPoints(fixedCorner, ClampPoint(adjustedMovingCorner));
		return annotation.WithBounds(newBounds);
	}

	private ImageEditorAnnotation UpdateLineEndpoint(
		ImageEditorAnnotation annotation,
		ImageEditorPoint point,
		bool updateStart)
	{
		var updated = updateStart
			? annotation.WithStart(point)
			: annotation.WithEnd(point);

		return updated.IsMeaningful(MinimumAnnotationPixels)
			? updated
			: annotation;
	}

	private static ImageEditorPoint EnsureMinimumCornerDistance(ImageEditorPoint fixedCorner, ImageEditorPoint movingCorner)
	{
		var adjustedX = movingCorner.X;
		var adjustedY = movingCorner.Y;
		if (Math.Abs(movingCorner.X - fixedCorner.X) < MinimumAnnotationPixels)
		{
			adjustedX = fixedCorner.X + (movingCorner.X < fixedCorner.X ? -MinimumAnnotationPixels : MinimumAnnotationPixels);
		}

		if (Math.Abs(movingCorner.Y - fixedCorner.Y) < MinimumAnnotationPixels)
		{
			adjustedY = fixedCorner.Y + (movingCorner.Y < fixedCorner.Y ? -MinimumAnnotationPixels : MinimumAnnotationPixels);
		}

		return new ImageEditorPoint(adjustedX, adjustedY);
	}

	private ImageEditorPoint ClampPoint(ImageEditorPoint point)
	{
		var maxX = Math.Max(DocumentInfo.PixelWidth, 1);
		var maxY = Math.Max(DocumentInfo.PixelHeight, 1);

		return new ImageEditorPoint(
			Math.Clamp(point.X, 0d, maxX),
			Math.Clamp(point.Y, 0d, maxY));
	}

	private void ResetToolPresets()
	{
		foreach (var tool in Enum.GetValues<ImageEditorTool>())
		{
			_toolPresets[tool] = ImageEditorToolDefaults.Get(tool);
		}
	}

	private void UpdateCurrentPreset(Func<ImageEditorToolPreset, ImageEditorToolPreset> update)
	{
		var tool = SelectedAnnotation?.Tool ?? ActiveTool;
		_toolPresets[tool] = update(_toolPresets[tool]);
	}

	private void ApplyToSelectedAnnotation(Func<ImageEditorAnnotation, ImageEditorAnnotation> update)
	{
		if (SelectedAnnotation is null)
		{
			return;
		}

		ReplaceAnnotation(update(SelectedAnnotation));
	}
}

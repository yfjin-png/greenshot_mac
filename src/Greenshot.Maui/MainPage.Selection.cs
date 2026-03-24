using Greenshot.Maui.Core.Services;
using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Greenshot.Maui;

public partial class MainPage
{
	private void OnSelectionPointerEntered(object? sender, PointerEventArgs e)
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			SetSelectionPointerCursor(true);
			UpdateSelectionPointerIndicator(e.GetPosition(SelectionInputSurface));
		}
	}

	private void OnSelectionPointerExited(object? sender, PointerEventArgs e)
	{
		SetSelectionPointerCursor(false);
		ClearSelectionPointerIndicator();
	}

	private void OnSelectionPointerPressed(object? sender, PointerEventArgs e)
	{
		if (!_workspaceSession.IsSelectingRegion)
		{
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(SelectionInputSurface);
		if (point is null)
		{
			return;
		}

		UpdateSelectionPointerIndicator(point);
		_workspaceSession.StartSelection(ToSelectionCanvasPoint(point.Value));
		UpdateSelectionVisual();
	}

	private void OnSelectionPointerMoved(object? sender, PointerEventArgs e)
	{
		if (!_workspaceSession.IsSelectingRegion)
		{
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(SelectionInputSurface);
		UpdateSelectionPointerIndicator(point);

		if (!_workspaceSession.IsDraggingSelection || !_workspaceSession.SelectionStartPoint.HasValue || point is null)
		{
			return;
		}

		_workspaceSession.UpdateSelection(ToSelectionCanvasPoint(point.Value));
		UpdateSelectionVisual();
	}

	private void OnSelectionPointerReleased(object? sender, PointerEventArgs e)
	{
		if (!_workspaceSession.IsSelectingRegion || !_workspaceSession.IsDraggingSelection)
		{
			return;
		}

		SetSelectionPointerCursor(true);

		var point = e.GetPosition(SelectionInputSurface);
		UpdateSelectionPointerIndicator(point);
		_workspaceSession.EndSelection(point is null ? null : ToSelectionCanvasPoint(point.Value));
		UpdateSelectionVisual();
	}

	private void OnPreviewSurfaceSizeChanged(object? sender, EventArgs e)
	{
		if (_imageEditorSession.IsEditing)
		{
			UpdateEditorVisual();
		}

		if (_workspaceSession.IsSelectingRegion)
		{
			UpdateSelectionVisual();
		}
	}

	private void EnterRegionSelectionMode(ScreenshotCaptureResult captureResult)
	{
		_workspaceSession.BeginRegionSelection(captureResult);
		RefreshWorkspaceState("Select region");
	}

	private void ExitRegionSelectionMode()
	{
		SetSelectionPointerCursor(false);
		ClearSelectionPointerIndicator();
		_workspaceSession.ExitRegionSelection();
		ClearSelectionRectangle();
	}

	private void UpdateSelectionVisual()
	{
		if (!_workspaceSession.IsSelectingRegion || !TryGetSelectionProjection(out var projection))
		{
			ClearSelectionRectangle();
			UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
			return;
		}

		AbsoluteLayout.SetLayoutBounds(
			SelectionBox,
			new Rect(
				projection.SelectionDisplayRect.X,
				projection.SelectionDisplayRect.Y,
				projection.SelectionDisplayRect.Width,
				projection.SelectionDisplayRect.Height));
		SelectionBox.IsVisible = true;
		UpdateActionState(hasImage: !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath));
	}

	private void ClearSelectionRectangle()
	{
		_workspaceSession.ResetSelectionPoints();
		SelectionBox.IsVisible = false;
		AbsoluteLayout.SetLayoutBounds(SelectionBox, new Rect(0d, 0d, 0d, 0d));
		ApplySelectionButton.IsEnabled = false;
	}

	private void UpdateSelectionPointerIndicator(Point? point)
	{
		if (!_workspaceSession.IsSelectingRegion || point is null)
		{
			ClearSelectionPointerIndicator();
			return;
		}

		SelectionCursorIndicator.IsVisible = true;
		AbsoluteLayout.SetLayoutBounds(
			SelectionCursorIndicator,
			new Rect(
				point.Value.X - (SelectionPointerIndicatorSize / 2d),
				point.Value.Y - (SelectionPointerIndicatorSize / 2d),
				SelectionPointerIndicatorSize,
				SelectionPointerIndicatorSize));
	}

	private void ClearSelectionPointerIndicator()
	{
		SelectionCursorIndicator.IsVisible = false;
		AbsoluteLayout.SetLayoutBounds(
			SelectionCursorIndicator,
			new Rect(0d, 0d, SelectionPointerIndicatorSize, SelectionPointerIndicatorSize));
	}

	private bool TryGetSelectionProjection(out ScreenshotSelectionProjection projection)
	{
		var stageWidth = SelectionInputSurface.Width > 0d ? SelectionInputSurface.Width : PreviewStage.Width;
		var stageHeight = SelectionInputSurface.Height > 0d ? SelectionInputSurface.Height : PreviewStage.Height;

		return _workspaceSession.TryProjectSelection(
			stageWidth,
			stageHeight,
			MinimumSelectionDisplaySize,
			out projection);
	}

	private static SelectionCanvasPoint ToSelectionCanvasPoint(Point point) =>
		new(point.X, point.Y);
}

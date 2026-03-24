namespace Greenshot.Maui.Core.Services;

public sealed class ScreenshotWorkspaceSession
{
	public string? SelectedImagePath { get; private set; }

	public string? SelectionSourcePath { get; private set; }

	public int SelectionSourcePixelWidth { get; private set; }

	public int SelectionSourcePixelHeight { get; private set; }

	public bool IsSelectingRegion { get; private set; }

	public bool IsDraggingSelection { get; private set; }

	public SelectionCanvasPoint? SelectionStartPoint { get; private set; }

	public SelectionCanvasPoint? SelectionCurrentPoint { get; private set; }

	public void LoadImage(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		ResetSelectionState();
		SelectedImagePath = filePath;
	}

	public void ClearImage()
	{
		ResetSelectionState();
		SelectedImagePath = null;
	}

	public void BeginRegionSelection(ScreenshotCaptureResult captureResult)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(captureResult.FilePath);

		SelectedImagePath = captureResult.FilePath;
		SelectionSourcePath = captureResult.FilePath;
		SelectionSourcePixelWidth = captureResult.PixelWidth;
		SelectionSourcePixelHeight = captureResult.PixelHeight;
		IsSelectingRegion = true;
		IsDraggingSelection = false;
		ResetSelectionPoints();
	}

	public string? CancelRegionSelection()
	{
		var sourcePath = SelectionSourcePath;

		ResetSelectionState();
		SelectedImagePath = sourcePath;
		return sourcePath;
	}

	public void CompleteRegionSelection(string filePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		ResetSelectionState();
		SelectedImagePath = filePath;
	}

	public void ExitRegionSelection() => ResetSelectionState();

	public void StartSelection(SelectionCanvasPoint point)
	{
		if (!IsSelectingRegion)
		{
			return;
		}

		IsDraggingSelection = true;
		SelectionStartPoint = point;
		SelectionCurrentPoint = point;
	}

	public void UpdateSelection(SelectionCanvasPoint point)
	{
		if (!IsSelectingRegion || !IsDraggingSelection || !SelectionStartPoint.HasValue)
		{
			return;
		}

		SelectionCurrentPoint = point;
	}

	public void EndSelection(SelectionCanvasPoint? point = null)
	{
		if (!IsSelectingRegion || !IsDraggingSelection)
		{
			return;
		}

		if (point.HasValue)
		{
			SelectionCurrentPoint = point.Value;
		}

		IsDraggingSelection = false;
	}

	public void ResetSelectionPoints()
	{
		SelectionStartPoint = null;
		SelectionCurrentPoint = null;
	}

	public bool TryProjectSelection(
		double stageWidth,
		double stageHeight,
		double minimumSelectionDisplaySize,
		out ScreenshotSelectionProjection projection)
		=> ScreenshotSelectionMapper.TryProjectSelection(
			SelectionStartPoint,
			SelectionCurrentPoint,
			stageWidth,
			stageHeight,
			SelectionSourcePixelWidth,
			SelectionSourcePixelHeight,
			minimumSelectionDisplaySize,
			out projection);

	private void ResetSelectionState()
	{
		IsSelectingRegion = false;
		IsDraggingSelection = false;
		ResetSelectionPoints();
		ClearSelectionSource();
	}

	private void ClearSelectionSource()
	{
		SelectionSourcePath = null;
		SelectionSourcePixelWidth = 0;
		SelectionSourcePixelHeight = 0;
	}
}

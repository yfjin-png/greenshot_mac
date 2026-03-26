namespace Greenshot.Maui.Core.Services;

public sealed class ScreenshotWorkspaceSession
{
	private readonly List<string> _imageHistory = [];
	private int _selectedImageHistoryIndex = -1;

	public string? SelectedImagePath { get; private set; }

	public IReadOnlyList<string> ImageHistory => _imageHistory;

	public int SelectedImageHistoryNumber => _selectedImageHistoryIndex >= 0 ? _selectedImageHistoryIndex + 1 : 0;

	public bool CanSelectPreviousImage => _selectedImageHistoryIndex > 0;

	public bool CanSelectNextImage => _selectedImageHistoryIndex >= 0 && _selectedImageHistoryIndex < _imageHistory.Count - 1;

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
		SelectOrAppendImage(filePath);
	}

	public void ClearImage()
	{
		ResetSelectionState();
		SelectedImagePath = null;
		_imageHistory.Clear();
		_selectedImageHistoryIndex = -1;
	}

	public void BeginRegionSelection(ScreenshotCaptureResult captureResult)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(captureResult.FilePath);

		SelectOrAppendImage(captureResult.FilePath);
		SelectionSourcePath = SelectedImagePath;
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
		SelectOrAppendImage(filePath);
	}

	public void ExitRegionSelection() => ResetSelectionState();

	public bool SelectPreviousImage()
	{
		if (!CanSelectPreviousImage)
		{
			return false;
		}

		ResetSelectionState();
		_selectedImageHistoryIndex--;
		SelectedImagePath = _imageHistory[_selectedImageHistoryIndex];
		return true;
	}

	public bool SelectNextImage()
	{
		if (!CanSelectNextImage)
		{
			return false;
		}

		ResetSelectionState();
		_selectedImageHistoryIndex++;
		SelectedImagePath = _imageHistory[_selectedImageHistoryIndex];
		return true;
	}

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

	private void SelectOrAppendImage(string filePath)
	{
		if (_selectedImageHistoryIndex >= 0 &&
			string.Equals(_imageHistory[_selectedImageHistoryIndex], filePath, StringComparison.Ordinal))
		{
			SelectedImagePath = filePath;
			return;
		}

		if (_selectedImageHistoryIndex >= 0 && _selectedImageHistoryIndex < _imageHistory.Count - 1)
		{
			_imageHistory.RemoveRange(
				_selectedImageHistoryIndex + 1,
				_imageHistory.Count - _selectedImageHistoryIndex - 1);
		}

		if (_imageHistory.Count == 0 || !string.Equals(_imageHistory[^1], filePath, StringComparison.Ordinal))
		{
			_imageHistory.Add(filePath);
		}

		_selectedImageHistoryIndex = _imageHistory.Count - 1;
		SelectedImagePath = filePath;
	}
}

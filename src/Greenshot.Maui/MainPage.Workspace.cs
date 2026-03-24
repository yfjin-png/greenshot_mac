using System.IO;

namespace Greenshot.Maui;

public partial class MainPage
{
	private void RefreshWorkspaceState(string? statusText = null)
	{
		var fullPath = _workspaceSession.SelectedImagePath;
		var hasImage = !string.IsNullOrWhiteSpace(fullPath);
		var isEditing = hasImage && _imageEditorSession.IsEditing;
		ConfigureSelectedImageWatcher(fullPath);

		PreviewImage.IsVisible = hasImage;
		EmptyStatePanel.IsVisible = !hasImage;
		EditorAnnotationLayer.IsVisible = isEditing;
		EditorInputOverlay.IsVisible = isEditing;
		EditorToolPanel.IsVisible = isEditing;
		SelectionInstructionPanel.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing;
		SelectionOverlay.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing;
		SelectionActionsPanel.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing;
		if ((!_workspaceSession.IsSelectingRegion && !isEditing) || !hasImage)
		{
			SetSelectionPointerCursor(false);
			ClearSelectionPointerIndicator();
		}
		if (!isEditing)
		{
			ClearEditorPointerIndicator();
		}

		if (!hasImage)
		{
			PreviewImage.Source = null;
			SelectedImageLabel.Text = "Ready to import a screenshot";
			SelectionInstructionLabel.Text = string.Empty;
			WorkspaceHintLabel.Text = _screenshotCaptureService.IsSupported
				? "Capture the primary display, a specific window, or a region, or open an existing image file to validate the new Mac-native entry point."
				: "Start with an existing image file while native screen capture is still being moved out of the Windows-specific code path.";
			StatusValueLabel.Text = statusText ?? "Preview workspace";
			UpdateActionState(hasImage: false);
			ClearSelectionRectangle();
			UpdateEditorVisual();
			return;
		}

		ReloadPreviewImage();
		SelectedImageLabel.Text = Path.GetFileName(fullPath);

		if (isEditing)
		{
			SelectionInstructionLabel.Text = string.Empty;
			WorkspaceHintLabel.Text = "Rectangle, arrow, line, text, callout, and highlight now run inside the native MAUI editor. Style changes apply to the selected annotation or the active tool preset, and Save Copy writes an annotated PNG into the app cache.";
			StatusValueLabel.Text = statusText ?? "Editing image";
			UpdateActionState(hasImage: true);
			UpdateEditorVisual();
			ClearSelectionRectangle();
			return;
		}

		if (_workspaceSession.IsSelectingRegion)
		{
			SelectionInstructionLabel.Text = "Drag across the preview to define the region to keep, then confirm to write a cropped PNG into the app cache.";
			WorkspaceHintLabel.Text = "Region mode is using a cached full-screen capture as the source image.";
			StatusValueLabel.Text = statusText ?? "Select region";
			UpdateActionState(hasImage: true);
			UpdateSelectionVisual();
			return;
		}

		SelectionInstructionLabel.Text = string.Empty;
		WorkspaceHintLabel.Text = fullPath;
		StatusValueLabel.Text = statusText ?? "Image loaded";
		UpdateActionState(hasImage: true);
		ClearSelectionRectangle();
	}

	private void UpdateActionState(bool hasImage)
	{
		var captureSupported = _screenshotCaptureService.IsSupported;
		var selectionReady = _workspaceSession.IsSelectingRegion && TryGetSelectionProjection(out _);
		var isEditing = _imageEditorSession.IsEditing;

		CaptureButton.IsEnabled = captureSupported && !_workspaceSession.IsSelectingRegion && !isEditing;
		CaptureWindowButton.IsEnabled = captureSupported && !_workspaceSession.IsSelectingRegion && !isEditing;
		CaptureRegionButton.IsEnabled = captureSupported && !_workspaceSession.IsSelectingRegion && !isEditing;
		OpenImageButton.IsEnabled = !_workspaceSession.IsSelectingRegion && !isEditing;
		EditImageButton.IsEnabled = hasImage && !_workspaceSession.IsSelectingRegion && !isEditing && _imageEditorService.IsSupported;
		CopyPathButton.IsEnabled = hasImage && !_workspaceSession.IsSelectingRegion && !isEditing;
		ClearButton.IsEnabled = hasImage && !_workspaceSession.IsSelectingRegion && !isEditing;
		ApplySelectionButton.IsEnabled = selectionReady;
		EditorRectangleButton.IsEnabled = isEditing;
		EditorArrowButton.IsEnabled = isEditing;
		EditorLineButton.IsEnabled = isEditing;
		EditorHighlightButton.IsEnabled = isEditing;
		EditorTextButton.IsEnabled = isEditing;
		EditorCalloutButton.IsEnabled = isEditing;
		EditorUndoButton.IsEnabled = isEditing && _imageEditorSession.Annotations.Count > 0;
		EditorSaveButton.IsEnabled = isEditing && _imageEditorSession.Annotations.Count > 0;
		EditorCloseButton.IsEnabled = isEditing;
	}
}

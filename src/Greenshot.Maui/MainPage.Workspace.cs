using System.IO;

namespace Greenshot.Maui;

public partial class MainPage
{
	private void RefreshWorkspaceState(string? statusText = null)
	{
		var fullPath = _workspaceSession.SelectedImagePath;
		var hasImage = !string.IsNullOrWhiteSpace(fullPath);
		var isEditing = hasImage && _imageEditorSession.IsEditing;
		var isShowingSettings = _isSettingsOpen;
		var usesMacMenuBar = DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst;
		ConfigureSelectedImageWatcher(fullPath);
		UpdatePreviewNavigationState(hasImage);

		PreviewImage.IsVisible = hasImage && !isShowingSettings;
		EmptyStatePanel.IsVisible = !hasImage && !isShowingSettings;
		SettingsPanel.IsVisible = isShowingSettings;
		EditorAnnotationLayer.IsVisible = isEditing && !isShowingSettings;
		EditorInputOverlay.IsVisible = isEditing && !isShowingSettings;
		EditorToolPanel.IsVisible = isEditing && !isShowingSettings;
		SelectionInstructionPanel.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing && !isShowingSettings;
		SelectionOverlay.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing && !isShowingSettings;
		SelectionActionsPanel.IsVisible = _workspaceSession.IsSelectingRegion && hasImage && !isEditing && !isShowingSettings;
		if ((!_workspaceSession.IsSelectingRegion && !isEditing) || !hasImage)
		{
			SetSelectionPointerCursor(false);
			ClearSelectionPointerIndicator();
		}
		if (!isEditing)
		{
			ClearEditorPointerIndicator();
		}

		if (isShowingSettings)
		{
			SelectedImageLabel.Text = "Capture defaults";
			SelectionInstructionLabel.Text = string.Empty;
			WorkspaceHintLabel.Text = "Choose whether captures are copied into a local folder and/or to the clipboard by default.";
			StatusValueLabel.Text = statusText ?? "Settings";
			EditorAnnotationLayer.Children.Clear();
			EditorInputOverlay.InputTransparent = false;
			HideInlineTextEditor(updateVisual: false);
			UpdateActionState(hasImage);
			ClearSelectionRectangle();
			return;
		}

		if (!hasImage)
		{
			PreviewImage.Source = null;
			SelectedImageLabel.Text = "Ready to import a screenshot";
			SelectionInstructionLabel.Text = string.Empty;
			WorkspaceHintLabel.Text = usesMacMenuBar
				? "Use the menu in the top-right corner for capture and file actions, or open an existing image from there."
				: _screenshotCaptureService.IsSupported
					? "Capture a display, a specific window, or a region, or open an existing image file to validate the new Mac-native entry point."
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
			WorkspaceHintLabel.Text = "Pencil, rectangle, arrow, line, text, callout, and highlight now run inside the native MAUI editor. Paste adds clipboard images as movable/resizable overlays and clipboard text as text annotations.";
			StatusValueLabel.Text = statusText ?? "Editing image";
			UpdateActionState(hasImage: true);
			UpdateEditorVisual();
			ClearSelectionRectangle();
			return;
		}

		if (_workspaceSession.IsSelectingRegion)
		{
			SelectionInstructionLabel.Text = "Drag across the preview to define the region to keep, then confirm to write a cropped PNG into the app cache.";
			WorkspaceHintLabel.Text = "Region mode is using a cached display capture as the source image.";
			StatusValueLabel.Text = statusText ?? "Select region";
			UpdateActionState(hasImage: true);
			UpdateSelectionVisual();
			return;
		}

		SelectionInstructionLabel.Text = string.Empty;
		WorkspaceHintLabel.Text = usesMacMenuBar
			? $"{fullPath}\nUse the top-right menu for capture and image actions."
			: fullPath;
		StatusValueLabel.Text = statusText ?? "Image loaded";
		UpdateActionState(hasImage: true);
		ClearSelectionRectangle();
	}

	private void UpdateActionState(bool hasImage)
	{
		var selectionReady = _workspaceSession.IsSelectingRegion && TryGetSelectionProjection(out _);
		var isEditing = _imageEditorSession.IsEditing;

		UpdateWorkspaceMenuState();
		PreviousImageButton.IsEnabled = hasImage && !_workspaceSession.IsSelectingRegion && !isEditing && _workspaceSession.CanSelectPreviousImage;
		NextImageButton.IsEnabled = hasImage && !_workspaceSession.IsSelectingRegion && !isEditing && _workspaceSession.CanSelectNextImage;
		ApplySelectionButton.IsEnabled = selectionReady;
		EditorPencilButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorRectangleButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorArrowButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorLineButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorHighlightButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorTextButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorCalloutButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorPasteButton.IsEnabled = isEditing && !_isSettingsOpen;
		EditorUndoButton.IsEnabled = isEditing && !_isSettingsOpen && _imageEditorSession.Annotations.Count > 0;
		EditorSaveButton.IsEnabled = isEditing && !_isSettingsOpen && _imageEditorSession.Annotations.Count > 0;
		EditorCloseButton.IsEnabled = isEditing && !_isSettingsOpen;
	}

	private void UpdatePreviewNavigationState(bool hasImage)
	{
		var hasHistory = hasImage && _workspaceSession.ImageHistory.Count > 1;
		PreviewHistoryLabel.IsVisible = hasHistory;
		PreviewHistoryLabel.Text = hasHistory
			? $"{_workspaceSession.SelectedImageHistoryNumber} / {_workspaceSession.ImageHistory.Count}"
			: string.Empty;
		PreviousImageButton.IsVisible = hasHistory;
		NextImageButton.IsVisible = hasHistory;
	}

	private void OnPreviousImageClicked(object? sender, EventArgs e)
		=> SelectPreviousImage();

	private void SelectPreviousImage()
	{
		if (_workspaceSession.IsSelectingRegion || _imageEditorSession.IsEditing || !_workspaceSession.SelectPreviousImage())
		{
			return;
		}

		RefreshWorkspaceState("Previous image loaded");
	}

	private void OnNextImageClicked(object? sender, EventArgs e)
		=> SelectNextImage();

	private void SelectNextImage()
	{
		if (_workspaceSession.IsSelectingRegion || _imageEditorSession.IsEditing || !_workspaceSession.SelectNextImage())
		{
			return;
		}

		RefreshWorkspaceState("Next image loaded");
	}
}

namespace Greenshot.Maui;

public partial class MainPage
{
	private sealed record WorkspaceMenuEntry(string Label, WorkspaceAction Action);
	private IReadOnlyList<WorkspaceMenuEntry> _workspaceMenuEntries = Array.Empty<WorkspaceMenuEntry>();
	private bool _isUpdatingWorkspaceMenu;
	private AppShell? _menuHost;
	internal event Action? WorkspaceMenuStateChanged;

	internal async Task ExecuteWorkspaceActionAsync(WorkspaceAction action, bool activateWindow = false)
	{
		if (action != WorkspaceAction.Settings && _isSettingsOpen)
		{
			CloseSettingsPanel();
		}

		switch (action)
		{
			case WorkspaceAction.ShowPreview:
				BringPreviewWindowToFront();
				return;
			case WorkspaceAction.CloseApp:
				_appVisibilityService.Quit();
				return;
			case WorkspaceAction.Settings:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				ShowSettingsPanel();
				return;
			case WorkspaceAction.CaptureScreen:
				await CaptureScreenAsync();
				if (activateWindow && !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath))
				{
					BringPreviewWindowToFront();
				}

				return;
			case WorkspaceAction.CaptureWindow:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await CaptureWindowAsync();
				return;
			case WorkspaceAction.CaptureRegion:
				var shouldActivateWindow = await CaptureRegionAsync();
				if (activateWindow && shouldActivateWindow)
				{
					BringPreviewWindowToFront();
				}

				return;
			case WorkspaceAction.OpenImage:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await OpenImageAsync();
				return;
			case WorkspaceAction.EditImage:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await EditImageAsync();
				return;
			case WorkspaceAction.CopyToClipboard:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await CopyCurrentImageToClipboardAsync();
				return;
			case WorkspaceAction.CopyPath:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await CopyPathAsync();
				return;
			case WorkspaceAction.ClearImage:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				ClearLoadedImage();
				return;
			case WorkspaceAction.PreviousImage:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				SelectPreviousImage();
				return;
			case WorkspaceAction.NextImage:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				SelectNextImage();
				return;
			case WorkspaceAction.ApplySelection:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await ApplySelectionAsync();
				return;
			case WorkspaceAction.CancelSelection:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				CancelRegionSelection();
				return;
			case WorkspaceAction.PasteClipboard:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await PasteClipboardAsync();
				return;
			case WorkspaceAction.SaveEditedCopy:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				await SaveEditedCopyAsync();
				return;
			case WorkspaceAction.CloseEditor:
				if (activateWindow)
				{
					BringPreviewWindowToFront();
				}

				CloseEditor();
				return;
			default:
				throw new ArgumentOutOfRangeException(nameof(action), action, null);
		}
	}

	private void BringPreviewWindowToFront()
	{
		var window = Window ?? Application.Current?.Windows.FirstOrDefault();
		if (window is null)
		{
			return;
		}

		_appVisibilityService.Show();
		Application.Current?.ActivateWindow(window);
	}

	private void OnShowPreviewMenuItemClicked(object? sender, EventArgs e)
		=> BringPreviewWindowToFront();

	private async void OnWorkspaceMenuButtonClicked(object? sender, EventArgs e)
		=> await ShowWorkspaceMenuAsync();

	private async void OnPreviewPointerPressed(object? sender, PointerEventArgs e)
	{
		if (e.Button != ButtonsMask.Secondary || string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath))
		{
			return;
		}

		await ShowWorkspaceMenuAsync();
	}

	private async Task ShowWorkspaceMenuAsync()
	{
		if (_workspaceMenuEntries.Count == 0)
		{
			return;
		}

		var menuTitle = GetWorkspaceMenuTitle();
		var selection = await DisplayActionSheetAsync(
			menuTitle,
			"Cancel",
			null,
			_workspaceMenuEntries.Select(entry => entry.Label).ToArray());

		if (string.IsNullOrWhiteSpace(selection) || string.Equals(selection, "Cancel", StringComparison.Ordinal))
		{
			return;
		}

		var entry = _workspaceMenuEntries.FirstOrDefault(candidate => candidate.Label == selection);
		if (entry is null)
		{
			return;
		}

		await ExecuteWorkspaceActionAsync(entry.Action);
	}

	private async void OnWorkspaceMenuSelectionChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingWorkspaceMenu
			|| WorkspaceMenuPicker.SelectedIndex < 0
			|| WorkspaceMenuPicker.SelectedIndex >= _workspaceMenuEntries.Count)
		{
			return;
		}

		var action = _workspaceMenuEntries[WorkspaceMenuPicker.SelectedIndex].Action;

		_isUpdatingWorkspaceMenu = true;
		try
		{
			WorkspaceMenuPicker.SelectedIndex = -1;
		}
		finally
		{
			_isUpdatingWorkspaceMenu = false;
		}

		await ExecuteWorkspaceActionAsync(action);
	}

	private void UpdateWorkspaceMenuState()
	{
		_workspaceMenuEntries = GetWorkspaceMenuEntries();
		UpdateWorkspacePickerState();
		UpdateNativeMenuBarState(_workspaceMenuEntries.Select(entry => entry.Action).ToHashSet());
		WorkspaceMenuStateChanged?.Invoke();
	}

	internal IReadOnlyList<(string Label, WorkspaceAction Action)> GetStatusBarMenuEntries()
	{
		var entries = new List<(string Label, WorkspaceAction Action)>
		{
			("Show Preview", WorkspaceAction.ShowPreview)
		};

		entries.AddRange(_workspaceMenuEntries.Select(entry => (entry.Label, entry.Action)));
		entries.Add(("Close App", WorkspaceAction.CloseApp));
		return entries;
	}

	private void UpdateWorkspacePickerState()
	{
		var menuTitle = GetWorkspaceMenuTitle();

		_isUpdatingWorkspaceMenu = true;
		try
		{
			WorkspaceMenuPicker.Title = menuTitle;
			WorkspaceMenuPicker.ItemsSource = _workspaceMenuEntries.Select(entry => entry.Label).ToList();
			WorkspaceMenuPicker.SelectedIndex = -1;
			WorkspaceMenuPicker.IsEnabled = _workspaceMenuEntries.Count > 0;
			WorkspaceMenuButton.Text = $"{menuTitle} v";
			WorkspaceMenuButton.IsEnabled = _workspaceMenuEntries.Count > 0;
		}
		finally
		{
			_isUpdatingWorkspaceMenu = false;
		}
	}

	private string GetWorkspaceMenuTitle() =>
		_isSettingsOpen
			? "Settings"
			: _imageEditorSession.IsEditing
			? "Editor"
			: _workspaceSession.IsSelectingRegion
				? "Selection"
				: "Capture / File";

	private void UpdateNativeMenuBarState(IReadOnlySet<WorkspaceAction> availableActions)
	{
		if (_menuHost is not null)
		{
			_menuHost.UpdateWorkspaceMenuState(availableActions);
			return;
		}

#if MACCATALYST
		CaptureScreenMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CaptureScreen);
		CaptureWindowMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CaptureWindow);
		CaptureRegionMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CaptureRegion);
		OpenImageMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.OpenImage);
		EditImageMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.EditImage);
		CopyToClipboardMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CopyToClipboard);
		CopyPathMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CopyPath);
		ClearImageMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.ClearImage);
		ApplySelectionMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.ApplySelection);
		CancelSelectionMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CancelSelection);
		SaveEditedCopyMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.SaveEditedCopy);
		CloseEditorMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.CloseEditor);
		PreviousImageMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.PreviousImage);
		NextImageMenuItem.IsEnabled = availableActions.Contains(WorkspaceAction.NextImage);
		ShowPreviewMenuItem.IsEnabled = true;

		FileMenuBarItem.IsEnabled = OpenImageMenuItem.IsEnabled;
		CaptureMenuBarItem.IsEnabled = CaptureScreenMenuItem.IsEnabled || CaptureWindowMenuItem.IsEnabled || CaptureRegionMenuItem.IsEnabled;
		ImageMenuBarItem.IsEnabled = EditImageMenuItem.IsEnabled || CopyToClipboardMenuItem.IsEnabled || CopyPathMenuItem.IsEnabled || ClearImageMenuItem.IsEnabled;
		SelectionMenuBarItem.IsEnabled = ApplySelectionMenuItem.IsEnabled || CancelSelectionMenuItem.IsEnabled;
		EditorMenuBarItem.IsEnabled = SaveEditedCopyMenuItem.IsEnabled || CloseEditorMenuItem.IsEnabled;
		NavigateMenuBarItem.IsEnabled = PreviousImageMenuItem.IsEnabled || NextImageMenuItem.IsEnabled;
		WindowMenuBarItem.IsEnabled = ShowPreviewMenuItem.IsEnabled;
#endif
	}

	private IReadOnlyList<WorkspaceMenuEntry> GetWorkspaceMenuEntries()
	{
		var hasImage = !string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath);
		var entries = new List<WorkspaceMenuEntry>();

		if (_imageEditorSession.IsEditing)
		{
			entries.Add(new WorkspaceMenuEntry("Paste", WorkspaceAction.PasteClipboard));
			entries.Add(new WorkspaceMenuEntry("Copy to Clipboard", WorkspaceAction.CopyToClipboard));

			if (_imageEditorSession.Annotations.Count > 0)
			{
				entries.Add(new WorkspaceMenuEntry("Save Copy", WorkspaceAction.SaveEditedCopy));
			}

			entries.Add(new WorkspaceMenuEntry("Close Editor", WorkspaceAction.CloseEditor));
			entries.Add(new WorkspaceMenuEntry("Settings", WorkspaceAction.Settings));
			return entries;
		}

		if (_workspaceSession.IsSelectingRegion)
		{
			if (TryGetSelectionProjection(out _))
			{
				entries.Add(new WorkspaceMenuEntry("Capture Selection", WorkspaceAction.ApplySelection));
			}

			entries.Add(new WorkspaceMenuEntry("Cancel Selection", WorkspaceAction.CancelSelection));
			entries.Add(new WorkspaceMenuEntry("Settings", WorkspaceAction.Settings));
			return entries;
		}

		if (_screenshotCaptureService.IsSupported)
		{
			entries.Add(new WorkspaceMenuEntry("Capture Screen", WorkspaceAction.CaptureScreen));
			entries.Add(new WorkspaceMenuEntry("Capture Window", WorkspaceAction.CaptureWindow));
			entries.Add(new WorkspaceMenuEntry("Capture Region", WorkspaceAction.CaptureRegion));
		}

		entries.Add(new WorkspaceMenuEntry("Open Image", WorkspaceAction.OpenImage));

		if (hasImage && _imageEditorService.IsSupported)
		{
			entries.Add(new WorkspaceMenuEntry("Edit Image", WorkspaceAction.EditImage));
		}

		if (hasImage)
		{
			entries.Add(new WorkspaceMenuEntry("Copy to Clipboard", WorkspaceAction.CopyToClipboard));
			entries.Add(new WorkspaceMenuEntry("Copy Path", WorkspaceAction.CopyPath));
			entries.Add(new WorkspaceMenuEntry("Clear Image", WorkspaceAction.ClearImage));
		}

		if (hasImage && _workspaceSession.CanSelectPreviousImage)
		{
			entries.Add(new WorkspaceMenuEntry("Previous Image", WorkspaceAction.PreviousImage));
		}

		if (hasImage && _workspaceSession.CanSelectNextImage)
		{
			entries.Add(new WorkspaceMenuEntry("Next Image", WorkspaceAction.NextImage));
		}

		entries.Add(new WorkspaceMenuEntry("Settings", WorkspaceAction.Settings));
		return entries;
	}
}

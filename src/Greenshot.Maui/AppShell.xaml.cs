namespace Greenshot.Maui;

public partial class AppShell : Shell
{
	private readonly MainPage _mainPage;

	public AppShell(MainPage mainPage)
	{
		InitializeComponent();
		_mainPage = mainPage;

#if !MACCATALYST
		MenuBarItems.Clear();
#endif

		Items.Add(new ShellContent
		{
			Title = "Workspace",
			Route = nameof(MainPage),
			Content = mainPage
		});

		_mainPage.AttachMenuHost(this);
	}

	internal void UpdateWorkspaceMenuState(IReadOnlySet<WorkspaceAction> availableActions)
	{
#if !MACCATALYST
		return;
#endif

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
	}

	private async void OnOpenImageMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.OpenImage);

	private async void OnCaptureScreenMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CaptureScreen);

	private async void OnCaptureWindowMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CaptureWindow);

	private async void OnCaptureRegionMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CaptureRegion);

	private async void OnEditImageMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.EditImage);

	private async void OnCopyToClipboardMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CopyToClipboard);

	private async void OnCopyPathMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CopyPath);

	private async void OnClearImageMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.ClearImage);

	private async void OnApplySelectionMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.ApplySelection);

	private async void OnCancelSelectionMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CancelSelection);

	private async void OnSaveEditedCopyMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.SaveEditedCopy);

	private async void OnCloseEditorMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.CloseEditor);

	private async void OnPreviousImageMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.PreviousImage);

	private async void OnNextImageMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.NextImage);

	private async void OnShowPreviewMenuItemClicked(object? sender, EventArgs e)
		=> await _mainPage.ExecuteWorkspaceActionAsync(WorkspaceAction.ShowPreview);
}

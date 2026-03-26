using System.IO;

namespace Greenshot.Maui;

public partial class MainPage
{
	private bool _isSettingsOpen;
	private bool _isUpdatingSettingsControls;
	private string? _selectedCaptureDirectoryBookmark;

	private void InitializeSettingsPanel()
	{
		SettingsPanel.IsVisible = false;
		SettingsChooseFolderButton.IsVisible = _localFolderAccessService.IsSupported;
		SettingsSaveLocationEntry.IsReadOnly = _localFolderAccessService.IsSupported;
		SettingsDefaultFolderLabel.Text = _captureDefaultsService.DefaultLocalCaptureSaveDirectory;
	}

	private void ShowSettingsPanel()
	{
		HideInlineTextEditor();
		_isSettingsOpen = true;
		LoadSettingsIntoControls();
		RefreshWorkspaceState("Settings");
	}

	private void CloseSettingsPanel(string? statusText = null)
	{
		if (!_isSettingsOpen)
		{
			return;
		}

		_isSettingsOpen = false;
		RefreshWorkspaceState(statusText);
	}

	private void LoadSettingsIntoControls()
	{
		_isUpdatingSettingsControls = true;
		try
		{
			SettingsSaveToLocalSwitch.IsToggled = _captureDefaultsService.SaveCapturesToLocal;
			SettingsSaveLocationEntry.Text = _captureDefaultsService.LocalCaptureSaveDirectory;
			_selectedCaptureDirectoryBookmark = _captureDefaultsService.LocalCaptureSaveDirectoryBookmark;
			SettingsCopyToClipboardSwitch.IsToggled = _captureDefaultsService.CopyCapturesToClipboard;
			SettingsDefaultFolderLabel.Text = _captureDefaultsService.DefaultLocalCaptureSaveDirectory;
			UpdateSettingsPanelState();
		}
		finally
		{
			_isUpdatingSettingsControls = false;
		}
	}

	private void UpdateSettingsPanelState()
	{
		var isSaveToLocalEnabled = SettingsSaveToLocalSwitch.IsToggled;
		SettingsSaveLocationEntry.IsEnabled = isSaveToLocalEnabled;
		SettingsUseDefaultFolderButton.IsEnabled = isSaveToLocalEnabled;
	}

	private void OnSettingsSaveToLocalToggled(object? sender, ToggledEventArgs e)
	{
		if (_isUpdatingSettingsControls)
		{
			return;
		}

		UpdateSettingsPanelState();
	}

	private void OnUseDefaultCaptureFolderClicked(object? sender, EventArgs e)
	{
		SettingsSaveLocationEntry.Text = _captureDefaultsService.DefaultLocalCaptureSaveDirectory;
		_selectedCaptureDirectoryBookmark = null;
	}

	private async void OnChooseCaptureFolderClicked(object? sender, EventArgs e)
		=> await ChooseCaptureFolderAsync();

	private async Task ChooseCaptureFolderAsync()
	{
		try
		{
			var selection = await _localFolderAccessService.PickFolderAsync(SettingsSaveLocationEntry.Text);
			if (selection is null)
			{
				return;
			}

			SettingsSaveLocationEntry.Text = selection.DirectoryPath;
			_selectedCaptureDirectoryBookmark = selection.Bookmark;
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Folder selection failed", ex.Message, "OK");
		}
	}

	private async void OnSaveSettingsClicked(object? sender, EventArgs e)
		=> await SaveSettingsAsync();

	private async Task SaveSettingsAsync()
	{
		var saveToLocal = SettingsSaveToLocalSwitch.IsToggled;
		var copyToClipboard = SettingsCopyToClipboardSwitch.IsToggled;
		var targetDirectory = SettingsSaveLocationEntry.Text;

		try
		{
			if (saveToLocal)
			{
				targetDirectory = _captureDefaultsService.NormalizeDirectoryPath(targetDirectory);
				var requiredBookmark = GetRequiredLocalFolderBookmark(targetDirectory, _selectedCaptureDirectoryBookmark);
				EnsureLocalFolderAccessConfigured(targetDirectory, requiredBookmark);

				using var folderAccess = _localFolderAccessService.BeginWriteAccess(targetDirectory, requiredBookmark);
				Directory.CreateDirectory(targetDirectory);
			}

			_captureDefaultsService.SaveCapturesToLocal = saveToLocal;
			_captureDefaultsService.LocalCaptureSaveDirectory = saveToLocal
				? targetDirectory!
				: _captureDefaultsService.LocalCaptureSaveDirectory;
			_captureDefaultsService.LocalCaptureSaveDirectoryBookmark = saveToLocal
				? GetRequiredLocalFolderBookmark(targetDirectory!, _selectedCaptureDirectoryBookmark)
				: _captureDefaultsService.LocalCaptureSaveDirectoryBookmark;
			_captureDefaultsService.CopyCapturesToClipboard = copyToClipboard;

			CloseSettingsPanel("Settings saved");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Settings could not be saved", ex.Message, "OK");
		}
	}

	private void OnCloseSettingsClicked(object? sender, EventArgs e)
		=> CloseSettingsPanel();

	private void EnsureLocalFolderAccessConfigured(string targetDirectory, string? bookmark)
	{
		if (!RequiresLocalFolderBookmark(targetDirectory))
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(bookmark))
		{
			throw new InvalidOperationException("Folders outside Pictures need explicit macOS write permission. Open Settings and use Choose Folder for the save location.");
		}
	}

	private string? GetRequiredLocalFolderBookmark(string targetDirectory, string? bookmark) =>
		RequiresLocalFolderBookmark(targetDirectory) ? bookmark : null;

	private bool RequiresLocalFolderBookmark(string targetDirectory) =>
		_localFolderAccessService.IsSupported
		&& !IsPathWithinDirectory(
			_captureDefaultsService.NormalizeDirectoryPath(targetDirectory),
			_captureDefaultsService.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));

	private static bool IsPathWithinDirectory(string candidatePath, string directoryPath)
	{
		var normalizedDirectoryPath = directoryPath.TrimEnd(Path.DirectorySeparatorChar);
		var normalizedCandidatePath = candidatePath.TrimEnd(Path.DirectorySeparatorChar);

		return string.Equals(normalizedCandidatePath, normalizedDirectoryPath, StringComparison.Ordinal)
			|| normalizedCandidatePath.StartsWith(
				normalizedDirectoryPath + Path.DirectorySeparatorChar,
				StringComparison.Ordinal);
	}
}

using System.IO;
using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui;

public partial class MainPage
{
	private async void OnOpenImageClicked(object? sender, EventArgs e)
		=> await OpenImageAsync();

	private async Task OpenImageAsync()
	{
		try
		{
			if (_workspaceSession.IsSelectingRegion)
			{
				ExitRegionSelectionMode();
			}

			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Open a screenshot",
				FileTypes = FilePickerFileType.Images
			});

			if (result is null)
			{
				return;
			}

			var importedPath = await ImportPickedImageAsync(result);
			_workspaceSession.LoadImage(importedPath);
			RefreshWorkspaceState("Image loaded");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Open failed", ex.Message, "OK");
		}
	}

	private async void OnCaptureScreenClicked(object? sender, EventArgs e)
		=> await CaptureScreenAsync();

	private async Task CaptureScreenAsync()
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			ExitRegionSelectionMode();
		}

		var result = await CaptureSelectedDisplayAsync();
		if (result is null)
		{
			return;
		}

		await FinalizeCapturedImageAsync(result, "Screen captured");
	}

	private async void OnCaptureRegionClicked(object? sender, EventArgs e)
		=> await CaptureRegionAsync();

	private async Task<bool> CaptureRegionAsync()
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			return false;
		}

		var result = await CaptureSelectedDisplayAsync();
		if (result is null)
		{
			return false;
		}

		EnterRegionSelectionMode(result);
		return true;
	}

	private async void OnCaptureWindowClicked(object? sender, EventArgs e)
		=> await CaptureWindowAsync();

	private async Task CaptureWindowAsync()
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			ExitRegionSelectionMode();
		}

		var catalog = await _screenshotCaptureService.GetAvailableWindowsAsync();
		if (!catalog.IsSuccessful)
		{
			await ShowCaptureFailureAsync(catalog.Status, catalog.Message);
			return;
		}

		if (catalog.Windows.Count == 0)
		{
			await DisplayAlertAsync("No windows found", "No shareable windows are currently available for capture.", "OK");
			return;
		}

		var labels = ScreenshotWindowTargetLabelBuilder.Build(catalog.Windows);
		var options = catalog.Windows.Select(window => labels[window.WindowId]).ToArray();
		var selection = await DisplayActionSheetAsync("Choose a window to capture", "Cancel", null, options);
		if (string.IsNullOrWhiteSpace(selection) || string.Equals(selection, "Cancel", StringComparison.Ordinal))
		{
			return;
		}

		var selectedWindow = catalog.Windows.FirstOrDefault(window => labels[window.WindowId] == selection);
		if (selectedWindow is null)
		{
			return;
		}

		var result = await _screenshotCaptureService.CaptureWindowAsync(selectedWindow.WindowId);
		if (!result.IsSuccessful)
		{
			await ShowCaptureFailureAsync(result);
			return;
		}

		await FinalizeCapturedImageAsync(result, "Window captured");
	}

	private async Task<ScreenshotCaptureResult?> CaptureSelectedDisplayAsync()
	{
		var catalog = await _screenshotCaptureService.GetAvailableDisplaysAsync();
		if (!catalog.IsSuccessful)
		{
			await ShowCaptureFailureAsync(catalog.Status, catalog.Message);
			return null;
		}

		if (catalog.Displays.Count == 0)
		{
			await DisplayAlertAsync("No displays found", "No shareable displays are currently available for capture.", "OK");
			return null;
		}

		var selectedDisplay = catalog.Displays.Count == 1
			? catalog.Displays[0]
			: await SelectDisplayTargetAsync(catalog.Displays);
		if (selectedDisplay is null)
		{
			return null;
		}

		var result = await _screenshotCaptureService.CaptureDisplayAsync(selectedDisplay.DisplayId);
		if (!result.IsSuccessful)
		{
			await ShowCaptureFailureAsync(result);
			return null;
		}

		return result;
	}

	private async Task<ScreenshotDisplayTarget?> SelectDisplayTargetAsync(IReadOnlyList<ScreenshotDisplayTarget> displays)
	{
		var labels = ScreenshotDisplayTargetLabelBuilder.Build(displays);
		var options = displays.Select(display => labels[display.DisplayId]).ToArray();
		var selection = await DisplayActionSheetAsync("Choose a display to capture", "Cancel", null, options);
		if (string.IsNullOrWhiteSpace(selection) || string.Equals(selection, "Cancel", StringComparison.Ordinal))
		{
			return null;
		}

		return displays.FirstOrDefault(display => labels[display.DisplayId] == selection);
	}

	private async void OnApplySelectionClicked(object? sender, EventArgs e)
		=> await ApplySelectionAsync();

	private async Task ApplySelectionAsync()
	{
		if (!_workspaceSession.IsSelectingRegion || string.IsNullOrWhiteSpace(_workspaceSession.SelectionSourcePath))
		{
			return;
		}

		if (!TryGetSelectionProjection(out var projection))
		{
			await DisplayAlertAsync("Selection needed", "Drag across the preview to choose a region to keep.", "OK");
			return;
		}

		var result = await _screenshotCaptureService.CropImageAsync(_workspaceSession.SelectionSourcePath, projection.CropBounds);
		if (!result.IsSuccessful)
		{
			await ShowCaptureFailureAsync(result);
			return;
		}

		await FinalizeRegionCaptureAsync(result, "Region captured");
	}

	private void OnCancelSelectionClicked(object? sender, EventArgs e)
		=> CancelRegionSelection();

	private void CancelRegionSelection()
	{
		if (!_workspaceSession.IsSelectingRegion)
		{
			return;
		}

		var sourcePath = _workspaceSession.CancelRegionSelection();
		RefreshWorkspaceState(sourcePath is null ? "Preview workspace" : "Screen captured");
	}

	private void OnClearImageClicked(object? sender, EventArgs e)
		=> ClearLoadedImage();

	private void ClearLoadedImage()
	{
		_workspaceSession.ClearImage();
		RefreshWorkspaceState();
	}

	private async void OnCopyPathClicked(object? sender, EventArgs e)
		=> await CopyPathAsync();

	private async Task CopyPathAsync()
	{
		if (_workspaceSession.SelectedImagePath is null)
		{
			await DisplayAlertAsync("Nothing to copy", "Open an image first.", "OK");
			return;
		}

		await Clipboard.Default.SetTextAsync(_workspaceSession.SelectedImagePath);
		await DisplayAlertAsync("Path copied", Path.GetFileName(_workspaceSession.SelectedImagePath), "OK");
	}

	private async void OnEditImageClicked(object? sender, EventArgs e)
		=> await EditImageAsync();

	private async Task EditImageAsync()
	{
		if (_workspaceSession.SelectedImagePath is null)
		{
			await DisplayAlertAsync("Nothing to edit", "Capture or open an image first.", "OK");
			return;
		}

		try
		{
			await EnterImageEditorModeAsync(_workspaceSession.SelectedImagePath);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Editor launch failed", ex.Message, "OK");
		}
	}

	private async Task ShowCaptureFailureAsync(ScreenshotCaptureResult result)
		=> await ShowCaptureFailureAsync(result.Status, result.Message);

	private async Task ShowCaptureFailureAsync(ScreenshotCaptureStatus status, string message)
	{
		var feedback = ScreenshotCaptureFeedbackBuilder.Build(
			status,
			message,
			GetPermissionTroubleshootingHint(status));

		StatusValueLabel.Text = feedback.StatusLabel;
		await DisplayAlertAsync(feedback.Title, feedback.Message, "OK");
	}

	private static string? GetPermissionTroubleshootingHint(ScreenshotCaptureStatus status) =>
		status == ScreenshotCaptureStatus.PermissionDenied && DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst
			? MacCatalystPermissionTroubleshootingHint
			: null;

	private static async Task<string> ImportPickedImageAsync(FileResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		var extension = Path.GetExtension(result.FileName);
		if (string.IsNullOrWhiteSpace(extension))
		{
			extension = ".png";
		}

		var importedPath = Path.Combine(
			FileSystem.Current.CacheDirectory,
			$"greenshot-import-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}{extension}");

		await using var input = await result.OpenReadAsync();
		await using var output = File.Create(importedPath);
		await input.CopyToAsync(output);

		return importedPath;
	}

	private async Task FinalizeCapturedImageAsync(ScreenshotCaptureResult result, string successStatus)
	{
		var finalPath = result.FilePath!;
		var localSaveApplied = false;
		string? localSaveFailure = null;
		string? clipboardFailure = null;

		try
		{
			finalPath = await SaveCapturedImageToConfiguredLocationAsync(finalPath);
			localSaveApplied = !string.Equals(finalPath, result.FilePath, StringComparison.Ordinal);
		}
		catch (Exception ex)
		{
			localSaveFailure = ex.Message;
			finalPath = result.FilePath!;
		}

		_workspaceSession.LoadImage(finalPath);

		var statusParts = new List<string> { successStatus };
		if (localSaveApplied)
		{
			statusParts.Add("saved locally");
		}

		if (_captureDefaultsService.CopyCapturesToClipboard)
		{
			try
			{
				await CopyImageToClipboardAsync(finalPath);
				statusParts.Add("copied to clipboard");
			}
			catch (Exception ex)
			{
				clipboardFailure = ex.Message;
			}
		}

		RefreshWorkspaceState(string.Join("; ", statusParts));

		if (localSaveFailure is not null)
		{
			await DisplayAlertAsync("Local save failed", localSaveFailure, "OK");
		}

		if (clipboardFailure is not null)
		{
			await DisplayAlertAsync("Clipboard copy failed", clipboardFailure, "OK");
		}
	}

	private async Task FinalizeRegionCaptureAsync(ScreenshotCaptureResult result, string successStatus)
	{
		await FinalizeCapturedImageAsync(result, successStatus);
		_workspaceSession.CompleteRegionSelection(_workspaceSession.SelectedImagePath!);
	}

	private async Task<string> SaveCapturedImageToConfiguredLocationAsync(string sourcePath)
	{
		if (!_captureDefaultsService.SaveCapturesToLocal)
		{
			return sourcePath;
		}

		var targetDirectory = _captureDefaultsService.LocalCaptureSaveDirectory;
		var requiredBookmark = GetRequiredLocalFolderBookmark(
			targetDirectory,
			_captureDefaultsService.LocalCaptureSaveDirectoryBookmark);
		EnsureLocalFolderAccessConfigured(
			targetDirectory,
			requiredBookmark);
		using var folderAccess = _localFolderAccessService.BeginWriteAccess(
			targetDirectory,
			requiredBookmark);
		Directory.CreateDirectory(targetDirectory);

		var sourceFileName = Path.GetFileName(sourcePath);
		var destinationPath = GetUniqueDestinationPath(Path.Combine(targetDirectory, sourceFileName));

		await using var input = File.OpenRead(sourcePath);
		await using var output = File.Create(destinationPath);
		await input.CopyToAsync(output);

		return destinationPath;
	}

	private static string GetUniqueDestinationPath(string candidatePath)
	{
		if (!File.Exists(candidatePath))
		{
			return candidatePath;
		}

		var directory = Path.GetDirectoryName(candidatePath) ?? FileSystem.Current.CacheDirectory;
		var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(candidatePath);
		var extension = Path.GetExtension(candidatePath);

		for (var index = 1; ; index++)
		{
			var alternatePath = Path.Combine(directory, $"{fileNameWithoutExtension}-{index}{extension}");
			if (!File.Exists(alternatePath))
			{
				return alternatePath;
			}
		}
	}
}

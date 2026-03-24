using System.IO;
using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui;

public partial class MainPage
{
	private async void OnOpenImageClicked(object? sender, EventArgs e)
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
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			ExitRegionSelectionMode();
		}

		var result = await _screenshotCaptureService.CapturePrimaryDisplayAsync();
		if (!result.IsSuccessful)
		{
			await ShowCaptureFailureAsync(result);
			return;
		}

		_workspaceSession.LoadImage(result.FilePath!);
		RefreshWorkspaceState("Screen captured");
	}

	private async void OnCaptureRegionClicked(object? sender, EventArgs e)
	{
		if (_workspaceSession.IsSelectingRegion)
		{
			return;
		}

		var result = await _screenshotCaptureService.CapturePrimaryDisplayAsync();
		if (!result.IsSuccessful)
		{
			await ShowCaptureFailureAsync(result);
			return;
		}

		EnterRegionSelectionMode(result);
	}

	private async void OnCaptureWindowClicked(object? sender, EventArgs e)
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

		_workspaceSession.LoadImage(result.FilePath!);
		RefreshWorkspaceState("Window captured");
	}

	private async void OnApplySelectionClicked(object? sender, EventArgs e)
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

		_workspaceSession.CompleteRegionSelection(result.FilePath!);
		RefreshWorkspaceState("Region captured");
	}

	private void OnCancelSelectionClicked(object? sender, EventArgs e)
	{
		if (!_workspaceSession.IsSelectingRegion)
		{
			return;
		}

		var sourcePath = _workspaceSession.CancelRegionSelection();
		RefreshWorkspaceState(sourcePath is null ? "Preview workspace" : "Screen captured");
	}

	private void OnClearImageClicked(object? sender, EventArgs e)
	{
		_workspaceSession.ClearImage();
		RefreshWorkspaceState();
	}

	private async void OnCopyPathClicked(object? sender, EventArgs e)
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
}

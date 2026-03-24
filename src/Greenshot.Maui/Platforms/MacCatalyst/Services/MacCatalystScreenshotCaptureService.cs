using System.Runtime.Versioning;
using CoreGraphics;
using Greenshot.Maui.Core.Services;
using ScreenCaptureKit;
using UIKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

[SupportedOSPlatform("maccatalyst")]
public sealed class MacCatalystScreenshotCaptureService : IScreenshotCaptureService
{
	private readonly MacCatalystScreenCaptureSupport _screenCaptureSupport = new();
	private readonly MacCatalystShareableContentProvider _shareableContentProvider = new();
	private readonly MacCatalystPngArtifactWriter _artifactWriter = new();

	public bool IsSupported => _screenCaptureSupport.IsSupported;

	public async Task<ScreenshotCaptureResult> CapturePrimaryDisplayAsync(CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateCapturePrerequisites(out var failure))
		{
			return failure!;
		}

		return await CapturePrimaryDisplayCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<ScreenshotWindowCatalogResult> GetAvailableWindowsAsync(CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateWindowCatalogPrerequisites(out var failure))
		{
			return failure!;
		}

		return await GetAvailableWindowsCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<ScreenshotCaptureResult> CaptureWindowAsync(
		uint windowId,
		CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateCapturePrerequisites(out var failure))
		{
			return failure!;
		}

		return await CaptureWindowByIdCoreAsync(windowId, cancellationToken).ConfigureAwait(false);
	}

	public async Task<ScreenshotCaptureResult> CropImageAsync(
		string sourceFilePath,
		ScreenshotCropBounds cropBounds,
		CancellationToken cancellationToken = default)
	{
		if (!IsSupported)
		{
			return ScreenshotCaptureResult.Unsupported(MacCatalystScreenCaptureSupport.UnsupportedVersionMessage);
		}

		if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
		{
			return ScreenshotCaptureResult.Failed("The captured source image could not be found.");
		}

		if (cropBounds.IsEmpty)
		{
			return ScreenshotCaptureResult.Failed("Select a non-empty capture region.");
		}

		return await CropImageCoreAsync(sourceFilePath, cropBounds, cancellationToken).ConfigureAwait(false);
	}

	[SupportedOSPlatform("macos14.0")]
	private async Task<ScreenshotCaptureResult> CapturePrimaryDisplayCoreAsync(CancellationToken cancellationToken)
	{
		try
		{
			var shareableContent = await _shareableContentProvider.GetShareableContentAsync(cancellationToken).ConfigureAwait(false);
			var display = SelectPrimaryDisplay(shareableContent);
			if (display is null)
			{
				return ScreenshotCaptureResult.Failed("No shareable display was returned by ScreenCaptureKit.");
			}

			using var contentFilter = new SCContentFilter(display, Array.Empty<SCWindow>(), SCContentFilterOption.Exclude);
			using var configuration = new SCStreamConfiguration
			{
				Width = checked((nuint)display.Width),
				Height = checked((nuint)display.Height),
				ShowsCursor = true
			};

			using var image = await _shareableContentProvider.CaptureImageAsync(contentFilter, configuration, cancellationToken).ConfigureAwait(false);
			if (image is null)
			{
				return ScreenshotCaptureResult.Failed("ScreenCaptureKit returned an empty image.");
			}

			return await _artifactWriter.WriteAsync(
				image,
				"greenshot-capture",
				"Captured the primary display.",
				"The captured frame could not be encoded as PNG.",
				cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotCaptureResult.Failed(ex.Message);
		}
	}

	[SupportedOSPlatform("macos14.0")]
	private async Task<ScreenshotWindowCatalogResult> GetAvailableWindowsCoreAsync(CancellationToken cancellationToken)
	{
		try
		{
			var shareableContent = await _shareableContentProvider.GetWindowShareableContentAsync(cancellationToken).ConfigureAwait(false);
			var windows = (shareableContent.Windows ?? Array.Empty<SCWindow>())
				.Where(IsEligibleWindow)
				.Select(CreateWindowTarget)
				.OrderBy(static window => window.ApplicationName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(static window => window.Title, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			return windows.Length == 0
				? ScreenshotWindowCatalogResult.Failed("No shareable windows are currently available.")
				: ScreenshotWindowCatalogResult.Success(windows);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotWindowCatalogResult.Failed(ex.Message);
		}
	}

	[SupportedOSPlatform("macos14.0")]
	private async Task<ScreenshotCaptureResult> CaptureWindowByIdCoreAsync(
		uint windowId,
		CancellationToken cancellationToken)
	{
		try
		{
			var shareableContent = await _shareableContentProvider.GetWindowShareableContentAsync(cancellationToken).ConfigureAwait(false);
			var window = SelectWindow(shareableContent, windowId);
			if (window is null)
			{
				return ScreenshotCaptureResult.Failed("The selected window is no longer available.");
			}

			return await CaptureWindowCoreAsync(window, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotCaptureResult.Failed(ex.Message);
		}
	}

	[SupportedOSPlatform("macos14.0")]
	private async Task<ScreenshotCaptureResult> CropImageCoreAsync(
		string sourceFilePath,
		ScreenshotCropBounds cropBounds,
		CancellationToken cancellationToken)
	{
		try
		{
			using var image = UIImage.FromFile(sourceFilePath);
			var sourceImage = image?.CGImage;
			if (sourceImage is null)
			{
				return ScreenshotCaptureResult.Failed("The source image could not be decoded for cropping.");
			}

			var cropRect = ClampCropBounds(cropBounds, (int)sourceImage.Width, (int)sourceImage.Height);
			if (cropRect.Width <= 0 || cropRect.Height <= 0)
			{
				return ScreenshotCaptureResult.Failed("The selected region falls outside the captured image.");
			}

			using var croppedImage = sourceImage.WithImageInRect(cropRect);
			if (croppedImage is null)
			{
				return ScreenshotCaptureResult.Failed("The selected region could not be cropped.");
			}

			return await _artifactWriter.WriteAsync(
				croppedImage,
				"greenshot-region",
				"Captured the selected region.",
				"The cropped region could not be encoded as PNG.",
				cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotCaptureResult.Failed(ex.Message);
		}
	}

	[SupportedOSPlatform("macos14.0")]
	private async Task<ScreenshotCaptureResult> CaptureWindowCoreAsync(
		SCWindow window,
		CancellationToken cancellationToken)
	{
		try
		{
			using var contentFilter = new SCContentFilter(window);
			using var configuration = new SCStreamConfiguration
			{
				Width = checked((nuint)Math.Max(1, (int)Math.Ceiling(window.Frame.Width))),
				Height = checked((nuint)Math.Max(1, (int)Math.Ceiling(window.Frame.Height))),
				ShowsCursor = false,
				ScalesToFit = true,
				PreservesAspectRatio = true
			};

			using var image = await _shareableContentProvider.CaptureImageAsync(contentFilter, configuration, cancellationToken).ConfigureAwait(false);
			if (image is null)
			{
				return ScreenshotCaptureResult.Failed("ScreenCaptureKit returned an empty window image.");
			}

			return await _artifactWriter.WriteAsync(
				image,
				"greenshot-window",
				"Captured the selected window.",
				"The captured window frame could not be encoded as PNG.",
				cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotCaptureResult.Failed(ex.Message);
		}
	}

	[SupportedOSPlatform("macos14.0")]
	private static SCDisplay? SelectPrimaryDisplay(SCShareableContent shareableContent)
	{
		var displays = shareableContent.Displays ?? Array.Empty<SCDisplay>();
		if (displays.Length == 0)
		{
			return null;
		}

		var mainDisplayId = CGDisplay.MainDisplayID;
		return displays.FirstOrDefault(display => display.DisplayId == mainDisplayId) ?? displays[0];
	}

	[SupportedOSPlatform("macos14.0")]
	private static SCWindow? SelectWindow(SCShareableContent shareableContent, uint windowId) =>
		(shareableContent.Windows ?? Array.Empty<SCWindow>())
			.FirstOrDefault(window => window.WindowId == windowId && IsEligibleWindow(window));

	[SupportedOSPlatform("macos14.0")]
	private static bool IsEligibleWindow(SCWindow window)
	{
		if (!window.OnScreen || window.Frame.Width < 64d || window.Frame.Height < 64d)
		{
			return false;
		}

		return window.OwningApplication?.ProcessId != Environment.ProcessId;
	}

	[SupportedOSPlatform("macos14.0")]
	private static ScreenshotWindowTarget CreateWindowTarget(SCWindow window)
	{
		var title = window.Title?.Trim() ?? string.Empty;
		var applicationName = window.OwningApplication?.ApplicationName?.Trim() ?? "Unknown app";

		return new ScreenshotWindowTarget(
			window.WindowId,
			title,
			applicationName,
			Math.Max(1, (int)Math.Ceiling(window.Frame.Width)),
			Math.Max(1, (int)Math.Ceiling(window.Frame.Height)));
	}

	private static CGRect ClampCropBounds(ScreenshotCropBounds cropBounds, int imageWidth, int imageHeight)
	{
		var x = Math.Clamp(cropBounds.X, 0, imageWidth);
		var y = Math.Clamp(cropBounds.Y, 0, imageHeight);
		var maxWidth = Math.Max(0, imageWidth - x);
		var maxHeight = Math.Max(0, imageHeight - y);
		var width = Math.Clamp(cropBounds.Width, 0, maxWidth);
		var height = Math.Clamp(cropBounds.Height, 0, maxHeight);
		return new CGRect(x, y, width, height);
	}
}

using System.Runtime.Versioning;
using CoreGraphics;
using Greenshot.Maui.Core.Services;
using ScreenCaptureKit;
using UIKit;
using System.Diagnostics;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

[SupportedOSPlatform("maccatalyst")]
public sealed class MacCatalystScreenshotCaptureService : IScreenshotCaptureService
{
	private const string InteractiveRegionCancelledMessage = "Region capture was cancelled.";
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

	public async Task<ScreenshotDisplayCatalogResult> GetAvailableDisplaysAsync(CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateCapturePrerequisites(out var failure))
		{
			return MapDisplayCatalogFailure(failure!);
		}

		return await GetAvailableDisplaysCoreAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<ScreenshotCaptureResult> CaptureDisplayAsync(
		uint displayId,
		CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateCapturePrerequisites(out var failure))
		{
			return failure!;
		}

		return await CaptureDisplayByIdCoreAsync(displayId, cancellationToken).ConfigureAwait(false);
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

	public async Task<ScreenshotCaptureResult> CaptureInteractiveRegionAsync(CancellationToken cancellationToken = default)
	{
		if (!_screenCaptureSupport.TryValidateCapturePrerequisites(out var failure))
		{
			return failure!;
		}

		return await CaptureInteractiveRegionCoreAsync(cancellationToken).ConfigureAwait(false);
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

			return await CaptureDisplayCoreAsync(
				display,
				"Captured the primary display.",
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

	private static async Task<ScreenshotCaptureResult> CaptureInteractiveRegionCoreAsync(CancellationToken cancellationToken)
	{
		var outputPath = Path.Combine(
			FileSystem.Current.CacheDirectory,
			$"greenshot-region-{DateTimeOffset.Now:yyyyMMdd-HHmmssfff}.png");

		try
		{
			if (File.Exists(outputPath))
			{
				File.Delete(outputPath);
			}

			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/usr/sbin/screencapture",
					Arguments = $"-i -x \"{outputPath}\"",
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			if (!process.Start())
			{
				return ScreenshotCaptureResult.Failed("Interactive region capture could not be started.");
			}

			await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

			if (File.Exists(outputPath))
			{
				using var image = UIImage.FromFile(outputPath);
				var pixelWidth = image?.CGImage is null ? 0 : (int)image.CGImage.Width;
				var pixelHeight = image?.CGImage is null ? 0 : (int)image.CGImage.Height;

				return ScreenshotCaptureResult.Success(
					outputPath,
					"Captured the selected region.",
					pixelWidth,
					pixelHeight);
			}

			if (process.ExitCode != 0)
			{
				return ScreenshotCaptureResult.Cancelled(InteractiveRegionCancelledMessage);
			}

			return ScreenshotCaptureResult.Failed("The selected region was not written to disk.");
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
	private async Task<ScreenshotDisplayCatalogResult> GetAvailableDisplaysCoreAsync(CancellationToken cancellationToken)
	{
		try
		{
			var shareableContent = await _shareableContentProvider.GetShareableContentAsync(cancellationToken).ConfigureAwait(false);
			var mainDisplayId = checked((uint)CGDisplay.MainDisplayID);
			var displays = (shareableContent.Displays ?? Array.Empty<SCDisplay>())
				.Select(display => CreateDisplayTarget(display, mainDisplayId))
				.OrderByDescending(static display => display.IsPrimary)
				.ThenBy(static display => display.X)
				.ThenBy(static display => display.Y)
				.ToArray();

			return displays.Length == 0
				? ScreenshotDisplayCatalogResult.Failed("No shareable displays are currently available.")
				: ScreenshotDisplayCatalogResult.Success(displays);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return ScreenshotDisplayCatalogResult.Failed(ex.Message);
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
	private async Task<ScreenshotCaptureResult> CaptureDisplayByIdCoreAsync(
		uint displayId,
		CancellationToken cancellationToken)
	{
		try
		{
			var shareableContent = await _shareableContentProvider.GetShareableContentAsync(cancellationToken).ConfigureAwait(false);
			var display = SelectDisplay(shareableContent, displayId);
			if (display is null)
			{
				return ScreenshotCaptureResult.Failed("The selected display is no longer available.");
			}

			return await CaptureDisplayCoreAsync(
				display,
				"Captured the selected display.",
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

			var cropRect = ClampCropBounds(
				cropBounds.ToBottomLeftOrigin((int)sourceImage.Height),
				(int)sourceImage.Width,
				(int)sourceImage.Height);
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
	private async Task<ScreenshotCaptureResult> CaptureDisplayCoreAsync(
		SCDisplay display,
		string successMessage,
		CancellationToken cancellationToken)
	{
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
			successMessage,
			"The captured display frame could not be encoded as PNG.",
			cancellationToken).ConfigureAwait(false);
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

	private static ScreenshotDisplayCatalogResult MapDisplayCatalogFailure(ScreenshotCaptureResult failure) =>
		failure.Status switch
		{
			ScreenshotCaptureStatus.Unsupported => ScreenshotDisplayCatalogResult.Unsupported(failure.Message),
			ScreenshotCaptureStatus.PermissionDenied => ScreenshotDisplayCatalogResult.PermissionDenied(failure.Message),
			_ => ScreenshotDisplayCatalogResult.Failed(failure.Message)
		};

	[SupportedOSPlatform("macos14.0")]
	private static SCDisplay? SelectPrimaryDisplay(SCShareableContent shareableContent)
	{
		var displays = shareableContent.Displays ?? Array.Empty<SCDisplay>();
		if (displays.Length == 0)
		{
			return null;
		}

		var mainDisplayId = checked((uint)CGDisplay.MainDisplayID);
		return displays.FirstOrDefault(display => display.DisplayId == mainDisplayId) ?? displays[0];
	}

	[SupportedOSPlatform("macos14.0")]
	private static SCDisplay? SelectDisplay(SCShareableContent shareableContent, uint displayId) =>
		(shareableContent.Displays ?? Array.Empty<SCDisplay>())
			.FirstOrDefault(display => display.DisplayId == displayId);

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

	[SupportedOSPlatform("macos14.0")]
	private static ScreenshotDisplayTarget CreateDisplayTarget(SCDisplay display, uint mainDisplayId)
	{
		var frame = display.Frame;

		return new ScreenshotDisplayTarget(
			display.DisplayId,
			display.DisplayId == mainDisplayId,
			Math.Max(1, checked((int)display.Width)),
			Math.Max(1, checked((int)display.Height)),
			checked((int)Math.Round(frame.X)),
			checked((int)Math.Round(frame.Y)));
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

using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Services;

public sealed class UnsupportedScreenshotCaptureService : IScreenshotCaptureService
{
	public bool IsSupported => false;

	public Task<ScreenshotCaptureResult> CapturePrimaryDisplayAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Native screen capture has not been wired for this platform yet."));

	public Task<ScreenshotDisplayCatalogResult> GetAvailableDisplaysAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotDisplayCatalogResult.Unsupported(
				"Display capture has not been wired for this platform yet."));

	public Task<ScreenshotCaptureResult> CaptureDisplayAsync(
		uint displayId,
		CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Display capture has not been wired for this platform yet."));

	public Task<ScreenshotWindowCatalogResult> GetAvailableWindowsAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotWindowCatalogResult.Unsupported(
				"Window capture has not been wired for this platform yet."));

	public Task<ScreenshotCaptureResult> CaptureWindowAsync(
		uint windowId,
		CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Window capture has not been wired for this platform yet."));

	public Task<ScreenshotCaptureResult> CaptureInteractiveRegionAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Interactive region capture has not been wired for this platform yet."));

	public Task<ScreenshotCaptureResult> CropImageAsync(
		string sourceFilePath,
		ScreenshotCropBounds cropBounds,
		CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Image cropping has not been wired for this platform yet."));
}

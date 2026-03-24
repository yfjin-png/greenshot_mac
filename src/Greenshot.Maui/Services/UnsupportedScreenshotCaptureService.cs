using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Services;

public sealed class UnsupportedScreenshotCaptureService : IScreenshotCaptureService
{
	public bool IsSupported => false;

	public Task<ScreenshotCaptureResult> CapturePrimaryDisplayAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Native screen capture has not been wired for this platform yet."));

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

	public Task<ScreenshotCaptureResult> CropImageAsync(
		string sourceFilePath,
		ScreenshotCropBounds cropBounds,
		CancellationToken cancellationToken = default) =>
		Task.FromResult(
			ScreenshotCaptureResult.Unsupported(
				"Image cropping has not been wired for this platform yet."));
}

namespace Greenshot.Maui.Core.Services;

public interface IScreenshotCaptureService
{
	bool IsSupported { get; }

	Task<ScreenshotCaptureResult> CapturePrimaryDisplayAsync(CancellationToken cancellationToken = default);

	Task<ScreenshotDisplayCatalogResult> GetAvailableDisplaysAsync(CancellationToken cancellationToken = default);

	Task<ScreenshotCaptureResult> CaptureDisplayAsync(
		uint displayId,
		CancellationToken cancellationToken = default);

	Task<ScreenshotWindowCatalogResult> GetAvailableWindowsAsync(CancellationToken cancellationToken = default);

	Task<ScreenshotCaptureResult> CaptureWindowAsync(
		uint windowId,
		CancellationToken cancellationToken = default);

	Task<ScreenshotCaptureResult> CaptureInteractiveRegionAsync(CancellationToken cancellationToken = default);

	Task<ScreenshotCaptureResult> CropImageAsync(
		string sourceFilePath,
		ScreenshotCropBounds cropBounds,
		CancellationToken cancellationToken = default);
}

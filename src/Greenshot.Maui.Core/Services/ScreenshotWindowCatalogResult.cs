namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotWindowCatalogResult(
	ScreenshotCaptureStatus Status,
	IReadOnlyList<ScreenshotWindowTarget> Windows,
	string Message)
{
	public bool IsSuccessful => Status == ScreenshotCaptureStatus.Succeeded;

	public static ScreenshotWindowCatalogResult Success(IReadOnlyList<ScreenshotWindowTarget> windows) =>
		new(ScreenshotCaptureStatus.Succeeded, windows, string.Empty);

	public static ScreenshotWindowCatalogResult Unsupported(string message) =>
		new(ScreenshotCaptureStatus.Unsupported, [], message);

	public static ScreenshotWindowCatalogResult PermissionDenied(string message) =>
		new(ScreenshotCaptureStatus.PermissionDenied, [], message);

	public static ScreenshotWindowCatalogResult Failed(string message) =>
		new(ScreenshotCaptureStatus.Failed, [], message);
}

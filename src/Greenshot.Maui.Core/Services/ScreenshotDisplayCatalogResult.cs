namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotDisplayCatalogResult(
	ScreenshotCaptureStatus Status,
	IReadOnlyList<ScreenshotDisplayTarget> Displays,
	string Message)
{
	public bool IsSuccessful => Status == ScreenshotCaptureStatus.Succeeded;

	public static ScreenshotDisplayCatalogResult Success(IReadOnlyList<ScreenshotDisplayTarget> displays) =>
		new(ScreenshotCaptureStatus.Succeeded, displays, string.Empty);

	public static ScreenshotDisplayCatalogResult Unsupported(string message) =>
		new(ScreenshotCaptureStatus.Unsupported, [], message);

	public static ScreenshotDisplayCatalogResult PermissionDenied(string message) =>
		new(ScreenshotCaptureStatus.PermissionDenied, [], message);

	public static ScreenshotDisplayCatalogResult Failed(string message) =>
		new(ScreenshotCaptureStatus.Failed, [], message);
}

namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotCaptureResult(
	ScreenshotCaptureStatus Status,
	string? FilePath,
	string Message,
	int PixelWidth = 0,
	int PixelHeight = 0)
{
	public bool IsSuccessful => Status == ScreenshotCaptureStatus.Succeeded && !string.IsNullOrWhiteSpace(FilePath);

	public static ScreenshotCaptureResult Success(string filePath, string message, int pixelWidth = 0, int pixelHeight = 0) =>
		new(ScreenshotCaptureStatus.Succeeded, filePath, message, pixelWidth, pixelHeight);

	public static ScreenshotCaptureResult Cancelled(string message) =>
		new(ScreenshotCaptureStatus.Cancelled, null, message);

	public static ScreenshotCaptureResult Unsupported(string message) =>
		new(ScreenshotCaptureStatus.Unsupported, null, message);

	public static ScreenshotCaptureResult PermissionDenied(string message) =>
		new(ScreenshotCaptureStatus.PermissionDenied, null, message);

	public static ScreenshotCaptureResult Failed(string message) =>
		new(ScreenshotCaptureStatus.Failed, null, message);
}

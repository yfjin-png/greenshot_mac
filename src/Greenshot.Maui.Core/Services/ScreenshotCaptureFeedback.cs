namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotCaptureFeedback(
	string Title,
	string StatusLabel,
	string Message);

public static class ScreenshotCaptureFeedbackBuilder
{
	public static ScreenshotCaptureFeedback Build(
		ScreenshotCaptureStatus status,
		string message,
		string? permissionTroubleshootingHint = null)
	{
		var statusLabel = status switch
		{
			ScreenshotCaptureStatus.Cancelled => "Capture cancelled",
			ScreenshotCaptureStatus.PermissionDenied => "Permission needed",
			ScreenshotCaptureStatus.Unsupported => "Capture unavailable",
			_ => "Capture failed"
		};

		var composedMessage = status == ScreenshotCaptureStatus.PermissionDenied &&
			!string.IsNullOrWhiteSpace(permissionTroubleshootingHint)
				? $"{message}{Environment.NewLine}{Environment.NewLine}{permissionTroubleshootingHint}"
				: message;

		var title = status == ScreenshotCaptureStatus.Cancelled
			? "Capture cancelled"
			: "Capture failed";

		return new ScreenshotCaptureFeedback(title, statusLabel, composedMessage);
	}

	public static ScreenshotCaptureFeedback Build(
		ScreenshotCaptureResult result,
		string? permissionTroubleshootingHint = null)
		=> Build(result.Status, result.Message, permissionTroubleshootingHint);
}

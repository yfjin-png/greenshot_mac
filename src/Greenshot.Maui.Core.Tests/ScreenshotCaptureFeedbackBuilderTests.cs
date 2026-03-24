using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotCaptureFeedbackBuilderTests
{
	[Fact]
	public void Build_AppendsTroubleshootingHintForPermissionDenied()
	{
		var feedback = ScreenshotCaptureFeedbackBuilder.Build(
			ScreenshotCaptureResult.PermissionDenied("Screen recording permission is required."),
			"If this is a local debug build, use a stable signing identity.");

		Assert.Equal("Capture failed", feedback.Title);
		Assert.Equal("Permission needed", feedback.StatusLabel);
		Assert.Contains("Screen recording permission is required.", feedback.Message);
		Assert.Contains("stable signing identity", feedback.Message);
	}

	[Fact]
	public void Build_MapsUnsupportedToCaptureUnavailable()
	{
		var feedback = ScreenshotCaptureFeedbackBuilder.Build(
			ScreenshotCaptureResult.Unsupported("macOS 14 or newer is required."),
			"If this is a local debug build, use a stable signing identity.");

		Assert.Equal("Capture unavailable", feedback.StatusLabel);
		Assert.Equal("macOS 14 or newer is required.", feedback.Message);
	}

	[Fact]
	public void Build_LeavesGenericFailureMessageUntouched()
	{
		var feedback = ScreenshotCaptureFeedbackBuilder.Build(
			ScreenshotCaptureResult.Failed("Something went wrong."),
			"Unused hint");

		Assert.Equal("Capture failed", feedback.StatusLabel);
		Assert.Equal("Something went wrong.", feedback.Message);
	}
}

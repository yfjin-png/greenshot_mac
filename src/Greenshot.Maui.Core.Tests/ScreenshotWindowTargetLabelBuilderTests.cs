using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotWindowTargetLabelBuilderTests
{
	[Fact]
	public void Build_UsesTitleAppAndSizeForStandardWindows()
	{
		var windows = new[]
		{
			new ScreenshotWindowTarget(17, "Release notes", "Safari", 1440, 900)
		};

		var labels = ScreenshotWindowTargetLabelBuilder.Build(windows);

		Assert.Equal("Release notes - Safari - 1440x900", labels[17]);
	}

	[Fact]
	public void Build_FallsBackToApplicationNameWhenWindowTitleIsMissing()
	{
		var windows = new[]
		{
			new ScreenshotWindowTarget(21, string.Empty, "Preview", 1280, 720)
		};

		var labels = ScreenshotWindowTargetLabelBuilder.Build(windows);

		Assert.Equal("Preview - 1280x720", labels[21]);
	}

	[Fact]
	public void Build_AppendsWindowIdWhenLabelsWouldOtherwiseCollide()
	{
		var windows = new[]
		{
			new ScreenshotWindowTarget(30, "Inbox", "Mail", 1200, 800),
			new ScreenshotWindowTarget(31, "Inbox", "Mail", 1200, 800)
		};

		var labels = ScreenshotWindowTargetLabelBuilder.Build(windows);

		Assert.Equal("Inbox - Mail - 1200x800 [#30]", labels[30]);
		Assert.Equal("Inbox - Mail - 1200x800 [#31]", labels[31]);
	}
}

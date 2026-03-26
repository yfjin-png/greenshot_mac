using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotDisplayTargetLabelBuilderTests
{
	[Fact]
	public void Build_UsesOrdinalMainLabelSizeAndPosition()
	{
		var displays = new[]
		{
			new ScreenshotDisplayTarget(101, true, 3024, 1964, 0, 0)
		};

		var labels = ScreenshotDisplayTargetLabelBuilder.Build(displays);

		Assert.Equal("Display 1 (Main) - 3024x1964 - @ 0,0", labels[101]);
	}

	[Fact]
	public void Build_UsesOrdinalForSecondaryDisplays()
	{
		var displays = new[]
		{
			new ScreenshotDisplayTarget(101, true, 3024, 1964, 0, 0),
			new ScreenshotDisplayTarget(102, false, 2560, 1440, 3024, 0)
		};

		var labels = ScreenshotDisplayTargetLabelBuilder.Build(displays);

		Assert.Equal("Display 2 - 2560x1440 - @ 3024,0", labels[102]);
	}

	[Fact]
	public void Build_ReturnsEmptyDictionaryWhenNoDisplaysExist()
	{
		var labels = ScreenshotDisplayTargetLabelBuilder.Build(Array.Empty<ScreenshotDisplayTarget>());

		Assert.Empty(labels);
	}
}

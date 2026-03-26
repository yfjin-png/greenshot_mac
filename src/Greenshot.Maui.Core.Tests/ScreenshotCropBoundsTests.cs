using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotCropBoundsTests
{
	[Fact]
	public void ToBottomLeftOrigin_FlipsVerticalOriginForCoreGraphicsCropping()
	{
		var bounds = new ScreenshotCropBounds(200, 100, 800, 400);

		var converted = bounds.ToBottomLeftOrigin(800);

		Assert.Equal(new ScreenshotCropBounds(200, 300, 800, 400), converted);
	}

	[Fact]
	public void ToBottomLeftOrigin_ThrowsWhenImageHeightIsNegative()
	{
		var bounds = new ScreenshotCropBounds(0, 0, 100, 100);

		Assert.Throws<ArgumentOutOfRangeException>(() => bounds.ToBottomLeftOrigin(-1));
	}
}

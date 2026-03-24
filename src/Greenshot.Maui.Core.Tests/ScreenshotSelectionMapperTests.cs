using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Core.Tests;

public class ScreenshotSelectionMapperTests
{
	[Fact]
	public void GetDisplayedImageRect_CentersImageWhenPreviewIsLetterboxed()
	{
		var imageRect = ScreenshotSelectionMapper.GetDisplayedImageRect(
			stageWidth: 800d,
			stageHeight: 600d,
			imagePixelWidth: 1600,
			imagePixelHeight: 800);

		Assert.Equal(0d, imageRect.X, 6);
		Assert.Equal(100d, imageRect.Y, 6);
		Assert.Equal(800d, imageRect.Width, 6);
		Assert.Equal(400d, imageRect.Height, 6);
	}

	[Fact]
	public void TryProjectSelection_MapsSelectionIntoPixelCropBounds()
	{
		var projected = ScreenshotSelectionMapper.TryProjectSelection(
			new SelectionCanvasPoint(100d, 150d),
			new SelectionCanvasPoint(500d, 350d),
			stageWidth: 800d,
			stageHeight: 600d,
			imagePixelWidth: 1600,
			imagePixelHeight: 800,
			minimumSelectionDisplaySize: 8d,
			out var projection);

		Assert.True(projected);
		Assert.Equal(100d, projection.SelectionDisplayRect.X, 6);
		Assert.Equal(150d, projection.SelectionDisplayRect.Y, 6);
		Assert.Equal(400d, projection.SelectionDisplayRect.Width, 6);
		Assert.Equal(200d, projection.SelectionDisplayRect.Height, 6);
		Assert.Equal(new ScreenshotCropBounds(200, 100, 800, 400), projection.CropBounds);
	}

	[Fact]
	public void TryProjectSelection_ClampsSelectionThatStartsOutsideVisibleImage()
	{
		var projected = ScreenshotSelectionMapper.TryProjectSelection(
			new SelectionCanvasPoint(-50d, 200d),
			new SelectionCanvasPoint(700d, 700d),
			stageWidth: 600d,
			stageHeight: 900d,
			imagePixelWidth: 1200,
			imagePixelHeight: 600,
			minimumSelectionDisplaySize: 8d,
			out var projection);

		Assert.True(projected);
		Assert.Equal(new ScreenshotCropBounds(0, 0, 1200, 600), projection.CropBounds);
		Assert.Equal(0d, projection.SelectionDisplayRect.X, 6);
		Assert.Equal(300d, projection.SelectionDisplayRect.Y, 6);
		Assert.Equal(600d, projection.SelectionDisplayRect.Width, 6);
		Assert.Equal(300d, projection.SelectionDisplayRect.Height, 6);
	}

	[Fact]
	public void TryProjectSelection_RejectsSelectionsSmallerThanMinimumDisplaySize()
	{
		var projected = ScreenshotSelectionMapper.TryProjectSelection(
			new SelectionCanvasPoint(100d, 150d),
			new SelectionCanvasPoint(106d, 190d),
			stageWidth: 800d,
			stageHeight: 600d,
			imagePixelWidth: 1600,
			imagePixelHeight: 800,
			minimumSelectionDisplaySize: 8d,
			out _);

		Assert.False(projected);
	}
}

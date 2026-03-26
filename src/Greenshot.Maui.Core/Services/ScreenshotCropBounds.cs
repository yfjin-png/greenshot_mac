namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotCropBounds(
	int X,
	int Y,
	int Width,
	int Height)
{
	public bool IsEmpty => Width <= 0 || Height <= 0;

	public ScreenshotCropBounds ToBottomLeftOrigin(int imageHeight)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(imageHeight);
		return new ScreenshotCropBounds(X, imageHeight - Y - Height, Width, Height);
	}
}

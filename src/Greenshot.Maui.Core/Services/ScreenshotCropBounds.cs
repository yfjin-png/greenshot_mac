namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotCropBounds(
	int X,
	int Y,
	int Width,
	int Height)
{
	public bool IsEmpty => Width <= 0 || Height <= 0;
}

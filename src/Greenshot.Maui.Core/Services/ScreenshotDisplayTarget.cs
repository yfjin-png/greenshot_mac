namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotDisplayTarget(
	uint DisplayId,
	bool IsPrimary,
	int Width,
	int Height,
	int X,
	int Y);

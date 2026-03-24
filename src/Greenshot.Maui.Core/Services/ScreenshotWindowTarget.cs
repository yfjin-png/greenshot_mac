namespace Greenshot.Maui.Core.Services;

public sealed record ScreenshotWindowTarget(
	uint WindowId,
	string Title,
	string ApplicationName,
	int Width,
	int Height);

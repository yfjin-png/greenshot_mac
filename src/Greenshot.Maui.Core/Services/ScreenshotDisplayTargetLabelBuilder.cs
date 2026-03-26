namespace Greenshot.Maui.Core.Services;

public static class ScreenshotDisplayTargetLabelBuilder
{
	public static IReadOnlyDictionary<uint, string> Build(IReadOnlyList<ScreenshotDisplayTarget> displays)
	{
		if (displays.Count == 0)
		{
			return new Dictionary<uint, string>();
		}

		return displays
			.Select(static (display, index) => new KeyValuePair<uint, string>(display.DisplayId, CreateBaseLabel(display, index + 1)))
			.ToDictionary(static pair => pair.Key, static pair => pair.Value);
	}

	private static string CreateBaseLabel(ScreenshotDisplayTarget display, int ordinal)
	{
		var name = display.IsPrimary ? $"Display {ordinal} (Main)" : $"Display {ordinal}";
		var sizeText = display.Width > 0 && display.Height > 0
			? $"{display.Width}x{display.Height}"
			: null;
		var positionText = $"@ {display.X},{display.Y}";

		return string.Join(
			" - ",
			new[] { name, sizeText, positionText }.Where(static part => !string.IsNullOrWhiteSpace(part)));
	}
}

namespace Greenshot.Maui.Core.Services;

public static class ScreenshotWindowTargetLabelBuilder
{
	public static IReadOnlyDictionary<uint, string> Build(IReadOnlyList<ScreenshotWindowTarget> windows)
	{
		if (windows.Count == 0)
		{
			return new Dictionary<uint, string>();
		}

		var rawLabels = windows.ToDictionary(static window => window.WindowId, CreateBaseLabel);
		var duplicateGroups = rawLabels
			.GroupBy(static pair => pair.Value, StringComparer.Ordinal)
			.Where(static group => group.Count() > 1)
			.ToDictionary(static group => group.Key, static group => group.Select(static pair => pair.Key).ToHashSet());

		var labels = new Dictionary<uint, string>(windows.Count);
		foreach (var window in windows)
		{
			var label = rawLabels[window.WindowId];
			if (duplicateGroups.ContainsKey(label))
			{
				label = $"{label} [#{window.WindowId}]";
			}

			labels[window.WindowId] = label;
		}

		return labels;
	}

	private static string CreateBaseLabel(ScreenshotWindowTarget window)
	{
		var primaryText = string.IsNullOrWhiteSpace(window.Title)
			? window.ApplicationName
			: window.Title;
		var secondaryText = string.IsNullOrWhiteSpace(window.Title) || string.Equals(window.Title, window.ApplicationName, StringComparison.Ordinal)
			? null
			: window.ApplicationName;
		var sizeText = window.Width > 0 && window.Height > 0
			? $"{window.Width}x{window.Height}"
			: null;

		return string.Join(
			" - ",
			new[] { primaryText, secondaryText, sizeText }.Where(static part => !string.IsNullOrWhiteSpace(part)));
	}
}

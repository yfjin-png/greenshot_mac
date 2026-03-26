namespace Greenshot.Maui.Services;

public sealed class CaptureDefaultsService : ICaptureDefaultsService
{
	private const string SaveCapturesToLocalKey = "capture.save_to_local";
	private const string LocalCaptureSaveDirectoryKey = "capture.local_save_directory";
	private const string LocalCaptureSaveDirectoryBookmarkKey = "capture.local_save_directory_bookmark";
	private const string CopyCapturesToClipboardKey = "capture.copy_to_clipboard";

	public bool SaveCapturesToLocal
	{
		get => Preferences.Default.Get(SaveCapturesToLocalKey, true);
		set => Preferences.Default.Set(SaveCapturesToLocalKey, value);
	}

	public string LocalCaptureSaveDirectory
	{
		get => NormalizeDirectoryPath(Preferences.Default.Get(LocalCaptureSaveDirectoryKey, DefaultLocalCaptureSaveDirectory));
		set => Preferences.Default.Set(LocalCaptureSaveDirectoryKey, NormalizeDirectoryPath(value));
	}

	public string? LocalCaptureSaveDirectoryBookmark
	{
		get
		{
			var bookmark = Preferences.Default.Get(LocalCaptureSaveDirectoryBookmarkKey, string.Empty);
			return string.IsNullOrWhiteSpace(bookmark) ? null : bookmark;
		}
		set => Preferences.Default.Set(LocalCaptureSaveDirectoryBookmarkKey, value ?? string.Empty);
	}

	public bool CopyCapturesToClipboard
	{
		get => Preferences.Default.Get(CopyCapturesToClipboardKey, false);
		set => Preferences.Default.Set(CopyCapturesToClipboardKey, value);
	}

	public string DefaultLocalCaptureSaveDirectory =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
			"Greenshot");

	public string NormalizeDirectoryPath(string? path)
	{
		var trimmed = string.IsNullOrWhiteSpace(path)
			? DefaultLocalCaptureSaveDirectory
			: path.Trim();

		if (trimmed.StartsWith("~/", StringComparison.Ordinal))
		{
			trimmed = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				trimmed[2..]);
		}
		else if (string.Equals(trimmed, "~", StringComparison.Ordinal))
		{
			trimmed = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		return Path.GetFullPath(trimmed);
	}
}

namespace Greenshot.Maui.Services;

public interface ICaptureDefaultsService
{
	bool SaveCapturesToLocal { get; set; }

	string LocalCaptureSaveDirectory { get; set; }

	string? LocalCaptureSaveDirectoryBookmark { get; set; }

	bool CopyCapturesToClipboard { get; set; }

	string DefaultLocalCaptureSaveDirectory { get; }

	string NormalizeDirectoryPath(string? path);
}

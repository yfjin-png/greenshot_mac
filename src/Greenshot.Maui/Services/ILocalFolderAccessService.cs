namespace Greenshot.Maui.Services;

public sealed record LocalFolderSelection(string DirectoryPath, string? Bookmark);

public interface ILocalFolderAccessService
{
	bool IsSupported { get; }

	Task<LocalFolderSelection?> PickFolderAsync(string? initialDirectory = null);

	IDisposable? BeginWriteAccess(string directoryPath, string? bookmark);
}

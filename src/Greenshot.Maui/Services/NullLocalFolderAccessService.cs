namespace Greenshot.Maui.Services;

internal sealed class NullLocalFolderAccessService : ILocalFolderAccessService
{
	public bool IsSupported => false;

	public Task<LocalFolderSelection?> PickFolderAsync(string? initialDirectory = null)
		=> Task.FromResult<LocalFolderSelection?>(null);

	public IDisposable? BeginWriteAccess(string directoryPath, string? bookmark)
		=> null;
}

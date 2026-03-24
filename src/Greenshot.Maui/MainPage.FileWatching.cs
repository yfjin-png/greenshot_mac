using System.IO;

namespace Greenshot.Maui;

public partial class MainPage
{
	private FileSystemWatcher? _selectedImageWatcher;
	private string? _loadedPreviewImagePath;

	private void ConfigureSelectedImageWatcher(string? fullPath)
	{
		DisposeSelectedImageWatcher();

		if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
		{
			return;
		}

		var directoryPath = Path.GetDirectoryName(fullPath);
		var fileName = Path.GetFileName(fullPath);
		if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
		{
			return;
		}

		_selectedImageWatcher = new FileSystemWatcher(directoryPath, fileName)
		{
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
		};
		_selectedImageWatcher.Changed += OnSelectedImageFileChanged;
		_selectedImageWatcher.Created += OnSelectedImageFileChanged;
		_selectedImageWatcher.Renamed += OnSelectedImageFileChanged;
		_selectedImageWatcher.EnableRaisingEvents = true;
	}

	private void DisposeSelectedImageWatcher()
	{
		if (_selectedImageWatcher is null)
		{
			return;
		}

		_selectedImageWatcher.EnableRaisingEvents = false;
		_selectedImageWatcher.Changed -= OnSelectedImageFileChanged;
		_selectedImageWatcher.Created -= OnSelectedImageFileChanged;
		_selectedImageWatcher.Renamed -= OnSelectedImageFileChanged;
		_selectedImageWatcher.Dispose();
		_selectedImageWatcher = null;
	}

	private void OnSelectedImageFileChanged(object? sender, FileSystemEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath) || !File.Exists(_workspaceSession.SelectedImagePath))
		{
			return;
		}

		Dispatcher.DispatchDelayed(
			TimeSpan.FromMilliseconds(200),
			() => ReloadPreviewImage(forceReload: true));
	}

	private void ReloadPreviewImage(bool forceReload = false)
	{
		if (string.IsNullOrWhiteSpace(_workspaceSession.SelectedImagePath) || !File.Exists(_workspaceSession.SelectedImagePath))
		{
			_loadedPreviewImagePath = null;
			return;
		}

		if (!forceReload && string.Equals(_loadedPreviewImagePath, _workspaceSession.SelectedImagePath, StringComparison.Ordinal))
		{
			return;
		}

		PreviewImage.Source = null;
		PreviewImage.Source = ImageSource.FromFile(_workspaceSession.SelectedImagePath);
		_loadedPreviewImagePath = _workspaceSession.SelectedImagePath;
	}
}

#if MACCATALYST
#pragma warning disable CA1416
using Foundation;
using Greenshot.Maui.Services;
using UniformTypeIdentifiers;
using UIKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

internal sealed class MacCatalystLocalFolderAccessService : ILocalFolderAccessService
{
	private readonly List<NSObject> _retainedDelegates = [];

	public bool IsSupported => true;

	public Task<LocalFolderSelection?> PickFolderAsync(string? initialDirectory = null)
	{
		var taskSource = new TaskCompletionSource<LocalFolderSelection?>();

		MainThread.BeginInvokeOnMainThread(() =>
		{
			try
			{
				PresentFolderPicker(initialDirectory, taskSource);
			}
			catch (Exception ex)
			{
				taskSource.TrySetException(ex);
			}
		});

		return taskSource.Task;
	}

	public IDisposable? BeginWriteAccess(string directoryPath, string? bookmark)
	{
		if (string.IsNullOrWhiteSpace(bookmark))
		{
			return null;
		}

		using var bookmarkData = NSData.FromArray(Convert.FromBase64String(bookmark));
		var resolvedUrl = NSUrl.FromBookmarkData(
			bookmarkData,
			NSUrlBookmarkResolutionOptions.WithSecurityScope,
			null,
			out _,
			out var error);

		if (error is not null)
		{
			throw new InvalidOperationException(error.LocalizedDescription);
		}

		if (resolvedUrl is null)
		{
			throw new InvalidOperationException("The saved folder permission could not be resolved.");
		}

		var resolvedPath = NormalizeDirectoryPath(resolvedUrl.Path);
		var expectedPath = NormalizeDirectoryPath(directoryPath);
		if (!string.Equals(resolvedPath, expectedPath, StringComparison.Ordinal))
		{
			resolvedUrl.Dispose();
			throw new InvalidOperationException("The selected folder changed. Choose Folder again in Settings.");
		}

		if (!resolvedUrl.StartAccessingSecurityScopedResource())
		{
			resolvedUrl.Dispose();
			throw new InvalidOperationException("The app could not access the selected folder.");
		}

		return new SecurityScopedFolderAccess(resolvedUrl);
	}

	private void PresentFolderPicker(string? initialDirectory, TaskCompletionSource<LocalFolderSelection?> taskSource)
	{
		var presenter = GetPresenter();
		if (presenter is null)
		{
			throw new InvalidOperationException("The folder picker could not be shown.");
		}

		var picker = new UIDocumentPickerViewController(new[] { UTTypes.Folder }, false)
		{
			AllowsMultipleSelection = false
		};

		if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
		{
			picker.DirectoryUrl = NSUrl.CreateFileUrl(initialDirectory, true);
		}

		FolderPickerDelegate? pickerDelegate = null;
		pickerDelegate = new FolderPickerDelegate(
			onPicked: selection => CompleteFolderPicker(picker, pickerDelegate!, taskSource, selection),
			onCancelled: () => CompleteFolderPickerCancelled(picker, pickerDelegate!, taskSource),
			onFailed: exception => CompleteFolderPicker(picker, pickerDelegate!, taskSource, exception));

		_retainedDelegates.Add(pickerDelegate);
		picker.Delegate = pickerDelegate;
		presenter.PresentViewController(picker, true, null);
	}

	private void CompleteFolderPicker(
		UIDocumentPickerViewController picker,
		FolderPickerDelegate pickerDelegate,
		TaskCompletionSource<LocalFolderSelection?> taskSource,
		LocalFolderSelection? selection)
	{
		picker.DismissViewController(true, null);
		_retainedDelegates.Remove(pickerDelegate);
		pickerDelegate.Dispose();
		taskSource.TrySetResult(selection);
	}

	private void CompleteFolderPickerCancelled(
		UIDocumentPickerViewController picker,
		FolderPickerDelegate pickerDelegate,
		TaskCompletionSource<LocalFolderSelection?> taskSource)
	{
		picker.DismissViewController(true, null);
		_retainedDelegates.Remove(pickerDelegate);
		pickerDelegate.Dispose();
		taskSource.TrySetResult(null);
	}

	private void CompleteFolderPicker(
		UIDocumentPickerViewController picker,
		FolderPickerDelegate pickerDelegate,
		TaskCompletionSource<LocalFolderSelection?> taskSource,
		Exception exception)
	{
		picker.DismissViewController(true, null);
		_retainedDelegates.Remove(pickerDelegate);
		pickerDelegate.Dispose();
		taskSource.TrySetException(exception);
	}

	private static UIViewController? GetPresenter()
	{
		var windowScene = UIApplication.SharedApplication.ConnectedScenes
			.OfType<UIWindowScene>()
			.FirstOrDefault(scene => scene.ActivationState == UISceneActivationState.ForegroundActive)
			?? UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().FirstOrDefault();

		var window = windowScene?.Windows.FirstOrDefault(candidate => candidate.IsKeyWindow)
			?? windowScene?.Windows.FirstOrDefault();

		var presenter = window?.RootViewController;
		while (presenter?.PresentedViewController is not null)
		{
			presenter = presenter.PresentedViewController;
		}

		return presenter;
	}

	private static string NormalizeDirectoryPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new InvalidOperationException("The selected folder path was empty.");
		}

		return Path.GetFullPath(path);
	}

	private sealed class SecurityScopedFolderAccess(NSUrl folderUrl) : IDisposable
	{
		public void Dispose()
		{
			folderUrl.StopAccessingSecurityScopedResource();
			folderUrl.Dispose();
		}
	}

	private sealed class FolderPickerDelegate(
		Action<LocalFolderSelection> onPicked,
		Action onCancelled,
		Action<Exception> onFailed) : UIDocumentPickerDelegate
	{
		public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
		{
			var selectedUrl = urls.FirstOrDefault();
			if (selectedUrl is null)
			{
				onCancelled();
				return;
			}

			try
			{
				if (!selectedUrl.StartAccessingSecurityScopedResource())
				{
					throw new InvalidOperationException("The selected folder could not be opened.");
				}

				try
				{
					using var bookmarkData = selectedUrl.CreateBookmarkData(
						NSUrlBookmarkCreationOptions.WithSecurityScope,
						null,
						null,
						out var error);

					if (error is not null)
					{
						throw new InvalidOperationException(error.LocalizedDescription);
					}

					if (bookmarkData is null)
					{
						throw new InvalidOperationException("The selected folder permission could not be saved.");
					}

					onPicked(new LocalFolderSelection(
						NormalizeDirectoryPath(selectedUrl.Path),
						Convert.ToBase64String(bookmarkData.ToArray())));
				}
				finally
				{
					selectedUrl.StopAccessingSecurityScopedResource();
				}
			}
			catch (Exception ex)
			{
				onFailed(ex);
			}
		}

		public override void WasCancelled(UIDocumentPickerViewController controller)
			=> onCancelled();
	}
}
#endif

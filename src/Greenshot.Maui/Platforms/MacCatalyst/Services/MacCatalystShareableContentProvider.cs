using System.Runtime.Versioning;
using CoreGraphics;
using ScreenCaptureKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

[SupportedOSPlatform("maccatalyst")]
internal sealed class MacCatalystShareableContentProvider
{
	[SupportedOSPlatform("macos14.0")]
	public Task<SCShareableContent> GetShareableContentAsync(CancellationToken cancellationToken) =>
		CreateShareableContentTask(cancellationToken);

	[SupportedOSPlatform("macos14.0")]
	public Task<SCShareableContent> GetWindowShareableContentAsync(CancellationToken cancellationToken) =>
		CreateShareableContentTask(
			excludeDesktopWindows: true,
			onScreenWindowsOnly: true,
			cancellationToken);

	[SupportedOSPlatform("macos14.0")]
	public Task<CGImage?> CaptureImageAsync(
		SCContentFilter contentFilter,
		SCStreamConfiguration configuration,
		CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<CGImage?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
		_ = tcs.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		SCScreenshotManager.CaptureImage(contentFilter, configuration, (image, error) =>
		{
			if (error is not null)
			{
				tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
				return;
			}

			tcs.TrySetResult(image);
		});

		return tcs.Task;
	}

	[SupportedOSPlatform("macos14.0")]
	private static Task<SCShareableContent> CreateShareableContentTask(CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<SCShareableContent>(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
		_ = tcs.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		SCShareableContent.GetShareableContent((content, error) =>
		{
			if (error is not null)
			{
				tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
				return;
			}

			if (content is null)
			{
				tcs.TrySetException(new InvalidOperationException("ScreenCaptureKit did not return any shareable content."));
				return;
			}

			tcs.TrySetResult(content);
		});

		return tcs.Task;
	}

	[SupportedOSPlatform("macos14.0")]
	private static Task<SCShareableContent> CreateShareableContentTask(
		bool excludeDesktopWindows,
		bool onScreenWindowsOnly,
		CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<SCShareableContent>(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
		_ = tcs.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		SCShareableContent.GetShareableContent(excludeDesktopWindows, onScreenWindowsOnly, (content, error) =>
		{
			if (error is not null)
			{
				tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
				return;
			}

			if (content is null)
			{
				tcs.TrySetException(new InvalidOperationException("ScreenCaptureKit did not return any shareable content."));
				return;
			}

			tcs.TrySetResult(content);
		});

		return tcs.Task;
	}
}

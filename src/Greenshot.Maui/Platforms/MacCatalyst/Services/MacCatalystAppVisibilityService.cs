#if MACCATALYST
using Greenshot.Maui.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

internal sealed class MacCatalystAppVisibilityService : IAppVisibilityService
{
	private const double HiddenWindowX = -20000;
	private const double HiddenWindowY = -20000;
	private const double HiddenWindowWidth = 1;
	private const double HiddenWindowHeight = 1;
	private const double VisibleWindowX = 160;
	private const double VisibleWindowY = 120;
	private const double VisibleWindowWidth = 1320;
	private const double VisibleWindowHeight = 900;

	private Window? _trackedWindow;
	private UIWindow? _platformWindow;
	private bool _initialLaunchApplied;
	private CancellationTokenSource? _pendingInitialHideCancellation;

	public bool IsVisible => _platformWindow is null || !_platformWindow.Hidden;

	public void RegisterWindow(Window window)
	{
		_trackedWindow = window;
		_trackedWindow.HandlerChanged += OnTrackedWindowHandlerChanged;
		PrepareWindowForHiddenLaunch(window);
		TryCachePlatformWindow(window);
	}

	public void HideOnInitialLaunch()
	{
		if (_initialLaunchApplied)
		{
			return;
		}

		_initialLaunchApplied = true;
		_pendingInitialHideCancellation = new CancellationTokenSource();
		_ = HideApplicationUntilStableAsync(_pendingInitialHideCancellation.Token);
	}

	public void Hide()
	{
		_pendingInitialHideCancellation?.Cancel();
		_pendingInitialHideCancellation = null;
		MainThread.BeginInvokeOnMainThread(HideApplicationCore);
	}

	public void Show()
	{
		_pendingInitialHideCancellation?.Cancel();
		_pendingInitialHideCancellation = null;
		MainThread.BeginInvokeOnMainThread(ShowApplicationCore);
	}

	public void Quit()
	{
		_pendingInitialHideCancellation?.Cancel();
		_pendingInitialHideCancellation = null;
		MainThread.BeginInvokeOnMainThread(TerminateCurrentApplication);
	}

	private void OnTrackedWindowHandlerChanged(object? sender, EventArgs e)
	{
		if (sender is not Window window)
		{
			return;
		}

		TryCachePlatformWindow(window);
		if (_initialLaunchApplied)
		{
			HideTrackedWindowCore();
		}
	}

	private async Task HideApplicationUntilStableAsync(CancellationToken cancellationToken)
	{
		await Task.Yield();

		for (var attempt = 0; attempt < 30; attempt++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await MainThread.InvokeOnMainThreadAsync(HideApplicationCore);
			await Task.Delay(100, cancellationToken);
		}
	}

	private static void PrepareWindowForHiddenLaunch(Window window)
	{
		window.X = HiddenWindowX;
		window.Y = HiddenWindowY;
		window.Width = HiddenWindowWidth;
		window.Height = HiddenWindowHeight;
	}

	private void TryCachePlatformWindow(Window window)
	{
		if (window.Handler is not IElementHandler elementHandler)
		{
			return;
		}

		_platformWindow = elementHandler.PlatformView as UIWindow;
	}

	private void HideTrackedWindowCore()
	{
		if (_trackedWindow is null)
		{
			return;
		}

		_trackedWindow.X = HiddenWindowX;
		_trackedWindow.Y = HiddenWindowY;
		_trackedWindow.Width = HiddenWindowWidth;
		_trackedWindow.Height = HiddenWindowHeight;
		if (_platformWindow is not null)
		{
			_platformWindow.Hidden = true;
		}
	}

	private void HideApplicationCore()
	{
		HideTrackedWindowCore();
		HideCurrentApplication();
	}

	private void ShowApplicationCore()
	{
		if (_trackedWindow is null)
		{
			return;
		}

		UnhideCurrentApplication();
		_trackedWindow.Width = VisibleWindowWidth;
		_trackedWindow.Height = VisibleWindowHeight;
		_trackedWindow.X = VisibleWindowX;
		_trackedWindow.Y = VisibleWindowY;
		if (_platformWindow is not null)
		{
			_platformWindow.Hidden = false;
			_platformWindow.MakeKeyAndVisible();
		}

		Application.Current?.ActivateWindow(_trackedWindow);
	}

	private static void HideCurrentApplication()
	{
		var sharedApplicationHandle = GetSharedApplicationHandle();
		if (sharedApplicationHandle == NativeHandle.Zero)
		{
			return;
		}

		var hideSelector = Selector.GetHandle("hide:");
		Objc.void_objc_msgSend_NativeHandle(sharedApplicationHandle, hideSelector, NativeHandle.Zero);
	}

	private static void UnhideCurrentApplication()
	{
		var sharedApplicationHandle = GetSharedApplicationHandle();
		if (sharedApplicationHandle == NativeHandle.Zero)
		{
			return;
		}

		var unhideWithoutActivationSelector = Selector.GetHandle("unhideWithoutActivation");
		Objc.void_objc_msgSend(sharedApplicationHandle, unhideWithoutActivationSelector);

		var unhideSelector = Selector.GetHandle("unhide:");
		Objc.void_objc_msgSend_NativeHandle(sharedApplicationHandle, unhideSelector, NativeHandle.Zero);

		var activateSelector = Selector.GetHandle("activateIgnoringOtherApps:");
		Objc.void_objc_msgSend_Byte(sharedApplicationHandle, activateSelector, 1);
		ActivateCurrentRunningApplication();
	}

	private static NativeHandle GetSharedApplicationHandle()
	{
		var applicationClass = Class.GetHandle("NSApplication");
		if (applicationClass == NativeHandle.Zero)
		{
			return NativeHandle.Zero;
		}

		var sharedApplicationSelector = Selector.GetHandle("sharedApplication");
		return Objc.NativeHandle_objc_msgSend(applicationClass, sharedApplicationSelector);
	}

	private static void ActivateCurrentRunningApplication()
	{
		var runningApplicationClass = Class.GetHandle("NSRunningApplication");
		if (runningApplicationClass == NativeHandle.Zero)
		{
			return;
		}

		var currentApplicationSelector = Selector.GetHandle("currentApplication");
		var runningApplicationHandle = Objc.NativeHandle_objc_msgSend(runningApplicationClass, currentApplicationSelector);
		if (runningApplicationHandle == NativeHandle.Zero)
		{
			return;
		}

		const ulong activationOptions = 3;
		var activateWithOptionsSelector = Selector.GetHandle("activateWithOptions:");
		Objc.byte_objc_msgSend_UInt64(runningApplicationHandle, activateWithOptionsSelector, activationOptions);
	}

	private static void TerminateCurrentApplication()
	{
		var sharedApplicationHandle = GetSharedApplicationHandle();
		if (sharedApplicationHandle == NativeHandle.Zero)
		{
			return;
		}

		var terminateSelector = Selector.GetHandle("terminate:");
		Objc.void_objc_msgSend_NativeHandle(sharedApplicationHandle, terminateSelector, sharedApplicationHandle);
	}

	private static class Objc
	{
		private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern NativeHandle NativeHandle_objc_msgSend(NativeHandle receiver, NativeHandle selector);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern void void_objc_msgSend(NativeHandle receiver, NativeHandle selector);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern void void_objc_msgSend_NativeHandle(NativeHandle receiver, NativeHandle selector, NativeHandle arg1);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern void void_objc_msgSend_Byte(NativeHandle receiver, NativeHandle selector, byte arg1);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern byte byte_objc_msgSend_UInt64(NativeHandle receiver, NativeHandle selector, ulong arg1);
	}
}
#endif

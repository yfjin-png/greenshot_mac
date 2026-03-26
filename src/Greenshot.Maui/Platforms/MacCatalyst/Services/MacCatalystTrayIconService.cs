#if MACCATALYST
using Foundation;
using Greenshot.Maui.Services;
using ObjCRuntime;
using System.IO;
using System.Runtime.InteropServices;
using UIKit;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

internal sealed class MacCatalystTrayIconService : ITrayIconService
{
	private const double VariableStatusItemLength = -1d;

	private readonly MainPage _mainPage;
	private readonly List<NSObject> _retainedTargets = [];
	private NativeHandle _menuHandle;
	private NativeHandle _statusItemHandle;
	private bool _isInitialized;

	public MacCatalystTrayIconService(MainPage mainPage)
	{
		_mainPage = mainPage;
		_mainPage.WorkspaceMenuStateChanged += OnWorkspaceMenuStateChanged;
	}

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(EnsureStatusItemCreated);
	}

	private void EnsureStatusItemCreated()
	{
		if (_isInitialized)
		{
			return;
		}

		var statusBarClass = Class.GetHandle("NSStatusBar");
		if (statusBarClass == NativeHandle.Zero)
		{
			return;
		}

		var systemStatusBarSelector = Selector.GetHandle("systemStatusBar");
		var statusBarHandle = Objc.NativeHandle_objc_msgSend(statusBarClass, systemStatusBarSelector);
		if (statusBarHandle == NativeHandle.Zero)
		{
			return;
		}

		var statusItemWithLengthSelector = Selector.GetHandle("statusItemWithLength:");
		_statusItemHandle = Objc.NativeHandle_objc_msgSend_Double(statusBarHandle, statusItemWithLengthSelector, VariableStatusItemLength);
		if (_statusItemHandle == NativeHandle.Zero)
		{
			return;
		}

		_statusItemHandle = Objc.NativeHandle_objc_retain(_statusItemHandle);

		ConfigureStatusButton(_statusItemHandle);
		RebuildMenu();
		_isInitialized = true;
	}

	private void ConfigureStatusButton(NativeHandle statusItemHandle)
	{
		var buttonSelector = Selector.GetHandle("button");
		var buttonHandle = Objc.NativeHandle_objc_msgSend(statusItemHandle, buttonSelector);
		if (buttonHandle == NativeHandle.Zero)
		{
			return;
		}

		var statusImage = LoadStatusButtonImage();
		if (statusImage is not null)
		{
			var setImageSelector = Selector.GetHandle("setImage:");
			Objc.void_objc_msgSend_NativeHandle(buttonHandle, setImageSelector, statusImage.Handle);
		}
		else
		{
			using var title = new NSString("GS");
			var setTitleSelector = Selector.GetHandle("setTitle:");
			Objc.void_objc_msgSend_NativeHandle(buttonHandle, setTitleSelector, title.Handle);
		}

		using var toolTip = new NSString("Greenshot");
		var setToolTipSelector = Selector.GetHandle("setToolTip:");
		Objc.void_objc_msgSend_NativeHandle(buttonHandle, setToolTipSelector, toolTip.Handle);
	}

	private UIImage? LoadStatusButtonImage()
	{
		return UIImage.GetSystemImage("camera.viewfinder")
			?? UIImage.FromBundle("appicon")
			?? UIImage.FromFile(Path.Combine(NSBundle.MainBundle.ResourcePath, "appicon.icns"))
			?? UIImage.GetSystemImage("camera");
	}

	private void OnWorkspaceMenuStateChanged()
	{
		if (!_isInitialized)
		{
			return;
		}

		MainThread.BeginInvokeOnMainThread(RebuildMenu);
	}

	private void RebuildMenu()
	{
		var menuClass = Class.GetHandle("NSMenu");
		var menuHandle = CreateObject(menuClass);
		if (menuHandle == NativeHandle.Zero || _statusItemHandle == NativeHandle.Zero)
		{
			return;
		}

		var previousMenuHandle = _menuHandle;
		var previousTargets = _retainedTargets.ToArray();
		var nextTargets = new List<NSObject>();

		var entries = _mainPage.GetStatusBarMenuEntries();
		for (var index = 0; index < entries.Count; index++)
		{
			AddMenuItem(menuHandle, entries[index].Label, entries[index].Action, nextTargets);
			if (index == 0 && entries.Count > 1)
			{
				AddSeparator(menuHandle);
			}
		}

		var setMenuSelector = Selector.GetHandle("setMenu:");
		Objc.void_objc_msgSend_NativeHandle(_statusItemHandle, setMenuSelector, menuHandle);
		_menuHandle = menuHandle;
		_retainedTargets.Clear();
		_retainedTargets.AddRange(nextTargets);

		foreach (var target in previousTargets)
		{
			target.Dispose();
		}

		if (previousMenuHandle != NativeHandle.Zero)
		{
			Objc.void_objc_release(previousMenuHandle);
		}
	}

	private void AddSeparator(NativeHandle menuHandle)
	{
		var menuItemClass = Class.GetHandle("NSMenuItem");
		if (menuItemClass == NativeHandle.Zero)
		{
			return;
		}

		var separatorItemSelector = Selector.GetHandle("separatorItem");
		var separatorHandle = Objc.NativeHandle_objc_msgSend(menuItemClass, separatorItemSelector);
		if (separatorHandle == NativeHandle.Zero)
		{
			return;
		}

		var addItemSelector = Selector.GetHandle("addItem:");
		Objc.void_objc_msgSend_NativeHandle(menuHandle, addItemSelector, separatorHandle);
	}

	private void AddMenuItem(
		NativeHandle menuHandle,
		string title,
		WorkspaceAction action,
		List<NSObject> retainedTargets)
	{
		var menuItemClass = Class.GetHandle("NSMenuItem");
		var menuItemHandle = CreateObject(menuItemClass);
		if (menuItemHandle == NativeHandle.Zero)
		{
			return;
		}

		var target = new TrayMenuActionTarget(async () => await _mainPage.ExecuteWorkspaceActionAsync(action, activateWindow: true));
		retainedTargets.Add(target);

		using var itemTitle = new NSString(title);
		using var keyEquivalent = new NSString(string.Empty);
		var actionSelector = Selector.GetHandle("handleAction:");
		var initSelector = Selector.GetHandle("initWithTitle:action:keyEquivalent:");
		menuItemHandle = Objc.NativeHandle_objc_msgSend_NativeHandle_IntPtr_NativeHandle(
			menuItemHandle,
			initSelector,
			itemTitle.Handle,
			actionSelector,
			keyEquivalent.Handle);

		var setTargetSelector = Selector.GetHandle("setTarget:");
		Objc.void_objc_msgSend_NativeHandle(menuItemHandle, setTargetSelector, target.Handle);

		var addItemSelector = Selector.GetHandle("addItem:");
		Objc.void_objc_msgSend_NativeHandle(menuHandle, addItemSelector, menuItemHandle);
		Objc.void_objc_release(menuItemHandle);
	}

	private static NativeHandle CreateObject(NativeHandle classHandle)
	{
		if (classHandle == NativeHandle.Zero)
		{
			return NativeHandle.Zero;
		}

		var allocSelector = Selector.GetHandle("alloc");
		var initSelector = Selector.GetHandle("init");
		var allocatedHandle = Objc.NativeHandle_objc_msgSend(classHandle, allocSelector);
		return allocatedHandle == NativeHandle.Zero
			? NativeHandle.Zero
			: Objc.NativeHandle_objc_msgSend(allocatedHandle, initSelector);
	}

	[Register("GreenshotTrayMenuActionTarget")]
	private sealed class TrayMenuActionTarget : NSObject
	{
		private readonly Func<Task> _callback;

		public TrayMenuActionTarget(Func<Task> callback)
		{
			_callback = callback;
		}

		[Export("handleAction:")]
		public async void HandleAction(NSObject? sender)
		{
			await _callback();
		}
	}

	private static class Objc
	{
		private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern NativeHandle NativeHandle_objc_msgSend(NativeHandle receiver, NativeHandle selector);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern NativeHandle NativeHandle_objc_msgSend_Double(NativeHandle receiver, NativeHandle selector, double arg1);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern void void_objc_msgSend_NativeHandle(NativeHandle receiver, NativeHandle selector, NativeHandle arg1);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
		internal static extern NativeHandle NativeHandle_objc_msgSend_NativeHandle_IntPtr_NativeHandle(
			NativeHandle receiver,
			NativeHandle selector,
			NativeHandle arg1,
			IntPtr arg2,
			NativeHandle arg3);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_retain")]
		internal static extern NativeHandle NativeHandle_objc_retain(NativeHandle value);

		[DllImport(ObjectiveCLibrary, EntryPoint = "objc_release")]
		internal static extern void void_objc_release(NativeHandle value);
	}
}
#endif

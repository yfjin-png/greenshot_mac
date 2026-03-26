using Greenshot.Maui.Core.Capabilities;
using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Services;

public sealed class PlatformCapabilityService : IPlatformCapabilityService
{
	private readonly IScreenshotCaptureService _screenshotCaptureService;
	private readonly IImageEditorService _imageEditorService;

	public PlatformCapabilityService(
		IScreenshotCaptureService screenshotCaptureService,
		IImageEditorService imageEditorService)
	{
		_screenshotCaptureService = screenshotCaptureService;
		_imageEditorService = imageEditorService;
	}

	public IReadOnlyList<CapabilityStatus> GetCapabilities()
	{
		var platformName = DeviceInfo.Current.Platform.ToString();

		return
		[
			new CapabilityStatus(
				GreenshotCapability.ImageImport,
				"Image import",
				true,
				"The MAUI shell can already open existing screenshots from disk and render them in a cross-platform preview surface.",
				"#15875C"),
			new CapabilityStatus(
				GreenshotCapability.ScreenshotCapture,
				"Screen capture",
				_screenshotCaptureService.IsSupported,
				_screenshotCaptureService.IsSupported
						? $"Display capture, window targeting, and in-app region selection are now routed through a native {platformName} adapter. Desktop overlay capture and hotkey-triggered capture still need follow-up work."
						: $"Needs a native {platformName} capture adapter. The current Greenshot capture pipeline is still bound to Windows-specific APIs and window handles.",
					_screenshotCaptureService.IsSupported ? "#15875C" : "#D56A2F"),
			new CapabilityStatus(
				GreenshotCapability.TrayIntegration,
				"Tray or menu bar",
				false,
				"Requires platform-specific shell integration. The legacy NotifyIcon workflow cannot be dropped into MAUI unchanged.",
				"#D56A2F"),
			new CapabilityStatus(
				GreenshotCapability.GlobalHotkeys,
				"Global hotkeys",
				false,
				"Needs OS-level registration per platform. The existing PrintScreen-oriented helper is tightly coupled to the Windows shell.",
				"#D56A2F"),
			new CapabilityStatus(
				GreenshotCapability.ClipboardImage,
				"Clipboard image flow",
				false,
				"Text clipboard support exists in MAUI, but screenshot image copy and paste still need dedicated platform adapters.",
				"#D56A2F"),
			new CapabilityStatus(
				GreenshotCapability.EditorWorkflow,
				"Image editor",
				_imageEditorService.IsSupported,
				_imageEditorService.IsSupported
					? "A native MAUI annotation surface now supports rectangle, arrow, and highlight markup, then saves an edited PNG copy into the app cache. The richer legacy editor tools still need follow-up work."
					: "Image editing is not available on this platform yet. The legacy editor still needs a native MAUI replacement.",
				_imageEditorService.IsSupported ? "#15875C" : "#D56A2F")
		];
	}
}

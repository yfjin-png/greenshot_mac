using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Foundation;
using Greenshot.Maui.Core.Services;

namespace Greenshot.Maui.Platforms.MacCatalyst.Services;

[SupportedOSPlatform("maccatalyst")]
internal sealed class MacCatalystScreenCaptureSupport
{
	public const string PermissionDeniedMessage =
		"Screen recording permission is required. Allow Greenshot in System Settings > Privacy & Security > Screen & System Audio Recording, then try again.";
	public const string UnsupportedVersionMessage =
		"Native screen capture currently requires macOS 14 or newer on the MacCatalyst host.";

	public bool IsSupported => IsHostVersionSupported();

	public bool TryValidateCapturePrerequisites(out ScreenshotCaptureResult? failure)
	{
		if (!IsHostVersionSupported())
		{
			failure = ScreenshotCaptureResult.Unsupported(UnsupportedVersionMessage);
			return false;
		}

		if (!EnsureScreenCaptureAccess())
		{
			failure = ScreenshotCaptureResult.PermissionDenied(PermissionDeniedMessage);
			return false;
		}

		failure = null;
		return true;
	}

	public bool TryValidateWindowCatalogPrerequisites(out ScreenshotWindowCatalogResult? failure)
	{
		if (!IsHostVersionSupported())
		{
			failure = ScreenshotWindowCatalogResult.Unsupported(UnsupportedVersionMessage);
			return false;
		}

		if (!EnsureScreenCaptureAccess())
		{
			failure = ScreenshotWindowCatalogResult.PermissionDenied(PermissionDeniedMessage);
			return false;
		}

		failure = null;
		return true;
	}

	private static bool EnsureScreenCaptureAccess() =>
		CGPreflightScreenCaptureAccess() || CGRequestScreenCaptureAccess();

	[SupportedOSPlatform("maccatalyst")]
	[SupportedOSPlatformGuard("maccatalyst")]
#pragma warning disable CA1416
	private static bool IsHostVersionSupported() =>
		NSProcessInfo.ProcessInfo.IsOperatingSystemAtLeastVersion(new NSOperatingSystemVersion(14, 0, 0));
#pragma warning restore CA1416

	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool CGPreflightScreenCaptureAccess();

	[DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool CGRequestScreenCaptureAccess();
}

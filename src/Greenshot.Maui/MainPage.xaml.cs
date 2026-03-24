using Greenshot.Maui.Core.Services;
using Greenshot.Maui.Services;

namespace Greenshot.Maui;

public partial class MainPage : ContentPage
{
	private const double MinimumSelectionDisplaySize = 8d;
	private const double SelectionPointerIndicatorSize = 24d;
	private const string MacCatalystPermissionTroubleshootingHint =
		"If Screen Recording is already enabled in System Settings but capture still fails, run `tccutil reset ScreenCapture org.greenshot.maui`, relaunch the app, and approve the current build again. Rebuilding the MacCatalyst app before approving can still change the code requirement that TCC matches, so use the current build without rebuilding once permission has been granted.";

	private readonly IImageEditorService _imageEditorService;
	private readonly IPlatformCapabilityService _platformCapabilityService;
	private readonly IScreenshotCaptureService _screenshotCaptureService;
	private readonly ScreenshotWorkspaceSession _workspaceSession = new();

	public MainPage(
		IImageEditorService imageEditorService,
		IPlatformCapabilityService platformCapabilityService,
		IScreenshotCaptureService screenshotCaptureService)
	{
		_imageEditorService = imageEditorService;
		_platformCapabilityService = platformCapabilityService;
		_screenshotCaptureService = screenshotCaptureService;

		InitializeComponent();
		InitializeEditorStylePanel();

		PreviewStage.SizeChanged += OnPreviewSurfaceSizeChanged;
		EditorInputOverlay.SizeChanged += OnPreviewSurfaceSizeChanged;
		SelectionInputSurface.SizeChanged += OnPreviewSurfaceSizeChanged;

		BindCapabilities();
	}

	private void BindCapabilities()
	{
		PlatformValueLabel.Text = DeviceInfo.Current.Platform.ToString();
		StatusValueLabel.Text = "Preview workspace";
		BindableLayout.SetItemsSource(CapabilitiesPanel, _platformCapabilityService.GetCapabilities());
		RefreshWorkspaceState();
	}
}

using Greenshot.Maui.Core.Services;
using Greenshot.Maui.Services;

namespace Greenshot.Maui;

public partial class MainPage : ContentPage
{
	private const double MinimumSelectionDisplaySize = 8d;
	private const double SelectionPointerIndicatorSize = 24d;
	private const string MacCatalystPermissionTroubleshootingHint =
		"If Screen Recording is already enabled in System Settings but capture still fails, run `tccutil reset ScreenCapture org.greenshot.maui`, relaunch the app, and approve the current build again. Rebuilding the MacCatalyst app before approving can still change the code requirement that TCC matches, so use the current build without rebuilding once permission has been granted.";

	private readonly IAppVisibilityService _appVisibilityService;
	private readonly ICaptureDefaultsService _captureDefaultsService;
	private readonly IImageEditorService _imageEditorService;
	private readonly ILocalFolderAccessService _localFolderAccessService;
	private readonly IScreenshotCaptureService _screenshotCaptureService;
	private readonly ScreenshotWorkspaceSession _workspaceSession = new();

	public MainPage(
		IAppVisibilityService appVisibilityService,
		ICaptureDefaultsService captureDefaultsService,
		IImageEditorService imageEditorService,
		ILocalFolderAccessService localFolderAccessService,
		IScreenshotCaptureService screenshotCaptureService)
	{
		_appVisibilityService = appVisibilityService;
		_captureDefaultsService = captureDefaultsService;
		_imageEditorService = imageEditorService;
		_localFolderAccessService = localFolderAccessService;
		_screenshotCaptureService = screenshotCaptureService;

		InitializeComponent();
		InitializeEditorStylePanel();
		InitializeSettingsPanel();

		PreviewStage.SizeChanged += OnPreviewSurfaceSizeChanged;
		EditorInputOverlay.SizeChanged += OnPreviewSurfaceSizeChanged;
		SelectionInputSurface.SizeChanged += OnPreviewSurfaceSizeChanged;

		InitializePageState();
	}

	private void InitializePageState()
	{
		PlatformValueLabel.Text = DeviceInfo.Current.Platform.ToString();
		StatusValueLabel.Text = "Preview workspace";
		RefreshWorkspaceState();
	}

	internal void AttachMenuHost(AppShell menuHost)
	{
		ArgumentNullException.ThrowIfNull(menuHost);
		_menuHost = menuHost;
		UpdateWorkspaceMenuState();
	}
}

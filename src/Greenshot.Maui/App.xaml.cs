using Greenshot.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Greenshot.Maui;

public partial class App : Application
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IAppVisibilityService _appVisibilityService;
	private readonly MainPage _mainPage;
	private readonly ITrayIconService _trayIconService;
	private Window? _mainWindow;

	public App(
		IServiceProvider serviceProvider,
		IAppVisibilityService appVisibilityService,
		MainPage mainPage,
		ITrayIconService trayIconService)
	{
		InitializeComponent();
		_serviceProvider = serviceProvider;
		_appVisibilityService = appVisibilityService;
		_mainPage = mainPage;
		_trayIconService = trayIconService;
		_trayIconService.Initialize();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
#if MACCATALYST
		_mainWindow ??= CreateMacCatalystMainWindow();
		return _mainWindow;
#else
		return new Window(_serviceProvider.GetRequiredService<AppShell>());
#endif
	}

#if MACCATALYST
	private Window CreateMacCatalystMainWindow()
	{
		var window = new Window(new NavigationPage(_mainPage));
		_appVisibilityService.RegisterWindow(window);
		window.Created += OnMacCatalystWindowCreated;
		return window;
	}

	private void OnMacCatalystWindowCreated(object? sender, EventArgs e)
	{
		if (sender is Window window)
		{
			window.Created -= OnMacCatalystWindowCreated;
		}

		_appVisibilityService.HideOnInitialLaunch();
	}
#endif
}

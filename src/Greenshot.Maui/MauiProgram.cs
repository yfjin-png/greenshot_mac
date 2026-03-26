using Greenshot.Maui.Core.Services;
#if MACCATALYST
using Greenshot.Maui.Platforms.MacCatalyst.Services;
#endif
using Greenshot.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Greenshot.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IScreenshotCaptureService>(
#if MACCATALYST
			static _ => new MacCatalystScreenshotCaptureService()
#else
			static _ => new UnsupportedScreenshotCaptureService()
#endif
		);
		builder.Services.AddSingleton<IImageEditorService, NativeImageEditorService>();
		builder.Services.AddSingleton<ICaptureDefaultsService, CaptureDefaultsService>();
		builder.Services.AddSingleton<ILocalFolderAccessService>(
#if MACCATALYST
			static _ => new MacCatalystLocalFolderAccessService()
#else
			static _ => new NullLocalFolderAccessService()
#endif
		);
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<IAppVisibilityService>(
#if MACCATALYST
			static _ => new MacCatalystAppVisibilityService()
#else
			static _ => new NullAppVisibilityService()
#endif
		);
		builder.Services.AddSingleton<ITrayIconService>(
#if MACCATALYST
			static serviceProvider => new MacCatalystTrayIconService(serviceProvider.GetRequiredService<MainPage>())
#else
			static _ => new NullTrayIconService()
#endif
		);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

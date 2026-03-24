using Greenshot.Maui.Core.Services;
#if MACCATALYST
using Greenshot.Maui.Platforms.MacCatalyst.Services;
#endif
using Greenshot.Maui.Services;
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
			builder.Services.AddSingleton<IPlatformCapabilityService, PlatformCapabilityService>();
			builder.Services.AddSingleton<AppShell>();
			builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

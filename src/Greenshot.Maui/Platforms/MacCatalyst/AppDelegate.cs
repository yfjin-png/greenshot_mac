using Foundation;
using UIKit;

namespace Greenshot.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	[Export("application:shouldSaveSecureApplicationState:")]
	public bool ShouldSaveSecureApplicationState(UIApplication application, NSCoder coder) => false;

	[Export("application:shouldRestoreSecureApplicationState:")]
	public bool ShouldRestoreSecureApplicationState(UIApplication application, NSCoder coder) => false;
}

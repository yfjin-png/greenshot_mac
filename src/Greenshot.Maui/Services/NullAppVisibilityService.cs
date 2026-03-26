using Microsoft.Maui.Controls;

namespace Greenshot.Maui.Services;

internal sealed class NullAppVisibilityService : IAppVisibilityService
{
	public bool IsVisible => true;

	public void RegisterWindow(Window window)
	{
	}

	public void HideOnInitialLaunch()
	{
	}

	public void Hide()
	{
	}

	public void Show()
	{
	}

	public void Quit()
	{
		Environment.Exit(0);
	}
}

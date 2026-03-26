using Microsoft.Maui.Controls;

namespace Greenshot.Maui.Services;

public interface IAppVisibilityService
{
	void RegisterWindow(Window window);

	void HideOnInitialLaunch();

	bool IsVisible { get; }

	void Hide();

	void Show();

	void Quit();
}

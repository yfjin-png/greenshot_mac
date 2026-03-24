namespace Greenshot.Maui;

public partial class AppShell : Shell
{
	public AppShell(MainPage mainPage)
	{
		InitializeComponent();

		Items.Add(new ShellContent
		{
			Title = "Workspace",
			Route = nameof(MainPage),
			Content = mainPage
		});
	}
}

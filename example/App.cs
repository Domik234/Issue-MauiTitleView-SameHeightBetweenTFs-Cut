namespace TitleViewRepro;

public class App : Application
{
	public App()
	{
		// No theming: light colors are enough and keep the result comparable between target frameworks.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState activationState) =>
		new(new NoLineNavigationPage(new TitleViewPage(depth: 0)));
}

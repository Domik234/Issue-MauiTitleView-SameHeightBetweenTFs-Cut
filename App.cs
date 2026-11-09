namespace TitleViewRepro;

public class App : Application
{
	public App()
	{
		// Bez témat: světlé barvy stačí a výsledek je tak srovnatelný mezi TF.
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState activationState) =>
		new(new NoLineNavigationPage(new TitleViewPage(depth: 0)));
}

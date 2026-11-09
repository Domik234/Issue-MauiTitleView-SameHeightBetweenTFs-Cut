namespace TitleViewRepro;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		return MauiApp.CreateBuilder()
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("NunitoSansRegular.ttf", "NunitoSans-Regular");
				fonts.AddFont("NunitoSansSemiBold.ttf", "NunitoSans-SemiBold");
			})
			.Build();
	}
}

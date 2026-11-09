using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using UIKit;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;
using Page = Microsoft.Maui.Controls.Page;

namespace TitleViewRepro;

/// <summary>
/// NavigationPage bez čáry pod lištou, stejně nastavená jako ve výchozí aplikaci:
/// bez stínu a separátoru, neprůhledná lišta a nativní UINavigationBar s ClipsToBounds = true.
/// Právě ClipsToBounds ořízne část TitleView, která při posunu kontejneru přesáhne okraj lišty.
/// </summary>
public class NoLineNavigationPage : NavigationPage
{
	public NoLineNavigationPage(Page root) : base(root)
	{
		BarBackgroundColor = Colors.White;
		BarTextColor = Colors.Black;

		this.On<iOS>().SetIsShadowEnabled(false);
#pragma warning disable CS0618 // Zastaralé, ale výchozí aplikace ho používá
		this.On<iOS>().DisableTranslucentNavigationBar();
#pragma warning restore CS0618
		this.On<iOS>().SetHideNavigationBarSeparator(true);
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();

		if (Handler is IPlatformViewHandler { ViewController: UINavigationController controller })
			ClearTitleSeparator(controller);
	}

	static void ClearTitleSeparator(UINavigationController controller)
	{
		var navBar = controller.NavigationBar;

		navBar.SetBackgroundImage(new UIImage(), UIBarMetrics.Default);
		navBar.ShadowImage = new UIImage();
		navBar.ClipsToBounds = true;

		if (navBar.ScrollEdgeAppearance != null)
		{
			navBar.ScrollEdgeAppearance.ShadowImage = new UIImage();
			navBar.ScrollEdgeAppearance.ShadowColor = null;
		}
	}
}

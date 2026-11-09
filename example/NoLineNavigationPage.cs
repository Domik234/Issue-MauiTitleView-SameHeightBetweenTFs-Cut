using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using UIKit;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;
using Page = Microsoft.Maui.Controls.Page;

namespace TitleViewRepro;

/// <summary>
/// NavigationPage without the line under the bar, configured the same way as in the original app:
/// no shadow and no separator, opaque bar and a native UINavigationBar with ClipsToBounds = true.
/// It is ClipsToBounds that cuts off the part of the TitleView that overflows the bar when the container is shifted.
/// </summary>
public class NoLineNavigationPage : NavigationPage
{
	public NoLineNavigationPage(Page root) : base(root)
	{
		BarBackgroundColor = Colors.White;
		BarTextColor = Colors.Black;

		this.On<iOS>().SetIsShadowEnabled(false);
#pragma warning disable CS0618 // Obsolete, but the original app uses it
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

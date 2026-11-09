using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using CoreGraphics;
using Foundation;
using UIKit;

namespace TitleViewRepro;

/// <summary>
/// Výpis nativního layoutu TitleView: frame, bounds, clipsToBounds a metriky fontu labelů a řetěz nativních předků až po UINavigationBar.
/// Slouží k určení, kdo ořezává horní část TitleView (diakritika nad velkými písmeny).
/// </summary>
static class NativeLayoutDump
{
	static string N(nfloat value) => ((double)value).ToString("0.##", CultureInfo.InvariantCulture);

	static string R(CGRect r) => $"({N(r.X)},{N(r.Y)} {N(r.Width)}×{N(r.Height)})";

	static string E(UIEdgeInsets i) => $"T{N(i.Top)} L{N(i.Left)} B{N(i.Bottom)} R{N(i.Right)}";

	public static string Header()
	{
		var controls = typeof(Microsoft.Maui.Controls.Label).Assembly
			.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
			.OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion?.Split('+')[0] ?? "?";
		return $"{RuntimeInformation.FrameworkDescription} | MAUI Controls {controls} | iOS {UIDevice.CurrentDevice.SystemVersion} | scale {N(UIScreen.MainScreen.Scale)}";
	}

	public static string Take(string title, CustomTitleView view)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"== {title} ==");
		sb.AppendLine(DumpView("TitleView", view));
		sb.AppendLine(DumpView("TextStack", view.TextStack));
		sb.AppendLine(DumpView("Text", view.PrimaryLabel));
		sb.AppendLine(DumpView("Secondary", view.SecondaryLabel));
		sb.Append(DumpChain("Text ancestors", view.PrimaryLabel));
		return sb.ToString();
	}

	static string DumpView(string name, VisualElement element)
	{
		if (element.Handler?.PlatformView is not UIView v)
			return $"  {name}: no native view (MAUI {element.Width:0.##} × {element.Height:0.##})";

		var sb = new StringBuilder();
		sb.Append($"  {name} [{v.GetType().Name}] MAUI {element.Width:0.##} × {element.Height:0.##}; frame {R(v.Frame)}; bounds {R(v.Bounds)}; clipsToBounds {v.ClipsToBounds}; hidden {v.Hidden}");

		if (v is UILabel label && label.Font is { } font)
		{
			var fit = label.SizeThatFits(new CGSize(label.Bounds.Width, nfloat.MaxValue));
			sb.Append($"\n     font {font.Name} {N(font.PointSize)}pt: lineHeight {N(font.LineHeight)}, ascender {N(font.Ascender)}, descender {N(font.Descender)}, capHeight {N(font.CapHeight)}");
			sb.Append($"\n     sizeThatFits {N(fit.Width)}×{N(fit.Height)}, intrinsic {N(label.IntrinsicContentSize.Width)}×{N(label.IntrinsicContentSize.Height)}");
		}

		return sb.ToString();
	}

	static string DumpChain(string name, VisualElement element)
	{
		if (element.Handler?.PlatformView is not UIView v)
			return $"  {name}: no native view";

		var sb = new StringBuilder();
		sb.AppendLine($"  {name}:");
		for (var parent = v.Superview; parent != null; parent = parent.Superview)
		{
			sb.AppendLine($"     ↑ {parent.GetType().Name}: frame {R(parent.Frame)}; center ({N(parent.Center.X)},{N(parent.Center.Y)}); clips {parent.ClipsToBounds}; " +
				$"autoresizing {parent.AutoresizingMask}; translatesAR {parent.TranslatesAutoresizingMaskIntoConstraints}; constraints {parent.Constraints.Length}; layoutMargins {E(parent.LayoutMargins)}");

			if (parent.GetType().Name == "Container")
			{
				// Kontejner TitleView, který vytváří MAUI (NavigationRenderer.Container)
				sb.AppendLine($"       container: intrinsic {N(parent.IntrinsicContentSize.Width)}×{N(parent.IntrinsicContentSize.Height)}; " +
					$"sizeThatFits(bounds) {N(parent.SizeThatFits(parent.Bounds.Size).Width)}×{N(parent.SizeThatFits(parent.Bounds.Size).Height)}; alignmentRectInsets {E(parent.AlignmentRectInsets)}");
			}

			if (parent is UINavigationBar bar)
			{
				sb.AppendLine($"       bar: safeArea {E(bar.SafeAreaInsets)}; prefersLargeTitles {bar.PrefersLargeTitles}; sizeThatFits {N(bar.SizeThatFits(bar.Bounds.Size).Height)}");
				break;
			}
		}

		return sb.ToString();
	}
}

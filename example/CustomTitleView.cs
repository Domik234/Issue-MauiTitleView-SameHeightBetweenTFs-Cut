using Microsoft.Maui.Controls.Shapes;

namespace TitleViewRepro;

/// <summary>
/// Page title in the navigation bar: left icon, centered Text with an optional SecondaryText and right icon.
/// The layout matches the original component from the version where .NET 9 worked:
/// a root Grid with a single Auto row (HeightRequest 56) and a StackLayout with VerticalOptions=Center in the middle.
/// </summary>
public class CustomTitleView : Grid
{
	/// <summary>Tap on the left icon.</summary>
	public event EventHandler LeftClicked;

	// Public elements, measured by the page
	public Label PrimaryLabel { get; }
	public Label SecondaryLabel { get; }
	public StackLayout TextStack { get; }

	public string Text
	{
		get => PrimaryLabel.Text;
		set => PrimaryLabel.Text = value;
	}

	public string SecondaryText
	{
		get => SecondaryLabel.Text;
		set
		{
			SecondaryLabel.Text = value;
			SecondaryLabel.IsVisible = !string.IsNullOrWhiteSpace(value);
		}
	}

	public CustomTitleView(string leftIcon = "arrow_left", string rightIcon = "cog")
	{
		RowDefinitions = new RowDefinitionCollection { new() { Height = GridLength.Auto } };
		ColumnDefinitions = new ColumnDefinitionCollection
		{
			new() { Width = GridLength.Auto },
			new() { Width = GridLength.Star },
			new() { Width = GridLength.Auto }
		};
		RowSpacing = ColumnSpacing = 0;
		HeightRequest = 56;
		BackgroundColor = Colors.White;

		PrimaryLabel = new Label
		{
			FontFamily = "NunitoSans-SemiBold",
			FontSize = 18,
			CharacterSpacing = 0,
			TextColor = Colors.Black,
			VerticalTextAlignment = TextAlignment.Center,
			HorizontalTextAlignment = TextAlignment.Center,
			LineBreakMode = LineBreakMode.TailTruncation
		};

		SecondaryLabel = new Label
		{
			FontFamily = "NunitoSans-Regular",
			FontSize = 14,
			TextColor = Colors.Gray,
			HorizontalTextAlignment = TextAlignment.Center,
			MaxLines = 1,
			IsVisible = false
		};

		TextStack = new StackLayout
		{
			Children = { PrimaryLabel, SecondaryLabel },
			Spacing = 0,
			VerticalOptions = LayoutOptions.Center
		};

		var left = CreateIconContainer(leftIcon, LayoutOptions.Start);
		var tap = new TapGestureRecognizer();
		tap.Tapped += (_, _) => LeftClicked?.Invoke(this, EventArgs.Empty);
		left.GestureRecognizers.Add(tap);

		this.Add(left, 0, 0);
		this.Add(TextStack, 1, 0);
		this.Add(CreateIconContainer(rightIcon, LayoutOptions.End), 2, 0);
	}

	/// <summary>24 × 24 icon with padding 8 and a 40 × 40 circular clip (iOS variant of the original component).</summary>
	static ContentView CreateIconContainer(string source, LayoutOptions horizontal) => new()
	{
		Content = new Image
		{
			Source = source,
			HorizontalOptions = horizontal,
			VerticalOptions = LayoutOptions.Center
		},
		Padding = 8,
		Clip = new EllipseGeometry { Center = new Point(20, 20), RadiusX = 20, RadiusY = 20 }
	};
}

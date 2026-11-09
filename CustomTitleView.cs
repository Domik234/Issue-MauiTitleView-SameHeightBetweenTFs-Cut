using Microsoft.Maui.Controls.Shapes;

namespace TitleViewRepro;

/// <summary>
/// Titulek stránky v navigační liště: levá ikona, vycentrovaný Text s volitelným SecondaryText a pravá ikona.
/// Rozložení odpovídá původnímu komponentu z verze, kde .NET 9 fungoval:
/// kořenový Grid s jednou Auto řádkou (HeightRequest 56), uprostřed StackLayout s VerticalOptions=Center.
/// </summary>
public class CustomTitleView : Grid
{
	/// <summary>Klik na levou ikonu.</summary>
	public event EventHandler LeftClicked;

	// Veřejné prvky pro měření ve stránce
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

	/// <summary>Ikona 24 × 24 s odsazením 8 a kruhovým ořezem 40 × 40 (iOS varianta původního komponentu).</summary>
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

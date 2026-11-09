namespace TitleViewRepro;

/// <summary>
/// Page with a CustomTitleView in the navigation bar and the same component in the content for comparison.
/// The buttons change the text, SecondaryText and HeightRequest and print the native layout.
/// <para>Automatic mode for running without touch (environment variables passed through simctl or devicectl):
/// TITLEVIEW_AUTODUMP=1 prints the layout to the console after 1.5 s and again after 3 s,
/// TITLEVIEW_AUTOPUSH=1 then pushes another page and prints its layout too.</para>
/// </summary>
public class TitleViewPage : ContentPage
{
	// The sample texts are intentionally Czech: the diacritics are what gets clipped. The first text is in capitals because the original
	// app uppercases the title, so the carons above Ř Š Ž Ť are the highest part.
	static readonly string[] Texts = ["PŘÍLIŠ ŽLUŤOUČKÝ KŮŇ", "Příliš žluťoučký kůň", "Test", "Velmi dlouhý nadpis stránky, který se nevejde na jeden řádek"];
	static readonly string[] SecondaryTexts = ["Úpěl ďábelské ódy", null, "Podtitul"];

	readonly int _depth;
	readonly CustomTitleView _navTitleView;
	readonly CustomTitleView _contentTitleView;
	readonly Label _dumpLabel;
	int _textIndex, _secondaryIndex;
	bool _heightRequested = true;

	public TitleViewPage(int depth)
	{
		_depth = depth;
		Title = $"Page {depth}";
		BackgroundColor = Colors.White;

		_navTitleView = CreateTitleView();
		NavigationPage.SetTitleView(this, _navTitleView);

		// The system back button is hidden, the left icon of the title navigates back (as in the original app).
		NavigationPage.SetHasBackButton(this, false);

		_contentTitleView = CreateTitleView();

		_dumpLabel = new Label
		{
			FontFamily = "Menlo",
			FontSize = 9,
			TextColor = Colors.Black,
			Text = "(use the Dump native layout button)"
		};

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(8),
				Spacing = 8,
				Children =
				{
					new FlexLayout
					{
						Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
						Children =
						{
							CreateButton(() => $"Text: {_textIndex + 1}/{Texts.Length}", NextText),
							CreateButton(() => $"Secondary: {_secondaryIndex + 1}/{SecondaryTexts.Length}", NextSecondary),
							CreateButton(() => $"HeightRequest: {(_heightRequested ? "56" : "none")}", ToggleHeight),
							CreateButton(() => "Push page", Push),
							CreateButton(() => "Dump native layout", Dump)
						}
					},
					new Label { Text = NativeLayoutDump.Header(), FontSize = 11, TextColor = Colors.Gray },
					new Label { Text = "The same component in the page content (for comparison):", FontSize = 11, TextColor = Colors.Gray },
					_contentTitleView,
					_dumpLabel
				}
			}
		};

		Loaded += OnPageLoaded;
	}

	CustomTitleView CreateTitleView()
	{
		var view = new CustomTitleView { Text = Texts[0], SecondaryText = SecondaryTexts[0] };
		view.LeftClicked += async (_, _) =>
		{
			if (_depth > 0)
				await Navigation.PopAsync();
		};
		return view;
	}

	static Button CreateButton(Func<string> label, Action action)
	{
		var button = new Button { Text = label(), FontSize = 12, Margin = new Thickness(2), Padding = new Thickness(8, 0), HeightRequest = 36 };
		button.Clicked += (_, _) =>
		{
			action();
			button.Text = label();
		};
		return button;
	}

	void NextText()
	{
		_textIndex = (_textIndex + 1) % Texts.Length;
		_navTitleView.Text = _contentTitleView.Text = Texts[_textIndex];
	}

	void NextSecondary()
	{
		_secondaryIndex = (_secondaryIndex + 1) % SecondaryTexts.Length;
		_navTitleView.SecondaryText = _contentTitleView.SecondaryText = SecondaryTexts[_secondaryIndex];
	}

	void ToggleHeight()
	{
		_heightRequested = !_heightRequested;
		_navTitleView.HeightRequest = _contentTitleView.HeightRequest = _heightRequested ? 56 : -1;
	}

	void Push() => _ = Navigation.PushAsync(new TitleViewPage(_depth + 1));

	void Dump()
	{
		string text = NativeLayoutDump.Header() + "\n" +
			NativeLayoutDump.Take($"Page {_depth}: in navigation bar", _navTitleView) + "\n" +
			NativeLayoutDump.Take($"Page {_depth}: in content", _contentTitleView);

		_dumpLabel.Text = text;
		foreach (string line in text.Split('\n'))
			Console.WriteLine("[TitleViewDump] " + line.TrimEnd('\r'));
	}

	async void OnPageLoaded(object sender, EventArgs e)
	{
		Loaded -= OnPageLoaded;

		if (_depth == 0 && Environment.GetEnvironmentVariable("TITLEVIEW_AUTODUMP") == "1")
		{
			await Task.Delay(1500);
			Dump();
			await Task.Delay(1500);
			Dump();

			if (Environment.GetEnvironmentVariable("TITLEVIEW_AUTOPUSH") == "1")
			{
				// Delay, so that a screenshot of this page can be taken before the push animation
				await Task.Delay(3000);
				Push();
			}
		}
		else if (_depth > 0 && Environment.GetEnvironmentVariable("TITLEVIEW_AUTOPUSH") == "1")
		{
			await Task.Delay(1500);
			Dump();
		}
	}
}

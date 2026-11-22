namespace VersionHunterX;
public partial class DiscoverPage : ContentPage
{
    static readonly (string Title, string[] Items)[] Cats =
    {
        ("🎮 Игры с открытым кодом", new[] { "Mindustry", "Shattered Pixel Dungeon", "SuperTuxKart", "Luanti", "Pixel Dungeon", "OpenTTD", "2048", "Antimine" }),
        ("🌐 Интернет и общение", new[] { "NewPipe", "LibreTube", "Fennec", "Telegram FOSS", "Element", "Conversations", "Signal", "Tor Browser" }),
        ("🎵 Музыка и видео", new[] { "VLC", "AntennaPod", "Jellyfin", "Kodi", "Symphony", "Namida", "Seal", "YTDLnis" }),
        ("🛠 Утилиты", new[] { "Termux", "Obtainium", "Syncthing", "Material Files", "Simple Gallery", "Organic Maps", "Shizuku", "Aurora Store" }),
        ("🔐 Безопасность", new[] { "Aegis", "KeePassDX", "Bitwarden", "Orbot", "Proton", "Tutanota" }),
        ("📚 Чтение", new[] { "Mihon", "KOReader", "Librera", "Readest", "Komikku" }),
    };
    readonly Label _recentTitle = new() { Text = "🕘 Недавние поиски", TextColor = Colors.White, FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) };
    readonly FlexLayout _recent = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };

    public DiscoverPage()
    {
        InitializeComponent();
        Root.Add(_recentTitle); Root.Add(_recent);
        foreach (var (title, items) in Cats)
        {
            Root.Add(new Label { Text = title, TextColor = Colors.White, FontSize = 18, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) });
            var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
            foreach (var it in items.Distinct()) flex.Add(Chip(it, "#2A1850"));
            Root.Add(flex);
        }
    }
    Button Chip(string text, string bg)
    {
        var b = new Button { Text = text, BackgroundColor = Color.FromArgb(bg), TextColor = Color.FromArgb("#F0ABFC"), CornerRadius = 18, HeightRequest = 40, Padding = new Thickness(16, 0), Margin = new Thickness(0, 0, 8, 8), FontSize = 13 };
        b.Clicked += async (_, _) => await Shell.Current.GoToAsync($"//search?q={Uri.EscapeDataString(text)}");
        return b;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _recent.Children.Clear();
        foreach (var h in Store.History.Take(10)) _recent.Add(Chip(h, "#3B1D6B"));
        _recentTitle.IsVisible = _recent.IsVisible = Store.History.Count > 0;
        var items = Root.Children.OfType<VisualElement>().ToList();
        foreach (var v in items) { v.Opacity = 0; v.TranslationY = 18; }
        for (int i = 0; i < items.Count; i++) _ = Reveal(items[i], i);
    }
    static async Task Reveal(VisualElement v, int i)
    {
        await Task.Delay(Math.Min(i * 45, 700));
        await Task.WhenAll(v.FadeTo(1, 350), v.TranslateTo(0, 0, 350, Easing.CubicOut));
    }
    async void OnSearch(object? s, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(Bar.Text)) await Shell.Current.GoToAsync($"//search?q={Uri.EscapeDataString(Bar.Text.Trim())}");
    }
}

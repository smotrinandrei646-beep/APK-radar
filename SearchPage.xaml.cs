namespace VersionHunterX;
[QueryProperty(nameof(Query), "q")]
public partial class SearchPage : ContentPage
{
    string? _q;
    List<Repo> _res = new();
    public string? Query
    {
        get => _q;
        set { _q = value; if (!string.IsNullOrWhiteSpace(value)) { Bar.Text = value; _ = RunSearch(); } }
    }
    static readonly (string Name, string Url)[] Sites =
    {
        ("APKMirror", "https://www.apkmirror.com/?post_type=app_release&searchtype=apk&s={q}"),
        ("APKPure", "https://apkpure.com/search?q={q}"),
        ("APKCombo", "https://apkcombo.com/search/{q}"),
        ("Uptodown", "https://www.google.com/search?q={q}+site%3Auptodown.com"),
        ("Aptoide", "https://en.aptoide.com/search?query={q}"),
        ("F-Droid", "https://search.f-droid.org/?q={q}"),
        ("IzzyOnDroid", "https://apt.izzysoft.de/fdroid/index?searchterm={q}"),
        ("Google Play", "https://play.google.com/store/search?q={q}&c=apps"),
        ("Huawei AppGallery", "https://appgallery.huawei.com/search/{q}"),
        ("Amazon Appstore", "https://www.amazon.com/s?k={q}&i=mobile-apps"),
        ("SourceForge", "https://sourceforge.net/directory/?q={q}"),
        ("GitHub", "https://github.com/search?q={q}+android&type=repositories"),
        ("GitLab", "https://gitlab.com/search?search={q}"),
        ("Codeberg", "https://codeberg.org/explore/repos?q={q}"),
    };
    public SearchPage()
    {
        InitializeComponent();
        Fd.IsChecked = Preferences.Get("src_fd", true); Gh.IsChecked = Preferences.Get("src_gh", true);
        Gl.IsChecked = Preferences.Get("src_gl", true); Cb.IsChecked = Preferences.Get("src_cb", true); Ap.IsChecked = Preferences.Get("src_ap", true);
        SizeChanged += (_, _) => Ui.ApplySpan(List, Width);
        foreach (var (name, url) in Sites)
        {
            var b = new Button { Text = name, ClassId = url, BackgroundColor = Color.FromArgb("#2A1850"), TextColor = Color.FromArgb("#F0ABFC"), CornerRadius = 16, HeightRequest = 36, FontSize = 12, Padding = new Thickness(14, 0), Margin = new Thickness(0, 0, 6, 6) };
            b.Clicked += OnLink;
            Links.Add(b);
        }
    }
    async void OnSearch(object? s, EventArgs e) => await RunSearch();
    void OnSrc(object? s, CheckedChangedEventArgs e)
    {
        Preferences.Set("src_fd", Fd.IsChecked); Preferences.Set("src_gh", Gh.IsChecked);
        Preferences.Set("src_gl", Gl.IsChecked); Preferences.Set("src_cb", Cb.IsChecked); Preferences.Set("src_ap", Ap.IsChecked);
    }
    async Task RunSearch()
    {
        var q = Bar.Text?.Trim();
        if (string.IsNullOrEmpty(q)) return;
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) { Status.Text = "Нет подключения к интернету."; return; }
        Busy.IsRunning = true; List.ItemsSource = null; Status.Text = "Ищу и проверяю наличие APK…";
        Store.AddHistory(q);
        _res = await SearchService.Search(q, Gh.IsChecked, Gl.IsChecked, Cb.IsChecked, Ap.IsChecked, Fd.IsChecked);
        Busy.IsRunning = false;
        ApplySort();
        Status.Text = (_res.Count == 0 ? "APK не найдены. Попробуйте другое название или кнопки сайтов выше." : $"Найдено проектов с APK: {_res.Count}")
            + (SearchService.Warn.Length > 0 ? "\n" + SearchService.Warn : "");
        List.Opacity = 0; List.TranslationY = 20;
        await Task.WhenAll(List.FadeTo(1, 400), List.TranslateTo(0, 0, 400, Easing.CubicOut));
    }
    void ApplySort()
    {
        IEnumerable<Repo> r = SortPick.SelectedIndex switch
        {
            1 => _res.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            2 => _res.OrderBy(x => x.Source).ThenByDescending(x => x.Stars),
            _ => _res.OrderByDescending(x => x.Source == "Aptoide" ? 500 : x.Stars)
        };
        List.ItemsSource = r.ToList();
    }
    void OnSort(object? s, EventArgs e) { if (_res.Count > 0) ApplySort(); }
    async void OnLink(object? s, EventArgs e)
    {
        var q = Uri.EscapeDataString(Bar.Text?.Trim() ?? "");
        await Launcher.OpenAsync(((Button)s!).ClassId.Replace("{q}", q));
    }
    async void OnSelected(object? s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Repo r)
        {
            ((CollectionView)s!).SelectedItem = null;
            await Navigation.PushAsync(new ReleasesPage(r));
        }
    }
}

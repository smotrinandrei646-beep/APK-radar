namespace VersionHunterX;
public partial class ReleasesPage : ContentPage
{
    readonly Repo _repo;
    List<Rel> _all = new();
    public ReleasesPage(Repo r)
    {
        InitializeComponent(); _repo = r;
        Title = "Версии"; Header.Text = r.Name; Sub.Text = r.Subtitle; Desc.Text = r.Description;
        UpdateFav(); Load();
    }
    void UpdateFav() => FavItem.Text = Store.Favorites.Any(x => x.Id == _repo.Id && x.Source == _repo.Source) ? "★ В избранном" : "☆ В избранное";
    void OnFav(object? s, EventArgs e) { Store.ToggleFav(_repo); UpdateFav(); }
    async void Load()
    {
        Busy.IsRunning = true;
        try { _all = await SearchService.Releases(_repo); Apply(); }
        catch (Exception ex) { Status.Text = "Ошибка загрузки: " + ex.Message + (SearchService.Warn.Length > 0 ? "\n" + SearchService.Warn : ""); }
        Busy.IsRunning = false;
    }
    void Apply()
    {
        var q = Filter.Text?.Trim() ?? "";
        var l = _all.Where(r => (Pre.IsToggled || !r.Pre) && (q == "" || r.Tag.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        List.ItemsSource = l;
        Status.Text = _all.Count == 0 ? "APK в релизах нет — откройте страницу проекта." : $"Показано версий: {l.Count} из {_all.Count}";
    }
    void OnFilter(object? s, TextChangedEventArgs e) => Apply();
    void OnPre(object? s, ToggledEventArgs e) => Apply();
    CancellationTokenSource? _cts;
    async void OnDownload(object? s, EventArgs e)
    {
        if (((Button)s!).BindingContext is not Asset a) return;
        if (_cts != null) { Status.Text = "Дождитесь окончания текущей загрузки или отмените её."; return; }
        _cts = new(); Cancel.IsVisible = true; Prog.IsVisible = true; Prog.Progress = 0; Status.Text = "Скачивание: " + a.Name;
        try
        {
            var rec = await SearchService.Download(a, new Progress<(double Frac, double Mbps)>(t =>
            {
                Prog.Progress = t.Frac; Status.Text = $"Скачивание: {t.Frac:P0} · {t.Mbps:0.0} МБ/с";
            }), _cts.Token);
            Store.Downloads.Insert(0, rec); Store.Save();
            Status.Text = "Готово. " + rec.Check;
            if (await DisplayAlert("Скачано", $"{rec.Name}\n{rec.Check}\nSHA-256:\n{rec.Sha256}", "Установить", "Позже"))
                await Installer.Install(rec);
        }
        catch (OperationCanceledException) { Status.Text = "Загрузка отменена."; }
        catch (Exception ex) { Status.Text = "Ошибка: " + ex.Message; }
        _cts = null; Cancel.IsVisible = false; Prog.IsVisible = false;
    }
    void OnCancel(object? s, EventArgs e) => _cts?.Cancel();
    async void OnOpenLink(object? s, EventArgs e) { if (((Button)s!).BindingContext is Asset a) await Launcher.OpenAsync(a.Url); }
    async void OnRelease(object? s, EventArgs e) { if (((Button)s!).BindingContext is Rel r) await Launcher.OpenAsync(r.PageUrl); }
    async void OnProject(object? s, EventArgs e)
        => await Launcher.OpenAsync(_repo.Source == "Aptoide" ? "https://en.aptoide.com/search?query=" + Uri.EscapeDataString(_repo.Id) : _repo.Url);
}

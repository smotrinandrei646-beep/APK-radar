namespace VersionHunterX;
public partial class SettingsPage : ContentPage
{
    public SettingsPage() { InitializeComponent(); Token.Text = Preferences.Get("gh_token", ""); }
    async void OnHistory(object? s, EventArgs e) => await Navigation.PushAsync(new HistoryPage());
    async void OnVerify(object? s, EventArgs e) => await Navigation.PushAsync(new VerifyPage());
    async void OnSaveToken(object? s, EventArgs e)
    {
        Preferences.Set("gh_token", Token.Text?.Trim() ?? "");
        await DisplayAlert("Готово", "Токен сохранён.", "OK");
    }
    void OnClearHist(object? s, EventArgs e) { Store.History.Clear(); Store.Save(); }
    async void OnClearDl(object? s, EventArgs e)
    {
        if (!await DisplayAlert("Удалить всё?", "Все скачанные файлы будут удалены.", "Удалить", "Отмена")) return;
        foreach (var d in Store.Downloads.ToList()) { try { File.Delete(d.Path); } catch { } }
        Store.Downloads.Clear(); Store.Save();
    }
}

namespace VersionHunterX;
public partial class DownloadsPage : ContentPage
{
    public DownloadsPage() { InitializeComponent(); List.ItemsSource = Store.Downloads; SizeChanged += (_, _) => Ui.ApplySpan(List, Width); }
    async void OnSelected(object? s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not DownloadRec d) return;
        ((CollectionView)s!).SelectedItem = null;
        var act = await DisplayActionSheet(d.Name, "Отмена", null, "Установить", "Проверить на VirusTotal", "Копировать SHA-256", "Поделиться файлом", "Удалить");
        switch (act)
        {
            case "Установить": await Installer.Install(d); break;
            case "Проверить на VirusTotal": await Launcher.OpenAsync($"https://www.virustotal.com/gui/file/{d.Sha256}"); break;
            case "Копировать SHA-256": await Clipboard.SetTextAsync(d.Sha256); break;
            case "Поделиться файлом": await Share.RequestAsync(new ShareFileRequest(d.Name, new ShareFile(d.Path))); break;
            case "Удалить": try { File.Delete(d.Path); } catch { } Store.Downloads.Remove(d); Store.Save(); break;
        }
    }
}

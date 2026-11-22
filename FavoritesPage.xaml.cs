using System.Text;
namespace VersionHunterX;
public partial class FavoritesPage : ContentPage
{
    public FavoritesPage() { InitializeComponent(); List.ItemsSource = Store.Favorites; SizeChanged += (_, _) => Ui.ApplySpan(List, Width); }
    async void OnSelected(object? s, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Repo r) { ((CollectionView)s!).SelectedItem = null; await Navigation.PushAsync(new ReleasesPage(r)); }
    }
    async void OnCheck(object? s, EventArgs e)
    {
        if (Store.Favorites.Count == 0) { await DisplayAlert("Избранное пусто", "Сначала добавьте проекты звёздочкой.", "OK"); return; }
        var seen = Store.Seen; var sb = new StringBuilder();
        foreach (var r in Store.Favorites.ToList())
        {
            try
            {
                var l = (await SearchService.Releases(r)).FirstOrDefault();
                if (l == null) { sb.AppendLine($"• {r.Name}: нет APK"); continue; }
                var key = $"{r.Source}|{r.Id}";
                var mark = seen.TryGetValue(key, out var old) && old != l.Tag ? $"   🆕 было {old}" : "";
                sb.AppendLine($"• {r.Name}: {l.Tag}{mark}");
                seen[key] = l.Tag;
            }
            catch { sb.AppendLine($"• {r.Name}: ошибка проверки"); }
        }
        Store.SetSeen(seen);
        await DisplayAlert("Последние версии", sb.ToString(), "OK");
    }
}

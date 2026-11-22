namespace VersionHunterX;
public static class Installer
{
    public static async Task Install(DownloadRec d)
    {
        if (d.Name.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase) || d.Name.EndsWith(".apks", StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.DisplayAlert("Нужен установщик", "Файлы .xapk и .apks ставятся через специальный установщик (например, SAI или Obtainium).", "OK");
            return;
        }
        var ctx = Android.App.Application.Context;
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && !ctx.PackageManager!.CanRequestPackageInstalls())
        {
            if (await Shell.Current.DisplayAlert("Нужно разрешение", "Разрешите APK Радару устанавливать приложения, затем вернитесь и повторите.", "Открыть настройки", "Отмена"))
            {
                var i = new Android.Content.Intent(Android.Provider.Settings.ActionManageUnknownAppSources, Android.Net.Uri.Parse("package:" + ctx.PackageName));
                i.AddFlags(Android.Content.ActivityFlags.NewTask);
                ctx.StartActivity(i);
            }
            return;
        }
        await Launcher.OpenAsync(new OpenFileRequest(d.Name, new ReadOnlyFile(d.Path, "application/vnd.android.package-archive")));
    }
}
public static class Ui
{
    public static void ApplySpan(CollectionView cv, double width)
    {
        int span = width > 1100 ? 3 : width > 700 ? 2 : 1;
        if (cv.ItemsLayout is GridItemsLayout g && g.Span == span) return;
        cv.ItemsLayout = new GridItemsLayout(span, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 12 };
    }
}

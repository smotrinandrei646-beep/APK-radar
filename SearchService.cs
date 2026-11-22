using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
namespace VersionHunterX;
public static class SearchService
{
    static readonly HttpClient Http = Make(TimeSpan.FromSeconds(20));
    static readonly HttpClient DlHttp = Make(Timeout.InfiniteTimeSpan);
    static readonly ConcurrentDictionary<string, (DateTime T, JsonElement J)> Cache = new();
    public static string Warn = "";

    static HttpClient Make(TimeSpan t)
    {
        var h = new HttpClient { Timeout = t };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("APKRadar/2.0");
        h.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return h;
    }
    static string S(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    static int I(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
    static long L(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;

    static async Task<JsonElement> Get(string url)
    {
        if (Cache.TryGetValue(url, out var c) && DateTime.UtcNow - c.T < TimeSpan.FromMinutes(5)) return c.J;
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var tk = Preferences.Get("gh_token", "");
        if (tk.Length > 0 && url.StartsWith("https://api.github.com")) req.Headers.Authorization = new("Bearer", tk);
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode && url.Contains("github.com") && ((int)resp.StatusCode == 403 || (int)resp.StatusCode == 429))
            Warn = "Лимит GitHub исчерпан — добавьте токен в разделе «Ещё».";
        resp.EnsureSuccessStatusCode();
        using var s = await resp.Content.ReadAsStreamAsync();
        using var d = await JsonDocument.ParseAsync(s);
        var root = d.RootElement.Clone();
        Cache[url] = (DateTime.UtcNow, root);
        return root;
    }
    static async Task<List<Repo>> Safe(Func<Task<List<Repo>>> f) { try { return await f(); } catch { return new(); } }
    static string Short(string d) => DateTime.TryParse(d, out var t) ? t.ToString("dd.MM.yyyy") : d;
    static bool IsApk(string n) => n.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".apks", StringComparison.OrdinalIgnoreCase);

    public static async Task<List<Repo>> Search(string q, bool gh, bool gl, bool cb, bool ap, bool fd)
    {
        Warn = "";
        var e = Uri.EscapeDataString(q);
        var t = new List<Task<List<Repo>>>();
        if (fd) t.Add(Safe(async () =>
        {
            var j = await Get($"https://search.f-droid.org/api/search_apps?q={e}&lang=ru");
            var arr = j.ValueKind == JsonValueKind.Array ? j : (j.ValueKind == JsonValueKind.Object && j.TryGetProperty("apps", out var apps) ? apps : default(JsonElement));
            var res = new List<Repo>();
            if (arr.ValueKind != JsonValueKind.Array) return res;
            foreach (var x in arr.EnumerateArray())
            {
                var url = S(x, "url");
                var pkg = url.TrimEnd('/').Split('/').LastOrDefault() ?? "";
                if (pkg.Length == 0 || !pkg.Contains('.')) continue;
                res.Add(new Repo(pkg, S(x, "name"), S(x, "summary"), url, "F-Droid", 0));
            }
            return res.Take(12).ToList();
        }));
        if (ap) t.Add(Safe(async () =>
        {
            var j = await Get($"https://ws75.aptoide.com/api/7/apps/search?query={e}&limit=12");
            var res = new List<Repo>();
            foreach (var x in j.GetProperty("datalist").GetProperty("list").EnumerateArray())
            {
                if (!x.TryGetProperty("file", out var f)) continue;
                var pkg = S(x, "package");
                res.Add(new Repo(pkg, S(x, "name"), $"Версия {S(f, "vername")} · {pkg}", S(f, "path"), "Aptoide", 0));
            }
            return res;
        }));
        if (gh) t.Add(Safe(async () => (await Get($"https://api.github.com/search/repositories?q={e}+android&per_page=20")).GetProperty("items").EnumerateArray()
            .Select(x => new Repo(S(x, "full_name"), S(x, "full_name"), S(x, "description"), S(x, "html_url"), "GitHub", I(x, "stargazers_count"))).ToList()));
        if (gl) t.Add(Safe(async () => (await Get($"https://gitlab.com/api/v4/projects?search={e}&per_page=20&order_by=star_count")).EnumerateArray()
            .Select(x => new Repo(I(x, "id").ToString(), S(x, "path_with_namespace"), S(x, "description"), S(x, "web_url"), "GitLab", I(x, "star_count"))).ToList()));
        if (cb) t.Add(Safe(async () => (await Get($"https://codeberg.org/api/v1/repos/search?q={e}&limit=20")).GetProperty("data").EnumerateArray()
            .Select(x => new Repo(S(x, "full_name"), S(x, "full_name"), S(x, "description"), S(x, "html_url"), "Codeberg", I(x, "stars_count"))).ToList()));
        var all = (await Task.WhenAll(t)).SelectMany(x => x).ToList();
        // F-Droid и Aptoide — это уже готовые APK; у репозиториев проверяем, что в релизах есть APK
        var sure = all.Where(r => r.Source == "Aptoide" || r.Source == "F-Droid").ToList();
        var cand = all.Where(r => r.Source != "Aptoide" && r.Source != "F-Droid")
            .GroupBy(r => r.Source).SelectMany(g => g.OrderByDescending(r => r.Stars).Take(g.Key == "GitHub" ? 5 : 8)).ToList();
        var ok = await Task.WhenAll(cand.Select(async r => (Repo: r, Has: await HasApk(r))));
        return sure.Concat(ok.Where(x => x.Has).Select(x => x.Repo)).ToList();
    }
    static async Task<bool> HasApk(Repo r)
    {
        try { return (await Releases(r)).Count > 0; } catch { return true; }
    }

    public static async Task<List<Rel>> Releases(Repo r)
    {
        var list = new List<Rel>();
        if (r.Source == "F-Droid")
        {
            var j = await Get($"https://f-droid.org/api/v1/packages/{Uri.EscapeDataString(r.Id)}");
            var sug = L(j, "suggestedVersionCode");
            foreach (var p in j.GetProperty("packages").EnumerateArray())
            {
                var code = L(p, "versionCode");
                var file = $"{r.Id}_{code}.apk";
                list.Add(new(S(p, "versionName") + (code == sug ? "  ★ рекомендуемая" : ""), $"код {code}", r.Url,
                    new() { new(file, $"https://f-droid.org/repo/{file}", 0) }));
            }
            return list;
        }
        if (r.Source == "Aptoide")
        {
            var page = "https://en.aptoide.com/search?query=" + Uri.EscapeDataString(r.Id);
            try
            {
                var j = await Get($"https://ws75.aptoide.com/api/7/listAppVersions?package_name={Uri.EscapeDataString(r.Id)}&limit=30");
                foreach (var x in j.GetProperty("datalist").GetProperty("list").EnumerateArray())
                {
                    if (!x.TryGetProperty("file", out var f)) continue;
                    var path = S(f, "path");
                    if (path == "") continue;
                    list.Add(new(S(f, "vername"), Short(S(x, "added")), page, new() { new($"{r.Name}-{S(f, "vername")}.apk", path, L(f, "filesize")) }));
                }
            }
            catch { }
            if (list.Count == 0) list.Add(new(r.Description, "", page, new() { new(r.Name + ".apk", r.Url, 0) }));
            return list;
        }
        if (r.Source == "GitLab")
        {
            var a = await Get($"https://gitlab.com/api/v4/projects/{r.Id}/releases?per_page=30");
            foreach (var x in a.EnumerateArray())
            {
                var assets = new List<Asset>();
                if (x.TryGetProperty("assets", out var ass) && ass.TryGetProperty("links", out var ls))
                    foreach (var l in ls.EnumerateArray())
                    {
                        var u = S(l, "direct_asset_url") is { Length: > 0 } d ? d : S(l, "url");
                        if (IsApk(S(l, "name")) || IsApk(u)) assets.Add(new(S(l, "name"), u, 0));
                    }
                if (assets.Count > 0) list.Add(new(S(x, "tag_name"), Short(S(x, "released_at")), r.Url + "/-/releases", assets, S(x, "description")));
            }
            return list;
        }
        var url = r.Source == "GitHub"
            ? $"https://api.github.com/repos/{r.Id}/releases?per_page=30"
            : $"https://codeberg.org/api/v1/repos/{r.Id}/releases?limit=30";
        var arr = await Get(url);
        foreach (var x in arr.EnumerateArray())
        {
            var assets = new List<Asset>();
            if (x.TryGetProperty("assets", out var ass))
                foreach (var l in ass.EnumerateArray())
                    if (IsApk(S(l, "name")))
                    {
                        var dg = S(l, "digest");
                        assets.Add(new(S(l, "name"), S(l, "browser_download_url"), L(l, "size"), dg.StartsWith("sha256:") ? dg[7..] : ""));
                    }
            if (assets.Count > 0)
                list.Add(new(S(x, "tag_name"), Short(S(x, "published_at")), S(x, "html_url"), assets, S(x, "body"),
                    x.TryGetProperty("prerelease", out var pr) && pr.ValueKind == JsonValueKind.True));
        }
        return list;
    }

    public static async Task<DownloadRec> Download(Asset a, IProgress<(double Frac, double Mbps)> p, CancellationToken ct)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
        Directory.CreateDirectory(dir);
        var name = string.Concat(a.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(dir, name);
        try
        {
            using var resp = await DlHttp.GetAsync(a.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? a.Size;
            var sw = Stopwatch.StartNew(); long lastRep = 0;
            using var src = await resp.Content.ReadAsStreamAsync(ct);
            using var dst = File.Create(path);
            var buf = new byte[81920]; long n = 0; int rd;
            while ((rd = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, rd), ct); n += rd;
                if (sw.ElapsedMilliseconds - lastRep > 250)
                {
                    lastRep = sw.ElapsedMilliseconds;
                    p.Report((total > 0 ? (double)n / total : 0, n / 1048576.0 / Math.Max(sw.Elapsed.TotalSeconds, 0.001)));
                }
            }
        }
        catch { try { File.Delete(path); } catch { } throw; }
        var sha = await Sha(path);
        var check = "";
        if (a.Hash.Length > 0)
        {
            if (!string.Equals(a.Hash, sha, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
                throw new InvalidOperationException("SHA-256 не совпал с хэшем издателя — файл удалён ради безопасности.");
            }
            check = "✅ SHA-256 совпал с хэшем издателя";
        }
        return new(a.Name, path, sha, DateTime.Now.ToString("dd.MM.yyyy HH:mm"), new FileInfo(path).Length, check);
    }
    public static async Task<string> Sha(string path)
    {
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(fs)).ToLowerInvariant();
    }
}

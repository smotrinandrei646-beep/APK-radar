namespace VersionHunterX;
public record Repo(string Id, string Name, string Description, string Url, string Source, int Stars)
{
    public string Subtitle => Stars > 0 ? $"{Source}  ·  ★ {Stars}" : Source;
}
public record Asset(string Name, string Url, long Size, string Hash = "")
{
    static readonly string[] Abis = (Android.OS.Build.SupportedAbis ?? new List<string>()).ToArray();
    public string Label => Size > 0 ? $"{Name} · {Size / 1048576.0:0.0} МБ" : Name;
    public string Abi
    {
        get
        {
            var n = Name.ToLowerInvariant();
            if (n.Contains("arm64") || n.Contains("aarch64")) return "arm64";
            if (n.Contains("armeabi") || n.Contains("armv7") || n.Contains("arm32")) return "arm32";
            if (n.Contains("x86_64")) return "x86_64";
            if (n.Contains("x86")) return "x86";
            if (n.Contains("universal")) return "универсальный";
            return "";
        }
    }
    public bool Fits => Abi switch
    {
        "arm64" => Abis.Contains("arm64-v8a"),
        "arm32" => Abis.Contains("armeabi-v7a") || Abis.Contains("armeabi"),
        "x86_64" => Abis.Contains("x86_64"),
        "x86" => Abis.Contains("x86"),
        _ => true
    };
    public string Badge => Abi == "" ? "" : (Fits ? $"✔ подходит планшету · {Abi}" : $"✖ {Abi}");
}
public record Rel(string Tag, string Date, string PageUrl, List<Asset> Assets, string Notes = "", bool Pre = false)
{
    public string Title => Pre ? $"{Tag}  ·  {Date}  ·  тестовая" : $"{Tag}  ·  {Date}";
    public bool HasNotes => Notes.Length > 0;
    public string NotesShort => Notes.Length > 350 ? Notes[..350] + "…" : Notes;
}
public record DownloadRec(string Name, string Path, string Sha256, string Date, long Size = 0, string Check = "")
{
    public string Subtitle => $"{Date}  ·  {Size / 1048576.0:0.0} МБ" + (Check.Length > 0 ? "\n" + Check : "") + $"\nSHA-256: {Sha256}";
}

using System.Text.Json;

namespace OSCAutoClicker;

internal sealed class Settings
{
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public int Interval { get; set; } = 100;
    public int Hold { get; set; } = 20;
    public int Jitter { get; set; } = 0;
    public int Hotkey { get; set; }
    public string? Language { get; set; }

    private static string FilePath => Path.Combine(AppContext.BaseDirectory, FileName);

    public static Settings Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path)) return new Settings();

            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options) ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
        }
        catch
        {
        }
    }
}

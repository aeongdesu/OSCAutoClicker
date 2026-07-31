using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OSCAutoClicker;

internal sealed class Strings(IReadOnlyDictionary<string, string> values, Strings? fallback)
{
    public string Connection => Get();
    public string Host => Get();
    public string Port => Get();
    public string Interval => Get();
    public string Hold => Get();
    public string Jitter => Get();
    public string Hotkey => Get();
    public string Language => Get();

    public string Start => Get();
    public string Stop => Get();

    public string HotkeyNone => Get();
    public string HotkeyPressKey => Get();
    public string HotkeyCaptureHint => Get();
    public string HotkeyFailedTitle => Get();
    public string HotkeyFailedBody => Get();

    public string StatusIdle => Get();
    public string StatusStopped => Get();
    public string StatusRunning => Get();
    public string StatusConnectFailed => Get();

    public string PortErrorTitle => Get();
    public string PortErrorBody => Get();

    public string ConnectFailedTitle => Get();
    public string OpenTargetFailed => Get();
    public string NothingListening => Get();
    public string SendFailed => Get();
    public string SendErrorTitle => Get();

    private string Get([CallerMemberName] string key = "") =>
        values.TryGetValue(key, out string? value) && value.Length > 0
            ? value
            : fallback?.Get(key) ?? key;

    public static IReadOnlyList<string> AllKeys { get; } =
        typeof(Strings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
}

internal sealed record AppLanguage(string Code, string NativeName, Strings Strings);

internal static class Localization
{
    private const string FallbackCode = "en";
    private const string NameKey = "_name";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static AppLanguage[] All { get; private set; } = [];

    public static string? Initialize()
    {
        const string Marker = ".lang.";
        const string Extension = ".json";

        Assembly assembly = typeof(Localization).Assembly;
        var loaded = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var problems = new List<string>();

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)) continue;

            int marker = resource.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
            if (marker < 0) continue;

            int start = marker + Marker.Length;
            string code = resource[start..(resource.Length - Extension.Length)];

            try
            {
                using Stream? stream = assembly.GetManifestResourceStream(resource);
                if (stream is null)
                {
                    problems.Add($"{code}.json: resource stream unavailable");
                    continue;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8);
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    reader.ReadToEnd(), JsonOptions);

                if (values is null || values.Count == 0)
                {
                    problems.Add($"{code}.json: empty");
                    continue;
                }

                loaded[code] = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                problems.Add($"{code}.json: {ex.Message}");
            }
        }

        if (loaded.Count == 0)
        {
            UseBareFallback();
            return "No language resources are embedded in this build.\n\n" + string.Join("\n", problems);
        }

        Strings? fallback = null;
        foreach ((string code, Dictionary<string, string> values) in loaded)
        {
            if (string.Equals(code, FallbackCode, StringComparison.OrdinalIgnoreCase))
            {
                fallback = new Strings(values, null);
                break;
            }
        }

        var languages = new List<AppLanguage>();
        foreach ((string code, Dictionary<string, string> values) in loaded)
        {
            bool isFallback = string.Equals(code, FallbackCode, StringComparison.OrdinalIgnoreCase);
            Strings strings = isFallback && fallback is not null ? fallback : new Strings(values, fallback);

            string name = values.TryGetValue(NameKey, out string? native) && native.Length > 0 ? native : code;
            languages.Add(new AppLanguage(code, name, strings));

            string[] missing = Strings.AllKeys.Where(key => !values.ContainsKey(key)).ToArray();
            if (missing.Length > 0)
            {
                string sample = string.Join(", ", missing.Take(5));
                if (missing.Length > 5) sample += ", …";
                problems.Add($"{code}.json: {missing.Length} key(s) missing — {sample}");
            }
        }

        languages.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
        All = languages.ToArray();

        return problems.Count > 0 ? string.Join("\n", problems) : null;
    }

    private static void UseBareFallback() =>
        All = [new AppLanguage(FallbackCode, "English", new Strings(new Dictionary<string, string>(), null))];

    public static AppLanguage Resolve(string? code)
    {
        if (!string.IsNullOrEmpty(code))
        {
            foreach (AppLanguage language in All)
            {
                if (string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase)) return language;
            }
        }
        return Detect();
    }

    public static AppLanguage Detect()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        foreach (AppLanguage language in All)
        {
            if (string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase)) return language;
        }
        foreach (AppLanguage language in All)
        {
            if (string.Equals(language.Code, FallbackCode, StringComparison.OrdinalIgnoreCase)) return language;
        }
        return All[0];
    }
}

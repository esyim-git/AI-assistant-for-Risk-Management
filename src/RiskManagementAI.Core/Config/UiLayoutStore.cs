using System.Text.Json;

namespace RiskManagementAI.Core.Config;

public sealed record UiLayout(
    double WindowWidth,
    double WindowHeight,
    double EditorRowStar,
    double ResultRowStar,
    double SafetyColumnWidth,
    int SchemaVersion);

public static class UiLayoutStore
{
    public const int CurrentSchemaVersion = 1;
    public const string DefaultPath = "config/ui_layout.local.json";

    public static readonly UiLayout Default = new(
        WindowWidth: 1180,
        WindowHeight: 720,
        EditorRowStar: 2,
        ResultRowStar: 1,
        SafetyColumnWidth: 340,
        SchemaVersion: CurrentSchemaVersion);

    private const double MinWindowWidth = 1180;
    private const double MinWindowHeight = 720;
    private const double MinStar = 0.25;
    private const double DefaultEditorRowStar = 2;
    private const double DefaultResultRowStar = 1;
    private const double MinSafetyColumnWidth = 280;
    private const double MaxSafetyColumnWidth = 560;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static UiLayout Load(string relativePath = DefaultPath)
    {
        ValidateConfigJsonPath(relativePath);
        var path = ResolvePath(relativePath);
        if (!File.Exists(path))
        {
            return Default;
        }

        try
        {
            var json = File.ReadAllText(path);
            var layout = JsonSerializer.Deserialize<UiLayout>(json, JsonOptions);
            return layout?.SchemaVersion == CurrentSchemaVersion
                ? Normalize(layout)
                : Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return Default;
        }
    }

    public static void Save(UiLayout layout, string relativePath = DefaultPath)
    {
        ValidateConfigJsonPath(relativePath);
        var path = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var normalized = Normalize(layout with { SchemaVersion = CurrentSchemaVersion });
        File.WriteAllText(path, JsonSerializer.Serialize(normalized, JsonOptions));
    }

    private static UiLayout Normalize(UiLayout layout)
    {
        var windowWidth = ClampFinite(layout.WindowWidth, MinWindowWidth, double.MaxValue, Default.WindowWidth);
        var windowHeight = ClampFinite(layout.WindowHeight, MinWindowHeight, double.MaxValue, Default.WindowHeight);
        var editorStar = ClampFinite(layout.EditorRowStar, MinStar, double.MaxValue, DefaultEditorRowStar);
        var resultStar = ClampFinite(layout.ResultRowStar, MinStar, double.MaxValue, DefaultResultRowStar);
        var safetyWidth = ClampFinite(layout.SafetyColumnWidth, MinSafetyColumnWidth, MaxSafetyColumnWidth, Default.SafetyColumnWidth);
        return new UiLayout(windowWidth, windowHeight, editorStar, resultStar, safetyWidth, CurrentSchemaVersion);
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Min(Math.Max(value, min), max);
    }

    private static void ValidateConfigJsonPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath)
            || !string.Equals(Path.GetExtension(relativePath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("UI layout path must be a config-relative JSON file.", nameof(relativePath));
        }

        var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2
            || !string.Equals(segments[0], "config", StringComparison.OrdinalIgnoreCase)
            || segments.Any(segment => segment == "." || segment == ".."))
        {
            throw new ArgumentException("UI layout path must stay under the config directory.", nameof(relativePath));
        }
    }

    private static string ResolvePath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativePath));
    }
}

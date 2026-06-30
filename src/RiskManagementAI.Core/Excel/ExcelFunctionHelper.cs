using System.Reflection;
using System.Text;
using System.Text.Json;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Excel;

public sealed record ExcelFunctionInfo(
    string Name,
    string Description,
    string Args,
    string RiskMgmtExample,
    string FormulaExample,
    bool Is365Only,
    string Excel2021Alternative,
    bool Recommended);

public sealed class ExcelFunctionHelper
{
    private const string ResourceName = "RiskManagementAI.Core.Excel.Resources.excel_function_help.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly SafetyRuleSet ruleSet;
    private readonly IReadOnlyDictionary<string, ExcelFunctionInfo> functionsByName;
    private readonly bool resourceLoaded;

    public ExcelFunctionHelper()
        : this(RuleLoader.LoadDefault())
    {
    }

    public ExcelFunctionHelper(SafetyRuleSet ruleSet)
        : this(ruleSet, resourceJsonOverride: null)
    {
    }

    public ExcelFunctionHelper(SafetyRuleSet ruleSet, string? resourceJsonOverride)
    {
        this.ruleSet = ruleSet;
        var warnings = new List<string>();
        var entries = LoadEntries(resourceJsonOverride, warnings);
        resourceLoaded = warnings.Count == 0;
        Warnings = warnings;
        functionsByName = resourceLoaded
            ? BuildFunctionCatalog(entries)
            : new Dictionary<string, ExcelFunctionInfo>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Warnings { get; }

    public ExcelFunctionInfo? Lookup(string name)
    {
        if (!resourceLoaded)
        {
            return null;
        }

        var normalizedName = NormalizeFunctionName(name);
        return string.IsNullOrWhiteSpace(normalizedName)
            ? null
            : functionsByName.GetValueOrDefault(normalizedName);
    }

    public IReadOnlyList<ExcelFunctionInfo> Search(string query, int maxResults = 20)
    {
        if (!resourceLoaded || maxResults <= 0)
        {
            return Array.Empty<ExcelFunctionInfo>();
        }

        var normalizedQuery = NormalizeSearchText(query);
        var terms = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return functionsByName.Values
            .Select(info => new ScoredFunction(info, Score(info, normalizedQuery, terms)))
            .Where(scored => string.IsNullOrEmpty(normalizedQuery) || scored.Score > 0)
            .OrderByDescending(scored => scored.Score)
            .ThenBy(scored => scored.Info.Name, StringComparer.Ordinal)
            .Take(maxResults)
            .Select(scored => scored.Info)
            .ToArray();
    }

    public string BuildFormulaInsertion(ExcelFunctionInfo info)
    {
        return info.FormulaExample;
    }

    private IReadOnlyDictionary<string, ExcelFunctionInfo> BuildFunctionCatalog(IReadOnlyList<ResourceFunctionInfo> entries)
    {
        var catalog = new Dictionary<string, ExcelFunctionInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var name = NormalizeFunctionName(entry.Name);
            if (string.IsNullOrWhiteSpace(name) || catalog.ContainsKey(name))
            {
                continue;
            }

            catalog[name] = BuildInfo(
                name,
                Clean(entry.Description),
                Clean(entry.Args),
                Clean(entry.RiskMgmtExample),
                Clean(entry.FormulaExample),
                Clean(entry.Excel2021Alternative));
        }

        foreach (var functionName in ruleSet.ExcelBlockedFunctions
                     .Concat(ruleSet.ExcelPreferredFunctions)
                     .Concat(ruleSet.ExcelCompletionAllowFunctions)
                     .Select(NormalizeFunctionName)
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            catalog.TryAdd(functionName, BuildGeneratedInfo(functionName));
        }

        return catalog
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private ExcelFunctionInfo BuildInfo(
        string name,
        string description,
        string args,
        string riskMgmtExample,
        string formulaExample,
        string catalogAlternative)
    {
        var is365Only = ContainsRuleName(ruleSet.ExcelBlockedFunctions, name);
        return new ExcelFunctionInfo(
            name,
            string.IsNullOrWhiteSpace(description) ? BuildGeneratedDescription(name, is365Only) : description,
            string.IsNullOrWhiteSpace(args) ? "See Microsoft Excel function syntax for argument details." : args,
            string.IsNullOrWhiteSpace(riskMgmtExample) ? "Use with dummy review data only; do not store source data in logs." : riskMgmtExample,
            string.IsNullOrWhiteSpace(formulaExample) ? $"={name}(A2)" : formulaExample,
            is365Only,
            BuildAlternative(name, is365Only, catalogAlternative),
            !is365Only && (ContainsRuleName(ruleSet.ExcelPreferredFunctions, name) || ContainsRuleName(ruleSet.ExcelCompletionAllowFunctions, name)));
    }

    private ExcelFunctionInfo BuildGeneratedInfo(string name)
    {
        var is365Only = ContainsRuleName(ruleSet.ExcelBlockedFunctions, name);
        return BuildInfo(
            name,
            BuildGeneratedDescription(name, is365Only),
            "See Microsoft Excel function syntax for argument details.",
            "Use with dummy review data only; do not store source data in logs.",
            is365Only ? $"={name}(A2:A10)" : $"={name}(A2)",
            string.Empty);
    }

    private string BuildAlternative(string name, bool is365Only, string catalogAlternative)
    {
        var preferred = ruleSet.ExcelPreferredFunctions.Count == 0
            ? "XLOOKUP, INDEX, MATCH, SUMIFS, COUNTIFS, PivotTable, HelperColumn"
            : string.Join(", ", ruleSet.ExcelPreferredFunctions);
        if (is365Only)
        {
            var prefix = string.IsNullOrWhiteSpace(catalogAlternative)
                ? $"{name} is outside the Excel 2021 compatibility set."
                : catalogAlternative;
            return $"{prefix} Preferred alternatives from the active ruleset: {preferred}.";
        }

        return string.IsNullOrWhiteSpace(catalogAlternative)
            ? "Excel 2021-compatible. Keep formulas reviewable and prefer helper columns for complex logic."
            : catalogAlternative;
    }

    private static string BuildGeneratedDescription(string name, bool is365Only)
    {
        return is365Only
            ? $"{name} is outside the Excel 2021 compatibility set used by this app."
            : $"{name} is available for static Excel 2021 review workflows.";
    }

    private static IReadOnlyList<ResourceFunctionInfo> LoadEntries(string? resourceJsonOverride, List<string> warnings)
    {
        try
        {
            var json = resourceJsonOverride ?? ReadEmbeddedResource();
            var entries = JsonSerializer.Deserialize<List<ResourceFunctionInfo>>(json, JsonOptions);
            if (entries is null)
            {
                warnings.Add("Excel function helper resource parsed to null.");
                return Array.Empty<ResourceFunctionInfo>();
            }

            return entries;
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException or ArgumentException)
        {
            warnings.Add($"Excel function helper resource parse failed: {ex.Message}");
            return Array.Empty<ResourceFunctionInfo>();
        }
    }

    private static string ReadEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded resource not found: {ResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static int Score(ExcelFunctionInfo info, string normalizedQuery, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return info.Recommended ? 20 : info.Is365Only ? 10 : 1;
        }

        if (string.Equals(info.Name, normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        var searchable = NormalizeSearchText(string.Join(' ',
            info.Name,
            info.Description,
            info.Args,
            info.RiskMgmtExample,
            info.FormulaExample,
            info.Excel2021Alternative));
        if (terms.Count > 0 && terms.All(term => searchable.Contains(term, StringComparison.Ordinal)))
        {
            var score = info.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ? 80 : 40;
            if (info.Recommended)
            {
                score += 5;
            }

            return score;
        }

        return 0;
    }

    private static bool ContainsRuleName(IEnumerable<string> values, string name)
    {
        return values.Any(value => string.Equals(NormalizeFunctionName(value), name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeFunctionName(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.StartsWith('='))
        {
            trimmed = trimmed[1..];
        }

        var parenIndex = trimmed.IndexOf('(', StringComparison.Ordinal);
        if (parenIndex >= 0)
        {
            trimmed = trimmed[..parenIndex];
        }

        return trimmed.Trim().ToUpperInvariant();
    }

    private static string NormalizeSearchText(string value)
    {
        return string.Join(' ', (value ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string Clean(string? value)
    {
        return string.Join(' ', (value ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed record ResourceFunctionInfo(
        string Name,
        string Description,
        string Args,
        string RiskMgmtExample,
        string FormulaExample,
        string Excel2021Alternative);

    private sealed record ScoredFunction(ExcelFunctionInfo Info, int Score);
}

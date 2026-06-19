using System.Text.Json;

namespace RiskManagementAI.Core.Config;

public static class PolicyLoader
{
    private const string DefaultPolicyPath = "config/security_policy.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static PolicyLoadResult LoadDefault()
    {
        return LoadFromFile(DefaultPolicyPath);
    }

    public static PolicyLoadResult LoadFromFile(string relativePolicyPath)
    {
        if (!IsSafeRelativeConfigPath(relativePolicyPath))
        {
            return CreateFallback($"Invalid policy path '{relativePolicyPath}'. Only config-relative JSON files are allowed.");
        }

        var resolvedPath = ResolvePolicyPath(relativePolicyPath);
        if (resolvedPath is null)
        {
            return CreateFallback($"Policy file '{relativePolicyPath}' was not found.");
        }

        try
        {
            var json = File.ReadAllText(resolvedPath);
            var policy = JsonSerializer.Deserialize<SecurityPolicy>(json, JsonOptions);
            return policy is null
                ? CreateFallback($"Policy file '{relativePolicyPath}' was empty or invalid.")
                : new PolicyLoadResult(policy, UsedFallback: false, Warnings: Array.Empty<string>());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return CreateFallback($"Policy file '{relativePolicyPath}' could not be loaded safely: {ex.Message}");
        }
    }

    private static PolicyLoadResult CreateFallback(string warning)
    {
        return new PolicyLoadResult(SecurityPolicy.SafeDefaults(), UsedFallback: true, Warnings: [warning]);
    }

    private static bool IsSafeRelativeConfigPath(string relativePolicyPath)
    {
        if (string.IsNullOrWhiteSpace(relativePolicyPath)
            || Path.IsPathRooted(relativePolicyPath)
            || !string.Equals(Path.GetExtension(relativePolicyPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = relativePolicyPath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2
            && string.Equals(segments[0], "config", StringComparison.OrdinalIgnoreCase)
            && segments.All(segment => segment != "." && segment != "..");
    }

    private static string? ResolvePolicyPath(string relativePolicyPath)
    {
        var currentDirectoryCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativePolicyPath));
        if (File.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        var appBaseCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePolicyPath));
        if (File.Exists(appBaseCandidate))
        {
            return appBaseCandidate;
        }

        return null;
    }
}

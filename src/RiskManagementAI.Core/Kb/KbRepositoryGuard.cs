using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Kb;

public static class KbRepositoryGuard
{
    private static readonly string[] ScanDirectories = ["kb", "data_sources", "samples", "config/ncr"];

    private static readonly HashSet<string> MetadataAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        Normalize("kb/README.md"),
        Normalize("kb/public_regulation_catalog.csv"),
        Normalize("kb/ncr_placeholder.md")
    };

    private static readonly string[] SuspiciousNameTokens =
    [
        "internal_rule_original",
        "internal_regulation_original",
        "ncr_official_original",
        "official_text",
        "full_text"
    ];

    private static readonly string[] SuspiciousContentTokens =
    [
        "내부규정 원문",
        "NCR 공식본 원문",
        "official text",
        "full text"
    ];

    public static IReadOnlyList<SafetyFinding> Scan(string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            throw new ArgumentException("Repository root is empty.", nameof(repositoryRoot));
        }

        var root = Path.GetFullPath(repositoryRoot);
        var findings = new List<SafetyFinding>();

        foreach (var relativeDirectory in ScanDirectories)
        {
            var directory = Path.Combine(root, relativeDirectory);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativePath = Normalize(Path.GetRelativePath(root, file));
                if (MetadataAllowlist.Contains(relativePath))
                {
                    continue;
                }

                if (HasSuspiciousName(relativePath) || HasSuspiciousContent(file))
                {
                    findings.Add(new SafetyFinding(
                        "KB_FORBIDDEN_SOURCE_TEXT",
                        SafetySeverity.Blocker,
                        $"내부규정/NCR 원문 의심 파일은 repo에 둘 수 없습니다. path={relativePath}"));
                }
            }
        }

        return findings;
    }

    private static bool HasSuspiciousName(string relativePath)
    {
        return SuspiciousNameTokens.Any(token => relativePath.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSuspiciousContent(string path)
    {
        var extension = Path.GetExtension(path);
        if (!IsTextLike(extension))
        {
            return false;
        }

        var text = File.ReadAllText(path);
        return SuspiciousContentTokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTextLike(string extension)
    {
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sql", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }
}

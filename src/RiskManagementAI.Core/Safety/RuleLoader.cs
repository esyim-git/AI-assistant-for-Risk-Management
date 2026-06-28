using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RiskManagementAI.Core.Safety;

public static class RuleLoader
{
    private const string DefaultRulesDirectory = "rules";
    private const string RuleVersionPrefix = "ruleset-";
    private const string RequirePresentPrefix = "REQUIRE_PRESENT:";

    private static readonly string[] RequiredRuleFiles =
    [
        "excel_2021_blocked_functions.txt",
        "excel_2021_preferred_functions.txt",
        "sql_deny_patterns.txt",
        "sql_warn_patterns.txt",
        "vba_deny_patterns.txt",
        "vba_warn_patterns.txt"
    ];

    private static readonly string[] DefaultExcelBlockedFunctions =
    [
        "VSTACK", "HSTACK", "TOCOL", "TOROW", "TAKE", "DROP", "CHOOSECOLS",
        "TEXTSPLIT", "TEXTBEFORE", "TEXTAFTER", "GROUPBY", "PIVOTBY",
        "MAP", "REDUCE", "BYROW", "BYCOL", "REGEXTEST", "REGEXEXTRACT", "REGEXREPLACE"
    ];

    private static readonly string[] DefaultExcelPreferredFunctions =
    [
        "XLOOKUP", "XMATCH", "FILTER", "SORT", "SORTBY", "UNIQUE", "SEQUENCE",
        "LET", "SUMIFS", "COUNTIFS", "INDEX", "MATCH", "PivotTable",
        "HelperColumn", "VBA", "SQLAggregation"
    ];

    private static readonly string[] DefaultSqlDenyPatterns =
    [
        @"\bINSERT\b",
        @"\bUPDATE\b",
        @"\bDELETE\b",
        @"\bMERGE\b",
        @"\bCREATE\b",
        @"\bALTER\b",
        @"\bDROP\b",
        @"\bTRUNCATE\b",
        @"\bGRANT\b",
        @"\bREVOKE\b",
        @"\bEXEC\b",
        @"\bCALL\b",
        @"\bCOMMIT\b",
        @"\bROLLBACK\b"
    ];

    private static readonly string[] DefaultSqlWarnPatterns =
    [
        @"SELECT\s+\*",
        @"WHERE\s+1\s*=\s*1",
        @"CROSS\s+JOIN",
        @"/\*\+"
    ];

    private static readonly string[] DefaultVbaDenyPatterns =
    [
        @"\bShell\b",
        @"WScript\.Shell",
        @"\bKill\b",
        @"Declare\s+PtrSafe",
        @"Scripting\.FileSystemObject|FileSystemObject",
        @"FollowHyperlink",
        @"WinHttp",
        @"MSXML2\.XMLHTTP",
        @"Outlook\.Application"
    ];

    private static readonly string[] DefaultVbaWarnPatterns =
    [
        @"REQUIRE_PRESENT:Option\s+Explicit",
        @"Application\.DisplayAlerts\s*=\s*False",
        @"Application\.EnableEvents\s*=\s*False"
    ];

    public static SafetyRuleSet LoadDefault()
    {
        return LoadFromDirectory(DefaultRulesDirectory);
    }

    public static SafetyRuleSet LoadFromDirectory(string relativeRulesDirectory)
    {
        if (!IsSafeRelativeDirectory(relativeRulesDirectory))
        {
            return CreateFallbackRuleSet($"Invalid rules directory '{relativeRulesDirectory}'. Only relative app-local directories are allowed.");
        }

        var rulesDirectory = ResolveRulesDirectory(relativeRulesDirectory);
        if (rulesDirectory is null)
        {
            return CreateFallbackRuleSet($"Rules directory '{relativeRulesDirectory}' was not found.");
        }

        try
        {
            var fileContents = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fileName in RequiredRuleFiles)
            {
                var filePath = Path.Combine(rulesDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    return CreateFallbackRuleSet($"Required rule file '{fileName}' was not found.");
                }

                fileContents[fileName] = File.ReadAllText(filePath, Encoding.UTF8);
            }

            var sqlDenyPatterns = ReadActiveLines(fileContents["sql_deny_patterns.txt"]).ToArray();
            var sqlWarnPatterns = ReadActiveLines(fileContents["sql_warn_patterns.txt"]).ToArray();
            var vbaDenyPatterns = ReadActiveLines(fileContents["vba_deny_patterns.txt"]).ToArray();
            var (vbaWarnPatterns, vbaRequiredPatterns) = SplitRequiredPresent(ReadActiveLines(fileContents["vba_warn_patterns.txt"]));
            var excelBlockedFunctions = ReadActiveLines(fileContents["excel_2021_blocked_functions.txt"]).ToArray();
            var excelPreferredFunctions = ReadActiveLines(fileContents["excel_2021_preferred_functions.txt"]).ToArray();

            ValidateMandatoryGroup("sql_deny_patterns.txt", sqlDenyPatterns);
            ValidateMandatoryGroup("vba_deny_patterns.txt", vbaDenyPatterns);
            ValidateMandatoryGroup("vba_warn_patterns.txt REQUIRE_PRESENT", vbaRequiredPatterns);
            ValidateMandatoryGroup("excel_2021_blocked_functions.txt", excelBlockedFunctions);

            var sqlDenyRules = BuildSqlRules(sqlDenyPatterns, SafetySeverity.Blocker, isDeny: true).ToArray();
            var sqlWarnRules = BuildSqlRules(sqlWarnPatterns, SafetySeverity.Medium, isDeny: false).ToArray();
            var vbaDenyRules = BuildVbaDenyRules(vbaDenyPatterns).ToArray();
            var vbaWarnRules = BuildVbaWarnRules(vbaWarnPatterns).ToArray();
            var vbaRequiredRules = BuildVbaRequiredRules(vbaRequiredPatterns).ToArray();

            ValidateRegexRules(sqlDenyRules, sqlWarnRules, vbaDenyRules, vbaWarnRules, vbaRequiredRules);

            return new SafetyRuleSet(
                sqlDenyRules,
                sqlWarnRules,
                vbaDenyRules,
                vbaWarnRules,
                vbaRequiredRules,
                excelBlockedFunctions,
                excelPreferredFunctions,
                ComputeRuleVersion(fileContents),
                UsedFallback: false,
                LoadWarnings: Array.Empty<string>());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or RegexParseException)
        {
            return CreateFallbackRuleSet($"Rule files could not be loaded safely: {ex.Message}");
        }
    }

    private static SafetyRuleSet CreateFallbackRuleSet(string warning)
    {
        var sqlDenyRules = BuildSqlRules(DefaultSqlDenyPatterns, SafetySeverity.Blocker, isDeny: true).ToArray();
        var sqlWarnRules = BuildSqlRules(DefaultSqlWarnPatterns, SafetySeverity.Medium, isDeny: false).ToArray();
        var (vbaWarnPatterns, vbaRequiredPatterns) = SplitRequiredPresent(DefaultVbaWarnPatterns);
        var vbaDenyRules = BuildVbaDenyRules(DefaultVbaDenyPatterns).ToArray();
        var vbaWarnRules = BuildVbaWarnRules(vbaWarnPatterns).ToArray();
        var vbaRequiredRules = BuildVbaRequiredRules(vbaRequiredPatterns).ToArray();

        var fallbackContents = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["excel_2021_blocked_functions.txt"] = string.Join('\n', DefaultExcelBlockedFunctions),
            ["excel_2021_preferred_functions.txt"] = string.Join('\n', DefaultExcelPreferredFunctions),
            ["sql_deny_patterns.txt"] = string.Join('\n', DefaultSqlDenyPatterns),
            ["sql_warn_patterns.txt"] = string.Join('\n', DefaultSqlWarnPatterns),
            ["vba_deny_patterns.txt"] = string.Join('\n', DefaultVbaDenyPatterns),
            ["vba_warn_patterns.txt"] = string.Join('\n', DefaultVbaWarnPatterns)
        };

        return new SafetyRuleSet(
            sqlDenyRules,
            sqlWarnRules,
            vbaDenyRules,
            vbaWarnRules,
            vbaRequiredRules,
            DefaultExcelBlockedFunctions,
            DefaultExcelPreferredFunctions,
            ComputeRuleVersion(fallbackContents),
            UsedFallback: true,
            LoadWarnings: [warning]);
    }

    private static bool IsSafeRelativeDirectory(string relativeRulesDirectory)
    {
        if (string.IsNullOrWhiteSpace(relativeRulesDirectory) || Path.IsPathRooted(relativeRulesDirectory))
        {
            return false;
        }

        var segments = relativeRulesDirectory.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.All(segment => segment != "." && segment != "..");
    }

    private static string? ResolveRulesDirectory(string relativeRulesDirectory)
    {
        var appBaseCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeRulesDirectory));
        if (Directory.Exists(appBaseCandidate))
        {
            return appBaseCandidate;
        }

        var currentDirectoryCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relativeRulesDirectory));
        if (Directory.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        return null;
    }

    private static void ValidateMandatoryGroup(string groupName, IReadOnlyCollection<string> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidDataException($"Mandatory rule group '{groupName}' has no active rules.");
        }
    }

    private static IEnumerable<string> ReadActiveLines(string content)
    {
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            yield return trimmed;
        }
    }

    private static (IReadOnlyList<string> WarnPatterns, IReadOnlyList<string> RequiredPatterns) SplitRequiredPresent(IEnumerable<string> patterns)
    {
        var warnPatterns = new List<string>();
        var requiredPatterns = new List<string>();

        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith(RequirePresentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                requiredPatterns.Add(pattern[RequirePresentPrefix.Length..].Trim());
            }
            else
            {
                warnPatterns.Add(pattern);
            }
        }

        return (warnPatterns, requiredPatterns);
    }

    private static IEnumerable<RulePattern> BuildSqlRules(IEnumerable<string> patterns, SafetySeverity defaultSeverity, bool isDeny)
    {
        foreach (var pattern in patterns)
        {
            yield return BuildSqlRule(pattern, defaultSeverity, isDeny);
        }
    }

    private static RulePattern BuildSqlRule(string pattern, SafetySeverity defaultSeverity, bool isDeny)
    {
        var normalized = pattern.ToUpperInvariant();
        if (isDeny)
        {
            foreach (var keyword in new[] { "INSERT", "UPDATE", "DELETE", "MERGE" })
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return new RulePattern($"SQL_DML_{keyword}", pattern, SafetySeverity.Blocker, $"조회 전용 원칙에 위배되는 {keyword}가 포함되어 있습니다.");
                }
            }

            foreach (var keyword in new[] { "CREATE", "ALTER", "DROP", "TRUNCATE" })
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return new RulePattern($"SQL_DDL_{keyword}", pattern, SafetySeverity.Blocker, $"{keyword} 명령은 초기 MVP에서 금지됩니다.");
                }
            }

            foreach (var keyword in new[] { "GRANT", "REVOKE" })
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return new RulePattern($"SQL_PRIVILEGE_{keyword}", pattern, SafetySeverity.Blocker, "권한 변경 명령은 금지됩니다.");
                }
            }

            foreach (var keyword in new[] { "EXEC", "CALL" })
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return new RulePattern($"SQL_EXEC_{keyword}", pattern, SafetySeverity.Blocker, "프로시저 실행 명령은 금지됩니다.");
                }
            }

            foreach (var keyword in new[] { "COMMIT", "ROLLBACK" })
            {
                if (normalized.Contains(keyword, StringComparison.Ordinal))
                {
                    return new RulePattern($"SQL_TX_{keyword}", pattern, SafetySeverity.Blocker, "트랜잭션 제어 명령은 초기 MVP에서 허용하지 않습니다.");
                }
            }

            return new RulePattern("SQL_DENY_PATTERN", pattern, SafetySeverity.Blocker, "조회 전용 원칙에 맞지 않는 SQL 패턴이 포함되어 있습니다.");
        }

        if (normalized.Contains("SELECT", StringComparison.Ordinal) && pattern.Contains(@"\*", StringComparison.Ordinal))
        {
            return new RulePattern("SQL_SELECT_STAR", pattern, SafetySeverity.Medium, "SELECT * 사용은 컬럼 변경 및 성능 리스크가 있으므로 명시 컬럼을 권장합니다.");
        }

        if (normalized.Contains("WHERE", StringComparison.Ordinal) && normalized.Contains("1", StringComparison.Ordinal))
        {
            return new RulePattern("SQL_WHERE_ALWAYS_TRUE", pattern, SafetySeverity.Medium, "WHERE 1=1 패턴은 조건 누락을 숨길 수 있어 검토가 필요합니다.");
        }

        if (normalized.Contains("CROSS", StringComparison.Ordinal) && normalized.Contains("JOIN", StringComparison.Ordinal))
        {
            return new RulePattern("SQL_CROSS_JOIN", pattern, SafetySeverity.Medium, "CROSS JOIN은 결과 건수를 급증시킬 수 있어 검토가 필요합니다.");
        }

        if (pattern.Contains("/\\*\\+", StringComparison.Ordinal))
        {
            return new RulePattern("SQL_OPTIMIZER_HINT", pattern, SafetySeverity.Medium, "옵티마이저 힌트는 운영 DB 부하 리스크가 있어 검토가 필요합니다.");
        }

        return new RulePattern("SQL_WARN_PATTERN", pattern, defaultSeverity, "SQL 경고 패턴이 포함되어 있습니다.");
    }

    private static IEnumerable<RulePattern> BuildVbaDenyRules(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            var normalized = pattern.ToUpperInvariant();
            if (normalized.Contains("SHELL", StringComparison.Ordinal) && !normalized.Contains("WSCRIPT", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_SHELL", pattern, SafetySeverity.Blocker, "외부 프로그램 실행 가능성이 있는 Shell 호출은 금지됩니다.");
            }
            else if (normalized.Contains("WSCRIPT", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_WSCRIPT", pattern, SafetySeverity.Blocker, "WScript.Shell 호출은 금지됩니다.");
            }
            else if (normalized.Contains("KILL", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_KILL", pattern, SafetySeverity.Blocker, "파일 삭제 명령 Kill은 금지됩니다.");
            }
            else if (normalized.Contains("DECLARE", StringComparison.Ordinal) || normalized.Contains("PTRSAFE", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_WINAPI", pattern, SafetySeverity.High, "WinAPI 호출 가능성이 있어 검토가 필요합니다.");
            }
            else if (normalized.Contains("FILESYSTEMOBJECT", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_FSO", pattern, SafetySeverity.High, "FileSystemObject 사용은 파일 삭제/이동 가능성이 있어 검토가 필요합니다.");
            }
            else if (normalized.Contains("FOLLOWHYPERLINK", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_FOLLOW_HYPERLINK", pattern, SafetySeverity.Blocker, "외부 링크 실행 가능성이 있는 FollowHyperlink 호출은 금지됩니다.");
            }
            else if (normalized.Contains("WINHTTP", StringComparison.Ordinal) || normalized.Contains("XMLHTTP", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_HTTP", pattern, SafetySeverity.Blocker, "외부 네트워크 호출 가능성이 있는 HTTP 객체는 금지됩니다.");
            }
            else if (normalized.Contains("OUTLOOK", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_OUTLOOK", pattern, SafetySeverity.High, "Outlook 자동 발송 가능성이 있어 검토가 필요합니다.");
            }
            else
            {
                yield return new RulePattern("VBA_DENY_PATTERN", pattern, SafetySeverity.Blocker, "위험한 VBA 패턴이 포함되어 있습니다.");
            }
        }
    }

    private static IEnumerable<RulePattern> BuildVbaWarnRules(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            var normalized = pattern.ToUpperInvariant();
            if (normalized.Contains("DISPLAYALERTS", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_DISPLAY_ALERTS_DISABLED", pattern, SafetySeverity.Medium, "Application.DisplayAlerts 비활성화 후 원복 여부를 확인해야 합니다.");
            }
            else if (normalized.Contains("ENABLEEVENTS", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_ENABLE_EVENTS_DISABLED", pattern, SafetySeverity.Medium, "Application.EnableEvents 비활성화 후 원복 여부를 확인해야 합니다.");
            }
            else
            {
                yield return new RulePattern("VBA_WARN_PATTERN", pattern, SafetySeverity.Medium, "VBA 경고 패턴이 포함되어 있습니다.");
            }
        }
    }

    private static IEnumerable<RulePattern> BuildVbaRequiredRules(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            var normalized = pattern.ToUpperInvariant();
            if (normalized.Contains("OPTION", StringComparison.Ordinal) && normalized.Contains("EXPLICIT", StringComparison.Ordinal))
            {
                yield return new RulePattern("VBA_OPTION_EXPLICIT_MISSING", pattern, SafetySeverity.Medium, "Option Explicit이 누락되었습니다.");
            }
            else
            {
                yield return new RulePattern("VBA_REQUIRED_PATTERN_MISSING", pattern, SafetySeverity.Medium, "필수 VBA 패턴이 누락되었습니다.");
            }
        }
    }

    private static void ValidateRegexRules(params IReadOnlyList<RulePattern>[] ruleGroups)
    {
        foreach (var rule in ruleGroups.SelectMany(group => group))
        {
            _ = new Regex(rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }

    private static string ComputeRuleVersion(SortedDictionary<string, string> fileContents)
    {
        var builder = new StringBuilder();
        foreach (var (fileName, content) in fileContents)
        {
            builder.Append(fileName).Append('\n').Append(content).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return RuleVersionPrefix + hex[..12];
    }
}

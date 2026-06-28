internal sealed class SmokeTestContext
{
    private int failed;
    private int passed;
    private readonly SortedDictionary<string, int> smokeDomainPass = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> smokeDomainFail = new(StringComparer.Ordinal);
    private readonly List<string> unclassifiedNames = new();
    private readonly System.Diagnostics.Stopwatch smokeStopwatch = System.Diagnostics.Stopwatch.StartNew();

    public void AssertTrue(bool condition, string name)
    {
        var domain = SmokeDomain(name);
        if (domain == "Unclassified")
        {
            unclassifiedNames.Add(name);
        }

        if (condition)
        {
            Console.WriteLine($"PASS: {name}");
            passed++;
            smokeDomainPass[domain] = smokeDomainPass.TryGetValue(domain, out var p) ? p + 1 : 1;
        }
        else
        {
            Console.WriteLine($"FAIL: {name}");
            failed++;
            smokeDomainFail[domain] = smokeDomainFail.TryGetValue(domain, out var f) ? f + 1 : 1;
        }
    }

    public bool Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }

    public int PrintSummaryAndGetExitCode()
    {
        smokeStopwatch.Stop();
        var smokeTotal = passed + failed;
        Console.WriteLine();
        Console.WriteLine("=== SmokeTest Summary ===");
        Console.WriteLine($"Total={smokeTotal} PASS={passed} FAIL={failed} Duration={smokeStopwatch.Elapsed.TotalSeconds:F2}s");
        foreach (var domain in smokeDomainPass.Keys.Union(smokeDomainFail.Keys).OrderBy(k => k, StringComparer.Ordinal))
        {
            var p = smokeDomainPass.TryGetValue(domain, out var pv) ? pv : 0;
            var f = smokeDomainFail.TryGetValue(domain, out var fv) ? fv : 0;
            Console.WriteLine($"  {domain}: PASS={p} FAIL={f}");
        }
        Console.WriteLine("=========================");

        var unclassified = (smokeDomainPass.TryGetValue("Unclassified", out var up) ? up : 0)
            + (smokeDomainFail.TryGetValue("Unclassified", out var uf) ? uf : 0);
        if (unclassified > 0)
        {
            Console.WriteLine($"SmokeTest domain classification failed: Unclassified={unclassified}");
            foreach (var name in unclassifiedNames.OrderBy(name => name, StringComparer.Ordinal))
            {
                Console.WriteLine($"  UNCLASSIFIED: {name}");
            }

            return 1;
        }

        if (failed > 0)
        {
            Console.WriteLine($"SmokeTests failed: {failed}");
            return 1;
        }

        Console.WriteLine($"All SmokeTests passed. (Total={smokeTotal})");
        return 0;
    }

    private static string SmokeDomain(string name)
    {
        bool Has(params string[] keys) => keys.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (Has("XlsxReader", ".xlsx", "xlsx")) return "Xlsx";
        if (Has("CsvReader", "CP949", "UTF-8", "UTF8", "BOM", "encoding", "CSV parser")) return "Csv";
        if (Has("ColumnMapping", "mapping", "mapped", "renamed", "physical column")) return "Mapping";
        if (Has("Reconcil", "RECON", "원천합계", "analysis balance", "sum balance", "row amplification", "orphan limit", "duplicate limit", "base-date mismatch")) return "Reconciliation";
        if (Has("ExcelReport", "Excel report", "ReportBuilder", "report ", "report-side", "LIMIT_MONITORING", "EXCEPTION_LIST", "SUMMARY", "templates/report")) return "Report";
        if (Has("LimitMonitor", "limit", "한도", "exposure", "BASE_DT", "6상태", "NO_LIMIT", "INVALID_LIMIT", "BREACH", "WARNING", "MAPPING_ERROR", "usage ratio")) return "Limit";
        if (Has("Ncr", "NCR Rule", "NCR 공식", "Rule Set")) return "Ncr";
        if (Has("KbIndex", "KbSearch", "RegulationCatalog", "Regulation", "catalog", "citation", "document", "source locator", "source text", "license", "approval", "metadata", "인용", "검색", "원문", "공개")) return "Kb";
        if (Has("build/0", "VERSION", "global.json", "packaging", "source-text", "KbRepositoryGuard", "manifest", "Expand-Archive", "PowerShell")) return "Packaging";
        if (Has("TaskLog", "FeedbackLog", "Audit", "Feedback", "PromotedExample", "ExamplePromotion", "user id", "request hash", "raw request")) return "Audit";
        if (Has("NoModelDraftService", "DraftPipeline", "draft", "NoModel", "NO_MODEL", "generated draft")) return "Generation";
        if (Has("DashboardSnapshot", "SecuritySettingsSnapshot", "SettingsSnapshot", "Offline Mode", "Local Model", "Promoted Examples", "Reports")) return "UiContract";
        if (Has("DataProfiler", "profile", "null values", "duplicate rows", "numeric", "Small profile", "BASE_DT distribution", "source file name")) return "DataProfile";
        if (Has("UI ", "UI shell", "Left menu", "Main tab", "MainTabKey", "navigation", "screen", "snapshot", "MVP-3", "Risk Dashboard", "History", "Settings", "Feedback Center", "read-only status refresh")) return "UiContract";
        if (Has("RuleLoader", "RuleSet", "rules", "SQL", "SELECT", "VBA", "Option Explicit", "Excel 2021", "Excel2021", "PolicyLoader", "Security policy", "External API", "auto update", "telemetry", "auto execution", "safe fallback", "checker", "finding", "DEMO_ONLY")) return "Safety";
        return "Unclassified";
    }
}
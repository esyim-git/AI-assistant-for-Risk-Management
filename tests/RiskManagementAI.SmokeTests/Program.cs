using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Safety;

var failed = 0;

void AssertTrue(bool condition, string name)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {name}");
    }
    else
    {
        Console.WriteLine($"FAIL: {name}");
        failed++;
    }
}

var loadedRuleSet = RuleLoader.LoadDefault();
AssertTrue(!loadedRuleSet.UsedFallback, "RuleLoader should load repo rules");
AssertTrue(loadedRuleSet.RuleVersion.StartsWith("ruleset-", StringComparison.Ordinal) && loadedRuleSet.RuleVersion.Length == 20, "RuleLoader should produce deterministic ruleset version");
AssertTrue(loadedRuleSet.SqlDenyRules.Any(r => r.Code == "SQL_DML_DELETE"), "RuleLoader should map SQL deny patterns");
AssertTrue(loadedRuleSet.VbaRequiredPresentRules.Any(r => r.Code == "VBA_OPTION_EXPLICIT_MISSING"), "RuleLoader should map REQUIRE_PRESENT rules");
AssertTrue(loadedRuleSet.ExcelBlockedFunctions.Contains("MAP"), "RuleLoader should load Excel blocked functions");

var sqlChecker = new SqlSafetyChecker(loadedRuleSet);
var sqlFindings = sqlChecker.Check("DELETE FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
AssertTrue(sqlFindings.Any(f => f.Code == "SQL_DML_DELETE"), "SQL DELETE should be blocked");

var selectFindings = sqlChecker.Check("SELECT TRADE_ID, BASE_DT FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
AssertTrue(selectFindings.All(f => f.Severity != SafetySeverity.Blocker), "SELECT should not have blocker");

var sqlDenySamples = new Dictionary<string, string>
{
    ["INSERT"] = "INSERT INTO TRADE_SAMPLE (ID) VALUES (1)",
    ["UPDATE"] = "UPDATE TRADE_SAMPLE SET AMT = 0",
    ["DELETE"] = "DELETE FROM TRADE_SAMPLE",
    ["MERGE"] = "MERGE INTO TRADE_SAMPLE USING SRC ON (1=1)",
    ["CREATE"] = "CREATE TABLE TMP_SAMPLE (ID NUMBER)",
    ["ALTER"] = "ALTER TABLE TRADE_SAMPLE ADD TMP_COL NUMBER",
    ["DROP"] = "DROP TABLE TRADE_SAMPLE",
    ["TRUNCATE"] = "TRUNCATE TABLE TRADE_SAMPLE",
    ["GRANT"] = "GRANT SELECT ON TRADE_SAMPLE TO USER_SAMPLE",
    ["REVOKE"] = "REVOKE SELECT ON TRADE_SAMPLE FROM USER_SAMPLE",
    ["EXEC"] = "EXEC PROC_SAMPLE",
    ["CALL"] = "CALL PROC_SAMPLE()",
    ["COMMIT"] = "COMMIT",
    ["ROLLBACK"] = "ROLLBACK"
};

foreach (var (keyword, sql) in sqlDenySamples)
{
    var findings = sqlChecker.Check(sql).ToList();
    AssertTrue(findings.Any(f => f.Severity == SafetySeverity.Blocker && string.Equals(f.MatchedText, keyword, StringComparison.OrdinalIgnoreCase)), $"SQL {keyword} should be blocker");
}

AssertTrue(sqlChecker.Check("SELECT * FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_SELECT_STAR" && f.Severity == SafetySeverity.Medium), "SQL SELECT * should warn");
AssertTrue(sqlChecker.Check("SELECT TRADE_ID FROM TRADE_SAMPLE WHERE 1=1").Any(f => f.Code == "SQL_WHERE_ALWAYS_TRUE"), "SQL WHERE 1=1 should warn");
AssertTrue(sqlChecker.Check("SELECT A.ID FROM A CROSS JOIN B").Any(f => f.Code == "SQL_CROSS_JOIN"), "SQL CROSS JOIN should warn");
AssertTrue(sqlChecker.Check("SELECT /*+ INDEX(A IDX_A) */ A.ID FROM A").Any(f => f.Code == "SQL_OPTIMIZER_HINT"), "SQL optimizer hint should warn");

var vbaChecker = new VbaSafetyChecker(loadedRuleSet);
var vbaFindings = vbaChecker.Check("Sub Test()\nShell \"cmd.exe\"\nEnd Sub").ToList();
AssertTrue(vbaFindings.Any(f => f.Code == "VBA_SHELL"), "VBA Shell should be blocked");
AssertTrue(vbaFindings.Any(f => f.Code == "VBA_OPTION_EXPLICIT_MISSING"), "Option Explicit missing should warn");

var excelChecker = new Excel2021FunctionChecker(loadedRuleSet);
var excelFindings = excelChecker.CheckFormula("=VSTACK(A1:A3,B1:B3)").ToList();
AssertTrue(excelFindings.Any(f => f.Code == "EXCEL_365_FUNCTION"), "VSTACK should be blocked for Excel 2021");

var customRulesDirectory = Path.Combine("artifacts", "smoke-rules-b01");
if (Directory.Exists(customRulesDirectory))
{
    Directory.Delete(customRulesDirectory, recursive: true);
}

Directory.CreateDirectory(customRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(customRulesDirectory, Path.GetFileName(sourceFile)));
}

File.AppendAllText(Path.Combine(customRulesDirectory, "sql_deny_patterns.txt"), "\n\\bVACUUM\\b\n");
var customRuleSet = RuleLoader.LoadFromDirectory(customRulesDirectory);
var customRuleFindings = new SqlSafetyChecker(customRuleSet).Check("VACUUM TABLE TRADE_SAMPLE").ToList();
AssertTrue(!customRuleSet.UsedFallback, "Custom smoke rules should load without fallback");
AssertTrue(customRuleFindings.Any(f => f.Code == "SQL_DENY_PATTERN" && string.Equals(f.MatchedText, "VACUUM", StringComparison.OrdinalIgnoreCase)), "Temporary SQL rule file pattern should be injected");

var fallbackRuleSet = RuleLoader.LoadFromDirectory(Path.Combine("artifacts", "missing-rules-b01"));
var fallbackFindings = new SqlSafetyChecker(fallbackRuleSet).Check("DELETE FROM TRADE_SAMPLE").ToList();
AssertTrue(fallbackRuleSet.UsedFallback, "Missing rules directory should use fallback");
AssertTrue(fallbackFindings.Any(f => f.Code == "RULESET_FALLBACK"), "Fallback use should be reported as finding");
AssertTrue(fallbackFindings.Any(f => f.Code == "SQL_DML_DELETE"), "Fallback rules should still block SQL DELETE");

if (failed > 0)
{
    Console.WriteLine($"SmokeTests failed: {failed}");
    Environment.Exit(1);
}

Console.WriteLine("All SmokeTests passed.");

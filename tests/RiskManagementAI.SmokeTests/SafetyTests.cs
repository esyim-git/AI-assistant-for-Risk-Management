internal static class SafetyTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
context.AssertTrue(!loadedRuleSet.UsedFallback, "RuleLoader should load repo rules");
context.AssertTrue(loadedRuleSet.RuleVersion.StartsWith("ruleset-", StringComparison.Ordinal) && loadedRuleSet.RuleVersion.Length == 20, "RuleLoader should produce deterministic ruleset version");
context.AssertTrue(loadedRuleSet.SqlDenyRules.Any(r => r.Code == "SQL_DML_DELETE"), "RuleLoader should map SQL deny patterns");
context.AssertTrue(loadedRuleSet.VbaRequiredPresentRules.Any(r => r.Code == "VBA_OPTION_EXPLICIT_MISSING"), "RuleLoader should map REQUIRE_PRESENT rules");
context.AssertTrue(loadedRuleSet.ExcelBlockedFunctions.Contains("MAP"), "RuleLoader should load Excel blocked functions");
context.AssertTrue(loadedRuleSet.ExcelCompletionAllowFunctions.Contains("XLOOKUP") && !loadedRuleSet.ExcelCompletionAllowFunctions.Contains("PivotTable"), "RuleLoader should load Excel completion allow rules without non-function labels");

var sqlChecker = new SqlSafetyChecker(loadedRuleSet);
var sqlFindings = sqlChecker.Check("DELETE FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
context.AssertTrue(sqlFindings.Any(f => f.Code == "SQL_DML_DELETE"), "SQL DELETE should be blocked");

var selectFindings = sqlChecker.Check("SELECT TRADE_ID, BASE_DT FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
context.AssertTrue(selectFindings.All(f => f.Severity != SafetySeverity.Blocker), "SELECT should not have blocker");

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
    context.AssertTrue(findings.Any(f => f.Severity == SafetySeverity.Blocker && string.Equals(f.MatchedText, keyword, StringComparison.OrdinalIgnoreCase)), $"SQL {keyword} should be blocker");
}

var sqlBoundarySamples = new Dictionary<string, (string Sql, string MatchedText)>
{
    ["mixed case leading whitespace"] = ("\r\n\tdeLeTe FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT", "deLeTe"),
    ["line comment hidden keyword"] = ("SELECT TRADE_ID FROM TRADE_SAMPLE -- DELETE hidden\nWHERE BASE_DT = :BASE_DT", "DELETE"),
    ["block comment hidden keyword"] = ("SELECT TRADE_ID FROM TRADE_SAMPLE /* DROP hidden */ WHERE BASE_DT = :BASE_DT", "DROP"),
    ["multi statement second keyword"] = ("SELECT TRADE_ID FROM TRADE_SAMPLE; UPDATE TRADE_SAMPLE SET AMT = 0", "UPDATE")
};

foreach (var (label, sample) in sqlBoundarySamples)
{
    var findings = sqlChecker.Check(sample.Sql).ToList();
    context.AssertTrue(
        findings.Any(f => f.Severity == SafetySeverity.Blocker && string.Equals(f.MatchedText, sample.MatchedText, StringComparison.OrdinalIgnoreCase)),
        $"SQL checker boundary {label} should detect hidden blocker");
}

context.AssertTrue(sqlChecker.Check("   \r\n\t").Any(f => f.Code == "SQL_EMPTY" && f.Severity == SafetySeverity.Low), "SQL checker empty whitespace should return graceful finding");
context.AssertTrue(
    sqlChecker.Check("SELECT UPDATED_AT, CALL_AMOUNT FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").All(f => f.Severity != SafetySeverity.Blocker),
    "SQL checker should not block keywords embedded inside safe SELECT identifiers");
context.AssertTrue(
    sqlChecker.Check("SELECT " + new string(' ', 4096) + "TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").All(f => f.Severity != SafetySeverity.Blocker),
    "SQL checker long safe SELECT should remain graceful");

context.AssertTrue(sqlChecker.Check("SELECT * FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_SELECT_STAR" && f.Severity == SafetySeverity.Medium), "SQL SELECT * should warn");
context.AssertTrue(sqlChecker.Check("SELECT TRADE_ID FROM TRADE_SAMPLE WHERE 1=1").Any(f => f.Code == "SQL_WHERE_ALWAYS_TRUE"), "SQL WHERE 1=1 should warn");
context.AssertTrue(sqlChecker.Check("SELECT A.ID FROM A CROSS JOIN B").Any(f => f.Code == "SQL_CROSS_JOIN"), "SQL CROSS JOIN should warn");
context.AssertTrue(sqlChecker.Check("SELECT /*+ INDEX(A IDX_A) */ A.ID FROM A").Any(f => f.Code == "SQL_OPTIMIZER_HINT"), "SQL optimizer hint should warn");

var vbaChecker = new VbaSafetyChecker(loadedRuleSet);
var vbaFindings = vbaChecker.Check("Sub Test()\nShell \"cmd.exe\"\nEnd Sub").ToList();
context.AssertTrue(vbaFindings.Any(f => f.Code == "VBA_SHELL"), "VBA Shell should be blocked");
context.AssertTrue(vbaFindings.Any(f => f.Code == "VBA_OPTION_EXPLICIT_MISSING"), "Option Explicit missing should warn");

var vbaDenySamples = new Dictionary<string, (string Code, string Text)>
{
    ["Shell"] = ("VBA_SHELL", "Option Explicit\nSub Test()\nShell \"cmd.exe\"\nEnd Sub"),
    ["WScript.Shell"] = ("VBA_WSCRIPT", "Option Explicit\nSub Test()\nCreateObject(\"WScript.Shell\")\nEnd Sub"),
    ["Kill"] = ("VBA_KILL", "Option Explicit\nSub Test()\nKill \"C:\\temp\\sample.txt\"\nEnd Sub"),
    ["FileSystemObject"] = ("VBA_FSO", "Option Explicit\nSub Test()\nDim fso As FileSystemObject\nEnd Sub"),
    ["Declare PtrSafe"] = ("VBA_WINAPI", "Option Explicit\nDeclare PtrSafe Function GetTickCount Lib \"kernel32\" () As Long\nSub Test()\nEnd Sub"),
    ["Outlook.Application"] = ("VBA_OUTLOOK", "Option Explicit\nSub Test()\nCreateObject(\"Outlook.Application\")\nEnd Sub"),
    ["WinHttp"] = ("VBA_HTTP", "Option Explicit\nSub Test()\nCreateObject(\"WinHttp.WinHttpRequest.5.1\")\nEnd Sub"),
    ["MSXML2.XMLHTTP"] = ("VBA_HTTP", "Option Explicit\nSub Test()\nCreateObject(\"MSXML2.XMLHTTP\")\nEnd Sub"),
    ["FollowHyperlink"] = ("VBA_FOLLOW_HYPERLINK", "Option Explicit\nSub Test()\nActiveWorkbook.FollowHyperlink \"https://example.invalid\"\nEnd Sub")
};

foreach (var (label, sample) in vbaDenySamples)
{
    var findings = vbaChecker.Check(sample.Text).ToList();
    context.AssertTrue(findings.Any(f => f.Code == sample.Code), $"VBA {label} should be detected");
    context.AssertTrue(findings.All(f => f.Code != "VBA_OPTION_EXPLICIT_MISSING"), $"VBA {label} with Option Explicit should not warn missing Option Explicit");
}

context.AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\n    sHeLl \"cmd.exe\"\nEnd Sub").Any(f => f.Code == "VBA_SHELL" && f.Severity == SafetySeverity.Blocker), "VBA checker mixed case Shell should be blocked");
context.AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\n    ' WScript.Shell hidden\nEnd Sub").Any(f => f.Code == "VBA_WSCRIPT"), "VBA checker comment-hidden WScript.Shell should be detected");
context.AssertTrue(vbaChecker.Check("   \r\n\t").Any(f => f.Code == "VBA_EMPTY" && f.Severity == SafetySeverity.Low), "VBA checker empty whitespace should return graceful finding");
context.AssertTrue(
    vbaChecker.Check("Option Explicit\nSub Test()\n    Dim shellValue As String\n    shellValue = \"review only\"\nEnd Sub").All(f => f.Severity != SafetySeverity.Blocker),
    "VBA checker should not block safe identifiers that contain denied words");

context.AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.DisplayAlerts = False\nEnd Sub").Any(f => f.Code == "VBA_DISPLAY_ALERTS_DISABLED"), "VBA DisplayAlerts false should warn");
context.AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.EnableEvents = False\nEnd Sub").Any(f => f.Code == "VBA_ENABLE_EVENTS_DISABLED"), "VBA EnableEvents false should warn");

var excelChecker = new Excel2021FunctionChecker(loadedRuleSet);
var excelFindings = excelChecker.CheckFormula("=VSTACK(A1:A3,B1:B3)").ToList();
context.AssertTrue(excelFindings.Any(f => f.Code == "EXCEL_365_FUNCTION"), "VSTACK should be blocked for Excel 2021");
context.AssertTrue(excelFindings.Any(f => f.Message.Contains("XLOOKUP", StringComparison.OrdinalIgnoreCase)), "Excel blocked function finding should include preferred function guidance");

foreach (var functionName in loadedRuleSet.ExcelBlockedFunctions)
{
    var findings = excelChecker.CheckFormula($"={functionName}(A1:A3)").ToList();
    context.AssertTrue(findings.Any(f => f.Code == "EXCEL_365_FUNCTION"), $"Excel 2021 blocked function {functionName} should be detected");
}

var allowedExcelFindings = excelChecker.CheckFormula("=XLOOKUP(A1,B:B,C:C)").ToList();
context.AssertTrue(allowedExcelFindings.All(f => f.Code != "EXCEL_365_FUNCTION"), "Excel 2021 preferred function XLOOKUP should be allowed");
context.AssertTrue(excelChecker.CheckFormula("=textsplit(A1,\",\")").Any(f => f.Code == "EXCEL_365_FUNCTION" && f.Severity == SafetySeverity.High), "Excel 2021 checker lowercase TEXTSPLIT should be blocked");
context.AssertTrue(excelChecker.CheckFormula("=IF(TRUE,TEXTAFTER(A1,\"-\"),\"\")").Any(f => f.Code == "EXCEL_365_FUNCTION"), "Excel 2021 checker nested TEXTAFTER should be blocked");
context.AssertTrue(!excelChecker.CheckFormula("TEXTSPLIT").Any(f => f.Code == "EXCEL_365_FUNCTION"), "Excel 2021 checker should not block function name without call syntax");
context.AssertTrue(!excelChecker.CheckFormula("   ").Any(), "Excel 2021 checker empty whitespace should return no finding");

var excelFunctionHelper = new ExcelFunctionHelper(loadedRuleSet);
var xlookupInfo = excelFunctionHelper.Lookup("=xlookup(");
context.AssertTrue(
    xlookupInfo is not null
    && xlookupInfo.Name == "XLOOKUP"
    && !xlookupInfo.Is365Only
    && xlookupInfo.Recommended
    && !string.IsNullOrWhiteSpace(xlookupInfo.Description)
    && !string.IsNullOrWhiteSpace(xlookupInfo.Args)
    && !string.IsNullOrWhiteSpace(xlookupInfo.RiskMgmtExample)
    && !string.IsNullOrWhiteSpace(xlookupInfo.FormulaExample)
    && !string.IsNullOrWhiteSpace(xlookupInfo.Excel2021Alternative),
    "Excel 2021 Function Helper should return complete metadata for XLOOKUP");

var lookupSearch = excelFunctionHelper.Search("lookup").ToList();
context.AssertTrue(
    lookupSearch.Count >= 2
    && lookupSearch[0].Name == "XLOOKUP"
    && lookupSearch.SequenceEqual(excelFunctionHelper.Search("lookup")),
    "Excel 2021 Function Helper search should be deterministic and prioritize name matches");

var textSplitInfo = excelFunctionHelper.Lookup("TEXTSPLIT");
context.AssertTrue(
    textSplitInfo is not null
    && textSplitInfo.Is365Only
    && !textSplitInfo.Recommended
    && textSplitInfo.Excel2021Alternative.Contains("XLOOKUP", StringComparison.OrdinalIgnoreCase)
    && textSplitInfo.Excel2021Alternative.Contains("HelperColumn", StringComparison.OrdinalIgnoreCase),
    "Excel 2021 Function Helper should derive 365-only status and alternatives from active ruleset");
context.AssertTrue(
    textSplitInfo is not null
    && excelChecker.CheckFormula(textSplitInfo.FormulaExample).Any(f => f.Code == "EXCEL_365_FUNCTION"),
    "Excel 2021 Function Helper 365-only formula examples should remain blocked by Excel2021FunctionChecker");

var malformedExcelFunctionHelper = new ExcelFunctionHelper(loadedRuleSet, "{ broken json");
context.AssertTrue(
    malformedExcelFunctionHelper.Warnings.Count == 1
    && malformedExcelFunctionHelper.Lookup("XLOOKUP") is null
    && malformedExcelFunctionHelper.Search("XLOOKUP").Count == 0,
    "Excel 2021 Function Helper should safe-fallback to empty helper on embedded resource parse failure");
context.AssertTrue(
    xlookupInfo is not null
    && excelFunctionHelper.BuildFormulaInsertion(xlookupInfo) == xlookupInfo.FormulaExample,
    "Excel 2021 Function Helper insertion text should be available only through explicit caller invocation");

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
context.AssertTrue(!customRuleSet.UsedFallback, "Custom smoke rules should load without fallback");
context.AssertTrue(customRuleFindings.Any(f => f.Code == "SQL_DENY_PATTERN" && string.Equals(f.MatchedText, "VACUUM", StringComparison.OrdinalIgnoreCase)), "Temporary SQL rule file pattern should be injected");

var versionDriftRulesDirectory = Path.Combine("artifacts", "smoke-rules-b09-version");
if (Directory.Exists(versionDriftRulesDirectory))
{
    Directory.Delete(versionDriftRulesDirectory, recursive: true);
}

Directory.CreateDirectory(versionDriftRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(versionDriftRulesDirectory, Path.GetFileName(sourceFile)));
}

var versionBeforeRuleChange = RuleLoader.LoadFromDirectory(versionDriftRulesDirectory);
File.AppendAllText(Path.Combine(versionDriftRulesDirectory, "sql_warn_patterns.txt"), "\nFULL\\s+OUTER\\s+JOIN\n");
var versionAfterRuleChange = RuleLoader.LoadFromDirectory(versionDriftRulesDirectory);
context.AssertTrue(versionBeforeRuleChange.RuleVersion != versionAfterRuleChange.RuleVersion, "RuleLoader ruleset version should change when rule content changes");

var emptyMandatoryRulesDirectory = Path.Combine("artifacts", "smoke-rules-b09-empty-mandatory");
if (Directory.Exists(emptyMandatoryRulesDirectory))
{
    Directory.Delete(emptyMandatoryRulesDirectory, recursive: true);
}

Directory.CreateDirectory(emptyMandatoryRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(emptyMandatoryRulesDirectory, Path.GetFileName(sourceFile)));
}

File.WriteAllText(Path.Combine(emptyMandatoryRulesDirectory, "sql_deny_patterns.txt"), "# accidental truncation\n\n");
var emptyMandatoryRuleSet = RuleLoader.LoadFromDirectory(emptyMandatoryRulesDirectory);
context.AssertTrue(emptyMandatoryRuleSet.UsedFallback, "RuleLoader should fallback when a mandatory rule group is empty");
context.AssertTrue(new SqlSafetyChecker(emptyMandatoryRuleSet).Check("DELETE FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_DML_DELETE"), "Fallback after empty mandatory rules should still block DELETE");

var fallbackRuleSet = RuleLoader.LoadFromDirectory(Path.Combine("artifacts", "missing-rules-b01"));
var fallbackFindings = new SqlSafetyChecker(fallbackRuleSet).Check("DELETE FROM TRADE_SAMPLE").ToList();
context.AssertTrue(fallbackRuleSet.UsedFallback, "Missing rules directory should use fallback");
context.AssertTrue(fallbackFindings.Any(f => f.Code == "RULESET_FALLBACK"), "Fallback use should be reported as finding");
context.AssertTrue(fallbackFindings.Any(f => f.Code == "SQL_DML_DELETE"), "Fallback rules should still block SQL DELETE");
context.AssertTrue(
    new VbaSafetyChecker(fallbackRuleSet)
        .Check("Option Explicit\nSub Test()\nDim fso As FileSystemObject\nEnd Sub")
        .Any(f => f.Code == "VBA_FSO"),
    "Fallback rules should detect direct FileSystemObject declarations");

var policyLoadResult = PolicyLoader.LoadDefault();
context.AssertTrue(!policyLoadResult.UsedFallback, "PolicyLoader should load repo security policy");
context.AssertTrue(!policyLoadResult.Policy.Network.AllowExternalApi, "Security policy should block external API");
context.AssertTrue(!policyLoadResult.Policy.Network.AllowAutoUpdate, "Security policy should block auto update");
context.AssertTrue(!policyLoadResult.Policy.Network.AllowTelemetry, "Security policy should block telemetry");
context.AssertTrue(!policyLoadResult.Policy.Sql.AllowAutoExecute, "Security policy should block SQL auto execution");
context.AssertTrue(!policyLoadResult.Policy.Vba.AllowAutoExecute, "Security policy should block VBA auto execution");
context.AssertTrue(context.Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureExternalApiAllowed()), "Security policy should enforce external API block");
context.AssertTrue(context.Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureSqlAutoExecuteAllowed()), "Security policy should enforce SQL auto-execute block");
context.AssertTrue(context.Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureVbaAutoExecuteAllowed()), "Security policy should enforce VBA auto-execute block");

var missingPolicyResult = PolicyLoader.LoadFromFile("config/missing_policy_smoke.json");
context.AssertTrue(missingPolicyResult.UsedFallback, "Missing security policy should use safe fallback");
context.AssertTrue(!missingPolicyResult.Policy.Network.AllowExternalApi && !missingPolicyResult.Policy.Sql.AllowAutoExecute, "Security policy fallback should keep dangerous actions disabled");
context.AssertTrue(PolicyLoader.LoadFromFile("../security_policy.json").UsedFallback, "PolicyLoader should reject non-config-relative paths");

var invalidPolicyPath = Path.Combine("config", "smoke_invalid_policy.json");
File.WriteAllText(invalidPolicyPath, "{ invalid json");
var invalidPolicyResult = PolicyLoader.LoadFromFile(invalidPolicyPath);
File.Delete(invalidPolicyPath);
context.AssertTrue(invalidPolicyResult.UsedFallback && !invalidPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on invalid JSON policy");

var nullSectionPolicyPath = Path.Combine("config", "smoke_null_section_policy.json");
File.WriteAllText(nullSectionPolicyPath, "{ \"Network\": null }");
var nullSectionPolicyResult = PolicyLoader.LoadFromFile(nullSectionPolicyPath);
File.Delete(nullSectionPolicyPath);
context.AssertTrue(nullSectionPolicyResult.UsedFallback && !nullSectionPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on null policy sections");
    }
}

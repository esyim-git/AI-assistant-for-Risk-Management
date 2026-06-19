using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Feedback;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Kb;
using RiskManagementAI.Core.Logging;
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

bool Throws<TException>(Action action)
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
    AssertTrue(findings.Any(f => f.Code == sample.Code), $"VBA {label} should be detected");
    AssertTrue(findings.All(f => f.Code != "VBA_OPTION_EXPLICIT_MISSING"), $"VBA {label} with Option Explicit should not warn missing Option Explicit");
}

AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.DisplayAlerts = False\nEnd Sub").Any(f => f.Code == "VBA_DISPLAY_ALERTS_DISABLED"), "VBA DisplayAlerts false should warn");
AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.EnableEvents = False\nEnd Sub").Any(f => f.Code == "VBA_ENABLE_EVENTS_DISABLED"), "VBA EnableEvents false should warn");

var excelChecker = new Excel2021FunctionChecker(loadedRuleSet);
var excelFindings = excelChecker.CheckFormula("=VSTACK(A1:A3,B1:B3)").ToList();
AssertTrue(excelFindings.Any(f => f.Code == "EXCEL_365_FUNCTION"), "VSTACK should be blocked for Excel 2021");
AssertTrue(excelFindings.Any(f => f.Message.Contains("XLOOKUP", StringComparison.OrdinalIgnoreCase)), "Excel blocked function finding should include preferred function guidance");

foreach (var functionName in loadedRuleSet.ExcelBlockedFunctions)
{
    var findings = excelChecker.CheckFormula($"={functionName}(A1:A3)").ToList();
    AssertTrue(findings.Any(f => f.Code == "EXCEL_365_FUNCTION"), $"Excel 2021 blocked function {functionName} should be detected");
}

var allowedExcelFindings = excelChecker.CheckFormula("=XLOOKUP(A1,B:B,C:C)").ToList();
AssertTrue(allowedExcelFindings.All(f => f.Code != "EXCEL_365_FUNCTION"), "Excel 2021 preferred function XLOOKUP should be allowed");

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
AssertTrue(versionBeforeRuleChange.RuleVersion != versionAfterRuleChange.RuleVersion, "RuleLoader ruleset version should change when rule content changes");

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
AssertTrue(emptyMandatoryRuleSet.UsedFallback, "RuleLoader should fallback when a mandatory rule group is empty");
AssertTrue(new SqlSafetyChecker(emptyMandatoryRuleSet).Check("DELETE FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_DML_DELETE"), "Fallback after empty mandatory rules should still block DELETE");

var fallbackRuleSet = RuleLoader.LoadFromDirectory(Path.Combine("artifacts", "missing-rules-b01"));
var fallbackFindings = new SqlSafetyChecker(fallbackRuleSet).Check("DELETE FROM TRADE_SAMPLE").ToList();
AssertTrue(fallbackRuleSet.UsedFallback, "Missing rules directory should use fallback");
AssertTrue(fallbackFindings.Any(f => f.Code == "RULESET_FALLBACK"), "Fallback use should be reported as finding");
AssertTrue(fallbackFindings.Any(f => f.Code == "SQL_DML_DELETE"), "Fallback rules should still block SQL DELETE");
AssertTrue(
    new VbaSafetyChecker(fallbackRuleSet)
        .Check("Option Explicit\nSub Test()\nDim fso As FileSystemObject\nEnd Sub")
        .Any(f => f.Code == "VBA_FSO"),
    "Fallback rules should detect direct FileSystemObject declarations");

var policyLoadResult = PolicyLoader.LoadDefault();
AssertTrue(!policyLoadResult.UsedFallback, "PolicyLoader should load repo security policy");
AssertTrue(!policyLoadResult.Policy.Network.AllowExternalApi, "Security policy should block external API");
AssertTrue(!policyLoadResult.Policy.Network.AllowAutoUpdate, "Security policy should block auto update");
AssertTrue(!policyLoadResult.Policy.Network.AllowTelemetry, "Security policy should block telemetry");
AssertTrue(!policyLoadResult.Policy.Sql.AllowAutoExecute, "Security policy should block SQL auto execution");
AssertTrue(!policyLoadResult.Policy.Vba.AllowAutoExecute, "Security policy should block VBA auto execution");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureExternalApiAllowed()), "Security policy should enforce external API block");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureSqlAutoExecuteAllowed()), "Security policy should enforce SQL auto-execute block");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureVbaAutoExecuteAllowed()), "Security policy should enforce VBA auto-execute block");

var missingPolicyResult = PolicyLoader.LoadFromFile("config/missing_policy_smoke.json");
AssertTrue(missingPolicyResult.UsedFallback, "Missing security policy should use safe fallback");
AssertTrue(!missingPolicyResult.Policy.Network.AllowExternalApi && !missingPolicyResult.Policy.Sql.AllowAutoExecute, "Security policy fallback should keep dangerous actions disabled");
AssertTrue(PolicyLoader.LoadFromFile("../security_policy.json").UsedFallback, "PolicyLoader should reject non-config-relative paths");

var invalidPolicyPath = Path.Combine("config", "smoke_invalid_policy.json");
File.WriteAllText(invalidPolicyPath, "{ invalid json");
var invalidPolicyResult = PolicyLoader.LoadFromFile(invalidPolicyPath);
File.Delete(invalidPolicyPath);
AssertTrue(invalidPolicyResult.UsedFallback && !invalidPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on invalid JSON policy");

var nullSectionPolicyPath = Path.Combine("config", "smoke_null_section_policy.json");
File.WriteAllText(nullSectionPolicyPath, "{ \"Network\": null }");
var nullSectionPolicyResult = PolicyLoader.LoadFromFile(nullSectionPolicyPath);
File.Delete(nullSectionPolicyPath);
AssertTrue(nullSectionPolicyResult.UsedFallback && !nullSectionPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on null policy sections");

var noModelDraftService = new NoModelDraftService(policyLoadResult.Policy);
var noModelDraftResponse = noModelDraftService.GenerateDraft(new DraftRequest(
    DraftRequestKind.Sql,
    "SELECT draft request for review-only output",
    "dummy context"));
AssertTrue(!noModelDraftResponse.IsAvailable, "NoModelDraftService should keep generation unavailable");
AssertTrue(noModelDraftResponse.Mode == NoModelDraftService.ModeName, "NoModelDraftService should report NoModelMode");
AssertTrue(noModelDraftResponse.DraftText is null, "NoModelDraftService should not return generated draft text");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_NO_MODEL_MODE" && f.Severity == SafetySeverity.Info), "NoModelDraftService should return safe no-model guidance");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_EXTERNAL_COMM_BLOCKED"), "NoModelDraftService should confirm external communications are blocked");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_AUTO_EXECUTE_BLOCKED"), "NoModelDraftService should confirm auto execution is blocked");
AssertTrue(noModelDraftService.GenerateDraft(null).Findings.Any(f => f.Code == "DRAFT_PROMPT_EMPTY"), "NoModelDraftService should not throw for an empty request");

var unsafeDraftPolicy = policyLoadResult.Policy with
{
    Network = policyLoadResult.Policy.Network with
    {
        AllowExternalApi = true
    }
};
var unsafeDraftResponse = new NoModelDraftService(unsafeDraftPolicy).GenerateDraft(new DraftRequest(DraftRequestKind.General, "policy check"));
AssertTrue(unsafeDraftResponse.Findings.Any(f => f.Code == "DRAFT_POLICY_UNSAFE_NETWORK" && f.Severity == SafetySeverity.High), "NoModelDraftService should flag unsafe network policy");

var draftPipelineLogPath = Path.Combine("logs", "smoke_draft_pipeline_log.jsonl");
if (File.Exists(draftPipelineLogPath))
{
    File.Delete(draftPipelineLogPath);
}

var safeSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var safeSqlPipelineResult = safeSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make select draft",
    "user-smoke"));
AssertTrue(safeSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should accept safe SQL draft for review");
AssertTrue(safeSqlPipelineResult.SafetyResult == "PASS", "DraftPipeline safe SQL result should pass");
AssertTrue(safeSqlPipelineResult.AuditLogWritten, "DraftPipeline should audit safe SQL result");
AssertTrue(safeSqlPipelineResult.DraftText is not null && safeSqlPipelineResult.DraftText.Contains("SELECT", StringComparison.OrdinalIgnoreCase), "DraftPipeline should return accepted safe SQL draft");

var blockedSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "DELETE FROM TRADE_SAMPLE",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var blockedSqlPipelineResult = blockedSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make delete draft",
    "user-smoke"));
AssertTrue(!blockedSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should reject blocker SQL draft");
AssertTrue(blockedSqlPipelineResult.SafetyResult == "BLOCKED", "DraftPipeline blocker SQL result should be blocked");
AssertTrue(blockedSqlPipelineResult.DraftText is null, "DraftPipeline should suppress blocked draft text");
AssertTrue(blockedSqlPipelineResult.Findings.Any(f => f.Code == "SQL_DML_DELETE"), "DraftPipeline should include SQL checker blocker finding");

var noModelPipeline = new DraftPipeline(noModelDraftService, loadedRuleSet, new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var noModelPipelineResult = noModelPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Vba,
    "make vba draft",
    "user-smoke"));
AssertTrue(!noModelPipelineResult.IsAcceptedForReview, "DraftPipeline should keep NoModel result unavailable");
AssertTrue(noModelPipelineResult.SafetyResult == "NO_MODEL", "DraftPipeline NoModel result should report NO_MODEL");
AssertTrue(noModelPipelineResult.AuditLogWritten, "DraftPipeline should audit NoModel result");

var draftPipelineLogText = File.ReadAllText(draftPipelineLogPath);
AssertTrue(!draftPipelineLogText.Contains("make select draft", StringComparison.Ordinal), "DraftPipeline audit should not store raw prompt text");
AssertTrue(!draftPipelineLogText.Contains("SELECT TRADE_ID", StringComparison.Ordinal), "DraftPipeline audit should not store raw draft text");
AssertTrue(!draftPipelineLogText.Contains("user-smoke", StringComparison.Ordinal), "DraftPipeline audit should not store raw user id");

var kbSearchLogPath = Path.Combine("logs", "smoke_kb_search_log.jsonl");
if (File.Exists(kbSearchLogPath))
{
    File.Delete(kbSearchLogPath);
}

var regulationCatalog = RegulationCatalog.LoadDefault();
AssertTrue(regulationCatalog.Entries.Count >= 5, "RegulationCatalog should load public catalog entries");
var kbSearch = new KbSearch(
    regulationCatalog,
    new TaskLogWriter("logs", "smoke_kb_search_log.jsonl"),
    loadedRuleSet.RuleVersion);
var ncrSearchResponse = kbSearch.Search("NCR", "user-smoke");
AssertTrue(ncrSearchResponse.Results.Any(result => result.SourceId == "NCR_GUIDE"), "KbSearch should find NCR catalog entry");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal), "KbSearch answer should mark review draft");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch answer should always include sources");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("원문은 포함하지 않습니다", StringComparison.Ordinal), "KbSearch answer should state internal originals are excluded");
AssertTrue(ncrSearchResponse.AuditLogWritten, "KbSearch should write audit log when configured");

var publicRegSearchResponse = kbSearch.Search("금융투자업규정", "user-smoke");
AssertTrue(publicRegSearchResponse.Results.Any(result => result.SourceId == "FIA_REG"), "KbSearch should find public regulation catalog entry");
var emptyKbSearchResponse = kbSearch.Search("없는검색어", "user-smoke");
AssertTrue(emptyKbSearchResponse.Results.Count == 0, "KbSearch should return zero results for unmatched query");
AssertTrue(emptyKbSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal) && emptyKbSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch no-result answer should still include review draft and source");
var kbSearchLogText = File.ReadAllText(kbSearchLogPath);
AssertTrue(!kbSearchLogText.Contains("NCR", StringComparison.Ordinal), "KbSearch audit should not store raw query text");
AssertTrue(!kbSearchLogText.Contains("user-smoke", StringComparison.Ordinal), "KbSearch audit should not store raw user id");

var approvedFeedback = new FeedbackLogEntry(
    "feedback-approved-001",
    "task-smoke-approved-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("reviewer-one"),
    "APPROVED",
    "ReviewerApproved");
var rejectedFeedback = approvedFeedback with
{
    FeedbackId = "feedback-rejected-001",
    TaskId = "task-smoke-rejected-001",
    FeedbackCode = "REJECTED",
    ReviewStatus = "ReviewerRejected"
};
var pendingFeedback = approvedFeedback with
{
    FeedbackId = "feedback-pending-001",
    TaskId = "task-smoke-pending-001",
    FeedbackCode = "PENDING",
    ReviewStatus = "ReviewerPending"
};
var promotionResult = new ExamplePromotion().PromoteApproved(
    [approvedFeedback, rejectedFeedback, pendingFeedback, approvedFeedback],
    new DateTime(2026, 06, 19, 0, 0, 0, DateTimeKind.Utc));
AssertTrue(promotionResult.PromotedExamples.Count == 1, "ExamplePromotion should promote only approved feedback once");
AssertTrue(promotionResult.PromotedExamples[0].PromotionMode == ExamplePromotion.PromotionModeName, "ExamplePromotion should use curation-only mode");
AssertTrue(promotionResult.PromotedExamples[0].UserIdHash == approvedFeedback.UserId, "ExamplePromotion should preserve hashed user id");
AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == rejectedFeedback.FeedbackId), "ExamplePromotion should skip rejected feedback");
AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == pendingFeedback.FeedbackId), "ExamplePromotion should skip pending feedback");
AssertTrue(promotionResult.Warnings.Any(warning => warning.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)), "ExamplePromotion should warn on duplicate approved feedback");

var profiler = new DataProfiler();
var exposureProfile = profiler.ProfileCsv(Path.Combine("samples", "dummy_data", "risk_exposure_sample.csv"));
AssertTrue(exposureProfile.SourceName == "risk_exposure_sample.csv", "DataProfiler should preserve source file name");
AssertTrue(exposureProfile.RowCount == 6, "Risk exposure sample should have 6 data rows");
AssertTrue(exposureProfile.ColumnCount == 10, "Risk exposure sample should have 10 columns");
AssertTrue(exposureProfile.NullCounts.Values.All(count => count == 0), "Risk exposure sample should have zero nulls");
AssertTrue(exposureProfile.DuplicateRowCount == 0, "Risk exposure sample should have zero duplicate rows");
AssertTrue(exposureProfile.BaseDateDistribution["20260617"] == 5 && exposureProfile.BaseDateDistribution["20260616"] == 1, "Risk exposure sample should summarize BASE_DT distribution");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Sum == 3830000000m, "Risk exposure sample should compute numeric sum");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Min == -420000000m, "Risk exposure sample should compute numeric min");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Max == 1250000000m, "Risk exposure sample should compute numeric max");

var profileSmokeDirectory = Path.Combine("artifacts", "smoke-profile-b05");
Directory.CreateDirectory(profileSmokeDirectory);
var profileSmokeCsv = Path.Combine(profileSmokeDirectory, "profile_sample.csv");
File.WriteAllText(profileSmokeCsv, "BASE_DT,DESK_CD,AMT\n20260617,EQD,10\n20260617,EQD,10\n20260618,,20\n");
var smallProfile = profiler.ProfileCsv(profileSmokeCsv);
AssertTrue(smallProfile.RowCount == 3, "Small profile sample should have 3 data rows");
AssertTrue(smallProfile.NullCounts["DESK_CD"] == 1, "DataProfiler should count null values");
AssertTrue(smallProfile.DuplicateRowCount == 1, "DataProfiler should count duplicate rows");
AssertTrue(smallProfile.BaseDateDistribution["20260617"] == 2 && smallProfile.BaseDateDistribution["20260618"] == 1, "DataProfiler should count BASE_DT values");
AssertTrue(smallProfile.NumericColumns["AMT"].Sum == 40m, "DataProfiler should compute small numeric sum");

var noBaseDateCsv = Path.Combine(profileSmokeDirectory, "profile_no_base_dt.csv");
File.WriteAllText(noBaseDateCsv, "DESK_CD,AMT\nEQD,10\nFIC,20\n");
var noBaseDateProfile = profiler.ProfileCsv(noBaseDateCsv);
AssertTrue(noBaseDateProfile.Warnings.Any(w => w.Contains("BASE_DT", StringComparison.OrdinalIgnoreCase)), "DataProfiler should warn when BASE_DT is missing");

var taskLogPath = Path.Combine("logs", "smoke_task_log.jsonl");
var feedbackLogPath = Path.Combine("logs", "smoke_feedback_log.jsonl");
if (File.Exists(taskLogPath))
{
    File.Delete(taskLogPath);
}

if (File.Exists(feedbackLogPath))
{
    File.Delete(feedbackLogPath);
}

var rawRequest = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = 'DUMMY-001'";
var rawOutput = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = :ACCOUNT_NO";
var taskEntry = new TaskLogEntry(
    "task-smoke-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("user-smoke"),
    "SqlSafetyCheck",
    "SqlSafetyChecker",
    LogHash.Sha256Hex(rawRequest),
    LogHash.Sha256Hex(rawOutput),
    "PASS",
    loadedRuleSet.RuleVersion);

var taskLogWriter = new TaskLogWriter("logs", "smoke_task_log.jsonl");
taskLogWriter.Append(taskEntry);
var taskLogText = File.ReadAllText(taskLogWriter.LogFilePath);
AssertTrue(File.Exists(taskLogWriter.LogFilePath), "TaskLogWriter should create JSONL file");
AssertTrue(taskLogText.Contains(taskEntry.RequestHash, StringComparison.Ordinal), "TaskLogWriter should store request hash");
AssertTrue(!taskLogText.Contains(rawRequest, StringComparison.Ordinal) && !taskLogText.Contains("ACCOUNT_NO", StringComparison.Ordinal), "TaskLogWriter should not store raw request/output text");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RequestHash = rawRequest })), "TaskLogWriter should reject non-hash RequestHash");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { OutputHash = rawOutput })), "TaskLogWriter should reject non-hash OutputHash");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { UserId = "plain-user" })), "TaskLogWriter should reject non-hash UserId");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RuleVersion = "ruleset-not-valid" })), "TaskLogWriter should reject malformed RuleVersion");
AssertTrue(Throws<ArgumentException>(() => new TaskLogWriter("logs/../reports", "bad.jsonl")), "TaskLogWriter should reject paths outside logs");

var feedbackEntry = new FeedbackLogEntry(
    "feedback-smoke-001",
    taskEntry.TaskId,
    DateTime.UtcNow,
    taskEntry.UserId,
    "APPROVED",
    "ReviewerApproved");

var feedbackLogWriter = new FeedbackLogWriter("logs", "smoke_feedback_log.jsonl");
feedbackLogWriter.Append(feedbackEntry);
var feedbackLogText = File.ReadAllText(feedbackLogWriter.LogFilePath);
AssertTrue(File.Exists(feedbackLogWriter.LogFilePath), "FeedbackLogWriter should create JSONL file");
AssertTrue(feedbackLogText.Contains(feedbackEntry.FeedbackId, StringComparison.Ordinal), "FeedbackLogWriter should store feedback id");
AssertTrue(!feedbackLogText.Contains(rawRequest, StringComparison.Ordinal), "FeedbackLogWriter should not store raw request text");
AssertTrue(Throws<ArgumentException>(() => feedbackLogWriter.Append(feedbackEntry with { UserId = "plain-user" })), "FeedbackLogWriter should reject non-hash UserId");

if (failed > 0)
{
    Console.WriteLine($"SmokeTests failed: {failed}");
    Environment.Exit(1);
}

Console.WriteLine("All SmokeTests passed.");

sealed class StubDraftService : ILocalDraftService
{
    private readonly DraftResponse response;

    public StubDraftService(DraftResponse response)
    {
        this.response = response;
    }

    public DraftResponse GenerateDraft(DraftRequest? request)
    {
        return response;
    }
}

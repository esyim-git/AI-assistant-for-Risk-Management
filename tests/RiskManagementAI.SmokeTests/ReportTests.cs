internal static class ReportTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
var sqlChecker = new SqlSafetyChecker(loadedRuleSet);
var excelChecker = new Excel2021FunctionChecker(loadedRuleSet);
var (exposureProfile, sixStateResult, mappingErrorResult) = CreateReportSmokeInputs();
var excelReportLogPath = Path.Combine("logs", "smoke_excel_report_log.jsonl");
var excelReportPath = Path.Combine("reports", "smoke_m2_04_report.xlsx");
if (File.Exists(excelReportLogPath))
{
    File.Delete(excelReportLogPath);
}

if (File.Exists(excelReportPath))
{
    File.Delete(excelReportPath);
}

var reportSql = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT";
var reportBuilder = new ExcelReportBuilder(
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_excel_report_log.jsonl"));
var reportValidationFindings = sqlChecker.Check(reportSql).ToList();
reportValidationFindings.Add(new SafetyFinding(
    "REPORT_VALIDATION_HIGH_SMOKE",
    SafetySeverity.High,
    "High validation smoke finding for EXCEPTION_LIST merge."));
var reportResult = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_m2_04_report",
    exposureProfile,
    sixStateResult,
    reportValidationFindings,
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
context.AssertTrue(File.Exists(reportResult.ReportPath), "ExcelReportBuilder should create xlsx report");
context.AssertTrue(
    Path.GetFullPath(reportResult.ReportPath).StartsWith(Path.GetFullPath("reports") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
    "ExcelReportBuilder should write only under reports");
context.AssertTrue(reportResult.SheetNames.SequenceEqual(ExcelReportBuilder.ExpectedSheetNames), "ExcelReportBuilder should create the required MVP-2 sheets");
context.AssertTrue(reportResult.CheckedFormulas.Count >= 3, "ExcelReportBuilder should report checked formulas");
context.AssertTrue(reportResult.CheckedFormulas.SelectMany(formula => excelChecker.CheckFormula(formula)).All(f => f.Code != "EXCEL_365_FUNCTION"), "ExcelReportBuilder formulas should pass Excel 2021 checker");
context.AssertTrue(reportResult.AuditLogWritten, "ExcelReportBuilder should write audit log");

using (var reportArchive = ZipFile.OpenRead(reportResult.ReportPath))
{
    context.AssertTrue(reportArchive.GetEntry("[Content_Types].xml") is not null, "Excel report xlsx should include content types");
    context.AssertTrue(reportArchive.GetEntry("_rels/.rels") is not null, "Excel report xlsx should include root relationships");
    context.AssertTrue(reportArchive.GetEntry("xl/workbook.xml") is not null, "Excel report xlsx should include workbook part");
    context.AssertTrue(reportArchive.GetEntry("xl/_rels/workbook.xml.rels") is not null, "Excel report xlsx should include workbook relationships");
    context.AssertTrue(reportArchive.GetEntry("xl/styles.xml") is not null, "Excel report xlsx should include styles part");
    context.AssertTrue(reportArchive.GetEntry("xl/worksheets/sheet10.xml") is not null, "Excel report xlsx should include tenth worksheet");
    var summarySheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet5.xml");
    var limitMonitoringSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet6.xml");
    var exceptionSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet7.xml");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("0.95", StringComparison.Ordinal), "WP-07 report should reuse analysis WARNING usage ratio");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_BREACH", StringComparison.Ordinal) && limitMonitoringSheet.Contains("BREACH", StringComparison.Ordinal), "WP-07 report should expose analysis BREACH status");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_NOLIMIT", StringComparison.Ordinal) && limitMonitoringSheet.Contains("NO_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis NO_LIMIT status");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_ZERO", StringComparison.Ordinal) && limitMonitoringSheet.Contains("INVALID_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis INVALID_LIMIT status");
    context.AssertTrue(summarySheet.Contains("ReconciliationPassed", StringComparison.Ordinal) && summarySheet.Contains("FAIL", StringComparison.Ordinal), "WP-07 report summary should expose reconciliation PASS/FAIL");
    context.AssertTrue(summarySheet.Contains("DuplicateLimitCount", StringComparison.Ordinal), "Excel report SUMMARY should expose duplicate limit count");
    context.AssertTrue(exceptionSheet.Contains("RECON_EXPOSURE_NO_LIMIT", StringComparison.Ordinal) && exceptionSheet.Contains("RECON_NONPOSITIVE_LIMIT", StringComparison.Ordinal), "WP-07 report exception list should include analysis RECON exceptions");
    context.AssertTrue(exceptionSheet.Contains("REPORT_VALIDATION_HIGH_SMOKE", StringComparison.Ordinal), "WP-07 report exception list should merge high validation findings");
}

var mappingErrorReport = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_wp07_mapping_error_report",
    exposureProfile,
    mappingErrorResult,
    [],
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
using (var mappingErrorArchive = ZipFile.OpenRead(mappingErrorReport.ReportPath))
{
    var limitMonitoringSheet = ReadZipEntryText(mappingErrorArchive, "xl/worksheets/sheet6.xml");
    context.AssertTrue(limitMonitoringSheet.Contains("MAPPING_ERROR", StringComparison.Ordinal), "WP-07 report should expose analysis MAPPING_ERROR status");
}

var limitRequiredFinding = new SafetyFinding(
    "LIMIT_DATA_REQUIRED",
    SafetySeverity.High,
    "실제 한도 데이터가 필요합니다. 데모 합성 한도는 생성하거나 사용하지 않습니다.");
var demoOnlyFinding = new SafetyFinding(
    "DEMO_ONLY",
    SafetySeverity.Medium,
    "샘플/데모 데이터 기반 리포트입니다.");
var noSyntheticLimitReportPath = Path.Combine("reports", "smoke_wp_01_no_synthetic_limit.xlsx");
if (File.Exists(noSyntheticLimitReportPath))
{
    File.Delete(noSyntheticLimitReportPath);
}

var noSyntheticLimitReport = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_wp_01_no_synthetic_limit",
    exposureProfile,
    EmptyLimitAnalysis(),
    [limitRequiredFinding, demoOnlyFinding],
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
using (var noSyntheticArchive = ZipFile.OpenRead(noSyntheticLimitReport.ReportPath))
{
    var validationSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet4.xml");
    var limitSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet6.xml");
    var exceptionSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet7.xml");
    context.AssertTrue(validationSheet.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 report validation should include LIMIT_DATA_REQUIRED finding");
    context.AssertTrue(validationSheet.Contains("DEMO_ONLY", StringComparison.Ordinal), "WP-01 report validation should include DEMO_ONLY finding");
    context.AssertTrue(limitSheet.Contains("NO_LIMIT_ROW", StringComparison.Ordinal) && limitSheet.Contains("NO_DATA", StringComparison.Ordinal), "WP-01 report should not create synthetic limit rows");
    context.AssertTrue(exceptionSheet.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 report exception list should surface missing real limit data");
}

var excelReportLogText = File.ReadAllText(excelReportLogPath);
context.AssertTrue(!excelReportLogText.Contains(reportSql, StringComparison.Ordinal), "ExcelReportBuilder audit should not store raw SQL text");
context.AssertTrue(!excelReportLogText.Contains("user-smoke", StringComparison.Ordinal), "ExcelReportBuilder audit should not store raw user id");
context.AssertTrue(context.Throws<ArgumentException>(() => new ExcelReportBuilder(loadedRuleSet, reportsDirectory: "reports/../logs")), "ExcelReportBuilder should reject report paths outside reports");
context.AssertTrue(context.Throws<ArgumentException>(() => reportBuilder.BuildReport(new ExcelReportRequest(
    "../bad",
    exposureProfile,
    EmptyLimitAnalysis(),
    [],
    reportSql,
    "commentary",
    "user-smoke"))), "ExcelReportBuilder should reject report file path traversal");
var excelReportBuilderCode = File.ReadAllText(Path.Combine("src", "RiskManagementAI.Core", "Report", "ExcelReportBuilder.cs"));
context.AssertTrue(!excelReportBuilderCode.Contains("ExcelReportLimitRow", StringComparison.Ordinal), "WP-07 should remove ExcelReportLimitRow from report builder");
context.AssertTrue(!excelReportBuilderCode.Contains("CalculateLimitStatus", StringComparison.Ordinal), "WP-07 should remove three-state report status calculation");
context.AssertTrue(!excelReportBuilderCode.Contains("CalculateUtilization", StringComparison.Ordinal), "WP-07 should remove report-side utilization recalculation");
context.AssertTrue(Directory.Exists(Path.Combine("templates", "report")), "ExcelReportBuilder should use templates/report assets");
context.AssertTrue(
    !File.ReadAllText(Path.Combine("src", "RiskManagementAI.Core", "RiskManagementAI.Core.csproj")).Contains("PackageReference", StringComparison.OrdinalIgnoreCase)
    && !File.ReadAllText(Path.Combine("src", "RiskManagementAI.App", "RiskManagementAI.App.csproj")).Contains("PackageReference", StringComparison.OrdinalIgnoreCase),
    "ExcelReportBuilder should not add NuGet PackageReference");
    }
}

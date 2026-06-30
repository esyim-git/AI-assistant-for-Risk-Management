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
var expectedExceptionCount = sixStateResult.ExceptionList.Count
    + reportValidationFindings.Count(finding => finding.Severity is SafetySeverity.Blocker or SafetySeverity.High);
context.AssertTrue(ExcelReportBuilder.CountExceptions(sixStateResult, reportValidationFindings) == expectedExceptionCount, "R2-WP-04 report exact ExceptionCount should exclude headers and placeholders");
context.AssertTrue(ExcelReportBuilder.CountExceptions(EmptyLimitAnalysis(), []) == 0, "R2-WP-04 report exact ExceptionCount should be zero when EXCEPTION_LIST emits only NO_EXCEPTION placeholder");
context.AssertTrue(File.Exists(reportResult.ReportPath), "ExcelReportBuilder should create xlsx report");
context.AssertTrue(
    Path.GetFullPath(reportResult.ReportPath).StartsWith(Path.GetFullPath("reports") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
    "ExcelReportBuilder should write only under reports");
context.AssertTrue(reportResult.SheetNames.SequenceEqual(ExcelReportBuilder.ExpectedSheetNames), "ExcelReportBuilder should create the required MVP-2/R2 sheets");
context.AssertTrue(reportResult.CheckedFormulas.Count >= 2, "ExcelReportBuilder should report checked formulas");
context.AssertTrue(reportResult.CheckedFormulas.SelectMany(formula => excelChecker.CheckFormula(formula)).All(f => f.Code != "EXCEL_365_FUNCTION"), "ExcelReportBuilder formulas should pass Excel 2021 checker");
context.AssertTrue(reportResult.AuditLogWritten, "ExcelReportBuilder should write audit log");

using (var reportArchive = ZipFile.OpenRead(reportResult.ReportPath))
{
    var riskVisualSheetIndex = ExcelReportBuilder.ExpectedSheetNames.ToList().IndexOf("RISK_VISUAL") + 1;
    context.AssertTrue(reportArchive.GetEntry("[Content_Types].xml") is not null, "Excel report xlsx should include content types");
    context.AssertTrue(reportArchive.GetEntry("_rels/.rels") is not null, "Excel report xlsx should include root relationships");
    context.AssertTrue(reportArchive.GetEntry("xl/workbook.xml") is not null, "Excel report xlsx should include workbook part");
    context.AssertTrue(reportArchive.GetEntry("xl/_rels/workbook.xml.rels") is not null, "Excel report xlsx should include workbook relationships");
    context.AssertTrue(reportArchive.GetEntry("xl/styles.xml") is not null, "Excel report xlsx should include styles part");
    context.AssertTrue(reportArchive.GetEntry($"xl/worksheets/sheet{ExcelReportBuilder.ExpectedSheetNames.Count}.xml") is not null, "Excel report xlsx should include the final expected worksheet");
    context.AssertTrue(reportArchive.GetEntry($"xl/worksheets/sheet{riskVisualSheetIndex}.xml") is not null, "R2-WP-04 report xlsx should include RISK_VISUAL worksheet");
    var summarySheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet5.xml");
    var limitMonitoringSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet6.xml");
    var exceptionSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet7.xml");
    var riskVisualSheet = ReadZipEntryText(reportArchive, $"xl/worksheets/sheet{riskVisualSheetIndex}.xml");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("0.95", StringComparison.Ordinal), "WP-07 report should reuse analysis WARNING usage ratio");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_BREACH", StringComparison.Ordinal) && limitMonitoringSheet.Contains("BREACH", StringComparison.Ordinal), "WP-07 report should expose analysis BREACH status");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_NOLIMIT", StringComparison.Ordinal) && limitMonitoringSheet.Contains("NO_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis NO_LIMIT status");
    context.AssertTrue(limitMonitoringSheet.Contains("PF_ZERO", StringComparison.Ordinal) && limitMonitoringSheet.Contains("INVALID_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis INVALID_LIMIT status");
    context.AssertTrue(summarySheet.Contains("ReconciliationPassed", StringComparison.Ordinal) && summarySheet.Contains("FAIL", StringComparison.Ordinal), "WP-07 report summary should expose reconciliation PASS/FAIL");
    context.AssertTrue(summarySheet.Contains("DuplicateLimitCount", StringComparison.Ordinal), "Excel report SUMMARY should expose duplicate limit count");
    context.AssertTrue(summarySheet.Contains("ExceptionCount", StringComparison.Ordinal) && !summarySheet.Contains("ExceptionListCountFormula", StringComparison.Ordinal) && !summarySheet.Contains("COUNTA(EXCEPTION_LIST", StringComparison.Ordinal), "R2-WP-04 report summary should use exact numeric ExceptionCount, not EXCEPTION_LIST COUNTA");
    context.AssertTrue(exceptionSheet.Contains("RECON_EXPOSURE_NO_LIMIT", StringComparison.Ordinal) && exceptionSheet.Contains("RECON_NONPOSITIVE_LIMIT", StringComparison.Ordinal), "WP-07 report exception list should include analysis RECON exceptions");
    context.AssertTrue(exceptionSheet.Contains("REPORT_VALIDATION_HIGH_SMOKE", StringComparison.Ordinal), "WP-07 report exception list should merge high validation findings");
    context.AssertTrue(riskVisualSheet.Contains("STATUS_DISTRIBUTION", StringComparison.Ordinal) && riskVisualSheet.Contains("DUPLICATE_LIMIT", StringComparison.Ordinal), "R2-WP-04 report RISK_VISUAL should include all seven status distribution rows");
    context.AssertTrue(riskVisualSheet.Contains("TOP_EXPOSURE", StringComparison.Ordinal) && riskVisualSheet.Contains("CONCENTRATION", StringComparison.Ordinal) && riskVisualSheet.Contains("HHI", StringComparison.Ordinal) && riskVisualSheet.Contains("CurrencyCode", StringComparison.Ordinal), "R2-WP-04 report RISK_VISUAL should include TopN, concentration data, and currency labels");
    context.AssertTrue(riskVisualSheet.Contains("HEATMAP", StringComparison.Ordinal) && riskVisualSheet.Contains("LOW &lt;0.8 / MID 0.8~1.0 / HIGH &gt;1.0", StringComparison.Ordinal), "R2-WP-04 report RISK_VISUAL should include heatmap grade boundaries");
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

var visualRows = BuildRiskVisualSmokeRows();
var visualAnalysis = BuildRiskVisualAnalysis(visualRows);
var visual = RiskVisualAggregator.Aggregate(visualAnalysis, topN: 3);
context.AssertTrue(visual.StatusDistribution.Count == 7 && visual.StatusDistribution.Single(row => row.StatusCode == "DUPLICATE_LIMIT").Count == 1, "R2-WP-04 report visual aggregation should preserve seven-state distribution including DuplicateLimit");
context.AssertTrue(
    visual.TopExposures.Select(row => row.PortfolioId).Take(3).SequenceEqual(["PF_HIGH", "PF_A", "PF_B"]),
    "R2-WP-04 report visual TopN should sort by Abs(ExposureAmount) desc with PortfolioId ordinal tie-break");
context.AssertTrue(visual.Concentration.TotalAbsoluteExposure == visualRows.Sum(row => Math.Abs(row.ExposureAmount)) && visual.Concentration.TopNShare > 0m && visual.Concentration.Hhi > 0m, "R2-WP-04 report concentration should use Abs(ExposureAmount) denominator and positive HHI");
context.AssertTrue(visual.Heatmap.Single(row => row.PortfolioId == "PF_LOW").Grade == "LOW", "R2-WP-04 report heatmap usage below 0.8 should be LOW");
context.AssertTrue(visual.Heatmap.Single(row => row.PortfolioId == "PF_A").Grade == "MID" && visual.Heatmap.Single(row => row.PortfolioId == "PF_B").Grade == "MID", "R2-WP-04 report heatmap usage 0.8 and 1.0 should be MID");
context.AssertTrue(visual.Heatmap.Single(row => row.PortfolioId == "PF_HIGH").Grade == "HIGH", "R2-WP-04 report heatmap usage above 1.0 should be HIGH");
context.AssertTrue(visual.Findings.Any(finding => finding.Code == "MIXED_CURRENCY"), "R2-WP-04 report visual aggregation should note mixed currency inputs");
var mixedCurrencyReport = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_r2_wp04_mixed_currency_report",
    exposureProfile,
    visualAnalysis,
    [],
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
context.AssertTrue(mixedCurrencyReport.Findings.Any(finding => finding.Code == "MIXED_CURRENCY"), "R2-WP-04 report result findings should surface visual mixed-currency caveats");
var zeroDenominatorVisual = RiskVisualAggregator.Aggregate(BuildRiskVisualAnalysis([
    RiskVisualRow("PF_ZERO_A", "RF_ZERO_A", "KRW", 0m, 100m, 0m, LimitMonitorStatus.Normal),
    RiskVisualRow("PF_ZERO_B", "RF_ZERO_B", "KRW", 0m, 100m, 0m, LimitMonitorStatus.Normal)
]), topN: 2);
context.AssertTrue(
    zeroDenominatorVisual.Concentration.TotalAbsoluteExposure == 0m
    && zeroDenominatorVisual.Concentration.TopNShare == 0m
    && zeroDenominatorVisual.Concentration.Hhi == 0m
    && zeroDenominatorVisual.Findings.Any(finding => finding.Code == "VISUAL_CONCENTRATION_ZERO_DENOMINATOR"),
    "R2-WP-04 report concentration should handle zero denominator deterministically");

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

    private static LimitMonitorRow[] BuildRiskVisualSmokeRows()
    {
        return
        [
            RiskVisualRow("PF_HIGH", "RF_HIGH", "KRW", -200m, 100m, 1.01m, LimitMonitorStatus.Breach),
            RiskVisualRow("PF_A", "RF_TIE", "KRW", 100m, 125m, 0.8m, LimitMonitorStatus.Normal),
            RiskVisualRow("PF_B", "RF_TIE", "KRW", -100m, 100m, 1.0m, LimitMonitorStatus.Warning),
            RiskVisualRow("PF_LOW", "RF_LOW", "KRW", 79m, 100m, 0.79m, LimitMonitorStatus.Normal),
            RiskVisualRow("PF_DUP", "RF_DUP", "KRW", 10m, 100m, 0.1m, LimitMonitorStatus.DuplicateLimit),
            RiskVisualRow("PF_USD", "RF_USD", "USD", 50m, 100m, 0.5m, LimitMonitorStatus.Normal)
        ];
    }

    private static LimitMonitorRow RiskVisualRow(
        string portfolioId,
        string riskFactor,
        string currencyCode,
        decimal exposureAmount,
        decimal limitAmount,
        decimal usageRatio,
        LimitMonitorStatus status)
    {
        return new LimitMonitorRow(
            "20260617",
            "DESK",
            portfolioId,
            "PRODUCT",
            riskFactor,
            currencyCode,
            exposureAmount,
            limitAmount,
            usageRatio,
            limitAmount - Math.Abs(exposureAmount),
            status,
            "visual smoke");
    }

    private static LimitAnalysisResult BuildRiskVisualAnalysis(IReadOnlyList<LimitMonitorRow> rows)
    {
        return new LimitAnalysisResult(
            "20260617",
            rows,
            LimitAnalysisKpis.FromRows(rows),
            new LimitAnalysisMetadata("20260617", "visual-exposure.csv", "visual-limit.csv", ColumnMappingUsedFallback: false, ColumnMappingWarnings: Array.Empty<string>(), IsDeterministic: true, JoinAudit: Array.Empty<string>()),
            Array.Empty<LimitException>(),
            Array.Empty<SafetyFinding>(),
            new ReconciliationSummary(Passed: true, CheckCount: 0, Checks: Array.Empty<ReconciliationCheck>()));
    }
}

internal static class LimitReconciliationTests
{
    internal static void Run(SmokeTestContext context)
    {
        var profiler = new DataProfiler();
var exposureCsvPath = Path.Combine("samples", "dummy_data", "risk_exposure_sample.csv");
var limitCsvPath = Path.Combine("samples", "dummy_data", "risk_limit_sample.csv");
var exposureProfile = profiler.ProfileCsv(exposureCsvPath);
context.AssertTrue(exposureProfile.SourceName == "risk_exposure_sample.csv", "DataProfiler should preserve source file name");
context.AssertTrue(exposureProfile.RowCount == 6, "Risk exposure sample should have 6 data rows");
context.AssertTrue(exposureProfile.ColumnCount == 10, "Risk exposure sample should have 10 columns");
context.AssertTrue(exposureProfile.NullCounts.Values.All(count => count == 0), "Risk exposure sample should have zero nulls");
context.AssertTrue(exposureProfile.DuplicateRowCount == 0, "Risk exposure sample should have zero duplicate rows");
context.AssertTrue(exposureProfile.BaseDateDistribution["20260617"] == 5 && exposureProfile.BaseDateDistribution["20260616"] == 1, "Risk exposure sample should summarize BASE_DT distribution");
context.AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Sum == 3830000000m, "Risk exposure sample should compute numeric sum");
context.AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Min == -420000000m, "Risk exposure sample should compute numeric min");
context.AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Max == 1250000000m, "Risk exposure sample should compute numeric max");

var limitMonitor = new LimitMonitor();
var limitMonitorResult = limitMonitor.Analyze(exposureCsvPath, limitCsvPath, "20260617");
context.AssertTrue(limitMonitorResult.Rows.Count == 5, "LimitMonitor should filter exposure rows by BASE_DT");
context.AssertTrue(limitMonitorResult.Rows.All(row => row.BaseDate == "20260617"), "LimitMonitor should not match prior BASE_DT exposure rows to current limits");
context.AssertTrue(limitMonitorResult.NormalCount == 2 && limitMonitorResult.WarningCount == 2 && limitMonitorResult.BreachCount == 1, "LimitMonitor should classify NORMAL/WARNING/BREACH counts");
var warningEqRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_EQ_002");
context.AssertTrue(warningEqRow.Status == LimitMonitorStatus.Warning && warningEqRow.UsageRatio > 0.94m && warningEqRow.UsageRatio < 0.95m, "LimitMonitor should classify PF_EQ_002 as WARNING around 94%");
var breachCreditRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_CR_001");
context.AssertTrue(breachCreditRow.Status == LimitMonitorStatus.Breach && breachCreditRow.RemainingLimit < 0m, "LimitMonitor should classify PF_CR_001 as BREACH");
var shortSideRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_FI_001");
context.AssertTrue(shortSideRow.ExposureAmount < 0m && shortSideRow.UsageRatio == 0.84m && shortSideRow.Status == LimitMonitorStatus.Normal, "LimitMonitor should use ABS exposure for short-side rows");
context.AssertTrue(limitMonitorResult.Kpis.TotalCount == 5 && limitMonitorResult.Metadata.IsDeterministic, "LimitMonitor should return shared deterministic LimitAnalysisResult KPIs");
context.AssertTrue(!limitMonitorResult.Metadata.ColumnMappingUsedFallback && limitMonitorResult.Metadata.ExposureSourceName == "risk_exposure_sample.csv", "LimitAnalysisResult metadata should include source and mapping state");
context.AssertTrue(limitMonitorResult.Findings.Any(f => f.Code == "LIMIT_BREACH_DETECTED" && f.Severity == SafetySeverity.High), "LimitMonitor should emit a high finding when breaches exist");
context.AssertTrue(limitMonitorResult.Reconciliation.Passed, "LimitMonitor sample reconciliation should pass without fail-code exceptions");
context.AssertTrue(limitMonitorResult.Reconciliation.CheckCount == 9, "LimitMonitor should expose nine WP-06 reconciliation checks");
context.AssertTrue(
    limitMonitorResult.Reconciliation.Checks.Select(check => check.Code).SequenceEqual([
        "RECON_EXPOSURE_NO_LIMIT",
        "RECON_LIMIT_NO_EXPOSURE",
        "RECON_DUPLICATE_LIMIT",
        "RECON_BASEDATE_MISMATCH",
        "RECON_CURRENCY_MISMATCH",
        "RECON_UNIT_MISMATCH",
        "RECON_NONPOSITIVE_LIMIT",
        "RECON_ROW_AMPLIFICATION",
        "RECON_SUM_BALANCE"
    ]),
    "RECON nine-code order should remain stable for dashboard and report consumers");
context.AssertTrue(ReconciliationExceptionCount(limitMonitorResult, "RECON_BASEDATE_MISMATCH") == 0, "LimitMonitor should not flag normal multi-date exports when requested BASE_DT exists");
context.AssertTrue(
    !ReconciliationCheckFor(limitMonitorResult, "RECON_CURRENCY_MISMATCH").Applicable
        && !ReconciliationCheckFor(limitMonitorResult, "RECON_UNIT_MISMATCH").Applicable,
    "LimitMonitor should mark currency and unit reconciliation as N/A for R1 default inputs");
context.AssertTrue(
    Enum.GetValues<LimitMonitorStatus>().Select(status => (int)status).SequenceEqual([0, 1, 2, 3, 4, 5, 6]),
    "LimitMonitor seven-state enum ordinal should remain additive and stable");

var limitSmokeDirectory = Path.Combine("artifacts", "smoke-limit-wp05");
Directory.CreateDirectory(limitSmokeDirectory);
var wp05ExposureRows = new[]
{
    new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
    new[] { "20260617", "EQD", "PF_NORMAL", "ELS", "RF_NORMAL", "KRW", "50" },
    new[] { "20260617", "EQD", "PF_WARNING", "ELS", "RF_WARNING", "KRW", "95" },
    new[] { "20260617", "EQD", "PF_BREACH", "ELS", "RF_BREACH", "KRW", "110" },
    new[] { "20260617", "EQD", "PF_NOLIMIT", "ELS", "RF_NOLIMIT", "KRW", "10" },
    new[] { "20260617", "EQD", "PF_INACTIVE", "ELS", "RF_INACTIVE", "KRW", "10" },
    new[] { "20260617", "EQD", "PF_ZERO", "ELS", "RF_ZERO", "KRW", "10" }
};
var wp05LimitRows = new[]
{
    new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
    new[] { "20260617", "PF_NORMAL", "RF_NORMAL", "100", "Y" },
    new[] { "20260617", "PF_WARNING", "RF_WARNING", "100", "Y" },
    new[] { "20260617", "PF_BREACH", "RF_BREACH", "100", "Y" },
    new[] { "20260617", "PF_INACTIVE", "RF_INACTIVE", "100", "N" },
    new[] { "20260617", "PF_ZERO", "RF_ZERO", "0", "Y" }
};
var wp05ExposureCsv = Path.Combine(limitSmokeDirectory, "wp05_exposure.csv");
var wp05LimitCsv = Path.Combine(limitSmokeDirectory, "wp05_limit.csv");
WriteCsvRows(wp05ExposureCsv, wp05ExposureRows);
WriteCsvRows(wp05LimitCsv, wp05LimitRows);
var sixStateResult = limitMonitor.Analyze(wp05ExposureCsv, wp05LimitCsv, "20260617");
context.AssertTrue(
    sixStateResult.Kpis is { NormalCount: 1, WarningCount: 1, BreachCount: 1, NoLimitCount: 1, InvalidLimitCount: 2, MappingErrorCount: 0 },
    "LimitMonitor should classify NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT states");
context.AssertTrue(sixStateResult.Rows.Single(row => row.PortfolioId == "PF_NOLIMIT").StatusCode == "NO_LIMIT", "LimitMonitor should expose NO_LIMIT output string for unmatched joins");
context.AssertTrue(sixStateResult.ExceptionList.Count(exception => exception.Code == "INVALID_LIMIT") == 2, "LimitMonitor should split inactive or zero limits into INVALID_LIMIT exceptions");
context.AssertTrue(sixStateResult.Findings.Any(finding => finding.Code == "LIMIT_NO_LIMIT_DETECTED"), "LimitMonitor should emit finding when real limit row is absent");
context.AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_EXPOSURE_NO_LIMIT") == 1, "WP-06 should flag exposure rows without matching limits");
context.AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_NONPOSITIVE_LIMIT") == 1, "WP-06 should flag non-positive limit rows");
context.AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_SUM_BALANCE") == 0, "WP-06 should preserve source-vs-analysis exposure balance for valid six-state inputs");
context.AssertTrue(!sixStateResult.Reconciliation.Passed, "WP-06 reconciliation should fail when a fail-code exception exists");
var repeatedSixStateResult = limitMonitor.Analyze(wp05ExposureCsv, wp05LimitCsv, "20260617");
context.AssertTrue(repeatedSixStateResult.Kpis == sixStateResult.Kpis, "LimitAnalysisResult KPIs should be deterministic for repeated inputs");
context.AssertTrue(repeatedSixStateResult.Rows.Select(row => row.StatusCode).SequenceEqual(sixStateResult.Rows.Select(row => row.StatusCode)), "LimitAnalysisResult monitoring rows should be deterministic for repeated inputs");
context.AssertTrue(ReconciliationSignature(repeatedSixStateResult) == ReconciliationSignature(sixStateResult), "WP-06 reconciliation summary should be deterministic for repeated inputs");
var coreTableResult = limitMonitor.Analyze(CsvReader.Read(wp05ExposureCsv), CsvReader.Read(wp05LimitCsv), "20260617");
context.AssertTrue(coreTableResult.Kpis == sixStateResult.Kpis, "LimitMonitor CsvTable core interface should match path overload results");

var boundaryExposureCsv = Path.Combine(limitSmokeDirectory, "qa_wp02_usage_boundary_exposure.csv");
var boundaryLimitCsv = Path.Combine(limitSmokeDirectory, "qa_wp02_usage_boundary_limit.csv");
WriteCsvRows(
    boundaryExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260617", "EQD", "PF_EDGE_NORMAL", "ELS", "RF_EDGE", "KRW", "89.99" },
        new[] { "20260617", "EQD", "PF_EDGE_WARN_LOW", "ELS", "RF_EDGE", "KRW", "90" },
        new[] { "20260617", "EQD", "PF_EDGE_WARN_HIGH", "ELS", "RF_EDGE", "KRW", "100" },
        new[] { "20260617", "EQD", "PF_EDGE_BREACH", "ELS", "RF_EDGE", "KRW", "101" }
    ]);
WriteCsvRows(
    boundaryLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_EDGE_NORMAL", "RF_EDGE", "100", "Y" },
        new[] { "20260617", "PF_EDGE_WARN_LOW", "RF_EDGE", "100", "Y" },
        new[] { "20260617", "PF_EDGE_WARN_HIGH", "RF_EDGE", "100", "Y" },
        new[] { "20260617", "PF_EDGE_BREACH", "RF_EDGE", "100", "Y" }
    ]);
var boundaryResult = limitMonitor.Analyze(boundaryExposureCsv, boundaryLimitCsv, "20260617");
context.AssertTrue(
    boundaryResult.Kpis is { NormalCount: 1, WarningCount: 2, BreachCount: 1 }
        && boundaryResult.Rows.Single(row => row.PortfolioId == "PF_EDGE_NORMAL").Status == LimitMonitorStatus.Normal
        && boundaryResult.Rows.Single(row => row.PortfolioId == "PF_EDGE_WARN_LOW").Status == LimitMonitorStatus.Warning
        && boundaryResult.Rows.Single(row => row.PortfolioId == "PF_EDGE_WARN_HIGH").Status == LimitMonitorStatus.Warning
        && boundaryResult.Rows.Single(row => row.PortfolioId == "PF_EDGE_BREACH").Status == LimitMonitorStatus.Breach,
    "LimitMonitor usage ratio boundary should classify NORMAL below 0.9, WARNING at 0.9 and 1.0, BREACH above 1.0");
context.AssertTrue(boundaryResult.Reconciliation.Passed, "RECON clean usage ratio boundary inputs should pass reconciliation");

var wp06CleanExposureRows = new[]
{
    new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
    new[] { "20260617", "EQD", "PF_CLEAN", "ELS", "RF_CLEAN", "KRW", "50" }
};
var wp06CleanLimitRows = new[]
{
    new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
    new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "100", "Y" }
};
var wp06CleanExposureCsv = Path.Combine(limitSmokeDirectory, "wp06_clean_exposure.csv");
var wp06CleanLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_clean_limit.csv");
WriteCsvRows(wp06CleanExposureCsv, wp06CleanExposureRows);
WriteCsvRows(wp06CleanLimitCsv, wp06CleanLimitRows);
var cleanReconciliationResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06CleanLimitCsv, "20260617");
context.AssertTrue(cleanReconciliationResult.ExceptionList.All(exception => !exception.Code.StartsWith("RECON_", StringComparison.Ordinal)), "WP-06 clean inputs should not emit reconciliation exceptions");
context.AssertTrue(cleanReconciliationResult.Reconciliation.Passed, "WP-06 clean inputs should pass reconciliation");
context.AssertTrue(cleanReconciliationResult.DuplicateLimitCount == 0, "LimitMonitor should report zero duplicate limit rows for unique limit keys");

var normalizedDashBaseDateResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06CleanLimitCsv, "2026-06-17");
context.AssertTrue(normalizedDashBaseDateResult.Rows.Single().Status == LimitMonitorStatus.Normal && normalizedDashBaseDateResult.BaseDate == "20260617", "LimitMonitor should normalize yyyy-MM-dd BASE_DT input before exact row matching");
context.AssertTrue(normalizedDashBaseDateResult.Metadata.JoinAudit.Any(audit => audit.Contains("valid=True", StringComparison.Ordinal) && audit.Contains("normalized=20260617", StringComparison.Ordinal)), "LimitMonitor JoinAudit should record valid BASE_DT normalization");
context.AssertTrue(normalizedDashBaseDateResult.Kpis == cleanReconciliationResult.Kpis && ReconciliationSignature(normalizedDashBaseDateResult) == ReconciliationSignature(cleanReconciliationResult), "LimitMonitor BASE_DT yyyy-MM-dd normalization should preserve limit KPIs and RECON signature");

var wp06OrphanLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_orphan_limit.csv");
WriteCsvRows(
    wp06OrphanLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "100", "Y" },
        new[] { "20260617", "PF_ORPHAN", "RF_ORPHAN", "100", "Y" }
    ]);
var orphanLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06OrphanLimitCsv, "20260617");
context.AssertTrue(ReconciliationExceptionCount(orphanLimitResult, "RECON_LIMIT_NO_EXPOSURE") == 1, "WP-06 should flag orphan limit rows");
context.AssertTrue(orphanLimitResult.Reconciliation.Passed, "WP-06 orphan limits should not fail reconciliation unless a fail-code exists");

var wp06DuplicateLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_duplicate_limit.csv");
WriteCsvRows(
    wp06DuplicateLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "100", "Y" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "120", "Y" }
    ]);
var duplicateLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06DuplicateLimitCsv, "20260617");
var duplicateLimitRow = duplicateLimitResult.Rows.Single();
context.AssertTrue(duplicateLimitRow.Status == LimitMonitorStatus.DuplicateLimit && duplicateLimitRow.StatusCode == "DUPLICATE_LIMIT", "LimitMonitor should block duplicate limit keys as DUPLICATE_LIMIT status");
context.AssertTrue(duplicateLimitResult.DuplicateLimitCount == 1 && duplicateLimitResult.Kpis.DuplicateLimitCount == 1, "LimitAnalysisResult KPIs should count duplicate limit rows");
context.AssertTrue(duplicateLimitRow.Status != LimitMonitorStatus.Normal && duplicateLimitRow.Status != LimitMonitorStatus.Breach, "LimitMonitor duplicate limit rows should not be classified as NORMAL or BREACH");
context.AssertTrue(duplicateLimitResult.ExceptionList.Any(exception => exception.Code == "DUPLICATE_LIMIT"), "LimitMonitor should include DUPLICATE_LIMIT exception for blocked duplicate joins");
context.AssertTrue(ReconciliationExceptionCount(duplicateLimitResult, "RECON_DUPLICATE_LIMIT") == 1, "WP-06 should flag duplicate limit join keys");
context.AssertTrue(ReconciliationExceptionCount(duplicateLimitResult, "RECON_ROW_AMPLIFICATION") == 1, "WP-06 should flag duplicate-limit row amplification risk");
context.AssertTrue(duplicateLimitResult.Rows.Count == 1 && duplicateLimitResult.Kpis.TotalCount == 1, "WP-06 duplicate limit checks should not change existing monitoring row counts");
context.AssertTrue(!duplicateLimitResult.Reconciliation.Passed, "WP-06 row amplification should fail reconciliation");
var duplicateJoinAudit = string.Join("|", duplicateLimitResult.Metadata.JoinAudit);
context.AssertTrue(
    duplicateJoinAudit.Contains("DuplicateLimitRule=blocked", StringComparison.Ordinal)
        && duplicateJoinAudit.Contains("duplicateLimitKeys=1", StringComparison.Ordinal)
        && duplicateJoinAudit.Contains("duplicateLimitRows=2", StringComparison.Ordinal)
        && duplicateJoinAudit.Contains("blockedExposureRows=1", StringComparison.Ordinal),
    "LimitMonitor JoinAudit should record duplicate limit key counts and blocked exposure rows");
var retiredDuplicateSelectionText = "group" + ".Last";
context.AssertTrue(!duplicateJoinAudit.Contains(retiredDuplicateSelectionText, StringComparison.OrdinalIgnoreCase), "LimitMonitor JoinAudit should not mention arbitrary duplicate selection");

var wp06OrphanDuplicateLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_orphan_duplicate_limit.csv");
WriteCsvRows(
    wp06OrphanDuplicateLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_ORPHAN_DUP", "RF_ORPHAN_DUP", "100", "Y" },
        new[] { "20260617", "PF_ORPHAN_DUP", "RF_ORPHAN_DUP", "120", "Y" }
    ]);
var orphanDuplicateLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06OrphanDuplicateLimitCsv, "20260617");
var orphanDuplicateJoinAudit = string.Join("|", orphanDuplicateLimitResult.Metadata.JoinAudit);
context.AssertTrue(
    ReconciliationExceptionCount(orphanDuplicateLimitResult, "RECON_DUPLICATE_LIMIT") == 1
        && orphanDuplicateJoinAudit.Contains("duplicateLimitKeys=1", StringComparison.Ordinal)
        && orphanDuplicateJoinAudit.Contains("blockedExposureRows=0", StringComparison.Ordinal),
    "LimitMonitor JoinAudit should count duplicate limit keys even when no exposure row is blocked");

var wp06MismatchExposureCsv = Path.Combine(limitSmokeDirectory, "wp06_basedate_mismatch_exposure.csv");
var wp06MismatchLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_basedate_mismatch_limit.csv");
WriteCsvRows(
    wp06MismatchExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_OLD", "ELS", "RF_OLD", "KRW", "50" }
    ]);
WriteCsvRows(
    wp06MismatchLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_OLD", "RF_OLD", "100", "Y" }
    ]);
var baseDateMismatchResult = limitMonitor.Analyze(wp06MismatchExposureCsv, wp06MismatchLimitCsv, "20260617");
context.AssertTrue(ReconciliationExceptionCount(baseDateMismatchResult, "RECON_BASEDATE_MISMATCH") >= 1, "WP-06 should flag requested BASE_DT missing when other dates exist");
context.AssertTrue(baseDateMismatchResult.Reconciliation.Passed, "WP-06 base-date mismatch should remain non-fail severity in R1");
var invalidBaseDateResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06CleanLimitCsv, "2026/06/17");
context.AssertTrue(invalidBaseDateResult.Findings.Any(finding => finding.Code == "LIMIT_BASEDATE_FORMAT_INVALID"), "LimitMonitor should gracefully surface invalid BASE_DT format as a finding");
context.AssertTrue(ReconciliationExceptionCount(invalidBaseDateResult, "RECON_BASEDATE_MISMATCH") >= 1, "LimitMonitor invalid BASE_DT format should be represented by non-fail reconciliation mismatch");
context.AssertTrue(invalidBaseDateResult.Metadata.JoinAudit.Any(audit => audit.Contains("valid=False", StringComparison.Ordinal)), "LimitMonitor JoinAudit should record invalid BASE_DT validation state");

var wp06BadAmountExposureCsv = Path.Combine(limitSmokeDirectory, "wp06_bad_amount_exposure.csv");
WriteCsvRows(
    wp06BadAmountExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260617", "EQD", "PF_BAD", "ELS", "RF_BAD", "KRW", "BAD" }
    ]);
var wp06BadAmountLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_bad_amount_limit.csv");
WriteCsvRows(
    wp06BadAmountLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_BAD", "RF_BAD", "100", "Y" }
    ]);
var sumBalanceResult = limitMonitor.Analyze(wp06BadAmountExposureCsv, wp06BadAmountLimitCsv, "20260617");
context.AssertTrue(ReconciliationExceptionCount(sumBalanceResult, "RECON_SUM_BALANCE") == 1, "WP-06 should fail sum balance when source exposure amount is nonnumeric");
context.AssertTrue(!sumBalanceResult.Reconciliation.Passed, "WP-06 sum balance exceptions should fail reconciliation");
context.AssertTrue(sumBalanceResult.MappingErrorCount == 1, "WP-06 sum balance test should preserve WP-05 MappingError classification");

var wp05ExposureXlsx = Path.Combine(limitSmokeDirectory, "wp05_exposure.xlsx");
var wp05LimitXlsx = Path.Combine(limitSmokeDirectory, "wp05_limit.xlsx");
CreateSingleSheetXlsx(wp05ExposureXlsx, wp05ExposureRows);
CreateSingleSheetXlsx(wp05LimitXlsx, wp05LimitRows);
var xlsxLimitResult = limitMonitor.Analyze(wp05ExposureXlsx, wp05LimitXlsx, "20260617");
context.AssertTrue(xlsxLimitResult.Kpis == sixStateResult.Kpis, "LimitMonitor .xlsx path overload should match .csv path overload results");

var r2Mapping = new ColumnMapping(new Dictionary<LogicalColumn, string>
{
    [LogicalColumn.BaseDate] = "BASE_DATE",
    [LogicalColumn.PortfolioId] = "PORT_ID",
    [LogicalColumn.RiskFactor] = "RISK_NAME",
    [LogicalColumn.ExposureAmount] = "EXPOSURE",
    [LogicalColumn.LimitAmount] = "LIMIT",
    [LogicalColumn.UseYn] = "ACTIVE_YN",
    [LogicalColumn.CurrencyCode] = "CCY_CUSTOM",
    [LogicalColumn.UnitCode] = "UNIT_CUSTOM"
});
var r2MappedExposureCsv = Path.Combine(limitSmokeDirectory, "r2_mapped_exposure.csv");
var r2MappedLimitMismatchCsv = Path.Combine(limitSmokeDirectory, "r2_mapped_limit_mismatch.csv");
var r2MappedLimitCleanCsv = Path.Combine(limitSmokeDirectory, "r2_mapped_limit_clean.csv");
WriteCsvRows(
    r2MappedExposureCsv,
    [
        new[] { "BASE_DATE", "PORT_ID", "RISK_NAME", "EXPOSURE", "CCY_CUSTOM", "UNIT_CUSTOM" },
        new[] { "20260617", "PF_R2", "RF_R2", "50", "KRW", "KRW_MN" }
    ]);
WriteCsvRows(
    r2MappedLimitMismatchCsv,
    [
        new[] { "BASE_DATE", "PORT_ID", "RISK_NAME", "LIMIT", "ACTIVE_YN", "CCY_CUSTOM", "UNIT_CUSTOM" },
        new[] { "20260617", "PF_R2", "RF_R2", "100", "Y", "USD", "KRW" }
    ]);
WriteCsvRows(
    r2MappedLimitCleanCsv,
    [
        new[] { "BASE_DATE", "PORT_ID", "RISK_NAME", "LIMIT", "ACTIVE_YN", "CCY_CUSTOM", "UNIT_CUSTOM" },
        new[] { "20260617", "PF_R2", "RF_R2", "100", "Y", "KRW", "KRW_MN" }
    ]);
var mappedMismatchResult = new LimitMonitor(r2Mapping).Analyze(r2MappedExposureCsv, r2MappedLimitMismatchCsv, "20260617");
context.AssertTrue(mappedMismatchResult.Rows.Single().CurrencyCode == "KRW", "LimitMonitor should read mapped CurrencyCode from custom physical column");
context.AssertTrue(ReconciliationCheckFor(mappedMismatchResult, "RECON_CURRENCY_MISMATCH").Applicable && ReconciliationExceptionCount(mappedMismatchResult, "RECON_CURRENCY_MISMATCH") == 1, "LimitMonitor should use ColumnMapping for currency reconciliation");
context.AssertTrue(ReconciliationCheckFor(mappedMismatchResult, "RECON_UNIT_MISMATCH").Applicable && ReconciliationExceptionCount(mappedMismatchResult, "RECON_UNIT_MISMATCH") == 1, "LimitMonitor should activate RECON_UNIT_MISMATCH when mapped unit columns differ");
context.AssertTrue(mappedMismatchResult.Reconciliation.Passed, "RECON currency and unit mismatch should remain non-fail when no fail-code exceptions exist");
var mappedCleanResult = new LimitMonitor(r2Mapping).Analyze(r2MappedExposureCsv, r2MappedLimitCleanCsv, "20260617");
context.AssertTrue(ReconciliationCheckFor(mappedCleanResult, "RECON_UNIT_MISMATCH").Applicable && ReconciliationExceptionCount(mappedCleanResult, "RECON_UNIT_MISMATCH") == 0, "LimitMonitor should not emit RECON_UNIT_MISMATCH when mapped unit values match");
var sixOnlyMapping = new ColumnMapping(new Dictionary<LogicalColumn, string>
{
    [LogicalColumn.BaseDate] = "BASE_DT",
    [LogicalColumn.PortfolioId] = "PORTFOLIO_ID",
    [LogicalColumn.RiskFactor] = "RISK_FACTOR",
    [LogicalColumn.ExposureAmount] = "EXPOSURE_AMT",
    [LogicalColumn.LimitAmount] = "LIMIT_AMT",
    [LogicalColumn.UseYn] = "USE_YN"
});
var sixOnlyOptionalResult = new LimitMonitor(sixOnlyMapping).Analyze(wp06CleanExposureCsv, wp06CleanLimitCsv, "20260617");
context.AssertTrue(
    !ReconciliationCheckFor(sixOnlyOptionalResult, "RECON_CURRENCY_MISMATCH").Applicable
        && !ReconciliationCheckFor(sixOnlyOptionalResult, "RECON_UNIT_MISMATCH").Applicable,
    "LimitMonitor should keep optional currency and unit reconciliation inactive for six-column mappings");
var repeatedDuplicateLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06DuplicateLimitCsv, "20260617");
context.AssertTrue(
    repeatedDuplicateLimitResult.Rows.Select(row => row.StatusCode).SequenceEqual(duplicateLimitResult.Rows.Select(row => row.StatusCode))
        && repeatedDuplicateLimitResult.Metadata.JoinAudit.SequenceEqual(duplicateLimitResult.Metadata.JoinAudit),
    "LimitMonitor duplicate limit status and JoinAudit should be deterministic for repeated inputs");

var mappingErrorExposureCsv = Path.Combine(limitSmokeDirectory, "wp05_missing_amount.csv");
File.WriteAllText(
    mappingErrorExposureCsv,
    "BASE_DT,DESK_CD,PORTFOLIO_ID,PRODUCT_TYPE,RISK_FACTOR,CCY_CD\n20260617,EQD,PF_MAP,ELS,RF_MAP,KRW\n");
var mappingErrorResult = limitMonitor.Analyze(mappingErrorExposureCsv, wp05LimitCsv, "20260617");
context.AssertTrue(mappingErrorResult.MappingErrorCount == 1, "LimitMonitor should return graceful MAPPING_ERROR for missing mapped physical columns");
context.AssertTrue(mappingErrorResult.ExceptionList.Any(exception => exception.Code == "MAPPING_ERROR" && exception.Severity == SafetySeverity.High), "LimitMonitor should include high severity MappingError exception");
context.AssertTrue(mappingErrorResult.Findings.Any(finding => finding.Code == "LIMIT_MAPPING_ERROR"), "LimitMonitor should include MappingError finding instead of throwing");

var priorDayAnalyzer = new PriorDayAnalyzer();
var priorDayDirectory = Path.Combine("artifacts", "smoke-prior-day-r2-wp03");
Directory.CreateDirectory(priorDayDirectory);

var priorDayComparisonExposureCsv = Path.Combine(priorDayDirectory, "prior_day_comparison_exposure.csv");
var priorDayComparisonLimitCsv = Path.Combine(priorDayDirectory, "prior_day_comparison_limit.csv");
WriteCsvRows(
    priorDayComparisonExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_INC", "ELS", "RF_DELTA", "KRW", "40" },
        new[] { "20260617", "EQD", "PF_INC", "ELS", "RF_DELTA", "KRW", "60" },
        new[] { "20260616", "EQD", "PF_DEC", "ELS", "RF_DELTA", "KRW", "80" },
        new[] { "20260617", "EQD", "PF_DEC", "ELS", "RF_DELTA", "KRW", "30" },
        new[] { "20260616", "EQD", "PF_UNCH", "ELS", "RF_DELTA", "KRW", "50" },
        new[] { "20260617", "EQD", "PF_UNCH", "ELS", "RF_DELTA", "KRW", "50" }
    ]);
WriteCsvRows(
    priorDayComparisonLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_INC", "RF_DELTA", "100", "Y" },
        new[] { "20260617", "PF_INC", "RF_DELTA", "100", "Y" },
        new[] { "20260616", "PF_DEC", "RF_DELTA", "100", "Y" },
        new[] { "20260617", "PF_DEC", "RF_DELTA", "100", "Y" },
        new[] { "20260616", "PF_UNCH", "RF_DELTA", "100", "Y" },
        new[] { "20260617", "PF_UNCH", "RF_DELTA", "100", "Y" }
    ]);
var priorDayComparison = priorDayAnalyzer.Analyze(priorDayComparisonExposureCsv, priorDayComparisonLimitCsv, "20260617", "20260616");
var priorDayIncrease = priorDayComparison.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_INC");
var priorDayDecrease = priorDayComparison.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_DEC");
var priorDayUnchanged = priorDayComparison.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_UNCH");
context.AssertTrue(priorDayIncrease.Movement == PriorDayMovement.Increased && priorDayIncrease.UsageRatioDelta == 0.2m && priorDayIncrease.ExposureAmountDelta == 20m, "prior-day limit comparison should calculate Increased usage and exposure delta");
context.AssertTrue(priorDayDecrease.Movement == PriorDayMovement.Decreased && priorDayDecrease.UsageRatioDelta == -0.5m && priorDayDecrease.ExposureAmountDelta == -50m, "prior-day limit comparison should calculate Decreased usage and exposure delta");
context.AssertTrue(priorDayUnchanged.Movement == PriorDayMovement.Unchanged && priorDayUnchanged.UsageRatioDelta == 0m, "prior-day limit comparison should classify unchanged usage ratio");
context.AssertTrue(priorDayComparison.Contract.DataFact.Kpis is { ComparedCount: 3, IncreasedCount: 1, DecreasedCount: 1, UnchangedCount: 1 }, "prior-day LimitMonitor KPIs should summarize comparison movements");

var priorDayNewResolvedExposureCsv = Path.Combine(priorDayDirectory, "prior_day_new_resolved_exposure.csv");
var priorDayNewResolvedLimitCsv = Path.Combine(priorDayDirectory, "prior_day_new_resolved_limit.csv");
WriteCsvRows(
    priorDayNewResolvedExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260617", "EQD", "PF_NEW", "ELS", "RF_FLOW", "KRW", "20" },
        new[] { "20260616", "EQD", "PF_RESOLVED", "ELS", "RF_FLOW", "KRW", "40" }
    ]);
WriteCsvRows(
    priorDayNewResolvedLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_NEW", "RF_FLOW", "100", "Y" },
        new[] { "20260616", "PF_RESOLVED", "RF_FLOW", "100", "Y" }
    ]);
var priorDayNewResolved = priorDayAnalyzer.Analyze(priorDayNewResolvedExposureCsv, priorDayNewResolvedLimitCsv, "20260617", "20260616");
var priorDayNew = priorDayNewResolved.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_NEW");
var priorDayResolved = priorDayNewResolved.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_RESOLVED");
context.AssertTrue(priorDayNew.Movement == PriorDayMovement.New && priorDayNew.PriorStatus is null && priorDayNew.PriorExposureAmount == 0m, "prior-day limit comparison should classify New rows and zero missing prior values");
context.AssertTrue(priorDayResolved.Movement == PriorDayMovement.Resolved && priorDayResolved.CurrentStatus is null && priorDayResolved.CurrentExposureAmount == 0m, "prior-day limit comparison should classify Resolved rows and zero missing current values");

var priorDayTopNExposureCsv = Path.Combine(priorDayDirectory, "prior_day_topn_exposure.csv");
var priorDayTopNLimitCsv = Path.Combine(priorDayDirectory, "prior_day_topn_limit.csv");
WriteCsvRows(
    priorDayTopNExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_BIG", "ELS", "RF_TOP", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_BIG", "ELS", "RF_TOP", "KRW", "50" },
        new[] { "20260616", "EQD", "PF_A", "ELS", "RF_TOP", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_A", "ELS", "RF_TOP", "KRW", "30" },
        new[] { "20260616", "EQD", "PF_B", "ELS", "RF_TOP", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_B", "ELS", "RF_TOP", "KRW", "30" }
    ]);
WriteCsvRows(
    priorDayTopNLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_BIG", "RF_TOP", "100", "Y" },
        new[] { "20260617", "PF_BIG", "RF_TOP", "100", "Y" },
        new[] { "20260616", "PF_A", "RF_TOP", "100", "Y" },
        new[] { "20260617", "PF_A", "RF_TOP", "100", "Y" },
        new[] { "20260616", "PF_B", "RF_TOP", "100", "Y" },
        new[] { "20260617", "PF_B", "RF_TOP", "100", "Y" }
    ]);
var priorDayTopN = priorDayAnalyzer.Analyze(priorDayTopNExposureCsv, priorDayTopNLimitCsv, "20260617", "20260616", topN: 3);
context.AssertTrue(priorDayTopN.Contract.DataFact.Movers.TopByUsageRatioDelta.Select(row => row.PortfolioId).SequenceEqual(["PF_BIG", "PF_A", "PF_B"]), "prior-day limit TopN movers should order by absolute usage delta then PortfolioId");

var priorDayTransitionExposureCsv = Path.Combine(priorDayDirectory, "prior_day_transition_exposure.csv");
var priorDayTransitionLimitCsv = Path.Combine(priorDayDirectory, "prior_day_transition_limit.csv");
WriteCsvRows(
    priorDayTransitionExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_TRANS", "ELS", "RF_STATE", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_TRANS", "ELS", "RF_STATE", "KRW", "10" },
        new[] { "20260616", "EQD", "PF_DUP", "ELS", "RF_STATE", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_DUP", "ELS", "RF_STATE", "KRW", "10" }
    ]);
WriteCsvRows(
    priorDayTransitionLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_TRANS", "RF_STATE", "100", "Y" },
        new[] { "20260616", "PF_DUP", "RF_STATE", "100", "Y" },
        new[] { "20260616", "PF_DUP", "RF_STATE", "120", "Y" },
        new[] { "20260617", "PF_DUP", "RF_STATE", "100", "Y" },
        new[] { "20260617", "PF_DUP", "RF_STATE", "120", "Y" }
    ]);
var priorDayTransition = priorDayAnalyzer.Analyze(priorDayTransitionExposureCsv, priorDayTransitionLimitCsv, "20260617", "20260616");
var priorDayNormalToNoLimit = priorDayTransition.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_TRANS");
var priorDayDuplicateLimit = priorDayTransition.Contract.DataFact.ComparisonTable.Single(row => row.PortfolioId == "PF_DUP");
context.AssertTrue(priorDayNormalToNoLimit.Movement == PriorDayMovement.StateTransition && priorDayNormalToNoLimit.CurrentStatus == LimitMonitorStatus.NoLimit, "prior-day limit state-transition should classify Normal to NoLimit as non-numeric movement");
context.AssertTrue(priorDayDuplicateLimit.Movement == PriorDayMovement.StateTransition && priorDayDuplicateLimit.CurrentStatus == LimitMonitorStatus.DuplicateLimit && priorDayDuplicateLimit.PriorStatus == LimitMonitorStatus.DuplicateLimit, "prior-day limit state-transition should treat DuplicateLimit as non-numeric even on both days");
context.AssertTrue(priorDayTransition.Contract.DataFact.Movers.TopByUsageRatioDelta.All(row => row.Movement != PriorDayMovement.StateTransition) && priorDayTransition.Contract.HiddenRisk.Findings.Any(finding => finding.Code == "PRIOR_DAY_STATE_TRANSITION"), "prior-day limit movers should exclude StateTransition rows and expose Hidden-Risk findings");

var priorDayBaseDateMismatchExposureCsv = Path.Combine(priorDayDirectory, "prior_day_basedate_mismatch_exposure.csv");
var priorDayBaseDateMismatchLimitCsv = Path.Combine(priorDayDirectory, "prior_day_basedate_mismatch_limit.csv");
WriteCsvRows(
    priorDayBaseDateMismatchExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260617", "EQD", "PF_CURRENT_ONLY", "ELS", "RF_DATE", "KRW", "10" }
    ]);
WriteCsvRows(
    priorDayBaseDateMismatchLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_CURRENT_ONLY", "RF_DATE", "100", "Y" }
    ]);
var priorDayBaseDateMismatch = priorDayAnalyzer.Analyze(priorDayBaseDateMismatchExposureCsv, priorDayBaseDateMismatchLimitCsv, "20260617", "20260615");
context.AssertTrue(priorDayBaseDateMismatch.Contract.DataFact.Kpis.NewCount == 1 && priorDayBaseDateMismatch.Prior.Rows.Count == 0, "prior-day limit BASE_DT mismatch should keep matched current rows as New when prior date selects no rows");
context.AssertTrue(priorDayBaseDateMismatch.Contract.HiddenRisk.Findings.Any(finding => finding.Code == "BASE_DT_FORMAT_MISMATCH"), "prior-day limit BASE_DT mismatch should emit Hidden-Risk finding without arbitrary correction");
context.AssertTrue(context.Throws<ArgumentException>(() => priorDayAnalyzer.Analyze(priorDayBaseDateMismatchExposureCsv, priorDayBaseDateMismatchLimitCsv, "20260617", "2026-06-17")), "prior-day LimitMonitor should reject same-day comparison after BASE_DT normalization");

var priorDayDeterministicExposureCsv = Path.Combine(priorDayDirectory, "prior_day_deterministic_exposure.csv");
var priorDayDeterministicLimitCsv = Path.Combine(priorDayDirectory, "prior_day_deterministic_limit.csv");
WriteCsvRows(
    priorDayDeterministicExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_LIMIT_ONLY", "ELS", "RF_LIMIT", "KRW", "50" },
        new[] { "20260617", "EQD", "PF_LIMIT_ONLY", "ELS", "RF_LIMIT", "KRW", "50" }
    ]);
WriteCsvRows(
    priorDayDeterministicLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_LIMIT_ONLY", "RF_LIMIT", "100", "Y" },
        new[] { "20260617", "PF_LIMIT_ONLY", "RF_LIMIT", "80", "Y" }
    ]);
var priorDayDeterministicA = priorDayAnalyzer.Analyze(priorDayDeterministicExposureCsv, priorDayDeterministicLimitCsv, "2026-06-17", "20260616");
var priorDayDeterministicB = priorDayAnalyzer.Analyze(priorDayDeterministicExposureCsv, priorDayDeterministicLimitCsv, "2026-06-17", "20260616");
var priorDayLimitOnly = priorDayDeterministicA.Contract.DataFact.ComparisonTable.Single();
context.AssertTrue(PriorDaySignature(priorDayDeterministicA) == PriorDaySignature(priorDayDeterministicB), "prior-day 4-section limit contract should be deterministic for repeated inputs");
context.AssertTrue(priorDayDeterministicA.Contract.Methodology.DraftNotice.Contains("검토용 초안", StringComparison.Ordinal) && priorDayDeterministicA.Contract.UserValidation.ChecklistItems.Count >= 3, "prior-day 4-section limit contract should include draft notice and user validation checklist");
context.AssertTrue(priorDayDeterministicA.Current.DuplicateLimitCount == priorDayDeterministicA.Current.Kpis.DuplicateLimitCount && priorDayDeterministicA.Prior.DuplicateLimitCount == priorDayDeterministicA.Prior.Kpis.DuplicateLimitCount, "prior-day 4-section limit contract should preserve seven-state LimitAnalysisResult counters");
context.AssertTrue(priorDayLimitOnly.LimitAmountDelta == -20m && priorDayLimitOnly.ExposureAmountDelta == 0m && priorDayLimitOnly.Movement == PriorDayMovement.Increased, "prior-day limit delta should capture limit-only changes without exposure delta");

var priorDayDupKeyExposureCsv = Path.Combine(priorDayDirectory, "prior_day_duplicate_key_exposure.csv");
var priorDayDupKeyLimitCsv = Path.Combine(priorDayDirectory, "prior_day_duplicate_key_limit.csv");
WriteCsvRows(
    priorDayDupKeyExposureCsv,
    [
        new[] { "BASE_DT", "DESK_CD", "PORTFOLIO_ID", "PRODUCT_TYPE", "RISK_FACTOR", "CCY_CD", "EXPOSURE_AMT" },
        new[] { "20260616", "EQD", "PF_MULTI", "ELS", "RF_MULTI", "KRW", "10" },
        new[] { "20260617", "EQD", "PF_MULTI", "ELS", "RF_MULTI", "KRW", "20" },
        new[] { "20260617", "EQD", "PF_MULTI", "ELS", "RF_MULTI", "KRW", "30" }
    ]);
WriteCsvRows(
    priorDayDupKeyLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260616", "PF_MULTI", "RF_MULTI", "100", "Y" },
        new[] { "20260617", "PF_MULTI", "RF_MULTI", "100", "Y" }
    ]);
var priorDayDupKey = priorDayAnalyzer.Analyze(priorDayDupKeyExposureCsv, priorDayDupKeyLimitCsv, "20260617", "20260616");
context.AssertTrue(
    priorDayDupKey.Contract.HiddenRisk.Findings.Any(finding => finding.Code == "PRIOR_DAY_DUPLICATE_KEY")
        && priorDayDupKey.Contract.DataFact.ComparisonTable.Count(row => row.PortfolioId == "PF_MULTI") == 1,
    "prior-day limit duplicate join key should emit Hidden-Risk finding and compare deterministically");
    }
}

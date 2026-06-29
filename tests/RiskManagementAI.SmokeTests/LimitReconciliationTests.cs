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
context.AssertTrue(ReconciliationExceptionCount(limitMonitorResult, "RECON_BASEDATE_MISMATCH") == 0, "LimitMonitor should not flag normal multi-date exports when requested BASE_DT exists");
context.AssertTrue(
    !ReconciliationCheckFor(limitMonitorResult, "RECON_CURRENCY_MISMATCH").Applicable
        && !ReconciliationCheckFor(limitMonitorResult, "RECON_UNIT_MISMATCH").Applicable,
    "LimitMonitor should mark currency and unit reconciliation as N/A for R1 default inputs");

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
context.AssertTrue(duplicateJoinAudit.Contains("DuplicateLimitRule=blocked", StringComparison.Ordinal) && duplicateJoinAudit.Contains("duplicateExposureRows=1", StringComparison.Ordinal), "LimitMonitor JoinAudit should record duplicate limit blocking policy");
var retiredDuplicateSelectionText = "group" + ".Last";
context.AssertTrue(!duplicateJoinAudit.Contains(retiredDuplicateSelectionText, StringComparison.OrdinalIgnoreCase), "LimitMonitor JoinAudit should not mention arbitrary duplicate selection");

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
    }
}

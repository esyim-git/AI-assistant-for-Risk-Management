using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed record LimitAnalysisResult(
    string BaseDate,
    IReadOnlyList<LimitMonitorRow> MonitoringTable,
    LimitAnalysisKpis Kpis,
    LimitAnalysisMetadata Metadata,
    IReadOnlyList<LimitException> ExceptionList,
    IReadOnlyList<SafetyFinding> Findings,
    ReconciliationSummary Reconciliation)
{
    public IReadOnlyList<LimitMonitorRow> Rows => MonitoringTable;

    public int NormalCount => Kpis.NormalCount;

    public int WarningCount => Kpis.WarningCount;

    public int BreachCount => Kpis.BreachCount;

    public int NoLimitCount => Kpis.NoLimitCount;

    public int InvalidLimitCount => Kpis.InvalidLimitCount;

    public int MappingErrorCount => Kpis.MappingErrorCount;
}

public sealed record LimitAnalysisKpis(
    int TotalCount,
    int NormalCount,
    int WarningCount,
    int BreachCount,
    int NoLimitCount,
    int InvalidLimitCount,
    int MappingErrorCount,
    decimal ExposureAmountSum,
    decimal LimitAmountSum,
    decimal RemainingLimitSum)
{
    public static LimitAnalysisKpis FromRows(IReadOnlyList<LimitMonitorRow> rows)
    {
        return new LimitAnalysisKpis(
            rows.Count,
            rows.Count(row => row.Status == LimitMonitorStatus.Normal),
            rows.Count(row => row.Status == LimitMonitorStatus.Warning),
            rows.Count(row => row.Status == LimitMonitorStatus.Breach),
            rows.Count(row => row.Status == LimitMonitorStatus.NoLimit),
            rows.Count(row => row.Status == LimitMonitorStatus.InvalidLimit),
            rows.Count(row => row.Status == LimitMonitorStatus.MappingError),
            rows.Sum(row => row.ExposureAmount),
            rows.Sum(row => row.LimitAmount),
            rows.Sum(row => row.RemainingLimit));
    }
}

public sealed record LimitAnalysisMetadata(
    string BaseDate,
    string ExposureSourceName,
    string LimitSourceName,
    bool ColumnMappingUsedFallback,
    IReadOnlyList<string> ColumnMappingWarnings,
    bool IsDeterministic);

public sealed record LimitException(
    string Code,
    SafetySeverity Severity,
    string Message,
    string BaseDate,
    string PortfolioId,
    string RiskFactor);

public sealed record ReconciliationSummary(
    bool Passed,
    int CheckCount,
    IReadOnlyList<ReconciliationCheck> Checks);

public sealed record ReconciliationCheck(
    string Code,
    bool Applicable,
    int ExceptionCount,
    SafetySeverity MaxSeverity);

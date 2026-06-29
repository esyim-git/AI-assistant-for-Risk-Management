using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed class PriorDayAnalyzer
{
    private const string DraftNotice = "본 결과는 공식 해석이 아니라 검토용 초안입니다.";

    private static readonly HashSet<LimitMonitorStatus> NumericStatuses =
    [
        LimitMonitorStatus.Normal,
        LimitMonitorStatus.Warning,
        LimitMonitorStatus.Breach
    ];

    private static readonly HashSet<LimitMonitorStatus> NonNumericStatuses =
    [
        LimitMonitorStatus.NoLimit,
        LimitMonitorStatus.InvalidLimit,
        LimitMonitorStatus.MappingError,
        LimitMonitorStatus.DuplicateLimit
    ];

    private readonly LimitMonitor limitMonitor;

    public PriorDayAnalyzer()
        : this(new LimitMonitor())
    {
    }

    public PriorDayAnalyzer(LimitMonitor limitMonitor)
    {
        this.limitMonitor = limitMonitor ?? throw new ArgumentNullException(nameof(limitMonitor));
    }

    public PriorDayAnalysisResult Analyze(
        CsvTable exposure,
        CsvTable limit,
        string currentBaseDate,
        string priorBaseDate,
        int topN = 10)
    {
        ArgumentNullException.ThrowIfNull(exposure);
        ArgumentNullException.ThrowIfNull(limit);
        ValidateBaseDateArgument(currentBaseDate, nameof(currentBaseDate));
        ValidateBaseDateArgument(priorBaseDate, nameof(priorBaseDate));

        var current = limitMonitor.Analyze(exposure, limit, currentBaseDate);
        var prior = limitMonitor.Analyze(exposure, limit, priorBaseDate);
        return BuildResult(current, prior, topN);
    }

    public PriorDayAnalysisResult Analyze(
        string exposurePath,
        string limitPath,
        string currentBaseDate,
        string priorBaseDate,
        int topN = 10)
    {
        ValidateBaseDateArgument(currentBaseDate, nameof(currentBaseDate));
        ValidateBaseDateArgument(priorBaseDate, nameof(priorBaseDate));

        var current = limitMonitor.Analyze(exposurePath, limitPath, currentBaseDate);
        var prior = limitMonitor.Analyze(exposurePath, limitPath, priorBaseDate);
        return BuildResult(current, prior, topN);
    }

    private static PriorDayAnalysisResult BuildResult(
        LimitAnalysisResult current,
        LimitAnalysisResult prior,
        int topN)
    {
        if (string.Equals(current.BaseDate, prior.BaseDate, StringComparison.Ordinal))
        {
            throw new ArgumentException($"전일 대비 분석은 서로 다른 기준일이 필요합니다. normalizedBaseDate={current.BaseDate}");
        }

        var hiddenRisks = new List<SafetyFinding>();
        if (current.MonitoringTable.Count == 0)
        {
            hiddenRisks.Add(new SafetyFinding(
                "BASE_DT_FORMAT_MISMATCH",
                SafetySeverity.Low,
                $"Current BASE_DT={current.BaseDate} 선택 행이 없습니다. 데이터 BASE_DT 형식 또는 기준일 범위를 확인해야 합니다."));
        }

        if (prior.MonitoringTable.Count == 0)
        {
            hiddenRisks.Add(new SafetyFinding(
                "BASE_DT_FORMAT_MISMATCH",
                SafetySeverity.Low,
                $"Prior BASE_DT={prior.BaseDate} 선택 행이 없습니다. 데이터 BASE_DT 형식 또는 기준일 범위를 확인해야 합니다."));
        }

        var currentRows = BuildRowDictionary(current.MonitoringTable);
        var priorRows = BuildRowDictionary(prior.MonitoringTable);
        var keys = currentRows.Keys
            .Union(priorRows.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => RowForKey(key, currentRows, priorRows).PortfolioId, StringComparer.Ordinal)
            .ThenBy(key => RowForKey(key, currentRows, priorRows).RiskFactor, StringComparer.Ordinal)
            .ToArray();

        var rows = new List<PriorDayComparisonRow>();
        foreach (var key in keys)
        {
            currentRows.TryGetValue(key, out var currentRow);
            priorRows.TryGetValue(key, out var priorRow);
            var comparison = BuildComparisonRow(current.BaseDate, prior.BaseDate, currentRow, priorRow);
            rows.Add(comparison);

            if (comparison.Movement == PriorDayMovement.StateTransition)
            {
                hiddenRisks.Add(new SafetyFinding(
                    "PRIOR_DAY_STATE_TRANSITION",
                    SafetySeverity.Medium,
                    $"전일 대비 상태전이 검토 필요: {comparison.PortfolioId}/{comparison.RiskFactor}, prior={comparison.PriorStatus?.ToString() ?? "None"}, current={comparison.CurrentStatus?.ToString() ?? "None"}."));
            }
        }

        var orderedRows = rows
            .OrderBy(row => row.PortfolioId, StringComparer.Ordinal)
            .ThenBy(row => row.RiskFactor, StringComparer.Ordinal)
            .ToArray();
        var movers = orderedRows
            .Where(IsNumericMover)
            .OrderByDescending(row => Math.Abs(row.UsageRatioDelta))
            .ThenBy(row => row.PortfolioId, StringComparer.Ordinal)
            .ThenBy(row => row.RiskFactor, StringComparer.Ordinal)
            .Take(Math.Max(0, topN))
            .ToArray();
        var contract = new PriorDayContract(
            new PriorDayDataFact(
                current.BaseDate,
                prior.BaseDate,
                PriorDayKpis.FromRows(orderedRows),
                orderedRows,
                new PriorDayMovers(movers)),
            new PriorDayMethodology(
                "priorBaseDate는 호출자가 명시하며 달력/영업일 자동 산출을 하지 않습니다.",
                "JoinKey=Trim(PortfolioId)+U+001F+Trim(RiskFactor), OrdinalIgnoreCase pairing, output Ordinal sort.",
                "TopN movers는 New/Resolved/StateTransition 제외 후 |UsageRatioDelta| 내림차순, PortfolioId/RiskFactor Ordinal tie-break입니다.",
                DraftNotice),
            new PriorDayUserValidation(
            [
                "Current/Prior BASE_DT가 의도한 영업일인지 확인합니다.",
                "StateTransition 및 Hidden-Risk 항목은 숫자 증감으로 해석하지 않고 원천 한도/매핑 데이터를 확인합니다.",
                "TopN movers는 사용률 변화 기준이며 한도만 변경된 행은 LimitAmountDelta를 함께 검토합니다.",
                "본 결과는 검토용 초안이며 공식 보고 전 사용자가 원천 데이터를 재확인해야 합니다."
            ]),
            new PriorDayHiddenRisk(
                hiddenRisks
                    .OrderBy(finding => finding.Code, StringComparer.Ordinal)
                    .ThenBy(finding => finding.Message, StringComparer.Ordinal)
                    .ToArray()));

        return new PriorDayAnalysisResult(contract, current, prior, IsDeterministic: true);
    }

    private static PriorDayComparisonRow BuildComparisonRow(
        string currentBaseDate,
        string priorBaseDate,
        LimitMonitorRow? current,
        LimitMonitorRow? prior)
    {
        var reference = current ?? prior ?? throw new InvalidOperationException("Prior-day comparison row requires at least one side.");
        var currentStatus = current?.Status;
        var priorStatus = prior?.Status;
        var movement = ClassifyMovement(currentStatus, priorStatus, current?.UsageRatio ?? 0m, prior?.UsageRatio ?? 0m);

        return new PriorDayComparisonRow(
            reference.PortfolioId,
            reference.RiskFactor,
            currentBaseDate,
            priorBaseDate,
            currentStatus,
            priorStatus,
            current?.UsageRatio ?? 0m,
            prior?.UsageRatio ?? 0m,
            (current?.UsageRatio ?? 0m) - (prior?.UsageRatio ?? 0m),
            current?.ExposureAmount ?? 0m,
            prior?.ExposureAmount ?? 0m,
            (current?.ExposureAmount ?? 0m) - (prior?.ExposureAmount ?? 0m),
            current?.LimitAmount ?? 0m,
            prior?.LimitAmount ?? 0m,
            (current?.LimitAmount ?? 0m) - (prior?.LimitAmount ?? 0m),
            current?.RemainingLimit ?? 0m,
            prior?.RemainingLimit ?? 0m,
            (current?.RemainingLimit ?? 0m) - (prior?.RemainingLimit ?? 0m),
            movement);
    }

    private static PriorDayMovement ClassifyMovement(
        LimitMonitorStatus? currentStatus,
        LimitMonitorStatus? priorStatus,
        decimal currentUsageRatio,
        decimal priorUsageRatio)
    {
        if (currentStatus is null)
        {
            return PriorDayMovement.Resolved;
        }

        if (priorStatus is null)
        {
            return PriorDayMovement.New;
        }

        if (IsNonNumeric(currentStatus.Value)
            || IsNonNumeric(priorStatus.Value)
            || currentStatus.Value != priorStatus.Value)
        {
            return PriorDayMovement.StateTransition;
        }

        var delta = currentUsageRatio - priorUsageRatio;
        if (delta > 0m)
        {
            return PriorDayMovement.Increased;
        }

        return delta < 0m ? PriorDayMovement.Decreased : PriorDayMovement.Unchanged;
    }

    private static bool IsNumericMover(PriorDayComparisonRow row)
    {
        return row.Movement is PriorDayMovement.Increased or PriorDayMovement.Decreased or PriorDayMovement.Unchanged;
    }

    private static bool IsNonNumeric(LimitMonitorStatus status)
    {
        return NonNumericStatuses.Contains(status) || !NumericStatuses.Contains(status);
    }

    private static IReadOnlyDictionary<string, LimitMonitorRow> BuildRowDictionary(IReadOnlyList<LimitMonitorRow> rows)
    {
        return rows
            .GroupBy(row => LimitMonitor.BuildComparisonKey(row.PortfolioId, row.RiskFactor), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => row.PortfolioId, StringComparer.Ordinal)
                    .ThenBy(row => row.RiskFactor, StringComparer.Ordinal)
                    .ThenBy(row => row.StatusCode, StringComparer.Ordinal)
                    .First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static LimitMonitorRow RowForKey(
        string key,
        IReadOnlyDictionary<string, LimitMonitorRow> currentRows,
        IReadOnlyDictionary<string, LimitMonitorRow> priorRows)
    {
        return currentRows.TryGetValue(key, out var current)
            ? current
            : priorRows[key];
    }

    private static void ValidateBaseDateArgument(string baseDate, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(baseDate))
        {
            throw new ArgumentException("전일 대비 분석 기준일이 비어 있습니다.", parameterName);
        }
    }
}

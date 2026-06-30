using RiskManagementAI.Core.Risk;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Report;

public static class RiskVisualAggregator
{
    public const int DefaultTopN = 10;

    public static RiskVisualModel Aggregate(LimitAnalysisResult result, int topN = DefaultTopN)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (topN < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topN), "TopN은 0 이상이어야 합니다.");
        }

        var rows = result.Rows
            .OrderBy(row => row.PortfolioId, StringComparer.Ordinal)
            .ThenBy(row => row.RiskFactor, StringComparer.Ordinal)
            .ThenBy(row => row.BaseDate, StringComparer.Ordinal)
            .ToArray();
        var rowCount = rows.Length;
        var statusDistribution = Enum.GetValues<LimitMonitorStatus>()
            .Select(status =>
            {
                var count = rows.Count(row => row.Status == status);
                return new RiskVisualStatusDistributionRow(
                    ToStatusCode(status),
                    count,
                    rowCount == 0 ? 0m : RoundRatio(count / (decimal)rowCount));
            })
            .ToArray();

        var topExposureRows = rows
            .OrderByDescending(row => Math.Abs(row.ExposureAmount))
            .ThenBy(row => row.PortfolioId, StringComparer.Ordinal)
            .ThenBy(row => row.RiskFactor, StringComparer.Ordinal)
            .ThenBy(row => row.BaseDate, StringComparer.Ordinal)
            .Take(topN)
            .Select((row, index) => new RiskVisualTopExposureRow(
                index + 1,
                row.BaseDate,
                row.PortfolioId,
                row.RiskFactor,
                row.CurrencyCode,
                row.ExposureAmount,
                Math.Abs(row.ExposureAmount),
                row.UsageRatio,
                row.StatusCode))
            .ToArray();

        var totalAbsoluteExposure = rows.Sum(row => Math.Abs(row.ExposureAmount));
        var topNAbsoluteExposure = topExposureRows.Sum(row => row.AbsoluteExposureAmount);
        var hhi = totalAbsoluteExposure == 0m
            ? 0m
            : RoundHhi(rows.Sum(row =>
            {
                var share = Math.Abs(row.ExposureAmount) / totalAbsoluteExposure;
                return share * share;
            }));
        var concentration = new RiskVisualConcentrationSummary(
            totalAbsoluteExposure,
            topNAbsoluteExposure,
            totalAbsoluteExposure == 0m ? 0m : RoundRatio(topNAbsoluteExposure / totalAbsoluteExposure),
            hhi,
            rowCount,
            topExposureRows.Length);

        var heatmapRows = rows
            .Select(row => new RiskVisualHeatmapRow(
                row.BaseDate,
                row.PortfolioId,
                row.RiskFactor,
                row.UsageRatio,
                HeatmapGrade(row.UsageRatio),
                row.StatusCode))
            .ToArray();

        var findings = BuildFindings(rows, totalAbsoluteExposure);
        return new RiskVisualModel(statusDistribution, topExposureRows, concentration, heatmapRows, findings);
    }

    public static string HeatmapGrade(decimal usageRatio)
    {
        if (usageRatio < 0.8m)
        {
            return "LOW";
        }

        return usageRatio <= 1.0m ? "MID" : "HIGH";
    }

    public static string ToStatusCode(LimitMonitorStatus status)
    {
        return status switch
        {
            LimitMonitorStatus.Normal => "NORMAL",
            LimitMonitorStatus.Warning => "WARNING",
            LimitMonitorStatus.Breach => "BREACH",
            LimitMonitorStatus.NoLimit => "NO_LIMIT",
            LimitMonitorStatus.InvalidLimit => "INVALID_LIMIT",
            LimitMonitorStatus.MappingError => "MAPPING_ERROR",
            LimitMonitorStatus.DuplicateLimit => "DUPLICATE_LIMIT",
            _ => status.ToString().ToUpperInvariant()
        };
    }

    private static IReadOnlyList<SafetyFinding> BuildFindings(
        IReadOnlyList<LimitMonitorRow> rows,
        decimal totalAbsoluteExposure)
    {
        var findings = new List<SafetyFinding>();
        var currencyCodes = rows
            .Select(row => row.CurrencyCode.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (currencyCodes.Length > 1)
        {
            findings.Add(new SafetyFinding(
                "MIXED_CURRENCY",
                SafetySeverity.Medium,
                $"시각화 집계 입력에 통화가 혼재되어 있습니다: {string.Join("/", currencyCodes)}. 집중도는 Abs(ExposureAmount) 단순 합 기준 검토용 수치입니다."));
        }

        if (rows.Count > 0 && totalAbsoluteExposure == 0m)
        {
            findings.Add(new SafetyFinding(
                "VISUAL_CONCENTRATION_ZERO_DENOMINATOR",
                SafetySeverity.Low,
                "시각화 집중도 분모(Abs Exposure 합)가 0이어서 TopN Share와 HHI를 0으로 표시했습니다."));
        }

        return findings;
    }

    private static decimal RoundRatio(decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal RoundHhi(decimal value)
        => Math.Round(value, 6, MidpointRounding.AwayFromZero);
}

public sealed record RiskVisualModel(
    IReadOnlyList<RiskVisualStatusDistributionRow> StatusDistribution,
    IReadOnlyList<RiskVisualTopExposureRow> TopExposures,
    RiskVisualConcentrationSummary Concentration,
    IReadOnlyList<RiskVisualHeatmapRow> Heatmap,
    IReadOnlyList<SafetyFinding> Findings);

public sealed record RiskVisualStatusDistributionRow(
    string StatusCode,
    int Count,
    decimal Ratio);

public sealed record RiskVisualTopExposureRow(
    int Rank,
    string BaseDate,
    string PortfolioId,
    string RiskFactor,
    string CurrencyCode,
    decimal ExposureAmount,
    decimal AbsoluteExposureAmount,
    decimal UsageRatio,
    string StatusCode);

public sealed record RiskVisualConcentrationSummary(
    decimal TotalAbsoluteExposure,
    decimal TopNAbsoluteExposure,
    decimal TopNShare,
    decimal Hhi,
    int RowCount,
    int TopNCount);

public sealed record RiskVisualHeatmapRow(
    string BaseDate,
    string PortfolioId,
    string RiskFactor,
    decimal UsageRatio,
    string Grade,
    string StatusCode);

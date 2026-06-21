using System.Globalization;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Mapping;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed class LimitMonitor
{
    private readonly ColumnMappingLoadResult columnMappingLoadResult;

    public LimitMonitor()
        : this(ColumnMappingLoader.LoadDefault())
    {
    }

    public LimitMonitor(ColumnMapping mapping)
        : this(new ColumnMappingLoadResult(mapping, UsedFallback: false, Warnings: Array.Empty<string>()))
    {
    }

    public LimitMonitor(ColumnMappingLoadResult columnMappingLoadResult)
    {
        ArgumentNullException.ThrowIfNull(columnMappingLoadResult);
        this.columnMappingLoadResult = columnMappingLoadResult;
    }

    public LimitMonitorResult Analyze(string exposureCsvPath, string limitCsvPath, string baseDate)
    {
        if (string.IsNullOrWhiteSpace(baseDate))
        {
            throw new ArgumentException("기준일이 비어 있습니다.", nameof(baseDate));
        }

        var mapping = columnMappingLoadResult.Mapping;
        var baseDateColumn = mapping.Physical(LogicalColumn.BaseDate);
        var portfolioIdColumn = mapping.Physical(LogicalColumn.PortfolioId);
        var riskFactorColumn = mapping.Physical(LogicalColumn.RiskFactor);
        var exposureAmountColumn = mapping.Physical(LogicalColumn.ExposureAmount);
        var limitAmountColumn = mapping.Physical(LogicalColumn.LimitAmount);
        var useYnColumn = mapping.Physical(LogicalColumn.UseYn);
        var normalizedBaseDate = baseDate.Trim();
        var exposures = ReadCsv(exposureCsvPath)
            .Where(row => string.Equals(row.GetValue(baseDateColumn), normalizedBaseDate, StringComparison.Ordinal))
            .ToList();
        var limits = ReadCsv(limitCsvPath)
            .Where(row => string.Equals(row.GetValue(baseDateColumn), normalizedBaseDate, StringComparison.Ordinal))
            .ToList();
        var activeLimits = limits
            .GroupBy(row => BuildJoinKey(row.GetValue(portfolioIdColumn), row.GetValue(riskFactorColumn)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<LimitMonitorRow>();
        var findings = columnMappingLoadResult.Warnings
            .Select(warning => new SafetyFinding("COLUMN_MAPPING_FALLBACK", SafetySeverity.Medium, warning))
            .ToList();

        foreach (var exposure in exposures)
        {
            var key = BuildJoinKey(exposure.GetValue(portfolioIdColumn), exposure.GetValue(riskFactorColumn));
            var exposureAmount = ParseDecimal(exposure.GetValue(exposureAmountColumn), exposureAmountColumn);

            if (!activeLimits.TryGetValue(key, out var limit))
            {
                rows.Add(new LimitMonitorRow(
                    normalizedBaseDate,
                    exposure.GetValue("DESK_CD"),
                    exposure.GetValue(portfolioIdColumn),
                    exposure.GetValue("PRODUCT_TYPE"),
                    exposure.GetValue(riskFactorColumn),
                    exposure.GetValue("CCY_CD"),
                    exposureAmount,
                    0m,
                    0m,
                    0m,
                    LimitMonitorStatus.MissingLimit,
                    $"동일 {baseDateColumn}의 한도 행을 찾지 못했습니다."));
                continue;
            }

            var limitAmount = ParseDecimal(limit.GetValue(limitAmountColumn), limitAmountColumn);
            var useYn = limit.GetValue(useYnColumn);
            var status = Classify(exposureAmount, limitAmount, useYn);
            var usageRatio = limitAmount <= 0m ? 0m : Math.Abs(exposureAmount) / limitAmount;
            var remainingLimit = limitAmount <= 0m ? 0m : limitAmount - Math.Abs(exposureAmount);
            var note = status switch
            {
                LimitMonitorStatus.Breach => "한도 초과: 검토 필요",
                LimitMonitorStatus.Warning => "90% 이상 사용: 사전 점검 권장",
                LimitMonitorStatus.InactiveLimit => "비활성 한도",
                _ => "정상 범위"
            };

            rows.Add(new LimitMonitorRow(
                normalizedBaseDate,
                exposure.GetValue("DESK_CD"),
                exposure.GetValue(portfolioIdColumn),
                exposure.GetValue("PRODUCT_TYPE"),
                exposure.GetValue(riskFactorColumn),
                exposure.GetValue("CCY_CD"),
                exposureAmount,
                limitAmount,
                usageRatio,
                remainingLimit,
                status,
                note));
        }

        if (rows.Count == 0)
        {
            findings.Add(new SafetyFinding("LIMIT_MONITOR_NO_ROWS", SafetySeverity.Low, $"{baseDateColumn}={normalizedBaseDate} 기준 노출 행이 없습니다."));
        }
        else
        {
            findings.Add(new SafetyFinding(
                "LIMIT_MONITOR_COMPLETE",
                SafetySeverity.Info,
                $"{baseDateColumn}={normalizedBaseDate} 한도 모니터링 완료: rows={rows.Count:N0}, warning={rows.Count(r => r.Status == LimitMonitorStatus.Warning):N0}, breach={rows.Count(r => r.Status == LimitMonitorStatus.Breach):N0}."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.Breach))
        {
            findings.Add(new SafetyFinding("LIMIT_BREACH_DETECTED", SafetySeverity.High, "한도 초과 항목이 있습니다. 사용자가 검토해야 합니다."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.MissingLimit))
        {
            findings.Add(new SafetyFinding("LIMIT_MAPPING_MISSING", SafetySeverity.Medium, "동일 기준일 한도와 매칭되지 않은 노출 항목이 있습니다."));
        }

        return new LimitMonitorResult(normalizedBaseDate, rows, findings);
    }

    private static LimitMonitorStatus Classify(decimal exposureAmount, decimal limitAmount, string useYn)
    {
        if (!string.Equals(useYn, "Y", StringComparison.OrdinalIgnoreCase))
        {
            return LimitMonitorStatus.InactiveLimit;
        }

        if (limitAmount <= 0m)
        {
            return LimitMonitorStatus.MissingLimit;
        }

        var usageRatio = Math.Abs(exposureAmount) / limitAmount;
        if (usageRatio > 1m)
        {
            return LimitMonitorStatus.Breach;
        }

        return usageRatio >= 0.9m
            ? LimitMonitorStatus.Warning
            : LimitMonitorStatus.Normal;
    }

    private static string BuildJoinKey(string portfolioId, string riskFactor)
        => $"{portfolioId.Trim()}\u001F{riskFactor.Trim()}";

    private static decimal ParseDecimal(string value, string columnName)
    {
        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"{columnName} 값이 숫자가 아닙니다. value={value}");
    }

    private static IReadOnlyList<CsvRow> ReadCsv(string csvPath)
    {
        var table = CsvReader.Read(csvPath);
        foreach (var row in table.Rows)
        {
            if (row.RawFieldCount != table.Columns.Count)
            {
                throw new InvalidDataException($"Line {row.LineNumber}: 컬럼 수가 헤더와 다릅니다. expected={table.Columns.Count}, actual={row.RawFieldCount}");
            }
        }

        return table.Rows;
    }
}

public sealed record LimitMonitorResult(
    string BaseDate,
    IReadOnlyList<LimitMonitorRow> Rows,
    IReadOnlyList<SafetyFinding> Findings)
{
    public int NormalCount => Rows.Count(row => row.Status == LimitMonitorStatus.Normal);
    public int WarningCount => Rows.Count(row => row.Status == LimitMonitorStatus.Warning);
    public int BreachCount => Rows.Count(row => row.Status == LimitMonitorStatus.Breach);
    public int MissingLimitCount => Rows.Count(row => row.Status == LimitMonitorStatus.MissingLimit);
}

public sealed record LimitMonitorRow(
    string BaseDate,
    string DeskCode,
    string PortfolioId,
    string ProductType,
    string RiskFactor,
    string CurrencyCode,
    decimal ExposureAmount,
    decimal LimitAmount,
    decimal UsageRatio,
    decimal RemainingLimit,
    LimitMonitorStatus Status,
    string Note);

public enum LimitMonitorStatus
{
    Normal,
    Warning,
    Breach,
    MissingLimit,
    InactiveLimit
}

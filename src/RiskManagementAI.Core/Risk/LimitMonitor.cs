using System.Globalization;
using System.Text;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed class LimitMonitor
{
    private const string BaseDateColumn = "BASE_DT";
    private const string PortfolioIdColumn = "PORTFOLIO_ID";
    private const string RiskFactorColumn = "RISK_FACTOR";
    private const string ExposureAmountColumn = "EXPOSURE_AMT";
    private const string LimitAmountColumn = "LIMIT_AMT";
    private const string UseYnColumn = "USE_YN";

    public LimitMonitorResult Analyze(string exposureCsvPath, string limitCsvPath, string baseDate)
    {
        if (string.IsNullOrWhiteSpace(baseDate))
        {
            throw new ArgumentException("기준일(BASE_DT)이 비어 있습니다.", nameof(baseDate));
        }

        var normalizedBaseDate = baseDate.Trim();
        var exposures = ReadCsv(exposureCsvPath)
            .Where(row => string.Equals(row.GetValue(BaseDateColumn), normalizedBaseDate, StringComparison.Ordinal))
            .ToList();
        var limits = ReadCsv(limitCsvPath)
            .Where(row => string.Equals(row.GetValue(BaseDateColumn), normalizedBaseDate, StringComparison.Ordinal))
            .ToList();
        var activeLimits = limits
            .GroupBy(row => BuildJoinKey(row.GetValue(PortfolioIdColumn), row.GetValue(RiskFactorColumn)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<LimitMonitorRow>();
        var findings = new List<SafetyFinding>();

        foreach (var exposure in exposures)
        {
            var key = BuildJoinKey(exposure.GetValue(PortfolioIdColumn), exposure.GetValue(RiskFactorColumn));
            var exposureAmount = ParseDecimal(exposure.GetValue(ExposureAmountColumn), ExposureAmountColumn);

            if (!activeLimits.TryGetValue(key, out var limit))
            {
                rows.Add(new LimitMonitorRow(
                    normalizedBaseDate,
                    exposure.GetValue("DESK_CD"),
                    exposure.GetValue(PortfolioIdColumn),
                    exposure.GetValue("PRODUCT_TYPE"),
                    exposure.GetValue(RiskFactorColumn),
                    exposure.GetValue("CCY_CD"),
                    exposureAmount,
                    0m,
                    0m,
                    0m,
                    LimitMonitorStatus.MissingLimit,
                    "동일 BASE_DT의 한도 행을 찾지 못했습니다."));
                continue;
            }

            var limitAmount = ParseDecimal(limit.GetValue(LimitAmountColumn), LimitAmountColumn);
            var useYn = limit.GetValue(UseYnColumn);
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
                exposure.GetValue(PortfolioIdColumn),
                exposure.GetValue("PRODUCT_TYPE"),
                exposure.GetValue(RiskFactorColumn),
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
            findings.Add(new SafetyFinding("LIMIT_MONITOR_NO_ROWS", SafetySeverity.Low, $"BASE_DT={normalizedBaseDate} 기준 노출 행이 없습니다."));
        }
        else
        {
            findings.Add(new SafetyFinding(
                "LIMIT_MONITOR_COMPLETE",
                SafetySeverity.Info,
                $"BASE_DT={normalizedBaseDate} 한도 모니터링 완료: rows={rows.Count:N0}, warning={rows.Count(r => r.Status == LimitMonitorStatus.Warning):N0}, breach={rows.Count(r => r.Status == LimitMonitorStatus.Breach):N0}."));
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
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            throw new ArgumentException("CSV 파일 경로가 비어 있습니다.", nameof(csvPath));
        }

        if (!string.Equals(Path.GetExtension(csvPath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("한도 모니터링은 CSV 파일만 지원합니다.", nameof(csvPath));
        }

        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("CSV 파일을 찾을 수 없습니다.", csvPath);
        }

        using var reader = new StreamReader(csvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            throw new InvalidDataException("CSV 파일에 헤더가 없습니다.");
        }

        var columns = ParseCsvLine(headerLine)
            .Select((column, index) => index == 0 ? column.TrimStart('\uFEFF').Trim() : column.Trim())
            .ToArray();
        if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("CSV 헤더에 빈 컬럼명이 있습니다.");
        }

        var rows = new List<CsvRow>();
        var lineNumber = 1;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);
            if (values.Count != columns.Length)
            {
                throw new InvalidDataException($"Line {lineNumber}: 컬럼 수가 헤더와 다릅니다. expected={columns.Length}, actual={values.Count}");
            }

            rows.Add(new CsvRow(columns, values));
        }

        return rows;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private sealed class CsvRow
    {
        private readonly Dictionary<string, string> values;

        public CsvRow(IReadOnlyList<string> columns, IReadOnlyList<string> rowValues)
        {
            values = columns
                .Select((column, index) => new { column, value = rowValues[index].Trim() })
                .ToDictionary(item => item.column, item => item.value, StringComparer.OrdinalIgnoreCase);
        }

        public string GetValue(string columnName)
        {
            if (values.TryGetValue(columnName, out var value))
            {
                return value;
            }

            throw new InvalidDataException($"{columnName} 컬럼이 없습니다.");
        }
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

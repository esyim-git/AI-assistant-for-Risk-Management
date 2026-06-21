using System.Globalization;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Mapping;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed class LimitMonitor
{
    private const string DeskCodeColumn = "DESK_CD";
    private const string ProductTypeColumn = "PRODUCT_TYPE";
    private const string CurrencyCodeColumn = "CCY_CD";

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

    public LimitAnalysisResult Analyze(string exposurePath, string limitPath, string baseDate)
    {
        return Analyze(ReadTable(exposurePath), ReadTable(limitPath), baseDate);
    }

    public LimitAnalysisResult Analyze(CsvTable exposure, CsvTable limit, string baseDate)
    {
        ArgumentNullException.ThrowIfNull(exposure);
        ArgumentNullException.ThrowIfNull(limit);

        if (string.IsNullOrWhiteSpace(baseDate))
        {
            throw new ArgumentException("기준일이 비어 있습니다.", nameof(baseDate));
        }

        var mapping = columnMappingLoadResult.Mapping;
        var columns = new RequiredColumns(
            mapping.Physical(LogicalColumn.BaseDate),
            mapping.Physical(LogicalColumn.PortfolioId),
            mapping.Physical(LogicalColumn.RiskFactor),
            mapping.Physical(LogicalColumn.ExposureAmount),
            mapping.Physical(LogicalColumn.LimitAmount),
            mapping.Physical(LogicalColumn.UseYn));
        var normalizedBaseDate = baseDate.Trim();
        var rows = new List<LimitMonitorRow>();
        var exceptions = new List<LimitException>();
        var findings = columnMappingLoadResult.Warnings
            .Select(warning => new SafetyFinding("COLUMN_MAPPING_FALLBACK", SafetySeverity.Medium, warning))
            .ToList();

        foreach (var warning in columnMappingLoadResult.Warnings)
        {
            exceptions.Add(new LimitException(
                "COLUMN_MAPPING_FALLBACK",
                SafetySeverity.Medium,
                warning,
                normalizedBaseDate,
                string.Empty,
                string.Empty));
        }

        var missingExposureColumns = MissingColumns(exposure, [columns.BaseDate, columns.PortfolioId, columns.RiskFactor, columns.ExposureAmount]);
        if (missingExposureColumns.Count > 0)
        {
            var message = $"노출 입력에 매핑된 물리 컬럼이 없습니다: {string.Join(", ", missingExposureColumns)}";
            AddAnalysisMappingError(rows, exceptions, normalizedBaseDate, message);
            findings.Add(new SafetyFinding("LIMIT_MAPPING_ERROR", SafetySeverity.High, message));
            return BuildResult(normalizedBaseDate, exposure, limit, rows, exceptions, findings);
        }

        var exposures = exposure.Rows
            .Where(row => string.Equals(GetRequired(row, columns.BaseDate), normalizedBaseDate, StringComparison.Ordinal))
            .ToList();
        var missingLimitColumns = MissingColumns(limit, [columns.BaseDate, columns.PortfolioId, columns.RiskFactor, columns.LimitAmount, columns.UseYn]);
        if (missingLimitColumns.Count > 0)
        {
            var message = $"한도 입력에 매핑된 물리 컬럼이 없습니다: {string.Join(", ", missingLimitColumns)}";
            if (exposures.Count == 0)
            {
                AddMappingErrorRow(rows, exceptions, normalizedBaseDate, null, columns, message);
            }

            foreach (var exposureRow in exposures)
            {
                AddMappingErrorRow(rows, exceptions, normalizedBaseDate, exposureRow, columns, message);
            }

            findings.Add(new SafetyFinding("LIMIT_MAPPING_ERROR", SafetySeverity.High, message));
            return BuildResult(normalizedBaseDate, exposure, limit, rows, exceptions, findings);
        }

        var activeLimits = limit.Rows
            .Where(row => string.Equals(GetRequired(row, columns.BaseDate), normalizedBaseDate, StringComparison.Ordinal))
            .GroupBy(row => BuildJoinKey(GetRequired(row, columns.PortfolioId), GetRequired(row, columns.RiskFactor)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        foreach (var exposureRow in exposures)
        {
            var portfolioId = GetRequired(exposureRow, columns.PortfolioId);
            var riskFactor = GetRequired(exposureRow, columns.RiskFactor);
            var key = BuildJoinKey(portfolioId, riskFactor);
            if (!TryParseDecimal(GetRequired(exposureRow, columns.ExposureAmount), out var exposureAmount))
            {
                AddMappingErrorRow(
                    rows,
                    exceptions,
                    normalizedBaseDate,
                    exposureRow,
                    columns,
                    $"{columns.ExposureAmount} 값이 숫자가 아닙니다.");
                continue;
            }

            if (!activeLimits.TryGetValue(key, out var limitRow))
            {
                AddNoLimitRow(rows, exceptions, normalizedBaseDate, exposureRow, columns, exposureAmount);
                continue;
            }

            var limitValue = GetRequired(limitRow, columns.LimitAmount);
            var useYn = GetRequired(limitRow, columns.UseYn);
            if (!TryParseDecimal(limitValue, out var limitAmount)
                || limitAmount <= 0m
                || !string.Equals(useYn, "Y", StringComparison.OrdinalIgnoreCase))
            {
                AddInvalidLimitRow(rows, exceptions, normalizedBaseDate, exposureRow, columns, exposureAmount, limitAmount);
                continue;
            }

            AddValidLimitRow(rows, normalizedBaseDate, exposureRow, columns, exposureAmount, limitAmount);
        }

        if (rows.Count == 0)
        {
            findings.Add(new SafetyFinding("LIMIT_MONITOR_NO_ROWS", SafetySeverity.Low, $"{columns.BaseDate}={normalizedBaseDate} 기준 노출 행이 없습니다."));
        }
        else
        {
            findings.Add(new SafetyFinding(
                "LIMIT_MONITOR_COMPLETE",
                SafetySeverity.Info,
                $"{columns.BaseDate}={normalizedBaseDate} 한도 모니터링 완료: rows={rows.Count:N0}, warning={rows.Count(r => r.Status == LimitMonitorStatus.Warning):N0}, breach={rows.Count(r => r.Status == LimitMonitorStatus.Breach):N0}."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.Breach))
        {
            findings.Add(new SafetyFinding("LIMIT_BREACH_DETECTED", SafetySeverity.High, "한도 초과 항목이 있습니다. 사용자가 검토해야 합니다."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.NoLimit))
        {
            findings.Add(new SafetyFinding("LIMIT_NO_LIMIT_DETECTED", SafetySeverity.Medium, "동일 기준일 한도와 매칭되지 않은 노출 항목이 있습니다."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.InvalidLimit))
        {
            findings.Add(new SafetyFinding("LIMIT_INVALID_LIMIT_DETECTED", SafetySeverity.Medium, "사용 불가 한도 항목이 있습니다."));
        }

        if (rows.Any(row => row.Status == LimitMonitorStatus.MappingError))
        {
            findings.Add(new SafetyFinding("LIMIT_MAPPING_ERROR", SafetySeverity.High, "매핑된 물리 컬럼이 입력과 일치하지 않는 항목이 있습니다."));
        }

        return BuildResult(normalizedBaseDate, exposure, limit, rows, exceptions, findings);
    }

    private static CsvTable ReadTable(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("입력 파일 경로가 비어 있습니다.", nameof(path));
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csv" => CsvReader.Read(path),
            ".xlsx" => XlsxReader.Read(path),
            _ => throw new ArgumentException("한도 모니터링 입력은 .csv 또는 .xlsx 파일만 지원합니다.", nameof(path))
        };
    }

    private LimitAnalysisResult BuildResult(
        string baseDate,
        CsvTable exposure,
        CsvTable limit,
        IReadOnlyList<LimitMonitorRow> rows,
        IReadOnlyList<LimitException> exceptions,
        IReadOnlyList<SafetyFinding> findings)
    {
        var kpis = LimitAnalysisKpis.FromRows(rows);
        var metadata = new LimitAnalysisMetadata(
            baseDate,
            exposure.SourceName,
            limit.SourceName,
            columnMappingLoadResult.UsedFallback,
            columnMappingLoadResult.Warnings,
            IsDeterministic: true);

        return new LimitAnalysisResult(baseDate, rows, kpis, metadata, exceptions, findings);
    }

    private static void AddValidLimitRow(
        List<LimitMonitorRow> rows,
        string baseDate,
        CsvRow exposureRow,
        RequiredColumns columns,
        decimal exposureAmount,
        decimal limitAmount)
    {
        var usageRatio = Math.Abs(exposureAmount) / limitAmount;
        var remainingLimit = limitAmount - Math.Abs(exposureAmount);
        var status = Classify(exposureAmount, limitAmount);
        var note = status switch
        {
            LimitMonitorStatus.Breach => "한도 초과: 검토 필요",
            LimitMonitorStatus.Warning => "90% 이상 사용: 사전 점검 권장",
            _ => "정상 범위"
        };

        rows.Add(CreateRow(baseDate, exposureRow, columns, exposureAmount, limitAmount, usageRatio, remainingLimit, status, note));
    }

    private static void AddNoLimitRow(
        List<LimitMonitorRow> rows,
        List<LimitException> exceptions,
        string baseDate,
        CsvRow exposureRow,
        RequiredColumns columns,
        decimal exposureAmount)
    {
        var row = CreateRow(
            baseDate,
            exposureRow,
            columns,
            exposureAmount,
            0m,
            0m,
            0m,
            LimitMonitorStatus.NoLimit,
            $"동일 {columns.BaseDate}의 한도 행을 찾지 못했습니다.");
        rows.Add(row);
        exceptions.Add(CreateException("NO_LIMIT", SafetySeverity.Medium, row.Note, row));
    }

    private static void AddInvalidLimitRow(
        List<LimitMonitorRow> rows,
        List<LimitException> exceptions,
        string baseDate,
        CsvRow exposureRow,
        RequiredColumns columns,
        decimal exposureAmount,
        decimal limitAmount)
    {
        var row = CreateRow(
            baseDate,
            exposureRow,
            columns,
            exposureAmount,
            limitAmount,
            0m,
            0m,
            LimitMonitorStatus.InvalidLimit,
            "한도 행이 있으나 사용 불가 상태입니다.");
        rows.Add(row);
        exceptions.Add(CreateException("INVALID_LIMIT", SafetySeverity.Medium, row.Note, row));
    }

    private static void AddMappingErrorRow(
        List<LimitMonitorRow> rows,
        List<LimitException> exceptions,
        string baseDate,
        CsvRow? exposureRow,
        RequiredColumns columns,
        string message)
    {
        var row = exposureRow is null
            ? CreateAnalysisErrorRow(baseDate, message)
            : CreateRow(baseDate, exposureRow, columns, 0m, 0m, 0m, 0m, LimitMonitorStatus.MappingError, message);
        rows.Add(row);
        exceptions.Add(CreateException("MAPPING_ERROR", SafetySeverity.High, message, row));
    }

    private static void AddAnalysisMappingError(
        List<LimitMonitorRow> rows,
        List<LimitException> exceptions,
        string baseDate,
        string message)
    {
        var row = CreateAnalysisErrorRow(baseDate, message);
        rows.Add(row);
        exceptions.Add(CreateException("MAPPING_ERROR", SafetySeverity.High, message, row));
    }

    private static LimitMonitorRow CreateRow(
        string baseDate,
        CsvRow exposureRow,
        RequiredColumns columns,
        decimal exposureAmount,
        decimal limitAmount,
        decimal usageRatio,
        decimal remainingLimit,
        LimitMonitorStatus status,
        string note)
    {
        return new LimitMonitorRow(
            baseDate,
            GetOptional(exposureRow, DeskCodeColumn),
            GetOptional(exposureRow, columns.PortfolioId),
            GetOptional(exposureRow, ProductTypeColumn),
            GetOptional(exposureRow, columns.RiskFactor),
            GetOptional(exposureRow, CurrencyCodeColumn),
            exposureAmount,
            limitAmount,
            usageRatio,
            remainingLimit,
            status,
            note);
    }

    private static LimitMonitorRow CreateAnalysisErrorRow(string baseDate, string message)
    {
        return new LimitMonitorRow(
            baseDate,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0m,
            0m,
            0m,
            0m,
            LimitMonitorStatus.MappingError,
            message);
    }

    private static LimitException CreateException(string code, SafetySeverity severity, string message, LimitMonitorRow row)
    {
        return new LimitException(code, severity, message, row.BaseDate, row.PortfolioId, row.RiskFactor);
    }

    private static LimitMonitorStatus Classify(decimal exposureAmount, decimal limitAmount)
    {
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

    private static bool TryParseDecimal(string value, out decimal parsed)
    {
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out parsed);
    }

    private static IReadOnlyList<string> MissingColumns(CsvTable table, IReadOnlyList<string> requiredColumns)
    {
        return requiredColumns
            .Where(required => !table.Columns.Contains(required, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetRequired(CsvRow row, string columnName)
    {
        return row.GetValue(columnName);
    }

    private static string GetOptional(CsvRow row, string columnName)
    {
        return row.TryGetValue(columnName, out var value) ? value : string.Empty;
    }

    private sealed record RequiredColumns(
        string BaseDate,
        string PortfolioId,
        string RiskFactor,
        string ExposureAmount,
        string LimitAmount,
        string UseYn);
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
    string Note)
{
    public string StatusCode => Status switch
    {
        LimitMonitorStatus.Normal => "NORMAL",
        LimitMonitorStatus.Warning => "WARNING",
        LimitMonitorStatus.Breach => "BREACH",
        LimitMonitorStatus.NoLimit => "NO_LIMIT",
        LimitMonitorStatus.InvalidLimit => "INVALID_LIMIT",
        LimitMonitorStatus.MappingError => "MAPPING_ERROR",
        _ => Status.ToString().ToUpperInvariant()
    };
}

public enum LimitMonitorStatus
{
    Normal,
    Warning,
    Breach,
    NoLimit,
    InvalidLimit,
    MappingError
}

using System.Globalization;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Mapping;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Risk;

public sealed class LimitMonitor
{
    private const string DeskCodeColumn = "DESK_CD";
    private const string ProductTypeColumn = "PRODUCT_TYPE";

    private const string ReconExposureNoLimit = "RECON_EXPOSURE_NO_LIMIT";
    private const string ReconLimitNoExposure = "RECON_LIMIT_NO_EXPOSURE";
    private const string ReconDuplicateLimit = "RECON_DUPLICATE_LIMIT";
    private const string ReconBaseDateMismatch = "RECON_BASEDATE_MISMATCH";
    private const string ReconCurrencyMismatch = "RECON_CURRENCY_MISMATCH";
    private const string ReconUnitMismatch = "RECON_UNIT_MISMATCH";
    private const string ReconNonpositiveLimit = "RECON_NONPOSITIVE_LIMIT";
    private const string ReconRowAmplification = "RECON_ROW_AMPLIFICATION";
    private const string ReconSumBalance = "RECON_SUM_BALANCE";

    private static readonly IReadOnlyList<string> ReconciliationCodes =
    [
        ReconExposureNoLimit,
        ReconLimitNoExposure,
        ReconDuplicateLimit,
        ReconBaseDateMismatch,
        ReconCurrencyMismatch,
        ReconUnitMismatch,
        ReconNonpositiveLimit,
        ReconRowAmplification,
        ReconSumBalance
    ];

    private static readonly HashSet<string> ReconciliationFailCodes = new(StringComparer.Ordinal)
    {
        ReconNonpositiveLimit,
        ReconRowAmplification,
        ReconSumBalance
    };

    private static readonly string[] BaseDateInputFormats = ["yyyyMMdd", "yyyy-MM-dd"];

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
        mapping.TryPhysical(LogicalColumn.CurrencyCode, out var currencyCodeColumn);
        mapping.TryPhysical(LogicalColumn.UnitCode, out var unitCodeColumn);
        var columns = new RequiredColumns(
            mapping.Physical(LogicalColumn.BaseDate),
            mapping.Physical(LogicalColumn.PortfolioId),
            mapping.Physical(LogicalColumn.RiskFactor),
            mapping.Physical(LogicalColumn.ExposureAmount),
            mapping.Physical(LogicalColumn.LimitAmount),
            mapping.Physical(LogicalColumn.UseYn),
            currencyCodeColumn,
            unitCodeColumn);
        var baseDateValidation = ValidateBaseDate(baseDate);
        var normalizedBaseDate = baseDateValidation.NormalizedBaseDate;
        var rows = new List<LimitMonitorRow>();
        var exceptions = new List<LimitException>();
        var findings = columnMappingLoadResult.Warnings
            .Select(warning => new SafetyFinding("COLUMN_MAPPING_FALLBACK", SafetySeverity.Medium, warning))
            .ToList();

        if (!baseDateValidation.IsValid)
        {
            var message = $"요청 기준일 형식이 유효하지 않습니다: '{baseDateValidation.RequestedBaseDate}'. 허용 형식은 yyyyMMdd 또는 yyyy-MM-dd입니다.";
            findings.Add(new SafetyFinding("LIMIT_BASEDATE_FORMAT_INVALID", SafetySeverity.Low, message));
            exceptions.Add(new LimitException(
                ReconBaseDateMismatch,
                SafetySeverity.Low,
                message,
                normalizedBaseDate,
                string.Empty,
                string.Empty));
        }

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
            return BuildResult(baseDateValidation, exposure, limit, rows, exceptions, findings, columns);
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
            return BuildResult(baseDateValidation, exposure, limit, rows, exceptions, findings, columns);
        }

        var limitGroupsForBaseDate = limit.Rows
            .Where(row => string.Equals(GetRequired(row, columns.BaseDate), normalizedBaseDate, StringComparison.Ordinal))
            .GroupBy(row => BuildJoinKey(GetRequired(row, columns.PortfolioId), GetRequired(row, columns.RiskFactor)), StringComparer.OrdinalIgnoreCase)
            .Select(group => new LimitRowGroup(
                group.Key,
                GetRequired(group.First(), columns.PortfolioId),
                GetRequired(group.First(), columns.RiskFactor),
                group.ToArray()))
            .ToArray();
        var duplicateLimitCounts = limitGroupsForBaseDate
            .Where(group => group.Rows.Count > 1)
            .ToDictionary(group => group.Key, group => group.Rows.Count, StringComparer.OrdinalIgnoreCase);
        var activeLimits = limitGroupsForBaseDate
            .Where(group => group.Rows.Count == 1)
            .ToDictionary(group => group.Key, group => group.Rows[0], StringComparer.OrdinalIgnoreCase);

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

            if (duplicateLimitCounts.TryGetValue(key, out var duplicateLimitCount))
            {
                AddDuplicateLimitRow(rows, exceptions, normalizedBaseDate, exposureRow, columns, exposureAmount, duplicateLimitCount);
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

        if (rows.Any(row => row.Status == LimitMonitorStatus.DuplicateLimit))
        {
            findings.Add(new SafetyFinding("LIMIT_DUPLICATE_LIMIT_DETECTED", SafetySeverity.Medium, "동일 기준일·동일 Join Key 한도 중복으로 차단된 항목이 있습니다."));
        }

        return BuildResult(baseDateValidation, exposure, limit, rows, exceptions, findings, columns);
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
        BaseDateValidation baseDateValidation,
        CsvTable exposure,
        CsvTable limit,
        IReadOnlyList<LimitMonitorRow> rows,
        IReadOnlyList<LimitException> exceptions,
        IReadOnlyList<SafetyFinding> findings,
        RequiredColumns columns)
    {
        var baseDate = baseDateValidation.NormalizedBaseDate;
        var kpis = LimitAnalysisKpis.FromRows(rows);
        var reconciliationComputation = BuildReconciliationExceptions(baseDate, exposure, limit, rows, columns);
        var allExceptions = exceptions.Concat(reconciliationComputation.Exceptions).ToArray();
        var reconciliation = BuildReconciliationSummary(
            allExceptions,
            reconciliationComputation.CurrencyApplicable,
            reconciliationComputation.UnitApplicable);
        var metadata = new LimitAnalysisMetadata(
            baseDate,
            exposure.SourceName,
            limit.SourceName,
            columnMappingLoadResult.UsedFallback,
            columnMappingLoadResult.Warnings,
            IsDeterministic: true,
            BuildJoinAudit(baseDateValidation, rows, reconciliationComputation, columns));
        var reconciliationSeverity = reconciliation.Passed ? SafetySeverity.Info : SafetySeverity.High;
        var allFindings = findings
            .Concat(new[]
            {
                new SafetyFinding(
                    "RECON_SUMMARY",
                    reconciliationSeverity,
                    $"대사 결과: {(reconciliation.Passed ? "PASS" : "FAIL")}, checks={reconciliation.CheckCount}, exceptions={allExceptions.Count(exception => exception.Code.StartsWith("RECON_", StringComparison.Ordinal)):N0}.")
            })
            .ToArray();

        return new LimitAnalysisResult(baseDate, rows, kpis, metadata, allExceptions, allFindings, reconciliation);
    }

    private static ReconciliationComputation BuildReconciliationExceptions(
        string baseDate,
        CsvTable exposure,
        CsvTable limit,
        IReadOnlyList<LimitMonitorRow> rows,
        RequiredColumns columns)
    {
        var exceptions = new List<LimitException>();
        var exposureRowsForBaseDate = FilterRowsByBaseDate(exposure, columns.BaseDate, baseDate);
        var limitRowsForBaseDate = FilterRowsByBaseDate(limit, columns.BaseDate, baseDate);
        var canBuildExposureKey = HasColumns(exposure, [columns.PortfolioId, columns.RiskFactor]);
        var canBuildLimitKey = HasColumns(limit, [columns.PortfolioId, columns.RiskFactor]);
        var exposureKeys = canBuildExposureKey
            ? exposureRowsForBaseDate
                .Select(row => BuildJoinKey(GetRequired(row, columns.PortfolioId), GetRequired(row, columns.RiskFactor)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var limitGroups = canBuildLimitKey
            ? limitRowsForBaseDate
                .GroupBy(row => BuildJoinKey(GetRequired(row, columns.PortfolioId), GetRequired(row, columns.RiskFactor)), StringComparer.OrdinalIgnoreCase)
                .Select(group => new LimitRowGroup(
                    group.Key,
                    GetRequired(group.First(), columns.PortfolioId),
                    GetRequired(group.First(), columns.RiskFactor),
                    group.ToArray()))
                .OrderBy(group => group.PortfolioId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.RiskFactor, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<LimitRowGroup>();
        var limitGroupByKey = limitGroups.ToDictionary(group => group.Key, group => group, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows
            .Where(row => row.Status == LimitMonitorStatus.NoLimit)
            .OrderBy(row => row.PortfolioId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.RiskFactor, StringComparer.OrdinalIgnoreCase))
        {
            exceptions.Add(CreateException(
                ReconExposureNoLimit,
                SafetySeverity.Medium,
                "노출에 매칭되는 동일 기준일 한도 행이 없습니다.",
                row));
        }

        if (canBuildExposureKey && canBuildLimitKey)
        {
            foreach (var group in limitGroups.Where(group => !exposureKeys.Contains(group.Key)))
            {
                exceptions.Add(CreateException(
                    ReconLimitNoExposure,
                    SafetySeverity.Low,
                    "한도에 매칭되는 동일 기준일 노출 행이 없습니다.",
                    baseDate,
                    group.PortfolioId,
                    group.RiskFactor));
            }
        }

        var duplicateLimitGroups = limitGroups
            .Where(group => group.Rows.Count > 1)
            .ToArray();
        foreach (var group in duplicateLimitGroups)
        {
            exceptions.Add(CreateException(
                ReconDuplicateLimit,
                SafetySeverity.Medium,
                $"동일 기준일·동일 Join Key 한도 행이 {group.Rows.Count:N0}건입니다.",
                baseDate,
                group.PortfolioId,
                group.RiskFactor));
        }

        AddBaseDateMismatchExceptions(exceptions, baseDate, exposure, limit, columns);

        var currencyApplicable = HasOptionalColumn(exposure, columns.CurrencyCode)
            && HasOptionalColumn(limit, columns.CurrencyCode)
            && canBuildExposureKey
            && canBuildLimitKey;
        if (currencyApplicable)
        {
            AddCurrencyMismatchExceptions(exceptions, baseDate, exposureRowsForBaseDate, limitGroupByKey, columns);
        }

        var unitApplicable = HasOptionalColumn(exposure, columns.UnitCode)
            && HasOptionalColumn(limit, columns.UnitCode)
            && canBuildExposureKey
            && canBuildLimitKey;
        if (unitApplicable)
        {
            AddUnitMismatchExceptions(exceptions, baseDate, exposureRowsForBaseDate, limitGroupByKey, columns);
        }

        if (HasColumn(limit, columns.LimitAmount))
        {
            foreach (var group in limitGroups)
            {
                foreach (var limitRow in group.Rows)
                {
                    var limitValue = GetRequired(limitRow, columns.LimitAmount);
                    if (!TryParseDecimal(limitValue, out var limitAmount) || limitAmount <= 0m)
                    {
                        exceptions.Add(CreateException(
                            ReconNonpositiveLimit,
                            SafetySeverity.Medium,
                            $"{columns.LimitAmount} 값이 0 이하이거나 숫자가 아닙니다.",
                            baseDate,
                            group.PortfolioId,
                            group.RiskFactor));
                    }
                }
            }
        }

        AddRowAmplificationException(exceptions, baseDate, rows, exposureRowsForBaseDate, limitGroupByKey, columns, canBuildExposureKey && canBuildLimitKey);
        AddSumBalanceException(exceptions, baseDate, exposure, rows, exposureRowsForBaseDate, columns);

        return new ReconciliationComputation(
            exceptions
                .OrderBy(exception => ReconciliationCodeOrder(exception.Code))
                .ThenBy(exception => exception.PortfolioId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(exception => exception.RiskFactor, StringComparer.OrdinalIgnoreCase)
                .ThenBy(exception => exception.Message, StringComparer.Ordinal)
                .ToArray(),
            currencyApplicable,
            unitApplicable,
            duplicateLimitGroups.Length,
            duplicateLimitGroups.Sum(group => group.Rows.Count));
    }

    private static void AddBaseDateMismatchExceptions(
        List<LimitException> exceptions,
        string baseDate,
        CsvTable exposure,
        CsvTable limit,
        RequiredColumns columns)
    {
        if (HasColumn(exposure, columns.BaseDate)
            && !exposure.Rows.Any(row => string.Equals(GetRequired(row, columns.BaseDate), baseDate, StringComparison.Ordinal))
            && exposure.Rows.Any(row => !string.IsNullOrWhiteSpace(GetRequired(row, columns.BaseDate))))
        {
            exceptions.Add(CreateException(
                ReconBaseDateMismatch,
                SafetySeverity.Low,
                "요청 기준일의 노출 행이 없고 다른 기준일 노출 행만 있습니다.",
                baseDate,
                string.Empty,
                string.Empty));
        }

        if (HasColumn(limit, columns.BaseDate)
            && !limit.Rows.Any(row => string.Equals(GetRequired(row, columns.BaseDate), baseDate, StringComparison.Ordinal))
            && limit.Rows.Any(row => !string.IsNullOrWhiteSpace(GetRequired(row, columns.BaseDate))))
        {
            exceptions.Add(CreateException(
                ReconBaseDateMismatch,
                SafetySeverity.Low,
                "요청 기준일의 한도 행이 없고 다른 기준일 한도 행만 있습니다.",
                baseDate,
                string.Empty,
                string.Empty));
        }
    }

    private static void AddCurrencyMismatchExceptions(
        List<LimitException> exceptions,
        string baseDate,
        IReadOnlyList<CsvRow> exposureRowsForBaseDate,
        IReadOnlyDictionary<string, LimitRowGroup> limitGroupByKey,
        RequiredColumns columns)
    {
        foreach (var exposureRow in exposureRowsForBaseDate)
        {
            var portfolioId = GetRequired(exposureRow, columns.PortfolioId);
            var riskFactor = GetRequired(exposureRow, columns.RiskFactor);
            var key = BuildJoinKey(portfolioId, riskFactor);
            if (!limitGroupByKey.TryGetValue(key, out var limitGroup))
            {
                continue;
            }

            var exposureCurrency = GetOptional(exposureRow, columns.CurrencyCode);
            var limitCurrencies = DistinctOptionalValues(limitGroup.Rows, columns.CurrencyCode);
            foreach (var limitCurrency in limitCurrencies)
            {
                if (!string.IsNullOrWhiteSpace(exposureCurrency)
                    && !string.IsNullOrWhiteSpace(limitCurrency)
                    && !string.Equals(exposureCurrency, limitCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    exceptions.Add(CreateException(
                        ReconCurrencyMismatch,
                        SafetySeverity.Medium,
                        $"노출 통화({exposureCurrency})와 한도 통화({limitCurrency})가 다릅니다.",
                        baseDate,
                        portfolioId,
                        riskFactor));
                }
            }
        }
    }

    private static void AddUnitMismatchExceptions(
        List<LimitException> exceptions,
        string baseDate,
        IReadOnlyList<CsvRow> exposureRowsForBaseDate,
        IReadOnlyDictionary<string, LimitRowGroup> limitGroupByKey,
        RequiredColumns columns)
    {
        foreach (var exposureRow in exposureRowsForBaseDate)
        {
            var portfolioId = GetRequired(exposureRow, columns.PortfolioId);
            var riskFactor = GetRequired(exposureRow, columns.RiskFactor);
            var key = BuildJoinKey(portfolioId, riskFactor);
            if (!limitGroupByKey.TryGetValue(key, out var limitGroup))
            {
                continue;
            }

            var exposureUnit = GetOptional(exposureRow, columns.UnitCode);
            var limitUnits = DistinctOptionalValues(limitGroup.Rows, columns.UnitCode);
            foreach (var limitUnit in limitUnits)
            {
                if (!string.IsNullOrWhiteSpace(exposureUnit)
                    && !string.IsNullOrWhiteSpace(limitUnit)
                    && !string.Equals(exposureUnit, limitUnit, StringComparison.OrdinalIgnoreCase))
                {
                    exceptions.Add(CreateException(
                        ReconUnitMismatch,
                        SafetySeverity.Medium,
                        $"노출 단위({exposureUnit})와 한도 단위({limitUnit})가 다릅니다.",
                        baseDate,
                        portfolioId,
                        riskFactor));
                }
            }
        }
    }

    private static void AddRowAmplificationException(
        List<LimitException> exceptions,
        string baseDate,
        IReadOnlyList<LimitMonitorRow> rows,
        IReadOnlyList<CsvRow> exposureRowsForBaseDate,
        IReadOnlyDictionary<string, LimitRowGroup> limitGroupByKey,
        RequiredColumns columns,
        bool canBuildKeys)
    {
        var exposureRowCount = exposureRowsForBaseDate.Count;
        var analysisRowCount = rows.Count;
        var potentialJoinRowCount = analysisRowCount;
        if (canBuildKeys)
        {
            potentialJoinRowCount = 0;
            foreach (var exposureRow in exposureRowsForBaseDate)
            {
                var key = BuildJoinKey(GetRequired(exposureRow, columns.PortfolioId), GetRequired(exposureRow, columns.RiskFactor));
                potentialJoinRowCount += limitGroupByKey.TryGetValue(key, out var limitGroup)
                    ? Math.Max(1, limitGroup.Rows.Count)
                    : 1;
            }
        }

        var comparedRowCount = Math.Max(analysisRowCount, potentialJoinRowCount);
        if (comparedRowCount > exposureRowCount)
        {
            exceptions.Add(CreateException(
                ReconRowAmplification,
                SafetySeverity.High,
                $"기준일 노출 행 수보다 분석/잠재 조인 행 수가 큽니다. exposure={exposureRowCount:N0}, analysis={analysisRowCount:N0}, potentialJoin={potentialJoinRowCount:N0}.",
                baseDate,
                string.Empty,
                string.Empty));
        }
    }

    private static void AddSumBalanceException(
        List<LimitException> exceptions,
        string baseDate,
        CsvTable exposure,
        IReadOnlyList<LimitMonitorRow> rows,
        IReadOnlyList<CsvRow> exposureRowsForBaseDate,
        RequiredColumns columns)
    {
        decimal sourceExposureSum = 0m;
        var missingCount = 0;
        if (!HasColumns(exposure, [columns.BaseDate, columns.ExposureAmount]))
        {
            missingCount = HasColumn(exposure, columns.BaseDate)
                ? exposureRowsForBaseDate.Count
                : exposure.Rows.Count;
        }
        else
        {
            foreach (var exposureRow in exposureRowsForBaseDate)
            {
                if (TryParseDecimal(GetRequired(exposureRow, columns.ExposureAmount), out var exposureAmount))
                {
                    sourceExposureSum += exposureAmount;
                }
                else
                {
                    missingCount++;
                }
            }
        }

        var analysisExposureSum = rows.Sum(row => row.ExposureAmount);
        if (sourceExposureSum != analysisExposureSum || missingCount > 0)
        {
            exceptions.Add(CreateException(
                ReconSumBalance,
                SafetySeverity.High,
                $"원천 노출합계와 분석 노출합계가 일치하지 않거나 누락 행이 있습니다. source={sourceExposureSum}, analysis={analysisExposureSum}, missing={missingCount:N0}.",
                baseDate,
                string.Empty,
                string.Empty));
        }
    }

    private static ReconciliationSummary BuildReconciliationSummary(
        IReadOnlyList<LimitException> exceptions,
        bool currencyApplicable,
        bool unitApplicable)
    {
        var checks = ReconciliationCodes
            .Select(code =>
            {
                var checkExceptions = exceptions.Where(exception => string.Equals(exception.Code, code, StringComparison.Ordinal)).ToArray();
                return new ReconciliationCheck(
                    code,
                    ReconciliationCodeApplicable(code, currencyApplicable, unitApplicable),
                    checkExceptions.Length,
                    checkExceptions.Length == 0 ? SafetySeverity.Info : checkExceptions.Max(exception => exception.Severity));
            })
            .ToArray();
        var passed = !exceptions.Any(exception => ReconciliationFailCodes.Contains(exception.Code));
        return new ReconciliationSummary(passed, checks.Length, checks);
    }

    private static bool ReconciliationCodeApplicable(string code, bool currencyApplicable, bool unitApplicable)
    {
        return code switch
        {
            ReconCurrencyMismatch => currencyApplicable,
            ReconUnitMismatch => unitApplicable,
            _ => true
        };
    }

    private static int ReconciliationCodeOrder(string code)
    {
        for (var index = 0; index < ReconciliationCodes.Count; index++)
        {
            if (string.Equals(ReconciliationCodes[index], code, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return int.MaxValue;
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

    private static void AddDuplicateLimitRow(
        List<LimitMonitorRow> rows,
        List<LimitException> exceptions,
        string baseDate,
        CsvRow exposureRow,
        RequiredColumns columns,
        decimal exposureAmount,
        int duplicateLimitCount)
    {
        var row = CreateRow(
            baseDate,
            exposureRow,
            columns,
            exposureAmount,
            0m,
            0m,
            0m,
            LimitMonitorStatus.DuplicateLimit,
            $"동일 기준일·동일 Join Key 한도가 {duplicateLimitCount:N0}건이라 단정 불가: 검토 필요");
        rows.Add(row);
        exceptions.Add(CreateException("DUPLICATE_LIMIT", SafetySeverity.Medium, row.Note, row));
    }

    private static IReadOnlyList<string> BuildJoinAudit(
        BaseDateValidation baseDateValidation,
        IReadOnlyList<LimitMonitorRow> rows,
        ReconciliationComputation reconciliationComputation,
        RequiredColumns columns)
    {
        var blockedExposureRows = rows.Count(row => row.Status == LimitMonitorStatus.DuplicateLimit);
        return
        [
            $"JoinKey={columns.BaseDate}+{columns.PortfolioId}+{columns.RiskFactor}",
            $"DuplicateLimitRule=blocked; duplicateLimitKeys={reconciliationComputation.DuplicateLimitKeyCount:N0}; duplicateLimitRows={reconciliationComputation.DuplicateLimitRowCount:N0}; blockedExposureRows={blockedExposureRows:N0}",
            $"CurrencyApplicable={reconciliationComputation.CurrencyApplicable}",
            $"UnitApplicable={reconciliationComputation.UnitApplicable}",
            $"BaseDateRequested={baseDateValidation.RequestedBaseDate}; normalized={baseDateValidation.NormalizedBaseDate}; valid={baseDateValidation.IsValid}; formats={string.Join("|", BaseDateInputFormats)}"
        ];
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
            GetOptional(exposureRow, columns.CurrencyCode),
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
        return CreateException(code, severity, message, row.BaseDate, row.PortfolioId, row.RiskFactor);
    }

    private static LimitException CreateException(
        string code,
        SafetySeverity severity,
        string message,
        string baseDate,
        string portfolioId,
        string riskFactor)
    {
        return new LimitException(code, severity, message, baseDate, portfolioId, riskFactor);
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

    private static BaseDateValidation ValidateBaseDate(string baseDate)
    {
        var requestedBaseDate = baseDate.Trim();
        if (DateTime.TryParseExact(
            requestedBaseDate,
            BaseDateInputFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedBaseDate))
        {
            return new BaseDateValidation(requestedBaseDate, parsedBaseDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), IsValid: true);
        }

        return new BaseDateValidation(requestedBaseDate, requestedBaseDate, IsValid: false);
    }

    private static IReadOnlyList<string> MissingColumns(CsvTable table, IReadOnlyList<string> requiredColumns)
    {
        return requiredColumns
            .Where(required => !table.Columns.Contains(required, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<CsvRow> FilterRowsByBaseDate(CsvTable table, string baseDateColumn, string baseDate)
    {
        return HasColumn(table, baseDateColumn)
            ? table.Rows
                .Where(row => string.Equals(GetRequired(row, baseDateColumn), baseDate, StringComparison.Ordinal))
                .ToArray()
            : Array.Empty<CsvRow>();
    }

    private static bool HasColumns(CsvTable table, IReadOnlyList<string> columnNames)
    {
        return columnNames.All(columnName => HasColumn(table, columnName));
    }

    private static bool HasColumn(CsvTable table, string columnName)
    {
        return table.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasOptionalColumn(CsvTable table, string? columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName)
            && HasColumn(table, columnName);
    }

    private static string GetRequired(CsvRow row, string columnName)
    {
        return row.GetValue(columnName);
    }

    private static string GetOptional(CsvRow row, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return string.Empty;
        }

        return row.TryGetValue(columnName, out var value) ? value : string.Empty;
    }

    private static IReadOnlyList<string> DistinctOptionalValues(IReadOnlyList<CsvRow> rows, string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return Array.Empty<string>();
        }

        return rows
            .Select(row => GetOptional(row, columnName))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record BaseDateValidation(
        string RequestedBaseDate,
        string NormalizedBaseDate,
        bool IsValid);

    private sealed record RequiredColumns(
        string BaseDate,
        string PortfolioId,
        string RiskFactor,
        string ExposureAmount,
        string LimitAmount,
        string UseYn,
        string? CurrencyCode,
        string? UnitCode);

    private sealed record ReconciliationComputation(
        IReadOnlyList<LimitException> Exceptions,
        bool CurrencyApplicable,
        bool UnitApplicable,
        int DuplicateLimitKeyCount,
        int DuplicateLimitRowCount);

    private sealed record LimitRowGroup(
        string Key,
        string PortfolioId,
        string RiskFactor,
        IReadOnlyList<CsvRow> Rows);
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
        LimitMonitorStatus.DuplicateLimit => "DUPLICATE_LIMIT",
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
    MappingError,
    DuplicateLimit
}

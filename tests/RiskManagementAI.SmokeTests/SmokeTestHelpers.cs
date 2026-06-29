internal static class SmokeTestHelpers
{
internal static IReadOnlyList<string> PrivateGuardStrings(string fieldName)
{
    var field = typeof(KbRepositoryGuard).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"KbRepositoryGuard private field not found: {fieldName}");
    var value = field.GetValue(null)
        ?? throw new InvalidOperationException($"KbRepositoryGuard private field is null: {fieldName}");

    if (value is IEnumerable<string> strings)
    {
        return strings.ToArray();
    }

    throw new InvalidOperationException($"KbRepositoryGuard private field is not string collection: {fieldName}");
}

internal static int ExpectedKbLinearScore(RegulationCatalogEntry entry, string query)
{
    var score = 0;
    score += KbContains(entry.SourceId, query) ? 10 : 0;
    score += KbContains(entry.Title, query) ? 8 : 0;
    score += KbContains(entry.Category, query) ? 5 : 0;
    score += KbContains(entry.SourceOrg, query) ? 3 : 0;
    score += KbContains(entry.SourceType, query) ? 3 : 0;
    score += KbContains(entry.Status, query) ? 2 : 0;
    score += KbContains(entry.Note, query) ? 1 : 0;

    foreach (var term in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (term.Length < 2)
        {
            continue;
        }

        score += KbContains(entry.Title, term) ? 2 : 0;
        score += KbContains(entry.Note, term) ? 1 : 0;
    }

    return score;
}

internal static IReadOnlyList<(string SourceId, int Score)> ExpectedKbLinearResults(RegulationCatalog catalog, string query, int maxResults = 5)
{
    var normalizedQuery = query.Trim();
    if (string.IsNullOrWhiteSpace(normalizedQuery))
    {
        return [];
    }

    return catalog.Entries
        .Select(entry => (entry.SourceId, Score: ExpectedKbLinearScore(entry, normalizedQuery)))
        .Where(item => item.Score > 0)
        .OrderByDescending(item => item.Score)
        .ThenBy(item => item.SourceId, StringComparer.Ordinal)
        .Take(Math.Max(1, maxResults))
        .ToList();
}

internal static IReadOnlyList<(string SourceId, int Score)> KbSearchSignature(KbSearchResponse response)
{
    return response.Results
        .Select(result => (result.SourceId, result.Score))
        .ToList();
}

internal static bool KbContains(string source, string value)
{
    return source.Contains(value, StringComparison.OrdinalIgnoreCase);
}

internal static string ReadZipEntryText(ZipArchive archive, string entryName)
{
    var entry = archive.GetEntry(entryName) ?? throw new InvalidDataException($"ZIP entry not found: {entryName}");
    using var stream = entry.Open();
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

internal static void WriteZipEntry(ZipArchive archive, string entryName, string content)
{
    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
    using var stream = entry.Open();
    using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    writer.Write(content);
}

internal static void CreateSmokeXlsx(string path, bool tooManyRows = false)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }

    using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
    WriteZipEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>
  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet7.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""");
    WriteZipEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");
    WriteZipEntry(archive, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="First Visible" sheetId="1" r:id="rIdFirst"/>
    <sheet name="위험데이터" sheetId="2" r:id="rIdRisk"/>
  </sheets>
</workbook>
""");
    WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rIdFirst" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet7.xml"/>
  <Relationship Id="rIdRisk" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
  <Relationship Id="rIdShared" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/>
</Relationships>
""");
    WriteZipEntry(archive, "xl/sharedStrings.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <si><t>Marker</t></si>
  <si><t>Value</t></si>
  <si><t>BASE_DT</t></si>
  <si><t>DESK_CD</t></si>
  <si><t>한글</t></si>
  <si><t>FIRST</t></si>
  <si><t>20260617</t></si>
  <si><t>EQD</t></si>
  <si><r><t>값</t></r><r><t>힣</t></r></si>
</sst>
""");

    var firstSheetRows = new StringBuilder();
    firstSheetRows.AppendLine("""<row r="1"><c r="A1" t="s"><v>0</v></c><c r="B1" t="s"><v>1</v></c></row>""");
    var firstSheetDataRowCount = tooManyRows ? XlsxReader.MaxWorksheetRows + 1 : 1;
    for (var index = 0; index < firstSheetDataRowCount; index++)
    {
        firstSheetRows.Append("<row r=\"");
        firstSheetRows.Append(index + 2);
        firstSheetRows.Append("\"><c r=\"A");
        firstSheetRows.Append(index + 2);
        firstSheetRows.Append("\" t=\"s\"><v>5</v></c><c r=\"B");
        firstSheetRows.Append(index + 2);
        firstSheetRows.Append("\" t=\"inlineStr\"><is><t>");
        firstSheetRows.Append(SecurityElement.Escape($"default-{index}"));
        firstSheetRows.AppendLine("</t></is></c></row>");
    }

    WriteZipEntry(archive, "xl/worksheets/sheet7.xml", $"""
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
{firstSheetRows}
  </sheetData>
</worksheet>
""");
    WriteZipEntry(archive, "xl/worksheets/sheet2.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    <row r="1"><c r="A1" t="s"><v>2</v></c><c r="B1" t="s"><v>3</v></c><c r="C1" t="s"><v>4</v></c><c r="D1" t="inlineStr"><is><t>AMT</t></is></c></row>
    <row r="2"><c r="A2" t="s"><v>6</v></c><c r="B2" t="s"><v>7</v></c><c r="C2" t="s"><v>8</v></c><c r="D2"><v>10.5</v></c></row>
  </sheetData>
</worksheet>
""");
}

internal static string ToExcelColumnName(int columnIndex)
{
    var name = string.Empty;
    while (columnIndex > 0)
    {
        columnIndex--;
        name = (char)('A' + (columnIndex % 26)) + name;
        columnIndex /= 26;
    }

    return name;
}

internal static void CreateSingleSheetXlsx(string path, string[][] rows)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }

    var sheetRows = new StringBuilder();
    for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
    {
        var rowNumber = rowIndex + 1;
        sheetRows.Append("<row r=\"");
        sheetRows.Append(rowNumber);
        sheetRows.Append("\">");
        for (var columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
        {
            var columnName = ToExcelColumnName(columnIndex + 1);
            sheetRows.Append("<c r=\"");
            sheetRows.Append(columnName);
            sheetRows.Append(rowNumber);
            sheetRows.Append("\" t=\"inlineStr\"><is><t>");
            sheetRows.Append(SecurityElement.Escape(rows[rowIndex][columnIndex]) ?? string.Empty);
            sheetRows.Append("</t></is></c>");
        }

        sheetRows.AppendLine("</row>");
    }

    using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
    WriteZipEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""");
    WriteZipEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");
    WriteZipEntry(archive, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Sheet1" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
""");
    WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
</Relationships>
""");
    WriteZipEntry(archive, "xl/worksheets/sheet1.xml", $"""
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
{sheetRows}
  </sheetData>
</worksheet>
""");
}

internal static void WriteCsvRows(string path, string[][] rows)
{
    File.WriteAllText(path, string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row))) + Environment.NewLine);
}

internal static int ReconciliationExceptionCount(LimitAnalysisResult result, string code)
{
    return result.ExceptionList.Count(exception => string.Equals(exception.Code, code, StringComparison.Ordinal));
}

internal static ReconciliationCheck ReconciliationCheckFor(LimitAnalysisResult result, string code)
{
    return result.Reconciliation.Checks.Single(check => string.Equals(check.Code, code, StringComparison.Ordinal));
}

internal static string ReconciliationSignature(LimitAnalysisResult result)
{
    return string.Join(
        "|",
        result.Reconciliation.Checks.Select(check => $"{check.Code}:{check.Applicable}:{check.ExceptionCount}:{check.MaxSeverity}"));
}

internal static string PriorDaySignature(PriorDayAnalysisResult result)
{
    var rows = string.Join(
        ";",
        result.Contract.DataFact.ComparisonTable.Select(row =>
            string.Join(
                ":",
                row.PortfolioId,
                row.RiskFactor,
                row.CurrentStatus?.ToString() ?? "None",
                row.PriorStatus?.ToString() ?? "None",
                row.CurrentUsageRatio.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.PriorUsageRatio.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.UsageRatioDelta.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.CurrentLimitAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.PriorLimitAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.LimitAmountDelta.ToString(System.Globalization.CultureInfo.InvariantCulture),
                row.Movement)));
    var movers = string.Join(
        ";",
        result.Contract.DataFact.Movers.TopByUsageRatioDelta.Select(row =>
            $"{row.PortfolioId}:{row.RiskFactor}:{row.UsageRatioDelta.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
    var findings = string.Join(
        ";",
        result.Contract.HiddenRisk.Findings.Select(finding => $"{finding.Code}:{finding.Severity}:{finding.Message}"));

    return string.Join(
        "||",
        result.Contract.DataFact.CurrentBaseDate,
        result.Contract.DataFact.PriorBaseDate,
        result.Contract.DataFact.Kpis.ToString(),
        rows,
        movers,
        result.Contract.Methodology.ToString(),
        string.Join(";", result.Contract.UserValidation.ChecklistItems),
        findings,
        result.IsDeterministic);
}

internal static LimitAnalysisResult EmptyLimitAnalysis(string baseDate = "20260617")
{
    var rows = Array.Empty<LimitMonitorRow>();
    return new LimitAnalysisResult(
        baseDate,
        rows,
        LimitAnalysisKpis.FromRows(rows),
        new LimitAnalysisMetadata(baseDate, "N/A", "N/A", ColumnMappingUsedFallback: false, ColumnMappingWarnings: Array.Empty<string>(), IsDeterministic: true, JoinAudit: Array.Empty<string>()),
        Array.Empty<LimitException>(),
        Array.Empty<SafetyFinding>(),
        new ReconciliationSummary(Passed: true, CheckCount: 0, Checks: Array.Empty<ReconciliationCheck>()));
}
    internal static ColumnMappingLoadResult LoadCompleteCustomColumnMapping()
    {
        var customColumnMappingPath = Path.Combine("config", $"smoke_column_mapping_wp04_custom_{Guid.NewGuid():N}.json");
        File.WriteAllText(customColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "EXPOSURE",
    "LimitAmount": "LIMIT",
    "UseYn": "ACTIVE_YN"
  }
}
""");
        var result = ColumnMappingLoader.LoadFromFile(customColumnMappingPath);
        File.Delete(customColumnMappingPath);
        return result;
    }

    internal static (DataProfileResult ExposureProfile, LimitAnalysisResult SixStateResult, LimitAnalysisResult MappingErrorResult) CreateReportSmokeInputs()
    {
        var profiler = new DataProfiler();
        var exposureCsvPath = Path.Combine("samples", "dummy_data", "risk_exposure_sample.csv");
        var exposureProfile = profiler.ProfileCsv(exposureCsvPath);
        var limitMonitor = new LimitMonitor();
        var limitSmokeDirectory = Path.Combine("artifacts", "smoke-limit-wp05-report-fixture");
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
        var mappingErrorExposureCsv = Path.Combine(limitSmokeDirectory, "wp05_missing_amount.csv");
        File.WriteAllText(
            mappingErrorExposureCsv,
            "BASE_DT,DESK_CD,PORTFOLIO_ID,PRODUCT_TYPE,RISK_FACTOR,CCY_CD\n20260617,EQD,PF_MAP,ELS,RF_MAP,KRW\n");
        var mappingErrorResult = limitMonitor.Analyze(mappingErrorExposureCsv, wp05LimitCsv, "20260617");
        return (exposureProfile, limitMonitor.Analyze(wp05ExposureCsv, wp05LimitCsv, "20260617"), mappingErrorResult);
    }
}

sealed class StubDraftService : ILocalDraftService
{
    private readonly DraftResponse response;

    public StubDraftService(DraftResponse response)
    {
        this.response = response;
    }

    public DraftResponse GenerateDraft(DraftRequest? request)
    {
        return response;
    }
}

sealed class FixedClock : IClock
{
    public FixedClock(DateOnly today)
    {
        Today = today;
    }

    public DateOnly Today { get; }
}

using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Xml.Linq;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Dashboard;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Feedback;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Kb;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Mapping;
using RiskManagementAI.Core.Ncr;
using RiskManagementAI.Core.Report;
using RiskManagementAI.Core.Risk;
using RiskManagementAI.Core.Safety;

var failed = 0;

void AssertTrue(bool condition, string name)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {name}");
    }
    else
    {
        Console.WriteLine($"FAIL: {name}");
        failed++;
    }
}

bool Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
        return false;
    }
    catch (TException)
    {
        return true;
    }
}

IReadOnlyList<string> PrivateGuardStrings(string fieldName)
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

int ExpectedKbLinearScore(RegulationCatalogEntry entry, string query)
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

IReadOnlyList<(string SourceId, int Score)> ExpectedKbLinearResults(RegulationCatalog catalog, string query, int maxResults = 5)
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

IReadOnlyList<(string SourceId, int Score)> KbSearchSignature(KbSearchResponse response)
{
    return response.Results
        .Select(result => (result.SourceId, result.Score))
        .ToList();
}

bool KbContains(string source, string value)
{
    return source.Contains(value, StringComparison.OrdinalIgnoreCase);
}

string ReadZipEntryText(ZipArchive archive, string entryName)
{
    var entry = archive.GetEntry(entryName) ?? throw new InvalidDataException($"ZIP entry not found: {entryName}");
    using var stream = entry.Open();
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

void WriteZipEntry(ZipArchive archive, string entryName, string content)
{
    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
    using var stream = entry.Open();
    using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    writer.Write(content);
}

void CreateSmokeXlsx(string path, bool tooManyRows = false)
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

string ToExcelColumnName(int columnIndex)
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

void CreateSingleSheetXlsx(string path, string[][] rows)
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

void WriteCsvRows(string path, string[][] rows)
{
    File.WriteAllText(path, string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row))) + Environment.NewLine);
}

int ReconciliationExceptionCount(LimitAnalysisResult result, string code)
{
    return result.ExceptionList.Count(exception => string.Equals(exception.Code, code, StringComparison.Ordinal));
}

ReconciliationCheck ReconciliationCheckFor(LimitAnalysisResult result, string code)
{
    return result.Reconciliation.Checks.Single(check => string.Equals(check.Code, code, StringComparison.Ordinal));
}

string ReconciliationSignature(LimitAnalysisResult result)
{
    return string.Join(
        "|",
        result.Reconciliation.Checks.Select(check => $"{check.Code}:{check.Applicable}:{check.ExceptionCount}:{check.MaxSeverity}"));
}

LimitAnalysisResult EmptyLimitAnalysis(string baseDate = "20260617")
{
    var rows = Array.Empty<LimitMonitorRow>();
    return new LimitAnalysisResult(
        baseDate,
        rows,
        LimitAnalysisKpis.FromRows(rows),
        new LimitAnalysisMetadata(baseDate, "N/A", "N/A", ColumnMappingUsedFallback: false, ColumnMappingWarnings: Array.Empty<string>(), IsDeterministic: true),
        Array.Empty<LimitException>(),
        Array.Empty<SafetyFinding>(),
        new ReconciliationSummary(Passed: true, CheckCount: 0, Checks: Array.Empty<ReconciliationCheck>()));
}

var loadedRuleSet = RuleLoader.LoadDefault();
AssertTrue(!loadedRuleSet.UsedFallback, "RuleLoader should load repo rules");
AssertTrue(loadedRuleSet.RuleVersion.StartsWith("ruleset-", StringComparison.Ordinal) && loadedRuleSet.RuleVersion.Length == 20, "RuleLoader should produce deterministic ruleset version");
AssertTrue(loadedRuleSet.SqlDenyRules.Any(r => r.Code == "SQL_DML_DELETE"), "RuleLoader should map SQL deny patterns");
AssertTrue(loadedRuleSet.VbaRequiredPresentRules.Any(r => r.Code == "VBA_OPTION_EXPLICIT_MISSING"), "RuleLoader should map REQUIRE_PRESENT rules");
AssertTrue(loadedRuleSet.ExcelBlockedFunctions.Contains("MAP"), "RuleLoader should load Excel blocked functions");

var sqlChecker = new SqlSafetyChecker(loadedRuleSet);
var sqlFindings = sqlChecker.Check("DELETE FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
AssertTrue(sqlFindings.Any(f => f.Code == "SQL_DML_DELETE"), "SQL DELETE should be blocked");

var selectFindings = sqlChecker.Check("SELECT TRADE_ID, BASE_DT FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT").ToList();
AssertTrue(selectFindings.All(f => f.Severity != SafetySeverity.Blocker), "SELECT should not have blocker");

var sqlDenySamples = new Dictionary<string, string>
{
    ["INSERT"] = "INSERT INTO TRADE_SAMPLE (ID) VALUES (1)",
    ["UPDATE"] = "UPDATE TRADE_SAMPLE SET AMT = 0",
    ["DELETE"] = "DELETE FROM TRADE_SAMPLE",
    ["MERGE"] = "MERGE INTO TRADE_SAMPLE USING SRC ON (1=1)",
    ["CREATE"] = "CREATE TABLE TMP_SAMPLE (ID NUMBER)",
    ["ALTER"] = "ALTER TABLE TRADE_SAMPLE ADD TMP_COL NUMBER",
    ["DROP"] = "DROP TABLE TRADE_SAMPLE",
    ["TRUNCATE"] = "TRUNCATE TABLE TRADE_SAMPLE",
    ["GRANT"] = "GRANT SELECT ON TRADE_SAMPLE TO USER_SAMPLE",
    ["REVOKE"] = "REVOKE SELECT ON TRADE_SAMPLE FROM USER_SAMPLE",
    ["EXEC"] = "EXEC PROC_SAMPLE",
    ["CALL"] = "CALL PROC_SAMPLE()",
    ["COMMIT"] = "COMMIT",
    ["ROLLBACK"] = "ROLLBACK"
};

foreach (var (keyword, sql) in sqlDenySamples)
{
    var findings = sqlChecker.Check(sql).ToList();
    AssertTrue(findings.Any(f => f.Severity == SafetySeverity.Blocker && string.Equals(f.MatchedText, keyword, StringComparison.OrdinalIgnoreCase)), $"SQL {keyword} should be blocker");
}

AssertTrue(sqlChecker.Check("SELECT * FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_SELECT_STAR" && f.Severity == SafetySeverity.Medium), "SQL SELECT * should warn");
AssertTrue(sqlChecker.Check("SELECT TRADE_ID FROM TRADE_SAMPLE WHERE 1=1").Any(f => f.Code == "SQL_WHERE_ALWAYS_TRUE"), "SQL WHERE 1=1 should warn");
AssertTrue(sqlChecker.Check("SELECT A.ID FROM A CROSS JOIN B").Any(f => f.Code == "SQL_CROSS_JOIN"), "SQL CROSS JOIN should warn");
AssertTrue(sqlChecker.Check("SELECT /*+ INDEX(A IDX_A) */ A.ID FROM A").Any(f => f.Code == "SQL_OPTIMIZER_HINT"), "SQL optimizer hint should warn");

var vbaChecker = new VbaSafetyChecker(loadedRuleSet);
var vbaFindings = vbaChecker.Check("Sub Test()\nShell \"cmd.exe\"\nEnd Sub").ToList();
AssertTrue(vbaFindings.Any(f => f.Code == "VBA_SHELL"), "VBA Shell should be blocked");
AssertTrue(vbaFindings.Any(f => f.Code == "VBA_OPTION_EXPLICIT_MISSING"), "Option Explicit missing should warn");

var vbaDenySamples = new Dictionary<string, (string Code, string Text)>
{
    ["Shell"] = ("VBA_SHELL", "Option Explicit\nSub Test()\nShell \"cmd.exe\"\nEnd Sub"),
    ["WScript.Shell"] = ("VBA_WSCRIPT", "Option Explicit\nSub Test()\nCreateObject(\"WScript.Shell\")\nEnd Sub"),
    ["Kill"] = ("VBA_KILL", "Option Explicit\nSub Test()\nKill \"C:\\temp\\sample.txt\"\nEnd Sub"),
    ["FileSystemObject"] = ("VBA_FSO", "Option Explicit\nSub Test()\nDim fso As FileSystemObject\nEnd Sub"),
    ["Declare PtrSafe"] = ("VBA_WINAPI", "Option Explicit\nDeclare PtrSafe Function GetTickCount Lib \"kernel32\" () As Long\nSub Test()\nEnd Sub"),
    ["Outlook.Application"] = ("VBA_OUTLOOK", "Option Explicit\nSub Test()\nCreateObject(\"Outlook.Application\")\nEnd Sub"),
    ["WinHttp"] = ("VBA_HTTP", "Option Explicit\nSub Test()\nCreateObject(\"WinHttp.WinHttpRequest.5.1\")\nEnd Sub"),
    ["MSXML2.XMLHTTP"] = ("VBA_HTTP", "Option Explicit\nSub Test()\nCreateObject(\"MSXML2.XMLHTTP\")\nEnd Sub"),
    ["FollowHyperlink"] = ("VBA_FOLLOW_HYPERLINK", "Option Explicit\nSub Test()\nActiveWorkbook.FollowHyperlink \"https://example.invalid\"\nEnd Sub")
};

foreach (var (label, sample) in vbaDenySamples)
{
    var findings = vbaChecker.Check(sample.Text).ToList();
    AssertTrue(findings.Any(f => f.Code == sample.Code), $"VBA {label} should be detected");
    AssertTrue(findings.All(f => f.Code != "VBA_OPTION_EXPLICIT_MISSING"), $"VBA {label} with Option Explicit should not warn missing Option Explicit");
}

AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.DisplayAlerts = False\nEnd Sub").Any(f => f.Code == "VBA_DISPLAY_ALERTS_DISABLED"), "VBA DisplayAlerts false should warn");
AssertTrue(vbaChecker.Check("Option Explicit\nSub Test()\nApplication.EnableEvents = False\nEnd Sub").Any(f => f.Code == "VBA_ENABLE_EVENTS_DISABLED"), "VBA EnableEvents false should warn");

var excelChecker = new Excel2021FunctionChecker(loadedRuleSet);
var excelFindings = excelChecker.CheckFormula("=VSTACK(A1:A3,B1:B3)").ToList();
AssertTrue(excelFindings.Any(f => f.Code == "EXCEL_365_FUNCTION"), "VSTACK should be blocked for Excel 2021");
AssertTrue(excelFindings.Any(f => f.Message.Contains("XLOOKUP", StringComparison.OrdinalIgnoreCase)), "Excel blocked function finding should include preferred function guidance");

foreach (var functionName in loadedRuleSet.ExcelBlockedFunctions)
{
    var findings = excelChecker.CheckFormula($"={functionName}(A1:A3)").ToList();
    AssertTrue(findings.Any(f => f.Code == "EXCEL_365_FUNCTION"), $"Excel 2021 blocked function {functionName} should be detected");
}

var allowedExcelFindings = excelChecker.CheckFormula("=XLOOKUP(A1,B:B,C:C)").ToList();
AssertTrue(allowedExcelFindings.All(f => f.Code != "EXCEL_365_FUNCTION"), "Excel 2021 preferred function XLOOKUP should be allowed");

var customRulesDirectory = Path.Combine("artifacts", "smoke-rules-b01");
if (Directory.Exists(customRulesDirectory))
{
    Directory.Delete(customRulesDirectory, recursive: true);
}

Directory.CreateDirectory(customRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(customRulesDirectory, Path.GetFileName(sourceFile)));
}

File.AppendAllText(Path.Combine(customRulesDirectory, "sql_deny_patterns.txt"), "\n\\bVACUUM\\b\n");
var customRuleSet = RuleLoader.LoadFromDirectory(customRulesDirectory);
var customRuleFindings = new SqlSafetyChecker(customRuleSet).Check("VACUUM TABLE TRADE_SAMPLE").ToList();
AssertTrue(!customRuleSet.UsedFallback, "Custom smoke rules should load without fallback");
AssertTrue(customRuleFindings.Any(f => f.Code == "SQL_DENY_PATTERN" && string.Equals(f.MatchedText, "VACUUM", StringComparison.OrdinalIgnoreCase)), "Temporary SQL rule file pattern should be injected");

var versionDriftRulesDirectory = Path.Combine("artifacts", "smoke-rules-b09-version");
if (Directory.Exists(versionDriftRulesDirectory))
{
    Directory.Delete(versionDriftRulesDirectory, recursive: true);
}

Directory.CreateDirectory(versionDriftRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(versionDriftRulesDirectory, Path.GetFileName(sourceFile)));
}

var versionBeforeRuleChange = RuleLoader.LoadFromDirectory(versionDriftRulesDirectory);
File.AppendAllText(Path.Combine(versionDriftRulesDirectory, "sql_warn_patterns.txt"), "\nFULL\\s+OUTER\\s+JOIN\n");
var versionAfterRuleChange = RuleLoader.LoadFromDirectory(versionDriftRulesDirectory);
AssertTrue(versionBeforeRuleChange.RuleVersion != versionAfterRuleChange.RuleVersion, "RuleLoader ruleset version should change when rule content changes");

var emptyMandatoryRulesDirectory = Path.Combine("artifacts", "smoke-rules-b09-empty-mandatory");
if (Directory.Exists(emptyMandatoryRulesDirectory))
{
    Directory.Delete(emptyMandatoryRulesDirectory, recursive: true);
}

Directory.CreateDirectory(emptyMandatoryRulesDirectory);
foreach (var sourceFile in Directory.EnumerateFiles("rules", "*.txt"))
{
    File.Copy(sourceFile, Path.Combine(emptyMandatoryRulesDirectory, Path.GetFileName(sourceFile)));
}

File.WriteAllText(Path.Combine(emptyMandatoryRulesDirectory, "sql_deny_patterns.txt"), "# accidental truncation\n\n");
var emptyMandatoryRuleSet = RuleLoader.LoadFromDirectory(emptyMandatoryRulesDirectory);
AssertTrue(emptyMandatoryRuleSet.UsedFallback, "RuleLoader should fallback when a mandatory rule group is empty");
AssertTrue(new SqlSafetyChecker(emptyMandatoryRuleSet).Check("DELETE FROM TRADE_SAMPLE").Any(f => f.Code == "SQL_DML_DELETE"), "Fallback after empty mandatory rules should still block DELETE");

var fallbackRuleSet = RuleLoader.LoadFromDirectory(Path.Combine("artifacts", "missing-rules-b01"));
var fallbackFindings = new SqlSafetyChecker(fallbackRuleSet).Check("DELETE FROM TRADE_SAMPLE").ToList();
AssertTrue(fallbackRuleSet.UsedFallback, "Missing rules directory should use fallback");
AssertTrue(fallbackFindings.Any(f => f.Code == "RULESET_FALLBACK"), "Fallback use should be reported as finding");
AssertTrue(fallbackFindings.Any(f => f.Code == "SQL_DML_DELETE"), "Fallback rules should still block SQL DELETE");
AssertTrue(
    new VbaSafetyChecker(fallbackRuleSet)
        .Check("Option Explicit\nSub Test()\nDim fso As FileSystemObject\nEnd Sub")
        .Any(f => f.Code == "VBA_FSO"),
    "Fallback rules should detect direct FileSystemObject declarations");

var policyLoadResult = PolicyLoader.LoadDefault();
AssertTrue(!policyLoadResult.UsedFallback, "PolicyLoader should load repo security policy");
AssertTrue(!policyLoadResult.Policy.Network.AllowExternalApi, "Security policy should block external API");
AssertTrue(!policyLoadResult.Policy.Network.AllowAutoUpdate, "Security policy should block auto update");
AssertTrue(!policyLoadResult.Policy.Network.AllowTelemetry, "Security policy should block telemetry");
AssertTrue(!policyLoadResult.Policy.Sql.AllowAutoExecute, "Security policy should block SQL auto execution");
AssertTrue(!policyLoadResult.Policy.Vba.AllowAutoExecute, "Security policy should block VBA auto execution");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureExternalApiAllowed()), "Security policy should enforce external API block");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureSqlAutoExecuteAllowed()), "Security policy should enforce SQL auto-execute block");
AssertTrue(Throws<InvalidOperationException>(() => policyLoadResult.Policy.EnsureVbaAutoExecuteAllowed()), "Security policy should enforce VBA auto-execute block");

var missingPolicyResult = PolicyLoader.LoadFromFile("config/missing_policy_smoke.json");
AssertTrue(missingPolicyResult.UsedFallback, "Missing security policy should use safe fallback");
AssertTrue(!missingPolicyResult.Policy.Network.AllowExternalApi && !missingPolicyResult.Policy.Sql.AllowAutoExecute, "Security policy fallback should keep dangerous actions disabled");
AssertTrue(PolicyLoader.LoadFromFile("../security_policy.json").UsedFallback, "PolicyLoader should reject non-config-relative paths");

var invalidPolicyPath = Path.Combine("config", "smoke_invalid_policy.json");
File.WriteAllText(invalidPolicyPath, "{ invalid json");
var invalidPolicyResult = PolicyLoader.LoadFromFile(invalidPolicyPath);
File.Delete(invalidPolicyPath);
AssertTrue(invalidPolicyResult.UsedFallback && !invalidPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on invalid JSON policy");

var nullSectionPolicyPath = Path.Combine("config", "smoke_null_section_policy.json");
File.WriteAllText(nullSectionPolicyPath, "{ \"Network\": null }");
var nullSectionPolicyResult = PolicyLoader.LoadFromFile(nullSectionPolicyPath);
File.Delete(nullSectionPolicyPath);
AssertTrue(nullSectionPolicyResult.UsedFallback && !nullSectionPolicyResult.Policy.Network.AllowExternalApi, "PolicyLoader should safe-fallback on null policy sections");

var settingsSnapshot = new SecuritySettingsSnapshotBuilder().Build(policyLoadResult, loadedRuleSet.RuleVersion, NoModelDraftService.ModeName);
AssertTrue(!settingsSnapshot.UsedFallback, "SecuritySettingsSnapshot should preserve policy load state");
AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Environment" && row.Name == "RuleVersion" && row.Value == loadedRuleSet.RuleVersion), "SecuritySettingsSnapshot should include rule version");
AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Environment" && row.Name == "LocalModelMode" && row.Value == NoModelDraftService.ModeName), "SecuritySettingsSnapshot should include local model mode");
AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Network" && row.Name == "AllowExternalApi" && row.Value == "False" && row.Meaning == "Blocked"), "SecuritySettingsSnapshot should show external API blocked");
AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Sql" && row.Name == "AllowAutoExecute" && row.Value == "False"), "SecuritySettingsSnapshot should show SQL auto execution blocked");
AssertTrue(settingsSnapshot.Findings.Any(f => f.Code == "SETTINGS_POLICY_LOADED" && f.Severity == SafetySeverity.Info), "SecuritySettingsSnapshot should emit loaded finding");
var fallbackSettingsSnapshot = new SecuritySettingsSnapshotBuilder().Build(missingPolicyResult, loadedRuleSet.RuleVersion, NoModelDraftService.ModeName);
AssertTrue(fallbackSettingsSnapshot.UsedFallback && fallbackSettingsSnapshot.Findings.Any(f => f.Code == "SETTINGS_POLICY_FALLBACK"), "SecuritySettingsSnapshot should report fallback state");

var dashboardSnapshot = new DashboardSnapshotBuilder().Build(new DashboardSnapshotRequest(
    policyLoadResult,
    loadedRuleSet.RuleVersion,
    NoModelDraftService.ModeName,
    new AuditLogReadResult(Array.Empty<AuditLogRecord>(), Array.Empty<SafetyFinding>()),
    PromotedExampleCount: 2,
    ReportCount: 3));
AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Offline Mode" && row.Value == "Enabled"), "DashboardSnapshot should show offline mode enabled");
AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Local Model" && row.Value == NoModelDraftService.ModeName), "DashboardSnapshot should show local model mode");
AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "RuleVersion" && row.Value == loadedRuleSet.RuleVersion), "DashboardSnapshot should show rule version");
AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Promoted Examples" && row.Value.Trim() == "2"), "DashboardSnapshot should show promoted example count");
AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Reports" && row.Value.Trim() == "3"), "DashboardSnapshot should show report count");
AssertTrue(dashboardSnapshot.Findings.Any(f => f.Code == "DASHBOARD_READY" && f.Severity == SafetySeverity.Info), "DashboardSnapshot should emit ready finding");

var columnMappingLoadResult = ColumnMappingLoader.LoadDefault();
AssertTrue(!columnMappingLoadResult.UsedFallback, "ColumnMappingLoader should load repo default mapping");
AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMapping default should preserve BASE_DT");
AssertTrue(columnMappingLoadResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORTFOLIO_ID", "ColumnMapping default should preserve PORTFOLIO_ID");

var customColumnMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_custom.json");
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
var customColumnMappingResult = ColumnMappingLoader.LoadFromFile(customColumnMappingPath);
File.Delete(customColumnMappingPath);
AssertTrue(!customColumnMappingResult.UsedFallback, "ColumnMappingLoader should load complete custom config mapping");
AssertTrue(customColumnMappingResult.Mapping.Physical(LogicalColumn.PortfolioId) == "PORT_ID", "ColumnMapping custom config should apply physical column names");

var partialColumnMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_partial.json");
File.WriteAllText(partialColumnMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE"
  }
}
""");
var partialColumnMappingResult = ColumnMappingLoader.LoadFromFile(partialColumnMappingPath);
File.Delete(partialColumnMappingPath);
AssertTrue(partialColumnMappingResult.UsedFallback && partialColumnMappingResult.Warnings.Count > 0, "ColumnMappingLoader should fallback for partial custom mappings");
AssertTrue(partialColumnMappingResult.Mapping.Physical(LogicalColumn.BaseDate) == "BASE_DT", "ColumnMapping partial fallback should discard partial overrides");

var duplicatePhysicalMappingPath = Path.Combine("config", "smoke_column_mapping_wp04_duplicate.json");
File.WriteAllText(duplicatePhysicalMappingPath, """
{
  "Mappings": {
    "BaseDate": "BASE_DATE",
    "PortfolioId": "PORT_ID",
    "RiskFactor": "RISK_NM",
    "ExposureAmount": "DUPLICATE_AMOUNT",
    "LimitAmount": "DUPLICATE_AMOUNT",
    "UseYn": "ACTIVE_YN"
  }
}
""");
var duplicatePhysicalMappingResult = ColumnMappingLoader.LoadFromFile(duplicatePhysicalMappingPath);
File.Delete(duplicatePhysicalMappingPath);
AssertTrue(duplicatePhysicalMappingResult.UsedFallback && duplicatePhysicalMappingResult.Warnings.Count > 0, "ColumnMappingLoader should fallback on physical column collisions");
AssertTrue(Throws<ArgumentException>(() => ColumnMappingLoader.LoadFromFile("../x.json")), "ColumnMappingLoader should reject parent traversal paths");
AssertTrue(Throws<ArgumentException>(() => ColumnMappingLoader.LoadFromFile("artifacts/x.json")), "ColumnMappingLoader should reject paths outside config");
AssertTrue(Throws<InvalidDataException>(() => new ColumnMapping(new Dictionary<LogicalColumn, string>()).Physical(LogicalColumn.BaseDate)), "ColumnMapping should throw when a logical column is unmapped");

var noModelDraftService = new NoModelDraftService(policyLoadResult.Policy);
var noModelDraftResponse = noModelDraftService.GenerateDraft(new DraftRequest(
    DraftRequestKind.Sql,
    "SELECT draft request for review-only output",
    "dummy context"));
AssertTrue(!noModelDraftResponse.IsAvailable, "NoModelDraftService should keep generation unavailable");
AssertTrue(noModelDraftResponse.Mode == NoModelDraftService.ModeName, "NoModelDraftService should report NoModelMode");
AssertTrue(noModelDraftResponse.DraftText is null, "NoModelDraftService should not return generated draft text");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_NO_MODEL_MODE" && f.Severity == SafetySeverity.Info), "NoModelDraftService should return safe no-model guidance");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_EXTERNAL_COMM_BLOCKED"), "NoModelDraftService should confirm external communications are blocked");
AssertTrue(noModelDraftResponse.Findings.Any(f => f.Code == "DRAFT_AUTO_EXECUTE_BLOCKED"), "NoModelDraftService should confirm auto execution is blocked");
AssertTrue(noModelDraftService.GenerateDraft(null).Findings.Any(f => f.Code == "DRAFT_PROMPT_EMPTY"), "NoModelDraftService should not throw for an empty request");

var unsafeDraftPolicy = policyLoadResult.Policy with
{
    Network = policyLoadResult.Policy.Network with
    {
        AllowExternalApi = true
    }
};
var unsafeDraftResponse = new NoModelDraftService(unsafeDraftPolicy).GenerateDraft(new DraftRequest(DraftRequestKind.General, "policy check"));
AssertTrue(unsafeDraftResponse.Findings.Any(f => f.Code == "DRAFT_POLICY_UNSAFE_NETWORK" && f.Severity == SafetySeverity.High), "NoModelDraftService should flag unsafe network policy");

var draftPipelineLogPath = Path.Combine("logs", "smoke_draft_pipeline_log.jsonl");
if (File.Exists(draftPipelineLogPath))
{
    File.Delete(draftPipelineLogPath);
}

var safeSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var safeSqlPipelineResult = safeSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make select draft",
    "user-smoke"));
AssertTrue(safeSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should accept safe SQL draft for review");
AssertTrue(safeSqlPipelineResult.SafetyResult == "PASS", "DraftPipeline safe SQL result should pass");
AssertTrue(safeSqlPipelineResult.AuditLogWritten, "DraftPipeline should audit safe SQL result");
AssertTrue(safeSqlPipelineResult.DraftText is not null && safeSqlPipelineResult.DraftText.Contains("SELECT", StringComparison.OrdinalIgnoreCase), "DraftPipeline should return accepted safe SQL draft");

var blockedSqlPipeline = new DraftPipeline(
    new StubDraftService(new DraftResponse(
        true,
        "StubMode",
        "draft generated",
        "DELETE FROM TRADE_SAMPLE",
        Array.Empty<SafetyFinding>())),
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var blockedSqlPipelineResult = blockedSqlPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Sql,
    "make delete draft",
    "user-smoke"));
AssertTrue(!blockedSqlPipelineResult.IsAcceptedForReview, "DraftPipeline should reject blocker SQL draft");
AssertTrue(blockedSqlPipelineResult.SafetyResult == "BLOCKED", "DraftPipeline blocker SQL result should be blocked");
AssertTrue(blockedSqlPipelineResult.DraftText is null, "DraftPipeline should suppress blocked draft text");
AssertTrue(blockedSqlPipelineResult.Findings.Any(f => f.Code == "SQL_DML_DELETE"), "DraftPipeline should include SQL checker blocker finding");

var noModelPipeline = new DraftPipeline(noModelDraftService, loadedRuleSet, new TaskLogWriter("logs", "smoke_draft_pipeline_log.jsonl"));
var noModelPipelineResult = noModelPipeline.Generate(new DraftPipelineRequest(
    DraftRequestKind.Vba,
    "make vba draft",
    "user-smoke"));
AssertTrue(!noModelPipelineResult.IsAcceptedForReview, "DraftPipeline should keep NoModel result unavailable");
AssertTrue(noModelPipelineResult.SafetyResult == "NO_MODEL", "DraftPipeline NoModel result should report NO_MODEL");
AssertTrue(noModelPipelineResult.AuditLogWritten, "DraftPipeline should audit NoModel result");

var draftPipelineLogText = File.ReadAllText(draftPipelineLogPath);
AssertTrue(!draftPipelineLogText.Contains("make select draft", StringComparison.Ordinal), "DraftPipeline audit should not store raw prompt text");
AssertTrue(!draftPipelineLogText.Contains("SELECT TRADE_ID", StringComparison.Ordinal), "DraftPipeline audit should not store raw draft text");
AssertTrue(!draftPipelineLogText.Contains("user-smoke", StringComparison.Ordinal), "DraftPipeline audit should not store raw user id");

var kbSearchLogPath = Path.Combine("logs", "smoke_kb_search_log.jsonl");
if (File.Exists(kbSearchLogPath))
{
    File.Delete(kbSearchLogPath);
}

var regulationCatalog = RegulationCatalog.LoadDefault();
AssertTrue(regulationCatalog.Entries.Count >= 5, "RegulationCatalog should load public catalog entries");
var publicRegEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "FIA_REG");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.Source) && publicRegEntry.Source != publicRegEntry.SourceOrg, "RegulationCatalog metadata should include source locator distinct from source org");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.Version), "RegulationCatalog metadata should include version field");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.EffectiveDate), "RegulationCatalog metadata should include effective date field");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.LoadedDate), "RegulationCatalog metadata should include loaded date field");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.ApprovalStatus), "RegulationCatalog metadata should include approval status");
AssertTrue(!string.IsNullOrWhiteSpace(publicRegEntry.LicenseStatus), "RegulationCatalog metadata should include license status");
AssertTrue(regulationCatalog.Warnings.Any(warning => warning.Contains("file_hash", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn when public source file hash is not loaded");
var internalCatalogEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "INTERNAL_RULES");
var ncrCatalogEntry = regulationCatalog.Entries.Single(entry => entry.SourceId == "NCR_GUIDE");
AssertTrue(internalCatalogEntry.Status == "PROD_ONLY" && internalCatalogEntry.Note.Contains("권한통제형", StringComparison.Ordinal), "RegulationCatalog should keep internal rules metadata-only and prod-only");
AssertTrue(ncrCatalogEntry.Status == "MANUAL_APPROVAL_REQUIRED" && ncrCatalogEntry.Note.Contains("원문 금지", StringComparison.Ordinal), "RegulationCatalog should keep NCR official text out of repo");

var legacyCatalogPath = Path.Combine(Path.GetTempPath(), $"legacy_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    legacyCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nLEGACY,PUBLIC_REG,Legacy Regulation,Public Org,Public regulation,CATALOG_ONLY,legacy only\n",
    Encoding.UTF8);
var legacyCatalog = RegulationCatalog.LoadFromFile(legacyCatalogPath);
AssertTrue(legacyCatalog.Entries.Single().Source == string.Empty, "RegulationCatalog should load legacy 7-column catalog with empty source metadata");
AssertTrue(legacyCatalog.Warnings.Any(warning => warning.Contains("missing optional column 'source'", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn on missing metadata columns without throwing");

var missingRequiredCatalogPath = Path.Combine(Path.GetTempPath(), $"missing_required_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    missingRequiredCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nSHORT,PUBLIC_REG,Short Regulation,Public Org,Public regulation,CATALOG_ONLY\n",
    Encoding.UTF8);
AssertTrue(Throws<InvalidDataException>(() => RegulationCatalog.LoadFromFile(missingRequiredCatalogPath)), "RegulationCatalog should reject rows that omit required catalog fields");

var emptyRequiredCatalogPath = Path.Combine(Path.GetTempPath(), $"empty_required_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    emptyRequiredCatalogPath,
    "source_id,category,title,source_org,source_type,status,note\nEMPTY_STATUS,PUBLIC_REG,Empty Status,Public Org,Public regulation,,required status is empty\n",
    Encoding.UTF8);
AssertTrue(Throws<InvalidDataException>(() => RegulationCatalog.LoadFromFile(emptyRequiredCatalogPath)), "RegulationCatalog should reject empty required catalog values");

var emptyMetadataCatalogPath = Path.Combine(Path.GetTempPath(), $"empty_metadata_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    emptyMetadataCatalogPath,
    "source_id,category,title,source_org,source_type,status,note,source,version,effective_date,repeal_date,file_hash,loaded_date,approval_status,superseded_by,license_status\nEMPTY,PUBLIC_REG,Empty Metadata,Public Org,Public regulation,CATALOG_ONLY,metadata empty,,,,,,,,,\n",
    Encoding.UTF8);
var emptyMetadataCatalog = RegulationCatalog.LoadFromFile(emptyMetadataCatalogPath);
AssertTrue(emptyMetadataCatalog.Entries.Single().LicenseStatus == string.Empty, "RegulationCatalog should keep empty metadata values as empty strings");
AssertTrue(emptyMetadataCatalog.Warnings.Any(warning => warning.Contains("metadata incomplete", StringComparison.OrdinalIgnoreCase)), "RegulationCatalog should warn on empty metadata values without throwing");
var kbSearch = new KbSearch(
    regulationCatalog,
    new TaskLogWriter("logs", "smoke_kb_search_log.jsonl"),
    loadedRuleSet.RuleVersion);
var ncrSearchResponse = kbSearch.Search("NCR", "user-smoke");
AssertTrue(ncrSearchResponse.Results.Any(result => result.SourceId == "NCR_GUIDE"), "KbSearch should find NCR catalog entry");
var ncrSearchResult = ncrSearchResponse.Results.Single(result => result.SourceId == "NCR_GUIDE");
AssertTrue(ncrSearchResult.Disclosure == KbDisclosure.ApprovalRequired, "KbSearch should mark NCR manual upload entries as approval required");
AssertTrue(ncrSearchResult.DisclosureReason.Contains("원문 미적재", StringComparison.Ordinal), "KbSearch should explain NCR source text is not loaded");
AssertTrue(ncrSearchResponse.Findings.Any(finding => finding.Code == "KB_APPROVAL_REQUIRED" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured approval-required finding for NCR entries");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal), "KbSearch answer should mark review draft");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch answer should always include sources");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("원문은 포함하지 않습니다", StringComparison.Ordinal), "KbSearch answer should state internal originals are excluded");
AssertTrue(!ncrSearchResponse.DraftAnswer.Contains("제1조", StringComparison.Ordinal), "KbSearch should not expose NCR clause source text");
AssertTrue(ncrSearchResponse.AuditLogWritten, "KbSearch should write audit log when configured");
AssertTrue(!ncrSearchResponse.DraftAnswer.Contains("NOT_LOADED", StringComparison.Ordinal), "KbSearch citation answer should not cite NOT_LOADED as real metadata");
AssertTrue(ncrSearchResponse.DraftAnswer.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render NOT_LOADED metadata as confirmation-needed");
AssertTrue(ncrSearchResponse.Warnings.Any(warning => warning.Contains("source_id=NCR_GUIDE", StringComparison.Ordinal) && warning.Contains("version", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for NOT_LOADED version metadata");

var publicRegSearchResponse = kbSearch.Search("금융투자업규정", "user-smoke");
AssertTrue(publicRegSearchResponse.Results.Any(result => result.SourceId == "FIA_REG"), "KbSearch should find public regulation catalog entry");
var publicRegSearchResult = publicRegSearchResponse.Results.First(result => result.SourceId == "FIA_REG");
AssertTrue(publicRegSearchResult.Disclosure == KbDisclosure.PublicCited, "KbSearch should mark public catalog entries as PublicCited");
AssertTrue(publicRegSearchResult.DisclosureReason.Contains("공개 catalog", StringComparison.Ordinal), "KbSearch should explain public catalog citation disclosure");
var internalRulesSearchResponse = kbSearch.Search("내부규정", "user-smoke");
var internalRulesSearchResult = internalRulesSearchResponse.Results.Single(result => result.SourceId == "INTERNAL_RULES");
AssertTrue(internalRulesSearchResult.Disclosure == KbDisclosure.MetadataOnly, "KbSearch should keep internal rules metadata-only");
AssertTrue(internalRulesSearchResult.DisclosureReason.Contains("Prod 권한통제", StringComparison.Ordinal), "KbSearch should route internal source text to Prod-controlled KB");
AssertTrue(internalRulesSearchResponse.Findings.Any(finding => finding.Code == "KB_PROD_ONLY_METADATA"), "KbSearch should emit structured metadata-only finding for internal rules");
var fixedKbSearch = new KbSearch(regulationCatalog, clock: new FixedClock(new DateOnly(2026, 6, 21)));
var citationSearchResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "2026-06-20");
var citationText = citationSearchResponse.DraftAnswer;
AssertTrue(citationText.Contains(publicRegEntry.Title, StringComparison.Ordinal), "KbSearch citation answer should include document name");
AssertTrue(citationText.Contains("버전", StringComparison.Ordinal) && citationText.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render placeholder version as confirmation-needed");
AssertTrue(citationText.Contains("시행일", StringComparison.Ordinal) && citationText.Contains("(확인 필요)", StringComparison.Ordinal), "KbSearch citation answer should render placeholder effective date as confirmation-needed");
AssertTrue(!citationText.Contains("CONFIRM_CURRENT_VERSION", StringComparison.Ordinal), "KbSearch citation answer should not cite placeholder version as real metadata");
AssertTrue(!citationText.Contains("CONFIRM_EFFECTIVE_DATE", StringComparison.Ordinal), "KbSearch citation answer should not cite placeholder effective date as real metadata");
AssertTrue(citationText.Contains("조항:", StringComparison.Ordinal), "KbSearch citation answer should include clause label");
AssertTrue(citationText.Contains(publicRegEntry.Source, StringComparison.Ordinal), "KbSearch citation answer should include source locator");
AssertTrue(citationText.Contains("검색 기준일: 2026-06-20", StringComparison.Ordinal), "KbSearch citation answer should include caller as-of date");
AssertTrue(citationText.Contains("검토 필요", StringComparison.Ordinal), "KbSearch citation answer should include review-needed wording");
AssertTrue(citationSearchResponse.Results.Any(result => result.SourceId == publicRegEntry.SourceId && result.Version == publicRegEntry.Version && result.Source == publicRegEntry.Source), "KbSearchResult should expose citation metadata");
AssertTrue(citationSearchResponse.Warnings.Any(warning => warning.Contains("source_id=FIA_REG", StringComparison.Ordinal) && warning.Contains("version", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for placeholder version metadata");
AssertTrue(citationSearchResponse.Warnings.Any(warning => warning.Contains("source_id=FIA_REG", StringComparison.Ordinal) && warning.Contains("effective_date", StringComparison.OrdinalIgnoreCase)), "KbSearch citation answer should warn for placeholder effective date metadata");
var repeatedCitationAnswer = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "2026-06-20").DraftAnswer;
AssertTrue(citationText == repeatedCitationAnswer, "KbSearch citation answer should be deterministic for the same as-of date");
var clockDateResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke");
AssertTrue(clockDateResponse.DraftAnswer.Contains("검색 기준일: 2026-06-21", StringComparison.Ordinal), "KbSearch citation answer should use injected clock date when asOfDate is omitted");
AssertTrue(!clockDateResponse.DraftAnswer.Contains("검색 기준일: (미기재)", StringComparison.Ordinal), "KbSearch citation answer should not use placeholder for search date");
var invalidAsOfDateResponse = fixedKbSearch.Search("금융투자업규정", "user-smoke", asOfDate: "not-a-date");
AssertTrue(invalidAsOfDateResponse.DraftAnswer.Contains("검색 기준일: 2026-06-21", StringComparison.Ordinal), "KbSearch citation answer should fallback to injected clock date for invalid asOfDate");
AssertTrue(!invalidAsOfDateResponse.DraftAnswer.Contains("검색 기준일: not-a-date", StringComparison.Ordinal), "KbSearch citation answer should not cite invalid asOfDate text");
AssertTrue(invalidAsOfDateResponse.Warnings.Any(warning => warning.Contains("yyyy-MM-dd", StringComparison.Ordinal)), "KbSearch citation answer should warn on invalid asOfDate");
var emptyMetadataSearch = new KbSearch(emptyMetadataCatalog, clock: new FixedClock(new DateOnly(2026, 6, 21)));
var emptyMetadataSearchResponse = emptyMetadataSearch.Search("Empty Metadata", "user-smoke", asOfDate: "2026-06-20");
AssertTrue(emptyMetadataSearchResponse.DraftAnswer.Contains("(미기재)", StringComparison.Ordinal), "KbSearch citation answer should render empty metadata as missing gracefully");
AssertTrue(!emptyMetadataSearchResponse.DraftAnswer.Contains("검색 기준일: (미기재)", StringComparison.Ordinal), "KbSearch citation answer should never render search date as missing");
AssertTrue(emptyMetadataSearchResponse.Findings.Any(finding => finding.Code == "KB_LICENSE_MISSING" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured finding for missing license metadata");
AssertTrue(emptyMetadataSearchResponse.Findings.Any(finding => finding.Code == "KB_APPROVAL_MISSING" && finding.Severity == SafetySeverity.Medium), "KbSearch should emit structured finding for missing approval metadata");
var unknownStatusCatalogPath = Path.Combine(Path.GetTempPath(), $"unknown_status_catalog_{Guid.NewGuid():N}.csv");
File.WriteAllText(
    unknownStatusCatalogPath,
    "source_id,category,title,source_org,source_type,status,note,source,version,effective_date,repeal_date,file_hash,loaded_date,approval_status,superseded_by,license_status\nUNKNOWN_STATUS,PUBLIC_REG,Mystery Status,Public Org,Public regulation,MYSTERY_STATUS,unknown status metadata,https://example.invalid,v1,2026-01-01,,hash,2026-06-21,PUBLIC_CATALOG_METADATA,,PUBLIC_REFERENCE\n",
    Encoding.UTF8);
var unknownStatusSearchResponse = new KbSearch(RegulationCatalog.LoadFromFile(unknownStatusCatalogPath)).Search("Mystery Status", "user-smoke");
var unknownStatusResult = unknownStatusSearchResponse.Results.Single(result => result.SourceId == "UNKNOWN_STATUS");
AssertTrue(unknownStatusResult.Disclosure == KbDisclosure.MetadataOnly, "KbSearch should conservatively mark unknown status as metadata-only");
AssertTrue(unknownStatusSearchResponse.Findings.Any(finding => finding.Code == "KB_UNKNOWN_STATUS" && finding.Severity == SafetySeverity.High), "KbSearch should emit structured finding for unknown status");
var repoGuardFindings = KbRepositoryGuard.Scan(Directory.GetCurrentDirectory());
AssertTrue(!repoGuardFindings.Any(finding => finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should not find internal/NCR original text in repo assets");
var suspiciousKbRoot = Path.Combine(Path.GetTempPath(), $"kb_guard_{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(suspiciousKbRoot, "kb"));
File.WriteAllText(Path.Combine(suspiciousKbRoot, "kb", "internal_rule_original.txt"), "내부규정 원문", Encoding.UTF8);
var suspiciousFindings = KbRepositoryGuard.Scan(suspiciousKbRoot);
AssertTrue(suspiciousFindings.Any(finding => finding.Code == "KB_FORBIDDEN_SOURCE_TEXT" && finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should block suspicious internal/NCR original files");
var suspiciousNcrRoot = Path.Combine(Path.GetTempPath(), $"ncr_guard_{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(suspiciousNcrRoot, "config", "ncr"));
File.WriteAllText(Path.Combine(suspiciousNcrRoot, "config", "ncr", "ncr_official_original.json"), "official text", Encoding.UTF8);
var suspiciousNcrFindings = KbRepositoryGuard.Scan(suspiciousNcrRoot);
AssertTrue(suspiciousNcrFindings.Any(finding => finding.Code == "KB_FORBIDDEN_SOURCE_TEXT" && finding.Severity == SafetySeverity.Blocker), "KbRepositoryGuard should scan config/ncr and block suspicious NCR originals");
var build03ScriptText = File.ReadAllText(Path.Combine("build", "03_verify-package.ps1"));
foreach (var token in PrivateGuardStrings("SuspiciousContentTokens"))
{
    if (token.All(ch => ch <= 0x7F))
    {
        AssertTrue(build03ScriptText.Contains(token, StringComparison.Ordinal), $"build/03 source-text scan should mirror ASCII content token '{token}'");
    }
    else
    {
        foreach (var codeUnit in token.Select(ch => $"0x{(int)ch:X4}").Distinct())
        {
            AssertTrue(build03ScriptText.Contains(codeUnit, StringComparison.OrdinalIgnoreCase), $"build/03 source-text scan should mirror non-ASCII content token '{token}' via code unit {codeUnit}");
        }
    }
}

foreach (var token in PrivateGuardStrings("SuspiciousNameTokens"))
{
    AssertTrue(build03ScriptText.Contains(token, StringComparison.Ordinal), $"build/03 source-text scan should mirror filename token '{token}'");
}

foreach (var allowlistPath in PrivateGuardStrings("MetadataAllowlist"))
{
    AssertTrue(build03ScriptText.Contains(allowlistPath, StringComparison.Ordinal), $"build/03 source-text scan should mirror allowlist path '{allowlistPath}'");
}

foreach (var extension in new[] { ".csv", ".json", ".jsonl", ".md", ".txt", ".sql" })
{
    AssertTrue(build03ScriptText.Contains(extension, StringComparison.Ordinal), $"build/03 source-text scan should include text extension '{extension}'");
}

foreach (var scanDirectory in new[] { "kb", "config", "samples", "data_sources" })
{
    AssertTrue(build03ScriptText.Contains(scanDirectory, StringComparison.Ordinal), $"build/03 source-text scan should include scan directory '{scanDirectory}'");
}

AssertTrue(build03ScriptText.Contains("Expand-Archive", StringComparison.Ordinal), "build/03 source-text scan should inspect extracted ZIP contents");
AssertTrue(build03ScriptText.Contains("New-StringFromCodeUnits", StringComparison.Ordinal), "build/03 source-text scan should avoid BOM-dependent non-ASCII PowerShell literals");
AssertTrue(build03ScriptText.Contains("Get-ZipRelativePath", StringComparison.Ordinal), "build/03 source-text scan should use a Windows PowerShell compatible relative path helper");
AssertTrue(!build03ScriptText.Contains("GetRelativePath", StringComparison.Ordinal), "build/03 source-text scan should not use .NET Core-only Path.GetRelativePath");
AssertTrue(build03ScriptText.Contains("GetEncoding(949)", StringComparison.Ordinal), "build/03 source-text scan should attempt CP949 decoding independently");
AssertTrue(build03ScriptText.Contains("PACKAGE SOURCE-TEXT VERIFICATION FAILED", StringComparison.Ordinal), "build/03 source-text scan should fail packaging on suspicious source text");

// STAB-WP-01: VERSION is the single source of truth; build scripts must read it and fail on mismatch (RR-11, ADR-006).
foreach (var buildScript in new[] { "01_publish-win-x64.ps1", "02_package-release.ps1", "03_verify-package.ps1" })
{
    var scriptText = File.ReadAllText(Path.Combine("build", buildScript));
    AssertTrue(!scriptText.Contains("0.2.0", StringComparison.Ordinal), $"build/{buildScript} should not hardcode default version 0.2.0 (VERSION is single source)");
    AssertTrue(scriptText.Contains("VERSION file", StringComparison.Ordinal), $"build/{buildScript} should resolve version from the VERSION file");
    AssertTrue(scriptText.Contains("does not match VERSION file", StringComparison.Ordinal), $"build/{buildScript} should fail when -Version mismatches the VERSION file");
}
AssertTrue(File.ReadAllText("VERSION").Trim() == "0.6.0", "VERSION file should be the single source of truth at 0.6.0");
AssertTrue(File.Exists("global.json") && File.ReadAllText("global.json").Contains("8.0", StringComparison.Ordinal), "global.json should pin the .NET 8 SDK band (ADR-005/006)");

var ncrRuleSetLoadResult = NcrRuleSetLoader.LoadDefault();
AssertTrue(!ncrRuleSetLoadResult.UsedFallback, "NcrRuleSetLoader should load repo sample structure");
AssertTrue(ncrRuleSetLoadResult.RuleSet.Components.Count > 0, "NcrRuleSet should include Components");
AssertTrue(ncrRuleSetLoadResult.RuleSet.ComponentMap.Count > 0, "NcrRuleSet should include ComponentMap");
AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RuleSetId), "NcrRuleSet should include RuleSetId");
AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RuleSetVersion), "NcrRuleSet should include RuleSetVersion");
AssertTrue(DateOnly.TryParseExact(ncrRuleSetLoadResult.RuleSet.EffectiveDate, "yyyy-MM-dd", out _), "NcrRuleSet should include YYYY-MM-DD EffectiveDate");
AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.FormulaDescription), "NcrRuleSet should include FormulaDescription");
AssertTrue(ncrRuleSetLoadResult.RuleSet.ValidationSqlTemplates.Count > 0, "NcrRuleSet should include Validation SQL templates");
AssertTrue(!string.IsNullOrWhiteSpace(ncrRuleSetLoadResult.RuleSet.RegulationBasis), "NcrRuleSet should include RegulationBasis");
AssertTrue(ncrRuleSetLoadResult.RuleSet.ApprovalHistory.Count > 0, "NcrRuleSet should include ApprovalHistory");
AssertTrue(!ncrRuleSetLoadResult.Findings.Any(finding => finding.Code.StartsWith("SQL_", StringComparison.Ordinal)), "NcrRuleSet sample validation SQL should be read-only");
AssertTrue(ncrRuleSetLoadResult.RuleSet.Components.All(component => component.ValuePolicy.Contains("APPROVAL_REQUIRED", StringComparison.Ordinal)), "NcrRuleSet sample should not contain real NCR coefficients");

var ncrExplanation = NcrExplain.Build(ncrRuleSetLoadResult.RuleSet);
AssertTrue(ncrExplanation.Contains("검토용 초안", StringComparison.Ordinal), "NcrExplain should always mark answers as review drafts");
AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.RuleSetVersion, StringComparison.Ordinal), "NcrExplain should include RuleSetVersion");
AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.EffectiveDate, StringComparison.Ordinal), "NcrExplain should include EffectiveDate");
AssertTrue(ncrExplanation.Contains("Component Map", StringComparison.Ordinal), "NcrExplain should include ComponentMap");
AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.FormulaDescription, StringComparison.Ordinal), "NcrExplain should include FormulaDescription");
AssertTrue(ncrExplanation.Contains(ncrRuleSetLoadResult.RuleSet.RegulationBasis, StringComparison.Ordinal), "NcrExplain should include RegulationBasis");

var missingNcrRuleSetResult = NcrRuleSetLoader.LoadFromFile("config/ncr/missing_smoke_ruleset.json");
AssertTrue(missingNcrRuleSetResult.UsedFallback && missingNcrRuleSetResult.Findings.Any(finding => finding.Code == "NCR_RULESET_MISSING"), "NcrRuleSetLoader should safe-fallback on missing files");
var rejectedNcrPathResult = NcrRuleSetLoader.LoadFromFile("../ncr_ruleset.json");
AssertTrue(rejectedNcrPathResult.UsedFallback && rejectedNcrPathResult.Findings.Any(finding => finding.Code == "NCR_RULESET_PATH_REJECTED"), "NcrRuleSetLoader should reject paths outside config/ncr");

var invalidNcrRelativePath = $"config/ncr/invalid_{Guid.NewGuid():N}.json";
Directory.CreateDirectory(Path.Combine("config", "ncr"));
File.WriteAllText(invalidNcrRelativePath, "{ broken json", Encoding.UTF8);
var invalidNcrResult = NcrRuleSetLoader.LoadFromFile(invalidNcrRelativePath);
AssertTrue(invalidNcrResult.UsedFallback && invalidNcrResult.Findings.Any(finding => finding.Code == "NCR_RULESET_LOAD_FAILED"), "NcrRuleSetLoader should safe-fallback on invalid JSON");
File.Delete(invalidNcrRelativePath);

var blockedSqlNcrRelativePath = $"config/ncr/blocked_sql_{Guid.NewGuid():N}.json";
File.WriteAllText(
    blockedSqlNcrRelativePath,
    """
{
  "RuleSetId": "BAD_NCR_RULESET",
  "RuleSetVersion": "bad-001",
  "EffectiveDate": "2026-06-21",
  "Components": [
    {
      "ComponentId": "BAD_COMPONENT",
      "Name": "Blocked component",
      "Category": "PLACEHOLDER",
      "ValuePolicy": "APPROVAL_REQUIRED_NO_REAL_COEFFICIENT"
    }
  ],
  "ComponentMap": [
    {
      "ComponentId": "BAD_COMPONENT",
      "SourceName": "APPROVED_SOURCE",
      "ColumnName": "APPROVED_COLUMN",
      "DataType": "DECIMAL",
      "Required": true
    }
  ],
  "FormulaDescription": "Structure-only description.",
  "ValidationSqlTemplates": [
    "DELETE FROM ncr_result WHERE base_dt = @BASE_DT"
  ],
  "RegulationBasis": "APPROVED_BASIS_REQUIRED",
  "ApprovalHistory": [
    {
      "Status": "TEST_ONLY",
      "ReviewerRole": "TEST",
      "ApprovedAt": "2026-06-21T00:00:00Z",
      "Note": "Test only."
    }
  ]
}
""",
    Encoding.UTF8);
var blockedSqlNcrResult = NcrRuleSetLoader.LoadFromFile(blockedSqlNcrRelativePath);
AssertTrue(blockedSqlNcrResult.UsedFallback && blockedSqlNcrResult.Findings.Any(finding => finding.Code == "SQL_DML_DELETE"), "NcrRuleSetLoader should flag blocked DML in validation SQL templates");
File.Delete(blockedSqlNcrRelativePath);
var kbIndexA = KbIndex.Build(regulationCatalog.Entries);
var kbIndexB = KbIndex.Build(regulationCatalog.Entries);
AssertTrue(kbIndexA.IndexedTermCount > regulationCatalog.Entries.Count, "KbIndex should build searchable inverted terms");
AssertTrue(kbIndexA.DeterministicSignature() == kbIndexB.DeterministicSignature(), "KbIndex build should be deterministic for the same catalog");
AssertTrue(kbIndexA.FindCandidates("투자업").Any(entry => entry.SourceId == "FIA_REG"), "KbIndex should preserve Korean substring candidates");
var longKbText = string.Concat(Enumerable.Range(0, 5000).Select(index => (char)('\uAC00' + index)));
var longKbEntry = publicRegEntry with
{
    SourceId = "LONG_NOTE",
    Note = longKbText
};
var longKbIndex = KbIndex.Build([longKbEntry]);
AssertTrue(longKbIndex.PostingCount < longKbText.Length * 40, "KbIndex should cap substring key generation for long catalog fields");
AssertTrue(longKbIndex.FindCandidates(longKbText.Substring(200, 12)).Any(entry => entry.SourceId == "LONG_NOTE"), "KbIndex bounded substrings should preserve long substring candidates");
var longQuery = new string('가', 5000);
AssertTrue(kbIndexA.FindCandidates(longQuery).Count == regulationCatalog.Entries.Count, "KbIndex should use full-catalog fallback for queries longer than the substring cap");
AssertTrue(
    ExpectedKbLinearResults(regulationCatalog, longQuery).SequenceEqual(KbSearchSignature(kbSearch.Search(longQuery, "user-smoke"))),
    "KbSearch long-query fallback should preserve linear scoring without unbounded substring expansion");
foreach (var query in new[]
         {
             "NCR",
             "금융투자업규정",
             "투자업",
             "Public regulation",
             "CATALOG_ONLY",
             "원문 금지",
             "법률 국가",
             "없는검색어",
             string.Empty
         })
{
    var expected = ExpectedKbLinearResults(regulationCatalog, query);
    var actual = KbSearchSignature(kbSearch.Search(query, "user-smoke"));
    AssertTrue(expected.SequenceEqual(actual), $"KbSearch indexed results should match linear scoring for query '{query}'");
}

var emptyKbSearchResponse = kbSearch.Search("없는검색어", "user-smoke");
AssertTrue(emptyKbSearchResponse.Results.Count == 0, "KbSearch should return zero results for unmatched query");
AssertTrue(emptyKbSearchResponse.DraftAnswer.Contains("검토용 초안", StringComparison.Ordinal) && emptyKbSearchResponse.DraftAnswer.Contains("출처", StringComparison.Ordinal), "KbSearch no-result answer should still include review draft and source");
var kbSearchLogText = File.ReadAllText(kbSearchLogPath);
AssertTrue(!kbSearchLogText.Contains("NCR", StringComparison.Ordinal), "KbSearch audit should not store raw query text");
AssertTrue(!kbSearchLogText.Contains("user-smoke", StringComparison.Ordinal), "KbSearch audit should not store raw user id");

var approvedFeedback = new FeedbackLogEntry(
    "feedback-approved-001",
    "task-smoke-approved-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("reviewer-one"),
    "APPROVED",
    "ReviewerApproved");
var rejectedFeedback = approvedFeedback with
{
    FeedbackId = "feedback-rejected-001",
    TaskId = "task-smoke-rejected-001",
    FeedbackCode = "REJECTED",
    ReviewStatus = "ReviewerRejected"
};
var pendingFeedback = approvedFeedback with
{
    FeedbackId = "feedback-pending-001",
    TaskId = "task-smoke-pending-001",
    FeedbackCode = "PENDING",
    ReviewStatus = "ReviewerPending"
};
var promotionResult = new ExamplePromotion().PromoteApproved(
    [approvedFeedback, rejectedFeedback, pendingFeedback, approvedFeedback],
    new DateTime(2026, 06, 19, 0, 0, 0, DateTimeKind.Utc));
AssertTrue(promotionResult.PromotedExamples.Count == 1, "ExamplePromotion should promote only approved feedback once");
AssertTrue(promotionResult.PromotedExamples[0].PromotionMode == ExamplePromotion.PromotionModeName, "ExamplePromotion should use curation-only mode");
AssertTrue(promotionResult.PromotedExamples[0].UserIdHash == approvedFeedback.UserId, "ExamplePromotion should preserve hashed user id");
AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == rejectedFeedback.FeedbackId), "ExamplePromotion should skip rejected feedback");
AssertTrue(promotionResult.SkippedEntries.Any(entry => entry.FeedbackId == pendingFeedback.FeedbackId), "ExamplePromotion should skip pending feedback");
AssertTrue(promotionResult.Warnings.Any(warning => warning.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)), "ExamplePromotion should warn on duplicate approved feedback");

var promotedExampleStorePath = Path.Combine("config", "smoke_promoted_examples.jsonl");
if (File.Exists(promotedExampleStorePath))
{
    File.Delete(promotedExampleStorePath);
}

var promotedExampleStore = new PromotedExampleStore("config", "smoke_promoted_examples.jsonl");
promotedExampleStore.Append(promotionResult.PromotedExamples);
promotedExampleStore.Append(promotionResult.PromotedExamples);
var storedPromotedExamples = promotedExampleStore.ReadAll();
var promotedExampleStoreText = File.ReadAllText(promotedExampleStore.FilePath);
AssertTrue(storedPromotedExamples.Count == 2, "PromotedExampleStore should append promoted examples");
AssertTrue(storedPromotedExamples.All(example => example.UserIdHash == approvedFeedback.UserId), "PromotedExampleStore should persist hashed user ids");
AssertTrue(!promotedExampleStoreText.Contains("user-smoke", StringComparison.Ordinal), "PromotedExampleStore should not store raw user id");
AssertTrue(Throws<ArgumentException>(() => promotedExampleStore.Append([promotionResult.PromotedExamples[0] with { UserIdHash = "plain-user" }])), "PromotedExampleStore should reject non-hash UserIdHash");
AssertTrue(Throws<ArgumentException>(() => new PromotedExampleStore("config/../logs", "bad.jsonl")), "PromotedExampleStore should reject paths outside config");
File.Delete(promotedExampleStore.FilePath);

var uiIntegrationLogPath = Path.Combine("logs", "smoke_ui_integration_log.jsonl");
if (File.Exists(uiIntegrationLogPath))
{
    File.Delete(uiIntegrationLogPath);
}

var uiIntegrationLogWriter = new TaskLogWriter("logs", "smoke_ui_integration_log.jsonl");
var uiDraftPipeline = new DraftPipeline(new NoModelDraftService(policyLoadResult.Policy), loadedRuleSet, uiIntegrationLogWriter);
var uiDraftResult = uiDraftPipeline.Generate(new DraftPipelineRequest(DraftRequestKind.Sql, "ui draft smoke", "user-smoke"));
var uiKbSearch = new KbSearch(regulationCatalog, uiIntegrationLogWriter, loadedRuleSet.RuleVersion);
var uiKbResponse = uiKbSearch.Search("NCR", "user-smoke");
var uiPromotionResult = new ExamplePromotion().PromoteApproved([approvedFeedback]);
AssertTrue(uiDraftResult.SafetyResult == "NO_MODEL" && uiDraftResult.AuditLogWritten, "UI integration smoke should run NoModel draft pipeline with audit");
AssertTrue(uiKbResponse.Results.Count > 0 && uiKbResponse.AuditLogWritten, "UI integration smoke should run catalog search with audit");
AssertTrue(uiPromotionResult.PromotedExamples.Count == 1, "UI integration smoke should run feedback promotion");
var uiIntegrationLogText = File.ReadAllText(uiIntegrationLogPath);
AssertTrue(!uiIntegrationLogText.Contains("ui draft smoke", StringComparison.Ordinal) && !uiIntegrationLogText.Contains("user-smoke", StringComparison.Ordinal), "UI integration audit should not store raw prompt or user id");

XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
var mainWindowXaml = XDocument.Load(Path.Combine("src", "RiskManagementAI.App", "MainWindow.xaml"));
var mainWindowCode = File.ReadAllText(Path.Combine("src", "RiskManagementAI.App", "MainWindow.xaml.cs"));
var expectedMenuButtons = new[]
{
    "Dashboard",
    "SQL Assistant",
    "VBA Assistant",
    "Data Analyzer",
    "Risk Dashboard",
    "Excel Report",
    "Regulation / NCR",
    "Feedback Center",
    "History",
    "Settings"
};
var menuButtonClicks = mainWindowXaml
    .Descendants(wpf + "Button")
    .Where(button => expectedMenuButtons.Contains((string?)button.Attribute("Content") ?? string.Empty))
    .ToDictionary(
        button => (string)button.Attribute("Content")!,
        button => (string?)button.Attribute("Click") ?? string.Empty);
AssertTrue(expectedMenuButtons.All(menuButtonClicks.ContainsKey), "UI shell should include all left menu buttons");
AssertTrue(menuButtonClicks.Values.All(click => !string.IsNullOrWhiteSpace(click)), "Left menu buttons should be wired to click handlers");

var expectedMenuHandlers = new Dictionary<string, string>
{
    ["Dashboard"] = "OnShowDashboard",
    ["SQL Assistant"] = "OnNavigateSql",
    ["VBA Assistant"] = "OnNavigateVba",
    ["Data Analyzer"] = "OnNavigateData",
    ["Risk Dashboard"] = "OnNavigateRiskDashboard",
    ["Excel Report"] = "OnNavigateReport",
    ["Regulation / NCR"] = "OnNavigateRegulation",
    ["Feedback Center"] = "OnNavigateFeedback",
    ["History"] = "OnShowHistory",
    ["Settings"] = "OnShowSettings"
};

foreach (var (label, handler) in expectedMenuHandlers)
{
    AssertTrue(menuButtonClicks.TryGetValue(label, out var actualHandler) && actualHandler == handler, $"Left menu {label} should use {handler}");
}

var expectedTabNames = new Dictionary<string, string>
{
    ["Dashboard"] = "DashboardTab",
    ["SQL"] = "SqlTab",
    ["Draft"] = "DraftTab",
    ["VBA"] = "VbaTab",
    ["Excel"] = "ExcelTab",
    ["Data"] = "DataTab",
    ["Risk"] = "RiskDashboardTab",
    ["Report"] = "ReportTab",
    ["Regulation"] = "RegulationTab",
    ["Feedback"] = "FeedbackTab",
    ["History"] = "HistoryTab",
    ["Settings"] = "SettingsTab"
};
var tabNamesByHeader = mainWindowXaml
    .Descendants(wpf + "TabItem")
    .ToDictionary(
        tab => (string?)tab.Attribute("Header") ?? string.Empty,
        tab => (string?)tab.Attribute(xaml + "Name") ?? string.Empty);
AssertTrue(tabNamesByHeader.Count == expectedTabNames.Count && tabNamesByHeader.Values.All(name => !string.IsNullOrWhiteSpace(name)), "Main tabs should all have stable x:Name values");

foreach (var (header, tabName) in expectedTabNames)
{
    AssertTrue(tabNamesByHeader.TryGetValue(header, out var actualTabName) && actualTabName == tabName, $"Main tab {header} should be named {tabName}");
}

AssertTrue(!mainWindowCode.Contains("MainTabs.SelectedIndex", StringComparison.Ordinal), "UI navigation should not depend on TabControl indexes");
AssertTrue(mainWindowCode.Contains("MainTabs.SelectedItem = tab;", StringComparison.Ordinal), "UI navigation should select stable TabItem instances");
AssertTrue(!mainWindowCode.Contains("1.1m", StringComparison.Ordinal), "WP-01 should remove synthetic 1.1x limit formula from UI code");
AssertTrue(!mainWindowCode.Contains("PROFILE_TOTAL", StringComparison.Ordinal), "WP-01 should not emit aggregate synthetic limit rows");
AssertTrue(mainWindowCode.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 should emit LIMIT_DATA_REQUIRED when real limit data is missing");
AssertTrue(mainWindowCode.Contains("DEMO_ONLY", StringComparison.Ordinal), "WP-01 should mark sample/demo report flows as DEMO_ONLY");
AssertTrue(!mainWindowCode.Contains("BuildUiLimitRows", StringComparison.Ordinal), "WP-07 should remove BuildUiLimitRows from UI code");
AssertTrue(!mainWindowCode.Contains("ExcelReportLimitRow", StringComparison.Ordinal), "WP-07 should remove ExcelReportLimitRow from UI code");
var retiredStubCodes = new[]
{
    "DASHBOARD_MVP_STATUS",
    "RISK_DASHBOARD_MVP_STATUS",
    "HISTORY_NOT_IMPLEMENTED",
    "SETTINGS_NOT_IMPLEMENTED"
};
AssertTrue(retiredStubCodes.All(code => !mainWindowCode.Contains(code, StringComparison.Ordinal)), "MVP-3 target screens should not use retired stub findings");
AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "한도 점검", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRunLimitMonitor", StringComparison.Ordinal)),
    "Risk Dashboard should expose a limit monitoring action");
AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "로그 새로고침", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRefreshHistory", StringComparison.Ordinal)),
    "History should expose a read-only refresh action");
AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "설정 새로고침", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRefreshSettings", StringComparison.Ordinal)),
    "Settings should expose a view-only refresh action");
AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "승인 승격", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnPromoteFeedbackExample", StringComparison.Ordinal)),
    "Feedback Center should expose an approval promotion action");
AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "상태 새로고침", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRefreshDashboard", StringComparison.Ordinal)),
    "Dashboard should expose a read-only status refresh action");

var expectedTabKeyMappings = new Dictionary<string, string>
{
    ["Dashboard"] = "DashboardTab",
    ["Sql"] = "SqlTab",
    ["Draft"] = "DraftTab",
    ["Vba"] = "VbaTab",
    ["Excel"] = "ExcelTab",
    ["Data"] = "DataTab",
    ["RiskDashboard"] = "RiskDashboardTab",
    ["Report"] = "ReportTab",
    ["Regulation"] = "RegulationTab",
    ["Feedback"] = "FeedbackTab",
    ["History"] = "HistoryTab",
    ["Settings"] = "SettingsTab"
};

foreach (var (tabKey, tabName) in expectedTabKeyMappings)
{
    AssertTrue(mainWindowCode.Contains($"[MainTabKey.{tabKey}] = {tabName}", StringComparison.Ordinal), $"MainTabKey.{tabKey} should map to {tabName}");
}

var expectedNavigationTargets = new Dictionary<string, string>
{
    ["OnShowDashboard"] = "MainTabKey.Dashboard",
    ["OnNavigateSql"] = "MainTabKey.Sql",
    ["OnNavigateVba"] = "MainTabKey.Vba",
    ["OnNavigateData"] = "MainTabKey.Data",
    ["OnNavigateRiskDashboard"] = "MainTabKey.RiskDashboard",
    ["OnNavigateReport"] = "MainTabKey.Report",
    ["OnNavigateRegulation"] = "MainTabKey.Regulation",
    ["OnNavigateFeedback"] = "MainTabKey.Feedback",
    ["OnShowHistory"] = "MainTabKey.History",
    ["OnShowSettings"] = "MainTabKey.Settings"
};

foreach (var (handler, tabKey) in expectedNavigationTargets)
{
    var marker = $"private void {handler}";
    var start = mainWindowCode.IndexOf(marker, StringComparison.Ordinal);
    var end = start < 0
        ? -1
        : mainWindowCode.IndexOf("\n    private void ", start + marker.Length, StringComparison.Ordinal);
    var methodBody = start < 0
        ? string.Empty
        : mainWindowCode[start..(end < 0 ? mainWindowCode.Length : end)];
    AssertTrue(methodBody.Contains("SelectMainTab", StringComparison.Ordinal) && methodBody.Contains(tabKey, StringComparison.Ordinal), $"Left menu handler {handler} should select {tabKey}");
}

var profiler = new DataProfiler();
var exposureCsvPath = Path.Combine("samples", "dummy_data", "risk_exposure_sample.csv");
var limitCsvPath = Path.Combine("samples", "dummy_data", "risk_limit_sample.csv");
var exposureProfile = profiler.ProfileCsv(exposureCsvPath);
AssertTrue(exposureProfile.SourceName == "risk_exposure_sample.csv", "DataProfiler should preserve source file name");
AssertTrue(exposureProfile.RowCount == 6, "Risk exposure sample should have 6 data rows");
AssertTrue(exposureProfile.ColumnCount == 10, "Risk exposure sample should have 10 columns");
AssertTrue(exposureProfile.NullCounts.Values.All(count => count == 0), "Risk exposure sample should have zero nulls");
AssertTrue(exposureProfile.DuplicateRowCount == 0, "Risk exposure sample should have zero duplicate rows");
AssertTrue(exposureProfile.BaseDateDistribution["20260617"] == 5 && exposureProfile.BaseDateDistribution["20260616"] == 1, "Risk exposure sample should summarize BASE_DT distribution");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Sum == 3830000000m, "Risk exposure sample should compute numeric sum");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Min == -420000000m, "Risk exposure sample should compute numeric min");
AssertTrue(exposureProfile.NumericColumns["EXPOSURE_AMT"].Max == 1250000000m, "Risk exposure sample should compute numeric max");

var limitMonitor = new LimitMonitor();
var limitMonitorResult = limitMonitor.Analyze(exposureCsvPath, limitCsvPath, "20260617");
AssertTrue(limitMonitorResult.Rows.Count == 5, "LimitMonitor should filter exposure rows by BASE_DT");
AssertTrue(limitMonitorResult.Rows.All(row => row.BaseDate == "20260617"), "LimitMonitor should not match prior BASE_DT exposure rows to current limits");
AssertTrue(limitMonitorResult.NormalCount == 2 && limitMonitorResult.WarningCount == 2 && limitMonitorResult.BreachCount == 1, "LimitMonitor should classify NORMAL/WARNING/BREACH counts");
var warningEqRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_EQ_002");
AssertTrue(warningEqRow.Status == LimitMonitorStatus.Warning && warningEqRow.UsageRatio > 0.94m && warningEqRow.UsageRatio < 0.95m, "LimitMonitor should classify PF_EQ_002 as WARNING around 94%");
var breachCreditRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_CR_001");
AssertTrue(breachCreditRow.Status == LimitMonitorStatus.Breach && breachCreditRow.RemainingLimit < 0m, "LimitMonitor should classify PF_CR_001 as BREACH");
var shortSideRow = limitMonitorResult.Rows.Single(row => row.PortfolioId == "PF_FI_001");
AssertTrue(shortSideRow.ExposureAmount < 0m && shortSideRow.UsageRatio == 0.84m && shortSideRow.Status == LimitMonitorStatus.Normal, "LimitMonitor should use ABS exposure for short-side rows");
AssertTrue(limitMonitorResult.Kpis.TotalCount == 5 && limitMonitorResult.Metadata.IsDeterministic, "LimitMonitor should return shared deterministic LimitAnalysisResult KPIs");
AssertTrue(!limitMonitorResult.Metadata.ColumnMappingUsedFallback && limitMonitorResult.Metadata.ExposureSourceName == "risk_exposure_sample.csv", "LimitAnalysisResult metadata should include source and mapping state");
AssertTrue(limitMonitorResult.Findings.Any(f => f.Code == "LIMIT_BREACH_DETECTED" && f.Severity == SafetySeverity.High), "LimitMonitor should emit a high finding when breaches exist");
AssertTrue(limitMonitorResult.Reconciliation.Passed, "LimitMonitor sample reconciliation should pass without fail-code exceptions");
AssertTrue(limitMonitorResult.Reconciliation.CheckCount == 9, "LimitMonitor should expose nine WP-06 reconciliation checks");
AssertTrue(ReconciliationExceptionCount(limitMonitorResult, "RECON_BASEDATE_MISMATCH") == 0, "LimitMonitor should not flag normal multi-date exports when requested BASE_DT exists");
AssertTrue(
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
AssertTrue(
    sixStateResult.Kpis is { NormalCount: 1, WarningCount: 1, BreachCount: 1, NoLimitCount: 1, InvalidLimitCount: 2, MappingErrorCount: 0 },
    "LimitMonitor should classify NORMAL/WARNING/BREACH/NO_LIMIT/INVALID_LIMIT states");
AssertTrue(sixStateResult.Rows.Single(row => row.PortfolioId == "PF_NOLIMIT").StatusCode == "NO_LIMIT", "LimitMonitor should expose NO_LIMIT output string for unmatched joins");
AssertTrue(sixStateResult.ExceptionList.Count(exception => exception.Code == "INVALID_LIMIT") == 2, "LimitMonitor should split inactive or zero limits into INVALID_LIMIT exceptions");
AssertTrue(sixStateResult.Findings.Any(finding => finding.Code == "LIMIT_NO_LIMIT_DETECTED"), "LimitMonitor should emit finding when real limit row is absent");
AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_EXPOSURE_NO_LIMIT") == 1, "WP-06 should flag exposure rows without matching limits");
AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_NONPOSITIVE_LIMIT") == 1, "WP-06 should flag non-positive limit rows");
AssertTrue(ReconciliationExceptionCount(sixStateResult, "RECON_SUM_BALANCE") == 0, "WP-06 should preserve source-vs-analysis exposure balance for valid six-state inputs");
AssertTrue(!sixStateResult.Reconciliation.Passed, "WP-06 reconciliation should fail when a fail-code exception exists");
var repeatedSixStateResult = limitMonitor.Analyze(wp05ExposureCsv, wp05LimitCsv, "20260617");
AssertTrue(repeatedSixStateResult.Kpis == sixStateResult.Kpis, "LimitAnalysisResult KPIs should be deterministic for repeated inputs");
AssertTrue(repeatedSixStateResult.Rows.Select(row => row.StatusCode).SequenceEqual(sixStateResult.Rows.Select(row => row.StatusCode)), "LimitAnalysisResult monitoring rows should be deterministic for repeated inputs");
AssertTrue(ReconciliationSignature(repeatedSixStateResult) == ReconciliationSignature(sixStateResult), "WP-06 reconciliation summary should be deterministic for repeated inputs");
var coreTableResult = limitMonitor.Analyze(CsvReader.Read(wp05ExposureCsv), CsvReader.Read(wp05LimitCsv), "20260617");
AssertTrue(coreTableResult.Kpis == sixStateResult.Kpis, "LimitMonitor CsvTable core interface should match path overload results");

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
AssertTrue(cleanReconciliationResult.ExceptionList.All(exception => !exception.Code.StartsWith("RECON_", StringComparison.Ordinal)), "WP-06 clean inputs should not emit reconciliation exceptions");
AssertTrue(cleanReconciliationResult.Reconciliation.Passed, "WP-06 clean inputs should pass reconciliation");

var wp06OrphanLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_orphan_limit.csv");
WriteCsvRows(
    wp06OrphanLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "100", "Y" },
        new[] { "20260617", "PF_ORPHAN", "RF_ORPHAN", "100", "Y" }
    ]);
var orphanLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06OrphanLimitCsv, "20260617");
AssertTrue(ReconciliationExceptionCount(orphanLimitResult, "RECON_LIMIT_NO_EXPOSURE") == 1, "WP-06 should flag orphan limit rows");
AssertTrue(orphanLimitResult.Reconciliation.Passed, "WP-06 orphan limits should not fail reconciliation unless a fail-code exists");

var wp06DuplicateLimitCsv = Path.Combine(limitSmokeDirectory, "wp06_duplicate_limit.csv");
WriteCsvRows(
    wp06DuplicateLimitCsv,
    [
        new[] { "BASE_DT", "PORTFOLIO_ID", "RISK_FACTOR", "LIMIT_AMT", "USE_YN" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "100", "Y" },
        new[] { "20260617", "PF_CLEAN", "RF_CLEAN", "120", "Y" }
    ]);
var duplicateLimitResult = limitMonitor.Analyze(wp06CleanExposureCsv, wp06DuplicateLimitCsv, "20260617");
AssertTrue(ReconciliationExceptionCount(duplicateLimitResult, "RECON_DUPLICATE_LIMIT") == 1, "WP-06 should flag duplicate limit join keys");
AssertTrue(ReconciliationExceptionCount(duplicateLimitResult, "RECON_ROW_AMPLIFICATION") == 1, "WP-06 should flag duplicate-limit row amplification risk");
AssertTrue(duplicateLimitResult.Rows.Count == 1 && duplicateLimitResult.Kpis.TotalCount == 1, "WP-06 duplicate limit checks should not change existing monitoring row counts");
AssertTrue(!duplicateLimitResult.Reconciliation.Passed, "WP-06 row amplification should fail reconciliation");

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
AssertTrue(ReconciliationExceptionCount(baseDateMismatchResult, "RECON_BASEDATE_MISMATCH") >= 1, "WP-06 should flag requested BASE_DT missing when other dates exist");
AssertTrue(baseDateMismatchResult.Reconciliation.Passed, "WP-06 base-date mismatch should remain non-fail severity in R1");

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
AssertTrue(ReconciliationExceptionCount(sumBalanceResult, "RECON_SUM_BALANCE") == 1, "WP-06 should fail sum balance when source exposure amount is nonnumeric");
AssertTrue(!sumBalanceResult.Reconciliation.Passed, "WP-06 sum balance exceptions should fail reconciliation");
AssertTrue(sumBalanceResult.MappingErrorCount == 1, "WP-06 sum balance test should preserve WP-05 MappingError classification");

var wp05ExposureXlsx = Path.Combine(limitSmokeDirectory, "wp05_exposure.xlsx");
var wp05LimitXlsx = Path.Combine(limitSmokeDirectory, "wp05_limit.xlsx");
CreateSingleSheetXlsx(wp05ExposureXlsx, wp05ExposureRows);
CreateSingleSheetXlsx(wp05LimitXlsx, wp05LimitRows);
var xlsxLimitResult = limitMonitor.Analyze(wp05ExposureXlsx, wp05LimitXlsx, "20260617");
AssertTrue(xlsxLimitResult.Kpis == sixStateResult.Kpis, "LimitMonitor .xlsx path overload should match .csv path overload results");

var mappingErrorExposureCsv = Path.Combine(limitSmokeDirectory, "wp05_missing_amount.csv");
File.WriteAllText(
    mappingErrorExposureCsv,
    "BASE_DT,DESK_CD,PORTFOLIO_ID,PRODUCT_TYPE,RISK_FACTOR,CCY_CD\n20260617,EQD,PF_MAP,ELS,RF_MAP,KRW\n");
var mappingErrorResult = limitMonitor.Analyze(mappingErrorExposureCsv, wp05LimitCsv, "20260617");
AssertTrue(mappingErrorResult.MappingErrorCount == 1, "LimitMonitor should return graceful MAPPING_ERROR for missing mapped physical columns");
AssertTrue(mappingErrorResult.ExceptionList.Any(exception => exception.Code == "MAPPING_ERROR" && exception.Severity == SafetySeverity.High), "LimitMonitor should include high severity MappingError exception");
AssertTrue(mappingErrorResult.Findings.Any(finding => finding.Code == "LIMIT_MAPPING_ERROR"), "LimitMonitor should include MappingError finding instead of throwing");

var excelReportLogPath = Path.Combine("logs", "smoke_excel_report_log.jsonl");
var excelReportPath = Path.Combine("reports", "smoke_m2_04_report.xlsx");
if (File.Exists(excelReportLogPath))
{
    File.Delete(excelReportLogPath);
}

if (File.Exists(excelReportPath))
{
    File.Delete(excelReportPath);
}

var reportSql = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE BASE_DT = :BASE_DT";
var reportBuilder = new ExcelReportBuilder(
    loadedRuleSet,
    new TaskLogWriter("logs", "smoke_excel_report_log.jsonl"));
var reportValidationFindings = sqlChecker.Check(reportSql).ToList();
reportValidationFindings.Add(new SafetyFinding(
    "REPORT_VALIDATION_HIGH_SMOKE",
    SafetySeverity.High,
    "High validation smoke finding for EXCEPTION_LIST merge."));
var reportResult = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_m2_04_report",
    exposureProfile,
    sixStateResult,
    reportValidationFindings,
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
AssertTrue(File.Exists(reportResult.ReportPath), "ExcelReportBuilder should create xlsx report");
AssertTrue(
    Path.GetFullPath(reportResult.ReportPath).StartsWith(Path.GetFullPath("reports") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
    "ExcelReportBuilder should write only under reports");
AssertTrue(reportResult.SheetNames.SequenceEqual(ExcelReportBuilder.ExpectedSheetNames), "ExcelReportBuilder should create the required MVP-2 sheets");
AssertTrue(reportResult.CheckedFormulas.Count >= 3, "ExcelReportBuilder should report checked formulas");
AssertTrue(reportResult.CheckedFormulas.SelectMany(formula => excelChecker.CheckFormula(formula)).All(f => f.Code != "EXCEL_365_FUNCTION"), "ExcelReportBuilder formulas should pass Excel 2021 checker");
AssertTrue(reportResult.AuditLogWritten, "ExcelReportBuilder should write audit log");

using (var reportArchive = ZipFile.OpenRead(reportResult.ReportPath))
{
    AssertTrue(reportArchive.GetEntry("[Content_Types].xml") is not null, "Excel report xlsx should include content types");
    AssertTrue(reportArchive.GetEntry("_rels/.rels") is not null, "Excel report xlsx should include root relationships");
    AssertTrue(reportArchive.GetEntry("xl/workbook.xml") is not null, "Excel report xlsx should include workbook part");
    AssertTrue(reportArchive.GetEntry("xl/_rels/workbook.xml.rels") is not null, "Excel report xlsx should include workbook relationships");
    AssertTrue(reportArchive.GetEntry("xl/styles.xml") is not null, "Excel report xlsx should include styles part");
    AssertTrue(reportArchive.GetEntry("xl/worksheets/sheet10.xml") is not null, "Excel report xlsx should include tenth worksheet");
    var summarySheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet5.xml");
    var limitMonitoringSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet6.xml");
    var exceptionSheet = ReadZipEntryText(reportArchive, "xl/worksheets/sheet7.xml");
    AssertTrue(limitMonitoringSheet.Contains("PF_WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("WARNING", StringComparison.Ordinal) && limitMonitoringSheet.Contains("0.95", StringComparison.Ordinal), "WP-07 report should reuse analysis WARNING usage ratio");
    AssertTrue(limitMonitoringSheet.Contains("PF_BREACH", StringComparison.Ordinal) && limitMonitoringSheet.Contains("BREACH", StringComparison.Ordinal), "WP-07 report should expose analysis BREACH status");
    AssertTrue(limitMonitoringSheet.Contains("PF_NOLIMIT", StringComparison.Ordinal) && limitMonitoringSheet.Contains("NO_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis NO_LIMIT status");
    AssertTrue(limitMonitoringSheet.Contains("PF_ZERO", StringComparison.Ordinal) && limitMonitoringSheet.Contains("INVALID_LIMIT", StringComparison.Ordinal), "WP-07 report should expose analysis INVALID_LIMIT status");
    AssertTrue(summarySheet.Contains("ReconciliationPassed", StringComparison.Ordinal) && summarySheet.Contains("FAIL", StringComparison.Ordinal), "WP-07 report summary should expose reconciliation PASS/FAIL");
    AssertTrue(exceptionSheet.Contains("RECON_EXPOSURE_NO_LIMIT", StringComparison.Ordinal) && exceptionSheet.Contains("RECON_NONPOSITIVE_LIMIT", StringComparison.Ordinal), "WP-07 report exception list should include analysis RECON exceptions");
    AssertTrue(exceptionSheet.Contains("REPORT_VALIDATION_HIGH_SMOKE", StringComparison.Ordinal), "WP-07 report exception list should merge high validation findings");
}

var mappingErrorReport = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_wp07_mapping_error_report",
    exposureProfile,
    mappingErrorResult,
    [],
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
using (var mappingErrorArchive = ZipFile.OpenRead(mappingErrorReport.ReportPath))
{
    var limitMonitoringSheet = ReadZipEntryText(mappingErrorArchive, "xl/worksheets/sheet6.xml");
    AssertTrue(limitMonitoringSheet.Contains("MAPPING_ERROR", StringComparison.Ordinal), "WP-07 report should expose analysis MAPPING_ERROR status");
}

var limitRequiredFinding = new SafetyFinding(
    "LIMIT_DATA_REQUIRED",
    SafetySeverity.High,
    "실제 한도 데이터가 필요합니다. 데모 합성 한도는 생성하거나 사용하지 않습니다.");
var demoOnlyFinding = new SafetyFinding(
    "DEMO_ONLY",
    SafetySeverity.Medium,
    "샘플/데모 데이터 기반 리포트입니다.");
var noSyntheticLimitReportPath = Path.Combine("reports", "smoke_wp_01_no_synthetic_limit.xlsx");
if (File.Exists(noSyntheticLimitReportPath))
{
    File.Delete(noSyntheticLimitReportPath);
}

var noSyntheticLimitReport = reportBuilder.BuildReport(new ExcelReportRequest(
    "smoke_wp_01_no_synthetic_limit",
    exposureProfile,
    EmptyLimitAnalysis(),
    [limitRequiredFinding, demoOnlyFinding],
    reportSql,
    "NoModelMode report commentary",
    "user-smoke"));
using (var noSyntheticArchive = ZipFile.OpenRead(noSyntheticLimitReport.ReportPath))
{
    var validationSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet4.xml");
    var limitSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet6.xml");
    var exceptionSheet = ReadZipEntryText(noSyntheticArchive, "xl/worksheets/sheet7.xml");
    AssertTrue(validationSheet.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 report validation should include LIMIT_DATA_REQUIRED finding");
    AssertTrue(validationSheet.Contains("DEMO_ONLY", StringComparison.Ordinal), "WP-01 report validation should include DEMO_ONLY finding");
    AssertTrue(limitSheet.Contains("NO_LIMIT_ROW", StringComparison.Ordinal) && limitSheet.Contains("NO_DATA", StringComparison.Ordinal), "WP-01 report should not create synthetic limit rows");
    AssertTrue(exceptionSheet.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 report exception list should surface missing real limit data");
}

var excelReportLogText = File.ReadAllText(excelReportLogPath);
AssertTrue(!excelReportLogText.Contains(reportSql, StringComparison.Ordinal), "ExcelReportBuilder audit should not store raw SQL text");
AssertTrue(!excelReportLogText.Contains("user-smoke", StringComparison.Ordinal), "ExcelReportBuilder audit should not store raw user id");
AssertTrue(Throws<ArgumentException>(() => new ExcelReportBuilder(loadedRuleSet, reportsDirectory: "reports/../logs")), "ExcelReportBuilder should reject report paths outside reports");
AssertTrue(Throws<ArgumentException>(() => reportBuilder.BuildReport(new ExcelReportRequest(
    "../bad",
    exposureProfile,
    EmptyLimitAnalysis(),
    [],
    reportSql,
    "commentary",
    "user-smoke"))), "ExcelReportBuilder should reject report file path traversal");
var excelReportBuilderCode = File.ReadAllText(Path.Combine("src", "RiskManagementAI.Core", "Report", "ExcelReportBuilder.cs"));
AssertTrue(!excelReportBuilderCode.Contains("ExcelReportLimitRow", StringComparison.Ordinal), "WP-07 should remove ExcelReportLimitRow from report builder");
AssertTrue(!excelReportBuilderCode.Contains("CalculateLimitStatus", StringComparison.Ordinal), "WP-07 should remove three-state report status calculation");
AssertTrue(!excelReportBuilderCode.Contains("CalculateUtilization", StringComparison.Ordinal), "WP-07 should remove report-side utilization recalculation");
AssertTrue(Directory.Exists(Path.Combine("templates", "report")), "ExcelReportBuilder should use templates/report assets");
AssertTrue(
    !File.ReadAllText(Path.Combine("src", "RiskManagementAI.Core", "RiskManagementAI.Core.csproj")).Contains("PackageReference", StringComparison.OrdinalIgnoreCase)
    && !File.ReadAllText(Path.Combine("src", "RiskManagementAI.App", "RiskManagementAI.App.csproj")).Contains("PackageReference", StringComparison.OrdinalIgnoreCase),
    "ExcelReportBuilder should not add NuGet PackageReference");

var profileSmokeDirectory = Path.Combine("artifacts", "smoke-profile-b05");
Directory.CreateDirectory(profileSmokeDirectory);
var profileSmokeCsv = Path.Combine(profileSmokeDirectory, "profile_sample.csv");
File.WriteAllText(profileSmokeCsv, "BASE_DT,DESK_CD,AMT\n20260617,EQD,10\n20260617,EQD,10\n20260618,,20\n");
var smallProfile = profiler.ProfileCsv(profileSmokeCsv);
AssertTrue(smallProfile.RowCount == 3, "Small profile sample should have 3 data rows");
AssertTrue(smallProfile.NullCounts["DESK_CD"] == 1, "DataProfiler should count null values");
AssertTrue(smallProfile.DuplicateRowCount == 1, "DataProfiler should count duplicate rows");
AssertTrue(smallProfile.BaseDateDistribution["20260617"] == 2 && smallProfile.BaseDateDistribution["20260618"] == 1, "DataProfiler should count BASE_DT values");
AssertTrue(smallProfile.NumericColumns["AMT"].Sum == 40m, "DataProfiler should compute small numeric sum");

var noBaseDateCsv = Path.Combine(profileSmokeDirectory, "profile_no_base_dt.csv");
File.WriteAllText(noBaseDateCsv, "DESK_CD,AMT\nEQD,10\nFIC,20\n");
var noBaseDateProfile = profiler.ProfileCsv(noBaseDateCsv);
AssertTrue(noBaseDateProfile.Warnings.Any(w => w.Contains("BASE_DT", StringComparison.OrdinalIgnoreCase)), "DataProfiler should warn when BASE_DT is missing");

var customMappingProfileCsv = Path.Combine(profileSmokeDirectory, "profile_custom_mapping_wp04.csv");
File.WriteAllText(customMappingProfileCsv, "BASE_DATE,DESK_CD,EXPOSURE\n20260617,EQD,10\n20260618,FIC,20\n");
var customMappingProfile = new DataProfiler(customColumnMappingResult).ProfileCsv(customMappingProfileCsv);
AssertTrue(customMappingProfile.BaseDateDistribution["20260617"] == 1 && customMappingProfile.BaseDateDistribution["20260618"] == 1, "DataProfiler should use ColumnMapping for BASE_DT distribution");

var customMappingExposureCsv = Path.Combine(profileSmokeDirectory, "limit_custom_mapping_exposure_wp04.csv");
var customMappingLimitCsv = Path.Combine(profileSmokeDirectory, "limit_custom_mapping_limit_wp04.csv");
File.WriteAllText(customMappingExposureCsv, "BASE_DATE,DESK_CD,PORT_ID,PRODUCT_TYPE,RISK_NM,CCY_CD,EXPOSURE\n20260617,EQD,PF_CUSTOM,Derivative,KOSPI200,KRW,95\n");
File.WriteAllText(customMappingLimitCsv, "BASE_DATE,PORT_ID,RISK_NM,LIMIT,ACTIVE_YN\n20260617,PF_CUSTOM,KOSPI200,100,Y\n");
var customMappingLimitResult = new LimitMonitor(customColumnMappingResult).Analyze(customMappingExposureCsv, customMappingLimitCsv, "20260617");
AssertTrue(customMappingLimitResult.Rows.Count == 1, "LimitMonitor should use ColumnMapping for renamed join columns");
AssertTrue(customMappingLimitResult.Rows.Single().Status == LimitMonitorStatus.Warning, "LimitMonitor should classify custom mapped rows with renamed amount columns");

Cp949Decoder.VerifyMapping();
AssertTrue(Cp949Decoder.MappingSha256 == Cp949Decoder.ExpectedMappingSha256, "CsvReader CP949 mapping hash should match pinned SHA256");
AssertTrue(Cp949Decoder.MappingEntryCount == Cp949Decoder.ExpectedMappingEntryCount, "CsvReader CP949 mapping should include full Windows-949/UHC entries");

var encodingSmokeDirectory = Path.Combine("artifacts", "smoke-csv-encoding-wp02");
Directory.CreateDirectory(encodingSmokeDirectory);
var utf8NoBomCsv = Path.Combine(encodingSmokeDirectory, "utf8_no_bom.csv");
File.WriteAllText(utf8NoBomCsv, "BASE_DT,DESK_CD,확장힣,AMT\n20260617,EQD,힣값,10\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
var utf8NoBomTable = CsvReader.Read(utf8NoBomCsv);
AssertTrue(utf8NoBomTable.Metadata.DetectedEncoding == CsvEncoding.Utf8 && !utf8NoBomTable.Metadata.HadUtf8Bom, "CsvReader should auto-detect UTF-8 without BOM");
AssertTrue(utf8NoBomTable.Rows.Single().GetValue("확장힣") == "힣값", "CsvReader should roundtrip UTF-8 UHC syllable text");

var utf8BomCsv = Path.Combine(encodingSmokeDirectory, "utf8_bom.csv");
File.WriteAllText(utf8BomCsv, "BASE_DT,DESK_CD,AMT\n20260617,EQD,10\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
var utf8BomTable = CsvReader.Read(utf8BomCsv);
AssertTrue(utf8BomTable.Metadata.DetectedEncoding == CsvEncoding.Utf8 && utf8BomTable.Metadata.HadUtf8Bom, "CsvReader should honor UTF-8 BOM");

var cp949UhcCsv = Path.Combine("samples", "dummy_data", "cp949_uhc_sample_cp949.csv");
var cp949UhcTable = CsvReader.Read(cp949UhcCsv);
AssertTrue(cp949UhcTable.Metadata.DetectedEncoding == CsvEncoding.Cp949, "CsvReader should auto-detect CP949 when UTF-8 decoding fails");
AssertTrue(cp949UhcTable.Columns.Contains("확장힣", StringComparer.Ordinal), "CsvReader CP949 should decode UHC extension syllable in header");
AssertTrue(cp949UhcTable.Rows.Single().GetValue("확장힣") == "힣값", "CsvReader CP949 should decode UHC extension syllable in value");
AssertTrue(cp949UhcTable.Metadata.Cp949MappingSha256 == Cp949Decoder.ExpectedMappingSha256, "CsvReader CP949 metadata should expose mapping SHA256");
AssertTrue(cp949UhcTable.Metadata.Cp949MappingEntryCount == Cp949Decoder.ExpectedMappingEntryCount, "CsvReader CP949 metadata should expose mapping entry count");
AssertTrue(CsvReader.Read(cp949UhcCsv, CsvEncoding.Cp949).Rows.Single().GetValue("확장힣") == "힣값", "CsvReader should support explicit CP949");
AssertTrue(Throws<InvalidDataException>(() => CsvReader.Read(cp949UhcCsv, CsvEncoding.Utf8)), "CsvReader explicit UTF-8 should reject CP949 bytes");

var cp949Profile = profiler.ProfileCsv(cp949UhcCsv);
AssertTrue(cp949Profile.SourceName == "cp949_uhc_sample_cp949.csv" && cp949Profile.RowCount == 1, "DataProfiler should use common CsvReader for CP949 files");
AssertTrue(cp949Profile.NumericColumns["AMT"].Sum == 10m, "DataProfiler should preserve numeric profiling through common CsvReader");

var cp949LimitResult = limitMonitor.Analyze(
    Path.Combine("samples", "dummy_data", "risk_exposure_sample_cp949.csv"),
    Path.Combine("samples", "dummy_data", "risk_limit_sample_cp949.csv"),
    "20260617");
AssertTrue(cp949LimitResult.Rows.Count == 1, "LimitMonitor should use common CsvReader for CP949 files");
AssertTrue(cp949LimitResult.Rows.Single().RiskFactor == "KOSPI200힣", "LimitMonitor should preserve CP949 UHC join key text");
AssertTrue(cp949LimitResult.Rows.Single().Status == LimitMonitorStatus.Warning, "LimitMonitor should classify CP949 sample after common CsvReader join");

var cp949Catalog = RegulationCatalog.LoadFromFile(Path.Combine("samples", "dummy_data", "regulation_catalog_cp949.csv"));
AssertTrue(cp949Catalog.Entries.Single().Title == "힣 규정", "RegulationCatalog should use common CsvReader for CP949 catalog files");

var xlsxSmokeDirectory = Path.Combine("artifacts", "smoke-xlsx-input-wp03");
Directory.CreateDirectory(xlsxSmokeDirectory);
var xlsxSmokePath = Path.Combine(xlsxSmokeDirectory, "risk_input_relationship_order.xlsx");
CreateSmokeXlsx(xlsxSmokePath);
var defaultXlsxTable = XlsxReader.Read(xlsxSmokePath);
AssertTrue(defaultXlsxTable.Columns.SequenceEqual(["Marker", "Value"]), "XlsxReader should read the first visible sheet by workbook order");
AssertTrue(defaultXlsxTable.Rows.Single().GetValue("Marker") == "FIRST", "XlsxReader default sheet should use workbook relationship target, not sheet file order");
var namedXlsxTable = XlsxReader.Read(xlsxSmokePath, "위험데이터");
AssertTrue(namedXlsxTable.Columns.SequenceEqual(["BASE_DT", "DESK_CD", "한글", "AMT"]), "XlsxReader should read named non-first sheet headers");
AssertTrue(namedXlsxTable.Rows.Single().GetValue("BASE_DT") == "20260617", "XlsxReader should read shared string cell values");
AssertTrue(namedXlsxTable.Rows.Single().GetValue("한글") == "값힣", "XlsxReader should read Korean rich shared strings");
AssertTrue(namedXlsxTable.Rows.Single().GetValue("AMT") == "10.5", "XlsxReader should read numeric cells as invariant text");
AssertTrue(namedXlsxTable.SourceName == "risk_input_relationship_order.xlsx", "XlsxReader should expose source name through CsvTable");
AssertTrue(profiler.ProfileTable(namedXlsxTable).NumericColumns["AMT"].Sum == 10.5m, "XlsxReader CsvTable should flow through DataProfiler pipeline");
AssertTrue(Throws<InvalidDataException>(() => XlsxReader.Read(xlsxSmokePath, "없는시트")), "XlsxReader should fail gracefully for missing sheet names");

var corruptXlsxPath = Path.Combine(xlsxSmokeDirectory, "corrupt.xlsx");
File.WriteAllText(corruptXlsxPath, "not a zip file");
AssertTrue(Throws<InvalidDataException>(() => XlsxReader.Read(corruptXlsxPath)), "XlsxReader should fail gracefully for corrupt xlsx files");

var tooManyRowsXlsxPath = Path.Combine(xlsxSmokeDirectory, "too_many_rows.xlsx");
CreateSmokeXlsx(tooManyRowsXlsxPath, tooManyRows: true);
AssertTrue(Throws<InvalidDataException>(() => XlsxReader.Read(tooManyRowsXlsxPath)), "XlsxReader should enforce worksheet row safety cap");
AssertTrue(Throws<ArgumentException>(() => XlsxReader.Read(Path.Combine(xlsxSmokeDirectory, "not_xlsx.csv"))), "XlsxReader should reject non-xlsx extensions");

var taskLogPath = Path.Combine("logs", "smoke_task_log.jsonl");
var feedbackLogPath = Path.Combine("logs", "smoke_feedback_log.jsonl");
if (File.Exists(taskLogPath))
{
    File.Delete(taskLogPath);
}

if (File.Exists(feedbackLogPath))
{
    File.Delete(feedbackLogPath);
}

var rawRequest = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = 'DUMMY-001'";
var rawOutput = "SELECT TRADE_ID FROM TRADE_SAMPLE WHERE ACCOUNT_NO = :ACCOUNT_NO";
var taskEntry = new TaskLogEntry(
    "task-smoke-001",
    DateTime.UtcNow,
    LogHash.Sha256Hex("user-smoke"),
    "SqlSafetyCheck",
    "SqlSafetyChecker",
    LogHash.Sha256Hex(rawRequest),
    LogHash.Sha256Hex(rawOutput),
    "PASS",
    loadedRuleSet.RuleVersion);

var taskLogWriter = new TaskLogWriter("logs", "smoke_task_log.jsonl");
taskLogWriter.Append(taskEntry);
var taskLogText = File.ReadAllText(taskLogWriter.LogFilePath);
AssertTrue(File.Exists(taskLogWriter.LogFilePath), "TaskLogWriter should create JSONL file");
AssertTrue(taskLogText.Contains(taskEntry.RequestHash, StringComparison.Ordinal), "TaskLogWriter should store request hash");
AssertTrue(!taskLogText.Contains(rawRequest, StringComparison.Ordinal) && !taskLogText.Contains("ACCOUNT_NO", StringComparison.Ordinal), "TaskLogWriter should not store raw request/output text");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RequestHash = rawRequest })), "TaskLogWriter should reject non-hash RequestHash");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { OutputHash = rawOutput })), "TaskLogWriter should reject non-hash OutputHash");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { UserId = "plain-user" })), "TaskLogWriter should reject non-hash UserId");
AssertTrue(Throws<ArgumentException>(() => taskLogWriter.Append(taskEntry with { RuleVersion = "ruleset-not-valid" })), "TaskLogWriter should reject malformed RuleVersion");
AssertTrue(Throws<ArgumentException>(() => new TaskLogWriter("logs/../reports", "bad.jsonl")), "TaskLogWriter should reject paths outside logs");

var feedbackEntry = new FeedbackLogEntry(
    "feedback-smoke-001",
    taskEntry.TaskId,
    DateTime.UtcNow,
    taskEntry.UserId,
    "APPROVED",
    "ReviewerApproved");

var feedbackLogWriter = new FeedbackLogWriter("logs", "smoke_feedback_log.jsonl");
feedbackLogWriter.Append(feedbackEntry);
var feedbackLogText = File.ReadAllText(feedbackLogWriter.LogFilePath);
AssertTrue(File.Exists(feedbackLogWriter.LogFilePath), "FeedbackLogWriter should create JSONL file");
AssertTrue(feedbackLogText.Contains(feedbackEntry.FeedbackId, StringComparison.Ordinal), "FeedbackLogWriter should store feedback id");
AssertTrue(!feedbackLogText.Contains(rawRequest, StringComparison.Ordinal), "FeedbackLogWriter should not store raw request text");
AssertTrue(Throws<ArgumentException>(() => feedbackLogWriter.Append(feedbackEntry with { UserId = "plain-user" })), "FeedbackLogWriter should reject non-hash UserId");

var historyLogDirectory = Path.Combine("logs", "smoke_history_reader");
if (Directory.Exists(historyLogDirectory))
{
    Directory.Delete(historyLogDirectory, recursive: true);
}

var historyTaskWriter = new TaskLogWriter(historyLogDirectory, "task_log.jsonl");
historyTaskWriter.Append(taskEntry);
File.AppendAllText(historyTaskWriter.LogFilePath, "{ broken json" + Environment.NewLine);
var historyFeedbackWriter = new FeedbackLogWriter(historyLogDirectory, "feedback_log.jsonl");
historyFeedbackWriter.Append(feedbackEntry);
var auditLogReader = new AuditLogReader();
var auditReadResult = auditLogReader.Read(historyLogDirectory, maxRows: 10);
AssertTrue(auditReadResult.Records.Count == 2, "AuditLogReader should read TaskLog and FeedbackLog records");
AssertTrue(auditReadResult.Findings.Any(f => f.Code == "AUDIT_LOG_LINE_INVALID"), "AuditLogReader should warn on invalid JSONL lines");
var taskAuditRecord = auditReadResult.Records.Single(record => record.Source == "TaskLog");
AssertTrue(taskAuditRecord.EntryId == taskEntry.TaskId && taskAuditRecord.ActivityType == taskEntry.TaskType, "AuditLogReader should project TaskLog schema");
AssertTrue(taskAuditRecord.RequestHashPrefix.Length == 12 && taskAuditRecord.RequestHashPrefix != taskEntry.RequestHash, "AuditLogReader should expose only request hash prefix");
var feedbackAuditRecord = auditReadResult.Records.Single(record => record.Source == "FeedbackLog");
AssertTrue(feedbackAuditRecord.EntryId == feedbackEntry.FeedbackId && feedbackAuditRecord.Result == feedbackEntry.ReviewStatus, "AuditLogReader should project FeedbackLog schema");
AssertTrue(auditReadResult.Records.All(record => record.UserHashPrefix.Length == 12 && record.UserHashPrefix != taskEntry.UserId), "AuditLogReader should expose only user hash prefixes");
var missingAuditResult = auditLogReader.Read(Path.Combine("logs", "smoke_history_missing"), maxRows: 10);
AssertTrue(missingAuditResult.Records.Count == 0 && missingAuditResult.Findings.Any(f => f.Code == "AUDIT_LOG_FILE_MISSING"), "AuditLogReader should gracefully report missing log files");
AssertTrue(Throws<ArgumentException>(() => auditLogReader.Read("logs/../reports")), "AuditLogReader should reject paths outside logs");

if (failed > 0)
{
    Console.WriteLine($"SmokeTests failed: {failed}");
    Environment.Exit(1);
}

Console.WriteLine("All SmokeTests passed.");

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

using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Risk;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Report;

public sealed record ExcelReportRequest(
    string ReportName,
    DataProfileResult DataProfile,
    LimitAnalysisResult Analysis,
    IReadOnlyList<SafetyFinding> ValidationFindings,
    string SqlUsed,
    string Commentary,
    string UserId);

public sealed record ExcelReportResult(
    string ReportPath,
    IReadOnlyList<string> SheetNames,
    IReadOnlyList<string> CheckedFormulas,
    IReadOnlyList<SafetyFinding> Findings,
    bool AuditLogWritten);

public sealed class ExcelReportBuilder
{
    public static readonly IReadOnlyList<string> ExpectedSheetNames =
    [
        "README",
        "RAW_DATA",
        "DATA_PROFILE",
        "VALIDATION",
        "SUMMARY",
        "LIMIT_MONITORING",
        "EXCEPTION_LIST",
        "RISK_VISUAL",
        "SQL_USED",
        "CHANGE_LOG",
        "AI_COMMENTARY"
    ];

    private const string ReportsRootName = "reports";
    private const string DefaultReportTemplateDirectory = "templates/report";
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly SafetyRuleSet ruleSet;
    private readonly Excel2021FunctionChecker functionChecker;
    private readonly TaskLogWriter taskLogWriter;
    private readonly string reportsDirectory;
    private readonly string templateDirectory;

    public ExcelReportBuilder(
        SafetyRuleSet ruleSet,
        TaskLogWriter? taskLogWriter = null,
        string reportsDirectory = ReportsRootName,
        string? templateDirectory = null)
    {
        this.ruleSet = ruleSet ?? throw new ArgumentNullException(nameof(ruleSet));
        functionChecker = new Excel2021FunctionChecker(ruleSet);
        this.taskLogWriter = taskLogWriter ?? new TaskLogWriter();
        this.reportsDirectory = ResolveReportsDirectory(reportsDirectory);
        this.templateDirectory = ResolveTemplateDirectory(templateDirectory ?? DefaultReportTemplateDirectory);
    }

    public ExcelReportResult BuildReport(ExcelReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var createdAt = DateTime.UtcNow;
        var reportPath = Path.Combine(reportsDirectory, BuildSafeReportFileName(request.ReportName));
        var workbook = BuildWorkbook(request, createdAt);
        var formulaFindings = workbook.Formulas
            .SelectMany(formula => functionChecker.CheckFormula(formula))
            .Where(finding => finding.Code == "EXCEL_365_FUNCTION")
            .ToList();
        if (formulaFindings.Count > 0)
        {
            throw new InvalidDataException("Excel 2021 호환 범위를 벗어난 수식이 있어 리포트를 생성하지 않았습니다.");
        }

        if (File.Exists(reportPath))
        {
            File.Delete(reportPath);
        }

        using (var archive = ZipFile.Open(reportPath, ZipArchiveMode.Create))
        {
            WriteWorkbookPackage(archive, workbook, createdAt);
        }

        var findings = new List<SafetyFinding>
        {
            new(
                "EXCEL_REPORT_CREATED",
                SafetySeverity.Info,
                $"Excel 2021 호환 xlsx 리포트를 생성했습니다. Sheets={ExpectedSheetNames.Count}, Path={Path.GetFileName(reportPath)}"),
            new(
                "EXCEL_REPORT_FORMULA_COMPATIBLE",
                SafetySeverity.Info,
                $"생성 수식 {workbook.Formulas.Count:N0}개가 Excel2021FunctionChecker를 통과했습니다.")
        };

        var auditLogWritten = TryAppendAuditLog(request, reportPath, workbook, findings);
        return new ExcelReportResult(reportPath, ExpectedSheetNames, workbook.Formulas, findings, auditLogWritten);
    }

    private WorkbookBuildResult BuildWorkbook(ExcelReportRequest request, DateTime createdAt)
    {
        var formulaRawDataRows = "=COUNTA(RAW_DATA!A:A)";
        var formulaLimitSum = "=SUM(LIMIT_MONITORING!E:E)";
        var formulas = new[] { formulaRawDataRows, formulaLimitSum };
        var exceptionCount = CountExceptions(request.Analysis, request.ValidationFindings);
        var riskVisual = RiskVisualAggregator.Aggregate(request.Analysis, RiskVisualAggregator.DefaultTopN);

        var sheets = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["README"] = BuildSheetData([
                Row("Item", "Value"),
                Row("Purpose", "Review-only Excel 2021 risk report"),
                Row("GeneratedAtUtc", createdAt.ToString("O", CultureInfo.InvariantCulture)),
                Row("NoMacro", "True"),
                Row("NoInterop", "True"),
                Row("NoExternalApi", "True"),
                Row("TemplateMode", "System.IO.Compression + templates/report XML replacement")
            ]),
            ["RAW_DATA"] = BuildSheetData(BuildRawDataRows(request.DataProfile)),
            ["DATA_PROFILE"] = BuildSheetData(BuildDataProfileRows(request.DataProfile)),
            ["VALIDATION"] = BuildSheetData(BuildValidationRows(request.ValidationFindings)),
            ["SUMMARY"] = BuildSheetData([
                Row("Metric", "Value"),
                Row("SourceName", request.DataProfile.SourceName),
                Row("Rows", Number(request.DataProfile.RowCount)),
                Row("Columns", Number(request.DataProfile.ColumnCount)),
                Row("DuplicateRows", Number(request.DataProfile.DuplicateRowCount)),
                Row("AnalysisBaseDate", request.Analysis.BaseDate),
                Row("LimitRows", Number(request.Analysis.Rows.Count)),
                Row("NormalCount", Number(request.Analysis.Kpis.NormalCount)),
                Row("WarningCount", Number(request.Analysis.Kpis.WarningCount)),
                Row("BreachCount", Number(request.Analysis.Kpis.BreachCount)),
                Row("NoLimitCount", Number(request.Analysis.Kpis.NoLimitCount)),
                Row("InvalidLimitCount", Number(request.Analysis.Kpis.InvalidLimitCount)),
                Row("MappingErrorCount", Number(request.Analysis.Kpis.MappingErrorCount)),
                Row("DuplicateLimitCount", Number(request.Analysis.Kpis.DuplicateLimitCount)),
                Row("ExposureAmountSum", Number(request.Analysis.Kpis.ExposureAmountSum)),
                Row("LimitAmountSum", Number(request.Analysis.Kpis.LimitAmountSum)),
                Row("RemainingLimitSum", Number(request.Analysis.Kpis.RemainingLimitSum)),
                Row("ReconciliationPassed", request.Analysis.Reconciliation.Passed ? "PASS" : "FAIL"),
                Row("ReconciliationCheckCount", Number(request.Analysis.Reconciliation.CheckCount)),
                Row("ExceptionCount", Number(exceptionCount)),
                Row("RawDataReferenceCountFormula", Formula(formulaRawDataRows)),
                Row("LimitAmountSumFormula", Formula(formulaLimitSum))
            ]),
            ["LIMIT_MONITORING"] = BuildSheetData(BuildLimitRows(request.Analysis)),
            ["EXCEPTION_LIST"] = BuildSheetData(BuildExceptionRows(request.Analysis, request.ValidationFindings)),
            ["RISK_VISUAL"] = BuildSheetData(BuildRiskVisualRows(riskVisual)),
            ["SQL_USED"] = BuildSheetData([
                Row("SQL", "Review-only text. 자동 실행 금지."),
                Row("Statement", request.SqlUsed)
            ]),
            ["CHANGE_LOG"] = BuildSheetData([
                Row("Field", "Value"),
                Row("GeneratedAtUtc", createdAt.ToString("O", CultureInfo.InvariantCulture)),
                Row("RuleVersion", ruleSet.RuleVersion),
                Row("Generator", nameof(ExcelReportBuilder)),
                Row("Decision", "DM-03: in-box xlsx, NuGet 0, Interop forbidden, OpenXML SDK not introduced")
            ]),
            ["AI_COMMENTARY"] = BuildSheetData([
                Row("Commentary", request.Commentary),
                Row("ReviewNotice", "검토용 초안. 사용자가 확인한 뒤 업무 문서로 활용해야 합니다.")
            ])
        };

        return new WorkbookBuildResult(sheets, formulas);
    }

    private void WriteWorkbookPackage(ZipArchive archive, WorkbookBuildResult workbook, DateTime createdAt)
    {
        var worksheetOverrides = new StringBuilder();
        var workbookSheets = new StringBuilder();
        var workbookRels = new StringBuilder();
        var sheetTitles = new StringBuilder();

        for (var index = 0; index < ExpectedSheetNames.Count; index++)
        {
            var sheetId = index + 1;
            var sheetName = ExpectedSheetNames[index];
            worksheetOverrides.AppendLine($"  <Override PartName=\"/xl/worksheets/sheet{sheetId}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            workbookSheets.AppendLine($"    <sheet name=\"{Xml(sheetName)}\" sheetId=\"{sheetId}\" r:id=\"rId{sheetId}\"/>");
            workbookRels.AppendLine($"  <Relationship Id=\"rId{sheetId}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{sheetId}.xml\"/>");
            sheetTitles.AppendLine($"      <vt:lpstr>{Xml(sheetName)}</vt:lpstr>");
        }

        workbookRels.AppendLine($"  <Relationship Id=\"rId{ExpectedSheetNames.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");

        WriteEntry(archive, "[Content_Types].xml", ReadTemplate("content_types.xml.tpl")
            .Replace("{{WORKSHEET_OVERRIDES}}", worksheetOverrides.ToString().TrimEnd()));
        WriteEntry(archive, "_rels/.rels", ReadTemplate("root_rels.xml.tpl"));
        WriteEntry(archive, "xl/workbook.xml", ReadTemplate("workbook.xml.tpl")
            .Replace("{{WORKBOOK_SHEETS}}", workbookSheets.ToString().TrimEnd()));
        WriteEntry(archive, "xl/_rels/workbook.xml.rels", ReadTemplate("workbook_rels.xml.tpl")
            .Replace("{{WORKBOOK_RELS}}", workbookRels.ToString().TrimEnd()));
        WriteEntry(archive, "xl/styles.xml", ReadTemplate("styles.xml.tpl"));
        WriteEntry(archive, "docProps/core.xml", ReadTemplate("core.xml.tpl")
            .Replace("{{CREATED_AT}}", createdAt.ToString("O", CultureInfo.InvariantCulture)));
        WriteEntry(archive, "docProps/app.xml", ReadTemplate("app.xml.tpl")
            .Replace("{{SHEET_COUNT}}", ExpectedSheetNames.Count.ToString(CultureInfo.InvariantCulture))
            .Replace("{{SHEET_TITLES}}", sheetTitles.ToString().TrimEnd()));

        var worksheetTemplate = ReadTemplate("worksheet.xml.tpl");
        for (var index = 0; index < ExpectedSheetNames.Count; index++)
        {
            var sheetName = ExpectedSheetNames[index];
            var sheetData = workbook.Sheets[sheetName];
            WriteEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", worksheetTemplate.Replace("{{SHEET_DATA}}", sheetData));
        }
    }

    private bool TryAppendAuditLog(
        ExcelReportRequest request,
        string reportPath,
        WorkbookBuildResult workbook,
        List<SafetyFinding> findings)
    {
        try
        {
            var validationDigest = string.Join('|', request.ValidationFindings.Select(f => $"{f.Severity}:{f.Code}:{f.Position}"));
            var requestMaterial = $"{request.ReportName}|{request.DataProfile.SourceName}|{request.DataProfile.RowCount}|{request.DataProfile.ColumnCount}|{request.Analysis.BaseDate}|{request.Analysis.Rows.Count}|{request.Analysis.Kpis}|{request.Analysis.Reconciliation.Passed}|{validationDigest}";
            var outputMaterial = $"{Path.GetFileName(reportPath)}|{string.Join(',', ExpectedSheetNames)}|{string.Join(',', workbook.Formulas)}";
            taskLogWriter.Append(new TaskLogEntry(
                $"task-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                LogHash.Sha256Hex(string.IsNullOrWhiteSpace(request.UserId) ? "anonymous" : request.UserId),
                "ExcelReportGeneration",
                nameof(ExcelReportBuilder),
                LogHash.Sha256Hex(requestMaterial),
                LogHash.Sha256Hex(outputMaterial),
                BuildSafetyResult(request.ValidationFindings),
                ruleSet.RuleVersion));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            findings.Add(new SafetyFinding("TASK_LOG_WRITE_FAILED", SafetySeverity.High, ex.Message));
            return false;
        }
    }

    private static string BuildSafetyResult(IReadOnlyList<SafetyFinding> findings)
    {
        if (findings.Any(f => f.Severity == SafetySeverity.Blocker))
        {
            return "BLOCKED";
        }

        if (findings.Any(f => f.Severity == SafetySeverity.High))
        {
            return "REVIEW_REQUIRED";
        }

        return "PASS";
    }

    public static int CountExceptions(
        LimitAnalysisResult analysis,
        IReadOnlyList<SafetyFinding> validationFindings)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(validationFindings);
        return analysis.ExceptionList.Count
            + validationFindings.Count(finding => finding.Severity is SafetySeverity.Blocker or SafetySeverity.High);
    }

    private IEnumerable<IReadOnlyList<ReportCell>> BuildRawDataRows(DataProfileResult profile)
    {
        yield return Row("Field", "Value");
        yield return Row("SourceName", profile.SourceName);
        yield return Row("RowsProfiled", Number(profile.RowCount));
        yield return Row("ColumnsProfiled", Number(profile.ColumnCount));
        yield return Row("RawRowsEmbedded", "No - MVP-2 stores source profile metadata only");
        yield return Row("Reason", "원본 데이터 대량 저장을 피하고 reports/ 산출물 범위를 명확히 합니다.");
    }

    private IEnumerable<IReadOnlyList<ReportCell>> BuildDataProfileRows(DataProfileResult profile)
    {
        yield return Row("Section", "Name", "Metric", "Value");
        foreach (var column in profile.Columns)
        {
            yield return Row("NullCount", column, "Nulls", Number(profile.NullCounts.GetValueOrDefault(column)));
        }

        foreach (var (baseDate, count) in profile.BaseDateDistribution.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            yield return Row("BaseDate", baseDate, "Rows", Number(count));
        }

        foreach (var numeric in profile.NumericColumns.Values.OrderBy(item => item.ColumnName, StringComparer.Ordinal))
        {
            yield return Row("Numeric", numeric.ColumnName, "NonNullCount", Number(numeric.NonNullCount));
            yield return Row("Numeric", numeric.ColumnName, "Sum", Number(numeric.Sum));
            yield return Row("Numeric", numeric.ColumnName, "Min", Number(numeric.Min));
            yield return Row("Numeric", numeric.ColumnName, "Max", Number(numeric.Max));
            yield return Row("Numeric", numeric.ColumnName, "OutlierCount", Number(numeric.OutlierCount));
        }

        foreach (var warning in profile.Warnings)
        {
            yield return Row("Warning", "DataProfiler", "Message", warning);
        }
    }

    private static IEnumerable<IReadOnlyList<ReportCell>> BuildValidationRows(IReadOnlyList<SafetyFinding> findings)
    {
        yield return Row("Severity", "Code", "Message", "Position");
        if (findings.Count == 0)
        {
            yield return Row("Info", "VALIDATION_OK", "위험 또는 호환성 경고가 없습니다.", string.Empty);
            yield break;
        }

        foreach (var finding in findings)
        {
            yield return Row(
                finding.Severity.ToString(),
                finding.Code,
                finding.Message,
                finding.Position?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static IEnumerable<IReadOnlyList<ReportCell>> BuildLimitRows(LimitAnalysisResult analysis)
    {
        yield return Row("BaseDate", "PortfolioId", "RiskFactor", "ExposureAmount", "LimitAmount", "UtilizationRatio", "RemainingLimit", "Status", "Note");
        if (analysis.Rows.Count == 0)
        {
            yield return Row(analysis.BaseDate, "NO_LIMIT_ROW", "N/A", Number(0), Number(0), Number(0), Number(0), "NO_DATA", "실제 한도 분석 행이 없습니다.");
            yield break;
        }

        foreach (var row in analysis.MonitoringTable)
        {
            yield return Row(
                row.BaseDate,
                row.PortfolioId,
                row.RiskFactor,
                Number(row.ExposureAmount),
                Number(row.LimitAmount),
                Number(row.UsageRatio),
                Number(row.RemainingLimit),
                row.StatusCode,
                row.Note);
        }
    }

    private static IEnumerable<IReadOnlyList<ReportCell>> BuildExceptionRows(
        LimitAnalysisResult analysis,
        IReadOnlyList<SafetyFinding> validationFindings)
    {
        yield return Row("Type", "Severity", "Code", "Message", "BaseDate", "PortfolioId", "RiskFactor");
        var emitted = false;
        foreach (var exception in analysis.ExceptionList)
        {
            emitted = true;
            yield return Row(
                "Analysis",
                exception.Severity.ToString(),
                exception.Code,
                exception.Message,
                exception.BaseDate,
                exception.PortfolioId,
                exception.RiskFactor);
        }

        foreach (var finding in validationFindings.Where(f => f.Severity is SafetySeverity.Blocker or SafetySeverity.High))
        {
            emitted = true;
            yield return Row("Validation", finding.Severity.ToString(), finding.Code, finding.Message, string.Empty, string.Empty, string.Empty);
        }

        if (!emitted)
        {
            yield return Row("Info", "Info", "NO_EXCEPTION", "예외 항목이 없습니다.", string.Empty, string.Empty, string.Empty);
        }
    }

    private static IEnumerable<IReadOnlyList<ReportCell>> BuildRiskVisualRows(RiskVisualModel visual)
    {
        yield return Row("Section", "Metric", "Value", "PortfolioId", "RiskFactor", "BaseDate", "Status", "CurrencyCode", "Note");
        yield return Row("STATUS_DISTRIBUTION", "StatusCode", "Count", "Ratio", string.Empty, string.Empty, string.Empty, string.Empty, "7-state distribution");
        foreach (var status in visual.StatusDistribution)
        {
            yield return Row("STATUS_DISTRIBUTION", status.StatusCode, Number(status.Count), Number(status.Ratio), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        yield return Row("TOP_EXPOSURE", "Rank", "ExposureAmount", "AbsoluteExposureAmount", "UsageRatio", "PortfolioId", "RiskFactor", "Status", "TopN by Abs(ExposureAmount)");
        foreach (var row in visual.TopExposures)
        {
            yield return Row(
                "TOP_EXPOSURE",
                Number(row.Rank),
                Number(row.ExposureAmount),
                Number(row.AbsoluteExposureAmount),
                Number(row.UsageRatio),
                row.PortfolioId,
                row.RiskFactor,
                row.StatusCode,
                row.CurrencyCode);
        }

        yield return Row("CONCENTRATION", "Metric", "Value", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "Denominator=Sum(Abs(ExposureAmount))");
        yield return Row("CONCENTRATION", "TotalAbsoluteExposure", Number(visual.Concentration.TotalAbsoluteExposure), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        yield return Row("CONCENTRATION", "TopNAbsoluteExposure", Number(visual.Concentration.TopNAbsoluteExposure), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        yield return Row("CONCENTRATION", "TopNShare", Number(visual.Concentration.TopNShare), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        yield return Row("CONCENTRATION", "HHI", Number(visual.Concentration.Hhi), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        yield return Row("CONCENTRATION", "RowCount", Number(visual.Concentration.RowCount), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        yield return Row("CONCENTRATION", "TopNCount", Number(visual.Concentration.TopNCount), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        yield return Row("HEATMAP", "Grade", "UsageRatio", "PortfolioId", "RiskFactor", "BaseDate", "Status", string.Empty, "LOW <0.8 / MID 0.8~1.0 / HIGH >1.0");
        foreach (var row in visual.Heatmap)
        {
            yield return Row("HEATMAP", row.Grade, Number(row.UsageRatio), row.PortfolioId, row.RiskFactor, row.BaseDate, row.StatusCode, string.Empty, string.Empty);
        }

        yield return Row("VISUAL_FINDING", "Severity", "Code", "Message", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        if (visual.Findings.Count == 0)
        {
            yield return Row("VISUAL_FINDING", "Info", "VISUAL_OK", "시각화 집계 주석이 없습니다.", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            yield break;
        }

        foreach (var finding in visual.Findings)
        {
            yield return Row("VISUAL_FINDING", finding.Severity.ToString(), finding.Code, finding.Message, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }

    private string ReadTemplate(string fileName)
    {
        var path = Path.Combine(templateDirectory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Excel 리포트 템플릿 파일을 찾을 수 없습니다.", path);
        }

        return File.ReadAllText(path, Utf8NoBom);
    }

    private static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Utf8NoBom);
        writer.Write(content);
    }

    private static string BuildSheetData(IEnumerable<IReadOnlyList<ReportCell>> rows)
    {
        var sb = new StringBuilder();
        var rowIndex = 1;
        foreach (var row in rows)
        {
            sb.Append("    <row r=\"");
            sb.Append(rowIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append("\">");
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                var reference = $"{ColumnName(columnIndex + 1)}{rowIndex}";
                sb.Append(BuildCell(reference, row[columnIndex]));
            }

            sb.AppendLine("</row>");
            rowIndex++;
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildCell(string reference, ReportCell cell)
    {
        return cell.Kind switch
        {
            ReportCellKind.Number => $"<c r=\"{reference}\"><v>{cell.Value}</v></c>",
            ReportCellKind.Formula => $"<c r=\"{reference}\"><f>{Xml(cell.Value.TrimStart('='))}</f><v>0</v></c>",
            _ => $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{Xml(cell.Value)}</t></is></c>"
        };
    }

    private static string ColumnName(int index)
    {
        var dividend = index;
        var name = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            dividend = (dividend - modulo) / 26;
        }

        return name;
    }

    private static IReadOnlyList<ReportCell> Row(params object?[] values)
    {
        return values.Select(value => value switch
        {
            ReportCell cell => cell,
            int number => Number(number),
            decimal number => Number(number),
            long number => Number(number),
            _ => Text(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
        }).ToArray();
    }

    private static ReportCell Text(string value) => new(value, ReportCellKind.Text);

    private static ReportCell Number(int value) => Number((decimal)value);

    private static ReportCell Number(long value) => Number((decimal)value);

    private static ReportCell Number(decimal value)
    {
        return new ReportCell(value.ToString(CultureInfo.InvariantCulture), ReportCellKind.Number);
    }

    private static ReportCell Formula(string value) => new(value, ReportCellKind.Formula);

    private static string Xml(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }

    private static void ValidateRequest(ExcelReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportName))
        {
            throw new ArgumentException("리포트 파일명이 비어 있습니다.", nameof(request));
        }

        if (request.ReportName.Contains(Path.DirectorySeparatorChar)
            || request.ReportName.Contains(Path.AltDirectorySeparatorChar)
            || request.ReportName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("리포트 파일명에는 경로 구분자나 상위 경로 참조를 사용할 수 없습니다.", nameof(request));
        }

        if (request.DataProfile is null)
        {
            throw new ArgumentException("DataProfile이 필요합니다.", nameof(request));
        }

        if (request.Analysis is null)
        {
            throw new ArgumentException("LimitAnalysisResult가 필요합니다.", nameof(request));
        }

        if (request.ValidationFindings is null)
        {
            throw new ArgumentException("ValidationFindings가 필요합니다.", nameof(request));
        }
    }

    private static string BuildSafeReportFileName(string reportName)
    {
        var baseName = Path.GetFileNameWithoutExtension(reportName.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(invalid, '_');
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "risk-report";
        }

        return $"{baseName}.xlsx";
    }

    private static string ResolveReportsDirectory(string reportDirectory)
    {
        if (string.IsNullOrWhiteSpace(reportDirectory))
        {
            throw new ArgumentException("리포트 디렉터리가 비어 있습니다.", nameof(reportDirectory));
        }

        if (Path.IsPathRooted(reportDirectory) || ContainsParentTraversal(reportDirectory))
        {
            throw new ArgumentException("리포트 경로는 repo/app 기준 reports 하위 상대경로만 허용됩니다.", nameof(reportDirectory));
        }

        var reportsRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ReportsRootName));
        var targetDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, reportDirectory));
        if (!targetDirectory.Equals(reportsRoot, StringComparison.OrdinalIgnoreCase)
            && !targetDirectory.StartsWith(reportsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("운영환경 쓰기 경로는 reports 하위만 허용됩니다.", nameof(reportDirectory));
        }

        Directory.CreateDirectory(targetDirectory);
        return targetDirectory;
    }

    private static string ResolveTemplateDirectory(string templateDirectory)
    {
        if (string.IsNullOrWhiteSpace(templateDirectory))
        {
            throw new ArgumentException("리포트 템플릿 디렉터리가 비어 있습니다.", nameof(templateDirectory));
        }

        if (Path.IsPathRooted(templateDirectory) || ContainsParentTraversal(templateDirectory))
        {
            throw new ArgumentException("리포트 템플릿 경로는 app/repo 기준 상대경로만 허용됩니다.", nameof(templateDirectory));
        }

        foreach (var baseDirectory in BuildTemplateSearchRoots())
        {
            var candidate = Path.GetFullPath(Path.Combine(baseDirectory, templateDirectory));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"Excel 리포트 템플릿 디렉터리를 찾을 수 없습니다: {templateDirectory}");
    }

    private static IEnumerable<string> BuildTemplateSearchRoots()
    {
        yield return AppContext.BaseDirectory;

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static bool ContainsParentTraversal(string path)
    {
        var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }

    private sealed record WorkbookBuildResult(
        IReadOnlyDictionary<string, string> Sheets,
        IReadOnlyList<string> Formulas);

    private sealed record ReportCell(string Value, ReportCellKind Kind);

    private enum ReportCellKind
    {
        Text,
        Number,
        Formula
    }
}

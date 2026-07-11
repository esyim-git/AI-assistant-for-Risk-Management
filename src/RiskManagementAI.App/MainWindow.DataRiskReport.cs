using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Dashboard;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Feedback;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Report;
using RiskManagementAI.Core.Risk;
using RiskManagementAI.Core.Safety;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private void OnProfileData(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = _dataProfiler.ProfileCsv(ResolveInputPath(DataPathBox.Text));
            DataPreviewBox.Text = BuildProfileSummary(profile);

            var displays = profile.Warnings
                .Select(warning => FindingDisplay.FromSafetyFinding(
                    new SafetyFinding("DATA_PROFILE_WARNING", SafetySeverity.Low, warning)))
                .DefaultIfEmpty(FindingDisplay.FromInfo("DATA_PROFILE_OK", "CSV 프로파일링이 완료되었습니다."))
                .ToList();

            FindingList.ItemsSource = displays;
            FindingSummaryText.Text = $"{displays.Count} data result";
            SafetyStatusText.Text = profile.Warnings.Count > 0
                ? "Data profile warning"
                : "Data profile ready";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            DataPreviewBox.Text = ex.Message;
            var error = new SafetyFinding("DATA_PROFILE_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Data Profile", [error]);
        }
    }

    private void OnRunLimitMonitor(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = _limitMonitor.Analyze(
                ResolveInputPath(RiskExposurePathBox.Text),
                ResolveInputPath(RiskLimitPathBox.Text),
                RiskBaseDateBox.Text);
            RiskLimitGrid.ItemsSource = result.Rows.Select(RiskLimitRowDisplay.FromRow).ToList();
            RiskDashboardSummaryText.Text = BuildRiskDashboardSummary(result);
            RenderRiskCharts(result);

            var findings = result.Findings.ToList();
            var resultSeverity = result.BreachCount > 0 || result.MappingErrorCount > 0
                ? SafetySeverity.High
                : result.WarningCount > 0 || result.NoLimitCount > 0 || result.InvalidLimitCount > 0 || result.DuplicateLimitCount > 0
                    ? SafetySeverity.Medium
                    : SafetySeverity.Info;
            findings.Add(new SafetyFinding(
                "RISK_DASHBOARD_RESULT",
                resultSeverity,
                $"Rows={result.Rows.Count:N0}, NORMAL={result.NormalCount:N0}, WARNING={result.WarningCount:N0}, BREACH={result.BreachCount:N0}, NO_LIMIT={result.NoLimitCount:N0}, INVALID_LIMIT={result.InvalidLimitCount:N0}, MAPPING_ERROR={result.MappingErrorCount:N0}, DUPLICATE_LIMIT={result.DuplicateLimitCount:N0}."));

            var auditFinding = AppendAuditLog(
                "RiskLimitMonitor",
                nameof(LimitMonitor),
                $"{RiskBaseDateBox.Text}|{RiskExposurePathBox.Text}|{RiskLimitPathBox.Text}",
                findings);
            if (auditFinding is not null)
            {
                findings.Add(auditFinding);
            }

            ShowFindings("Risk Dashboard", findings);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            RiskDashboardSummaryText.Text = ex.Message;
            RiskLimitGrid.ItemsSource = Array.Empty<RiskLimitRowDisplay>();
            RiskVisualCanvas.Children.Clear();
            RiskVisualNoteText.Text = ex.Message;
            var error = new SafetyFinding("RISK_DASHBOARD_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Risk Dashboard", [error]);
        }
    }

    private void RenderRiskCharts(LimitAnalysisResult result)
    {
        var visual = RiskVisualAggregator.Aggregate(result, topN: 5);
        RiskVisualCanvas.Children.Clear();
        var width = Math.Max(220d, RiskVisualCanvas.ActualWidth > 0 ? RiskVisualCanvas.ActualWidth : 260d);
        var height = Math.Max(360d, RiskVisualCanvas.ActualHeight > 0 ? RiskVisualCanvas.ActualHeight : 380d);
        RiskVisualCanvas.Width = width;
        RiskVisualCanvas.Height = height;

        DrawCanvasText(RiskVisualCanvas, "Status Distribution", 10, 6, 12, BrushesFor("#334155"), FontWeights.SemiBold);
        var maxCount = Math.Max(1, visual.StatusDistribution.Max(row => row.Count));
        var barLeft = 122d;
        var barMaxWidth = Math.Max(80d, width - barLeft - 48d);
        var y = 30d;
        foreach (var row in visual.StatusDistribution)
        {
            DrawCanvasText(RiskVisualCanvas, row.StatusCode, 10, y - 2, 10, BrushesFor("#475569"), FontWeights.Normal);
            var barWidth = row.Count == 0 ? 0d : barMaxWidth * row.Count / maxCount;
            DrawCanvasRect(RiskVisualCanvas, barLeft, y, barWidth, 12, BrushForStatus(row.StatusCode));
            DrawCanvasText(RiskVisualCanvas, row.Count.ToString("N0"), barLeft + barMaxWidth + 6, y - 3, 10, BrushesFor("#475569"), FontWeights.Normal);
            y += 20d;
        }

        var concentrationTop = y + 6d;
        DrawCanvasText(RiskVisualCanvas, "Concentration", 10, concentrationTop, 12, BrushesFor("#334155"), FontWeights.SemiBold);
        DrawCanvasText(
            RiskVisualCanvas,
            $"TopN Share {visual.Concentration.TopNShare:P1} / HHI {visual.Concentration.Hhi:N4}",
            10,
            concentrationTop + 20d,
            10,
            BrushesFor("#475569"),
            FontWeights.Normal);
        var shareWidth = Math.Max(0d, Math.Min(1d, (double)visual.Concentration.TopNShare) * (width - 24d));
        DrawCanvasRect(RiskVisualCanvas, 10, concentrationTop + 42d, width - 24d, 12, BrushesFor("#E2E8F0"));
        DrawCanvasRect(RiskVisualCanvas, 10, concentrationTop + 42d, shareWidth, 12, BrushesFor("#2563EB"));

        var heatmapTop = concentrationTop + 72d;
        DrawCanvasText(RiskVisualCanvas, "Heatmap", 10, heatmapTop, 12, BrushesFor("#334155"), FontWeights.SemiBold);
        var cellSize = 20d;
        var gap = 4d;
        var columns = Math.Max(1, (int)Math.Floor((width - 20d) / (cellSize + gap)));
        var availableRows = Math.Max(1, (int)Math.Floor((height - heatmapTop - 28d) / (cellSize + gap)));
        var maxCells = Math.Max(1, columns * availableRows);
        var index = 0;
        foreach (var row in visual.Heatmap.Take(maxCells))
        {
            var column = index % columns;
            var line = index / columns;
            var left = 10d + column * (cellSize + gap);
            var top = heatmapTop + 24d + line * (cellSize + gap);
            DrawCanvasRect(RiskVisualCanvas, left, top, cellSize, cellSize, BrushForHeatmapGrade(row.Grade));
            index++;
        }

        if (index == 0)
        {
            DrawCanvasText(RiskVisualCanvas, "No risk rows", 10, heatmapTop + 24d, 10, BrushesFor("#64748B"), FontWeights.Normal);
        }

        var findingNote = visual.Findings.Count == 0
            ? "Risk Visual: in-box deterministic summary."
            : $"Risk Visual: {string.Join(", ", visual.Findings.Select(finding => finding.Code))}";
        RiskVisualNoteText.Text = $"{findingNote} Rows={result.Rows.Count:N0}, TopN={visual.Concentration.TopNCount:N0}.";
    }

    private static void DrawCanvasText(
        Canvas canvas,
        string text,
        double left,
        double top,
        double fontSize,
        Brush foreground,
        FontWeight fontWeight)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Foreground = foreground,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        canvas.Children.Add(block);
    }

    private static void DrawCanvasRect(
        Canvas canvas,
        double left,
        double top,
        double width,
        double height,
        Brush fill)
    {
        var rect = new Rectangle
        {
            Width = Math.Max(0d, width),
            Height = Math.Max(0d, height),
            Fill = fill
        };
        Canvas.SetLeft(rect, left);
        Canvas.SetTop(rect, top);
        canvas.Children.Add(rect);
    }

    private static Brush BrushForStatus(string statusCode)
    {
        return statusCode switch
        {
            "BREACH" => BrushesFor("#DC2626"),
            "WARNING" => BrushesFor("#F59E0B"),
            "NO_LIMIT" => BrushesFor("#64748B"),
            "INVALID_LIMIT" => BrushesFor("#7C3AED"),
            "MAPPING_ERROR" => BrushesFor("#BE123C"),
            "DUPLICATE_LIMIT" => BrushesFor("#0F766E"),
            _ => BrushesFor("#16A34A")
        };
    }

    private static Brush BrushForHeatmapGrade(string grade)
    {
        return grade switch
        {
            "HIGH" => BrushesFor("#DC2626"),
            "MID" => BrushesFor("#F59E0B"),
            _ => BrushesFor("#22C55E")
        };
    }

    private static Brush BrushesFor(string color)
        => (Brush)new BrushConverter().ConvertFromString(color)!;

    private void OnGenerateExcelReport(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPath = ResolveInputPath(DataPathBox.Text);
            var profile = _dataProfiler.ProfileCsv(dataPath);
            var validationFindings = _sqlChecker.Check(SqlRequestBox.Text).ToList();
            var analysis = BuildReportLimitAnalysis(dataPath, profile, validationFindings);
            var reportResult = _excelReportBuilder.BuildReport(new ExcelReportRequest(
                ReportNameBox.Text,
                profile,
                analysis,
                validationFindings,
                SqlRequestBox.Text,
                "NoModelMode: 로컬 검토용 리포트입니다. 산출물은 사용자가 확인한 뒤 업무 문서로 활용해야 합니다.",
                Environment.UserName));
            ReportResultBox.Text = BuildReportSummary(reportResult);
            ShowFindings("Excel Report", reportResult.Findings.Concat(validationFindings).Concat(analysis.Findings).ToList());
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            ReportResultBox.Text = ex.Message;
            var error = new SafetyFinding("EXCEL_REPORT_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Excel Report", [error]);
        }
    }

    private SafetyFinding? AppendAuditLog(string taskType, string toolType, string inputText, IReadOnlyList<SafetyFinding> findings)
    {
        try
        {
            var resultText = BuildSafetyResult(findings);
            var outputDigest = string.Join('|', findings.Select(f => $"{f.Severity}:{f.Code}:{f.Position}"));
            _taskLogWriter.Append(new TaskLogEntry(
                $"task-{Guid.NewGuid():N}",
                DateTime.UtcNow,
                LogHash.Sha256Hex(Environment.UserName),
                taskType,
                toolType,
                LogHash.Sha256Hex(inputText),
                LogHash.Sha256Hex(outputDigest),
                resultText,
                _ruleSet.RuleVersion));
            return null;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return new SafetyFinding("TASK_LOG_WRITE_FAILED", SafetySeverity.High, ex.Message);
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

    private static string BuildDraftPipelineSummary(DraftPipelineResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SafetyResult: {result.SafetyResult}");
        sb.AppendLine($"AcceptedForReview: {result.IsAcceptedForReview}");
        sb.AppendLine($"AuditLogWritten: {result.AuditLogWritten}");
        sb.AppendLine($"TaskId: {result.TaskId}");
        sb.AppendLine(result.Message);
        sb.AppendLine();
        sb.AppendLine(result.DraftText ?? "(no draft text returned)");
        return sb.ToString();
    }

    private static string BuildPromotionSummary(ExamplePromotionResult result, int storedCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Promoted examples: {result.PromotedExamples.Count:N0}");
        sb.AppendLine($"Skipped entries: {result.SkippedEntries.Count:N0}");
        sb.AppendLine($"Stored examples: {storedCount:N0}");
        sb.AppendLine("Store: config/promoted_examples.jsonl");
        foreach (var example in result.PromotedExamples)
        {
            sb.AppendLine($"- {example.ExampleId} / {example.PromotionMode} / {example.ReviewStatus} / {example.ExampleBodyKind}:{example.ExampleBodyLength}");
        }

        foreach (var warning in result.Warnings)
        {
            sb.AppendLine($"Warning: {warning}");
        }

        return sb.ToString();
    }

    private static string BuildReportSummary(ExcelReportResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ReportPath: {result.ReportPath}");
        sb.AppendLine($"Sheets: {result.SheetNames.Count:N0}");
        sb.AppendLine($"CheckedFormulas: {result.CheckedFormulas.Count:N0}");
        sb.AppendLine($"AuditLogWritten: {result.AuditLogWritten}");
        foreach (var sheetName in result.SheetNames)
        {
            sb.AppendLine($"- {sheetName}");
        }

        return sb.ToString();
    }

    private static string BuildRiskDashboardSummary(LimitAnalysisResult result)
    {
        return $"BASE_DT={result.BaseDate} / Rows={result.Rows.Count:N0} / NORMAL={result.NormalCount:N0} / WARNING={result.WarningCount:N0} / BREACH={result.BreachCount:N0} / NO_LIMIT={result.NoLimitCount:N0} / INVALID_LIMIT={result.InvalidLimitCount:N0} / MAPPING_ERROR={result.MappingErrorCount:N0} / DUPLICATE_LIMIT={result.DuplicateLimitCount:N0}";
    }

    private static string BuildHistorySummary(AuditLogReadResult result)
    {
        var taskCount = result.Records.Count(record => string.Equals(record.Source, "TaskLog", StringComparison.Ordinal));
        var feedbackCount = result.Records.Count(record => string.Equals(record.Source, "FeedbackLog", StringComparison.Ordinal));
        return $"Records={result.Records.Count:N0} / TaskLog={taskCount:N0} / FeedbackLog={feedbackCount:N0} / Warnings={result.Findings.Count:N0}";
    }

    private static string BuildSettingsSummary(SecuritySettingsSnapshot snapshot)
    {
        var fallback = snapshot.UsedFallback ? "Fallback active" : "Config loaded";
        return $"{fallback} / RuleVersion={snapshot.RuleVersion} / ModelMode={snapshot.ModelMode} / Rows={snapshot.Rows.Count:N0}";
    }

    private static string BuildDashboardSummary(DashboardSnapshot snapshot)
    {
        return $"Metrics={snapshot.Rows.Count:N0} / Findings={snapshot.Findings.Count:N0}";
    }

    private static int CountReports()
    {
        var reportsDirectory = Path.Combine(Environment.CurrentDirectory, "reports");
        return Directory.Exists(reportsDirectory)
            ? Directory.EnumerateFiles(reportsDirectory, "*.xlsx", SearchOption.TopDirectoryOnly).Count()
            : 0;
    }

    private LimitAnalysisResult BuildReportLimitAnalysis(
        string dataPath,
        DataProfileResult profile,
        List<SafetyFinding> validationFindings)
    {
        var exposureInput = RiskExposurePathBox.Text.Trim();
        var limitInput = RiskLimitPathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(exposureInput) || string.IsNullOrWhiteSpace(limitInput))
        {
            validationFindings.AddRange(BuildMissingLimitFindings(dataPath, profile));
            return BuildEmptyLimitAnalysis(RiskBaseDateBox.Text, exposureInput, limitInput);
        }

        var exposurePath = ResolveInputPath(exposureInput);
        var limitPath = ResolveInputPath(limitInput);
        if (!File.Exists(exposurePath) || !File.Exists(limitPath))
        {
            validationFindings.AddRange(BuildMissingLimitFindings(dataPath, profile));
            validationFindings.Add(new SafetyFinding(
                "LIMIT_INPUT_FILE_MISSING",
                SafetySeverity.High,
                $"한도 분석 입력 파일을 찾을 수 없습니다. ExposureExists={File.Exists(exposurePath)}, LimitExists={File.Exists(limitPath)}"));
            return BuildEmptyLimitAnalysis(RiskBaseDateBox.Text, exposurePath, limitPath);
        }

        var analysis = _limitMonitor.Analyze(exposurePath, limitPath, RiskBaseDateBox.Text);
        var demoFinding = BuildDemoOnlyFinding(dataPath, profile, exposurePath, limitPath);
        if (demoFinding is not null)
        {
            validationFindings.Add(demoFinding);
        }

        return analysis;
    }

    private static LimitAnalysisResult BuildEmptyLimitAnalysis(string baseDate, string exposureSourceName, string limitSourceName)
    {
        var normalizedBaseDate = string.IsNullOrWhiteSpace(baseDate) ? "N/A" : baseDate.Trim();
        var rows = Array.Empty<LimitMonitorRow>();
        return new LimitAnalysisResult(
            normalizedBaseDate,
            rows,
            LimitAnalysisKpis.FromRows(rows),
            new LimitAnalysisMetadata(
                normalizedBaseDate,
                string.IsNullOrWhiteSpace(exposureSourceName) ? "N/A" : Path.GetFileName(exposureSourceName),
                string.IsNullOrWhiteSpace(limitSourceName) ? "N/A" : Path.GetFileName(limitSourceName),
                ColumnMappingUsedFallback: false,
                ColumnMappingWarnings: Array.Empty<string>(),
                IsDeterministic: true,
                JoinAudit: Array.Empty<string>()),
            Array.Empty<LimitException>(),
            Array.Empty<SafetyFinding>(),
            new ReconciliationSummary(Passed: false, CheckCount: 0, Checks: Array.Empty<ReconciliationCheck>()));
    }

    private static IReadOnlyList<SafetyFinding> BuildMissingLimitFindings(string dataPath, DataProfileResult profile)
    {
        var findings = new List<SafetyFinding>
        {
            new(
                "LIMIT_DATA_REQUIRED",
                SafetySeverity.High,
                "실제 한도 데이터가 필요합니다. 데모 합성 한도는 생성하거나 사용하지 않습니다.")
        };

        var demoFinding = BuildDemoOnlyFinding(dataPath, profile);
        if (demoFinding is not null)
        {
            findings.Add(demoFinding);
        }

        return findings;
    }

    private static SafetyFinding? BuildDemoOnlyFinding(
        string dataPath,
        DataProfileResult profile,
        string? exposurePath = null,
        string? limitPath = null)
    {
        return IsDemoDataPath(dataPath)
            || (exposurePath is not null && IsDemoDataPath(exposurePath))
            || (limitPath is not null && IsDemoDataPath(limitPath))
            || profile.SourceName.Contains("sample", StringComparison.OrdinalIgnoreCase)
            ? new SafetyFinding(
                "DEMO_ONLY",
                SafetySeverity.Medium,
                "샘플/데모 데이터 기반 리포트입니다. 운영 판단에 사용하지 마세요.")
            : null;
    }

    private static bool IsDemoDataPath(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalized.Contains($"{Path.DirectorySeparatorChar}samples{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains($"{Path.DirectorySeparatorChar}dummy_data{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

}

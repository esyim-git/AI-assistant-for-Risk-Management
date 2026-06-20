using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Feedback;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Kb;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Report;
using RiskManagementAI.Core.Risk;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private readonly SafetyRuleSet _ruleSet;
    private readonly SqlSafetyChecker _sqlChecker;
    private readonly VbaSafetyChecker _vbaChecker;
    private readonly Excel2021FunctionChecker _excelChecker;
    private readonly ExcelReportBuilder _excelReportBuilder;
    private readonly DataProfiler _dataProfiler = new();
    private readonly LimitMonitor _limitMonitor = new();
    private readonly AuditLogReader _auditLogReader = new();
    private readonly SecuritySettingsSnapshotBuilder _settingsSnapshotBuilder = new();
    private readonly TaskLogWriter _taskLogWriter = new();
    private readonly PolicyLoadResult _policyLoadResult = App.SecurityPolicyLoadResult;
    private readonly ILocalDraftService _draftService;
    private readonly DraftPipeline _draftPipeline;
    private readonly KbSearch? _kbSearch;
    private readonly SafetyFinding? _kbLoadFinding;
    private readonly ExamplePromotion _examplePromotion = new();
    private readonly PromotedExampleStore _promotedExampleStore = new();
    private IReadOnlyDictionary<MainTabKey, TabItem> _tabsByKey = new Dictionary<MainTabKey, TabItem>();

    public MainWindow()
    {
        _ruleSet = RuleLoader.LoadDefault();
        _sqlChecker = new SqlSafetyChecker(_ruleSet);
        _vbaChecker = new VbaSafetyChecker(_ruleSet);
        _excelChecker = new Excel2021FunctionChecker(_ruleSet);
        _excelReportBuilder = new ExcelReportBuilder(_ruleSet, _taskLogWriter);
        _draftService = new NoModelDraftService(_policyLoadResult.Policy);
        _draftPipeline = new DraftPipeline(_draftService, _ruleSet, _taskLogWriter);
        try
        {
            _kbSearch = new KbSearch(RegulationCatalog.LoadDefault(), _taskLogWriter, _ruleSet.RuleVersion);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            _kbLoadFinding = new SafetyFinding("KB_CATALOG_LOAD_FAILED", SafetySeverity.High, ex.Message);
        }

        InitializeComponent();
        _tabsByKey = new Dictionary<MainTabKey, TabItem>
        {
            [MainTabKey.Sql] = SqlTab,
            [MainTabKey.Draft] = DraftTab,
            [MainTabKey.Vba] = VbaTab,
            [MainTabKey.Excel] = ExcelTab,
            [MainTabKey.Data] = DataTab,
            [MainTabKey.RiskDashboard] = RiskDashboardTab,
            [MainTabKey.Report] = ReportTab,
            [MainTabKey.Regulation] = RegulationTab,
            [MainTabKey.Feedback] = FeedbackTab,
            [MainTabKey.History] = HistoryTab,
            [MainTabKey.Settings] = SettingsTab
        };
        EnvironmentText.Text = $"{BuildEnvironmentText(_policyLoadResult)} / {NoModelDraftService.ModeName}";
        SafetyStatusText.Text = _policyLoadResult.UsedFallback
            ? "Security policy fallback active"
            : "Security policy loaded";
        var draftStatus = _draftService.GenerateDraft(new DraftRequest(
            DraftRequestKind.General,
            "Startup local model availability check"));
        var startupFindings = new[]
        {
            FindingDisplay.FromInfo("SYSTEM_READY", "Risk Management AI Assistant가 오프라인 모드로 시작되었습니다."),
            FindingDisplay.FromInfo("SECURITY_POLICY", BuildPolicySummary(_policyLoadResult.Policy))
        }.ToList();
        startupFindings.AddRange(draftStatus.Findings.Select(FindingDisplay.FromSafetyFinding));
        if (_kbLoadFinding is not null)
        {
            startupFindings.Add(FindingDisplay.FromSafetyFinding(_kbLoadFinding));
        }

        FindingList.ItemsSource = startupFindings;
        FindingSummaryText.Text = $"{startupFindings.Count} startup finding(s)";
    }

    private static string BuildEnvironmentText(PolicyLoadResult policyLoadResult)
    {
        var policy = policyLoadResult.Policy;
        var networkState = !policy.Network.AllowExternalApi && !policy.Network.AllowTelemetry && !policy.Network.AllowAutoUpdate
            ? "External comms blocked"
            : "External comms policy review needed";
        var loadState = policyLoadResult.UsedFallback ? "Policy fallback" : "Policy loaded";
        return $"Offline Mode / {networkState} / {loadState}";
    }

    private static string BuildPolicySummary(SecurityPolicy policy)
    {
        return $"Policy: ExternalApi={policy.Network.AllowExternalApi}, AutoUpdate={policy.Network.AllowAutoUpdate}, Telemetry={policy.Network.AllowTelemetry}, SQLAutoExecute={policy.Sql.AllowAutoExecute}, VBAAutoExecute={policy.Vba.AllowAutoExecute}";
    }

    private void OnShowDashboard(object sender, RoutedEventArgs e)
    {
        ShowFindings("Dashboard", [
            new SafetyFinding(
                "DASHBOARD_MVP_STATUS",
                SafetySeverity.Info,
                "MVP-2에서는 가운데 탭의 SQL/Draft/VBA/Excel/Data/Report/Regulation/Feedback 기능을 사용합니다.")
        ]);
    }

    private void OnNavigateSql(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Sql,
            "SQL Assistant",
            "NAVIGATION_SQL",
            "SQL 탭으로 이동했습니다. SQL 검사 버튼으로 안전 검사를 실행하세요.");
    }

    private void OnNavigateVba(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Vba,
            "VBA Assistant",
            "NAVIGATION_VBA",
            "VBA 탭으로 이동했습니다. VBA 검사 버튼으로 안전 검사를 실행하세요.");
    }

    private void OnNavigateData(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Data,
            "Data Analyzer",
            "NAVIGATION_DATA",
            "Data 탭으로 이동했습니다. CSV 분석 버튼으로 샘플 프로파일링을 실행하세요.");
    }

    private void OnNavigateRiskDashboard(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.RiskDashboard,
            "Risk Dashboard",
            "NAVIGATION_RISK_DASHBOARD",
            "Risk Dashboard 탭으로 이동했습니다. 한도 점검 버튼으로 동일 기준일 한도 모니터링을 실행하세요.");
    }

    private void OnNavigateReport(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Report,
            "Excel Report",
            "NAVIGATION_REPORT",
            "Report 탭으로 이동했습니다. 리포트 생성 버튼으로 reports/ 아래 xlsx를 생성하세요.");
    }

    private void OnNavigateRegulation(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Regulation,
            "Regulation / NCR",
            "NAVIGATION_REGULATION",
            "Regulation 탭으로 이동했습니다. 공개 catalog 검색을 실행하세요.");
    }

    private void OnNavigateFeedback(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Feedback,
            "Feedback Center",
            "NAVIGATION_FEEDBACK",
            "Feedback 탭으로 이동했습니다. 승인형 예제 승격을 확인하세요.");
        RefreshPromotedExamples();
    }

    private void OnShowHistory(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.History,
            "History",
            "NAVIGATION_HISTORY",
            "History 탭으로 이동했습니다. 감사 로그는 read-only로 조회합니다.");
        RefreshHistory();
    }

    private void OnShowSettings(object sender, RoutedEventArgs e)
    {
        SelectMainTab(
            MainTabKey.Settings,
            "Settings",
            "NAVIGATION_SETTINGS",
            "Settings 탭으로 이동했습니다. 정책은 view-only로 표시됩니다.");
        RefreshSettings();
    }

    private void SelectMainTab(MainTabKey key, string title, string code, string message)
    {
        if (!_tabsByKey.TryGetValue(key, out var tab))
        {
            ShowFindings(title, [
                new SafetyFinding("NAVIGATION_TARGET_MISSING", SafetySeverity.High, $"내비게이션 대상 탭을 찾을 수 없습니다. Target={key}")
            ]);
            return;
        }

        MainTabs.SelectedItem = tab;
        ShowFindings(title, [
            new SafetyFinding(code, SafetySeverity.Info, message)
        ]);
    }

    private void OnCheckSql(object sender, RoutedEventArgs e)
    {
        var findings = _sqlChecker.Check(SqlRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("SqlSafetyCheck", nameof(SqlSafetyChecker), SqlRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("SQL Safety Check", findings);
    }

    private void OnCheckVba(object sender, RoutedEventArgs e)
    {
        var findings = _vbaChecker.Check(VbaRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("VbaSafetyCheck", nameof(VbaSafetyChecker), VbaRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("VBA Safety Check", findings);
    }

    private void OnGenerateSqlDraft(object sender, RoutedEventArgs e)
    {
        RunDraftPipeline(DraftRequestKind.Sql);
    }

    private void OnGenerateVbaDraft(object sender, RoutedEventArgs e)
    {
        RunDraftPipeline(DraftRequestKind.Vba);
    }

    private void RunDraftPipeline(DraftRequestKind kind)
    {
        var result = _draftPipeline.Generate(new DraftPipelineRequest(
            kind,
            DraftRequestBox.Text,
            Environment.UserName));
        DraftResultBox.Text = BuildDraftPipelineSummary(result);
        ShowFindings($"{kind} Draft Pipeline", result.Findings);
    }

    private void OnCheckExcel(object sender, RoutedEventArgs e)
    {
        var findings = _excelChecker.CheckFormula(ExcelRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("Excel2021FunctionCheck", nameof(Excel2021FunctionChecker), ExcelRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("Excel 2021 Function Check", findings);
    }

    private void OnSearchKbCatalog(object sender, RoutedEventArgs e)
    {
        if (_kbSearch is null)
        {
            var finding = _kbLoadFinding ?? new SafetyFinding("KB_CATALOG_UNAVAILABLE", SafetySeverity.High, "공개 catalog를 로드할 수 없습니다.");
            KbResultBox.Text = finding.Message;
            ShowFindings("Regulation Catalog", [finding]);
            return;
        }

        var response = _kbSearch.Search(KbQueryBox.Text, Environment.UserName);
        KbResultBox.Text = response.DraftAnswer;
        var findings = response.Warnings
            .Select(warning => new SafetyFinding("KB_SEARCH_WARNING", SafetySeverity.Low, warning))
            .ToList();
        findings.Add(new SafetyFinding(
            "KB_SEARCH_RESULT",
            SafetySeverity.Info,
            $"공개 catalog 검색 결과 {response.Results.Count:N0}건. AuditLogWritten={response.AuditLogWritten}"));
        ShowFindings("Regulation Catalog", findings);
    }

    private void OnPromoteFeedbackExample(object sender, RoutedEventArgs e)
    {
        try
        {
            var entry = new FeedbackLogEntry(
                FeedbackIdBox.Text.Trim(),
                FeedbackTaskIdBox.Text.Trim(),
                DateTime.UtcNow,
                LogHash.Sha256Hex(Environment.UserName),
                FeedbackCodeBox.Text.Trim(),
                FeedbackReviewStatusBox.Text.Trim());
            var result = _examplePromotion.PromoteApproved([entry]);
            _promotedExampleStore.Append(result.PromotedExamples);
            var storedExamples = _promotedExampleStore.ReadAll();
            FeedbackPromotionGrid.ItemsSource = storedExamples
                .OrderByDescending(example => example.PromotedAt)
                .Select(PromotedExampleDisplay.FromExample)
                .ToList();
            FeedbackPromotionSummaryText.Text = BuildPromotionSummary(result, storedExamples.Count);

            var findings = result.Warnings
                .Select(warning => new SafetyFinding("FEEDBACK_PROMOTION_WARNING", SafetySeverity.Low, warning))
                .ToList();
            findings.Add(new SafetyFinding(
                "FEEDBACK_PROMOTION_RESULT",
                SafetySeverity.Info,
                $"Promoted={result.PromotedExamples.Count:N0}, Skipped={result.SkippedEntries.Count:N0}, Stored={storedExamples.Count:N0}, Mode={ExamplePromotion.PromotionModeName}"));
            var auditFinding = AppendAuditLog(
                "FeedbackPromotion",
                nameof(ExamplePromotion),
                $"{entry.FeedbackId}|{entry.TaskId}|{entry.FeedbackCode}|{entry.ReviewStatus}",
                findings);
            if (auditFinding is not null)
            {
                findings.Add(auditFinding);
            }

            ShowFindings("Feedback Promotion", findings);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            FeedbackPromotionSummaryText.Text = ex.Message;
            var error = new SafetyFinding("FEEDBACK_PROMOTION_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Feedback Promotion", [error]);
        }
    }

    private void OnRefreshHistory(object sender, RoutedEventArgs e)
    {
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        try
        {
            var result = _auditLogReader.Read("logs");
            HistoryGrid.ItemsSource = result.Records.Select(AuditHistoryRowDisplay.FromRecord).ToList();
            HistorySummaryText.Text = BuildHistorySummary(result);

            var findings = result.Findings.ToList();
            findings.Add(new SafetyFinding(
                "AUDIT_HISTORY_LOADED",
                SafetySeverity.Info,
                $"History 조회 완료: records={result.Records.Count:N0}, warnings={result.Findings.Count:N0}."));
            ShowFindings("History", findings);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            HistorySummaryText.Text = ex.Message;
            HistoryGrid.ItemsSource = Array.Empty<AuditHistoryRowDisplay>();
            var error = new SafetyFinding("AUDIT_HISTORY_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("History", [error]);
        }
    }

    private void OnRefreshSettings(object sender, RoutedEventArgs e)
    {
        RefreshSettings();
    }

    private void RefreshPromotedExamples()
    {
        try
        {
            var storedExamples = _promotedExampleStore.ReadAll();
            FeedbackPromotionGrid.ItemsSource = storedExamples
                .OrderByDescending(example => example.PromotedAt)
                .Select(PromotedExampleDisplay.FromExample)
                .ToList();
            FeedbackPromotionSummaryText.Text = $"Stored examples: {storedExamples.Count:N0} / Store: config/promoted_examples.jsonl";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            FeedbackPromotionSummaryText.Text = ex.Message;
        }
    }

    private void RefreshSettings()
    {
        try
        {
            var snapshot = _settingsSnapshotBuilder.Build(_policyLoadResult, _ruleSet.RuleVersion, NoModelDraftService.ModeName);
            SettingsGrid.ItemsSource = snapshot.Rows.Select(SettingsRowDisplay.FromRow).ToList();
            SettingsSummaryText.Text = BuildSettingsSummary(snapshot);
            ShowFindings("Settings", snapshot.Findings);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            SettingsSummaryText.Text = ex.Message;
            SettingsGrid.ItemsSource = Array.Empty<SettingsRowDisplay>();
            var error = new SafetyFinding("SETTINGS_VIEW_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Settings", [error]);
        }
    }

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

            var findings = result.Findings.ToList();
            var resultSeverity = result.BreachCount > 0
                ? SafetySeverity.High
                : result.WarningCount > 0
                    ? SafetySeverity.Medium
                    : SafetySeverity.Info;
            findings.Add(new SafetyFinding(
                "RISK_DASHBOARD_RESULT",
                resultSeverity,
                $"Rows={result.Rows.Count:N0}, NORMAL={result.NormalCount:N0}, WARNING={result.WarningCount:N0}, BREACH={result.BreachCount:N0}, MissingLimit={result.MissingLimitCount:N0}."));

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
            var error = new SafetyFinding("RISK_DASHBOARD_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Risk Dashboard", [error]);
        }
    }

    private void OnGenerateExcelReport(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = _dataProfiler.ProfileCsv(ResolveInputPath(DataPathBox.Text));
            var validationFindings = _sqlChecker.Check(SqlRequestBox.Text).ToList();
            var reportResult = _excelReportBuilder.BuildReport(new ExcelReportRequest(
                ReportNameBox.Text,
                profile,
                BuildUiLimitRows(profile),
                validationFindings,
                SqlRequestBox.Text,
                "NoModelMode: 로컬 검토용 리포트입니다. 산출물은 사용자가 확인한 뒤 업무 문서로 활용해야 합니다.",
                Environment.UserName));
            ReportResultBox.Text = BuildReportSummary(reportResult);
            ShowFindings("Excel Report", reportResult.Findings);
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
            sb.AppendLine($"- {example.ExampleId} / {example.PromotionMode} / {example.ReviewStatus}");
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

    private static string BuildRiskDashboardSummary(LimitMonitorResult result)
    {
        return $"BASE_DT={result.BaseDate} / Rows={result.Rows.Count:N0} / NORMAL={result.NormalCount:N0} / WARNING={result.WarningCount:N0} / BREACH={result.BreachCount:N0}";
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

    private static IReadOnlyList<ExcelReportLimitRow> BuildUiLimitRows(DataProfileResult profile)
    {
        var exposureAmount = profile.NumericColumns.TryGetValue("EXPOSURE_AMT", out var exposureProfile)
            ? exposureProfile.Sum
            : 0m;
        var limitAmount = Math.Max(Math.Abs(exposureAmount) * 1.1m, 1m);
        return
        [
            new ExcelReportLimitRow(
                "PROFILE_TOTAL",
                "ALL",
                exposureAmount,
                limitAmount,
                "UI aggregate from DataProfiler; review-only")
        ];
    }

    private void ShowFindings(string title, System.Collections.Generic.IReadOnlyList<SafetyFinding> findings)
    {
        if (findings.Count == 0)
        {
            FindingList.ItemsSource = new[] { FindingDisplay.FromInfo("NO_FINDING", "위험 또는 호환성 경고가 탐지되지 않았습니다.") };
            FindingSummaryText.Text = $"{title}: 0 finding";
        }
        else
        {
            var displays = findings
                .OrderByDescending(f => SeverityRank(f.Severity))
                .ThenBy(f => f.Code, StringComparer.Ordinal)
                .Select(FindingDisplay.FromSafetyFinding)
                .ToList();

            FindingList.ItemsSource = displays;
            FindingSummaryText.Text = $"{title}: {displays.Count} finding(s)";
        }

        SafetyStatusText.Text = findings.Any(f => f.Severity == SafetySeverity.Blocker || f.Severity == SafetySeverity.High)
            ? "High risk finding detected"
            : "No high risk finding";
    }

    private static int SeverityRank(SafetySeverity severity) => severity switch
    {
        SafetySeverity.Blocker => 5,
        SafetySeverity.High => 4,
        SafetySeverity.Medium => 3,
        SafetySeverity.Low => 2,
        SafetySeverity.Info => 1,
        _ => 0
    };

    private static string ResolveInputPath(string inputPath)
    {
        var trimmed = inputPath.Trim();
        if (Path.IsPathRooted(trimmed) || File.Exists(trimmed))
        {
            return trimmed;
        }

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, trimmed);
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, trimmed);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return trimmed;
    }

    private static string BuildProfileSummary(DataProfileResult profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Source: {profile.SourceName}");
        sb.AppendLine($"Rows: {profile.RowCount:N0}");
        sb.AppendLine($"Columns: {profile.ColumnCount:N0}");
        sb.AppendLine($"Duplicate rows: {profile.DuplicateRowCount:N0}");
        sb.AppendLine();
        sb.AppendLine("BASE_DT distribution");
        foreach (var (baseDate, count) in profile.BaseDateDistribution.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"- {baseDate}: {count:N0}");
        }

        sb.AppendLine();
        sb.AppendLine("Numeric columns");
        foreach (var column in profile.NumericColumns.Values.OrderBy(item => item.ColumnName, StringComparer.Ordinal))
        {
            sb.AppendLine($"- {column.ColumnName}: count={column.NonNullCount:N0}, sum={column.Sum:N0.####}, min={column.Min:N0.####}, max={column.Max:N0.####}, outliers={column.OutlierCount:N0}");
        }

        return sb.ToString();
    }

    private sealed record FindingDisplay(
        string Severity,
        string Code,
        string Message,
        string Detail,
        Brush Background,
        Brush BorderBrush,
        Brush Foreground)
    {
        public static FindingDisplay FromInfo(string code, string message)
        {
            return new FindingDisplay(
                SafetySeverity.Info.ToString(),
                code,
                message,
                string.Empty,
                new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                new SolidColorBrush(Color.FromRgb(191, 219, 254)),
                new SolidColorBrush(Color.FromRgb(30, 64, 175)));
        }

        public static FindingDisplay FromSafetyFinding(SafetyFinding finding)
        {
            var detail = string.IsNullOrWhiteSpace(finding.MatchedText)
                ? string.Empty
                : $"Matched: {finding.MatchedText}" + (finding.Position.HasValue ? $" / Position: {finding.Position.Value}" : string.Empty);
            var colors = ColorsForSeverity(finding.Severity);
            return new FindingDisplay(
                finding.Severity.ToString(),
                finding.Code,
                finding.Message,
                detail,
                colors.Background,
                colors.Border,
                colors.Foreground);
        }

        private static (Brush Background, Brush Border, Brush Foreground) ColorsForSeverity(SafetySeverity severity)
        {
            return severity switch
            {
                SafetySeverity.Blocker => BrushesFor("#FEF2F2", "#FCA5A5", "#991B1B"),
                SafetySeverity.High => BrushesFor("#FFF1F2", "#FDA4AF", "#9F1239"),
                SafetySeverity.Medium => BrushesFor("#FFFBEB", "#FCD34D", "#92400E"),
                SafetySeverity.Low => BrushesFor("#F0FDF4", "#86EFAC", "#166534"),
                _ => BrushesFor("#EFF6FF", "#BFDBFE", "#1E40AF")
            };
        }

        private static (Brush Background, Brush Border, Brush Foreground) BrushesFor(string background, string border, string foreground)
        {
            return (
                (Brush)new BrushConverter().ConvertFromString(background)!,
                (Brush)new BrushConverter().ConvertFromString(border)!,
                (Brush)new BrushConverter().ConvertFromString(foreground)!);
        }
    }

    private sealed record RiskLimitRowDisplay(
        string BaseDate,
        string PortfolioId,
        string RiskFactor,
        decimal ExposureAmount,
        decimal LimitAmount,
        string UsagePercent,
        string Status,
        string Note)
    {
        public static RiskLimitRowDisplay FromRow(LimitMonitorRow row)
        {
            return new RiskLimitRowDisplay(
                row.BaseDate,
                row.PortfolioId,
                row.RiskFactor,
                row.ExposureAmount,
                row.LimitAmount,
                $"{row.UsageRatio * 100m:N1}%",
                row.Status switch
                {
                    LimitMonitorStatus.Normal => "NORMAL",
                    LimitMonitorStatus.Warning => "WARNING",
                    LimitMonitorStatus.Breach => "BREACH",
                    LimitMonitorStatus.MissingLimit => "MISSING_LIMIT",
                    LimitMonitorStatus.InactiveLimit => "INACTIVE_LIMIT",
                    _ => row.Status.ToString().ToUpperInvariant()
                },
                row.Note);
        }
    }

    private sealed record AuditHistoryRowDisplay(
        string Source,
        string CreatedAtText,
        string EntryId,
        string TaskId,
        string ActivityType,
        string Detail,
        string Result,
        string RuleVersion,
        string UserHashPrefix,
        string RequestHashPrefix,
        string OutputHashPrefix)
    {
        public static AuditHistoryRowDisplay FromRecord(AuditLogRecord record)
        {
            return new AuditHistoryRowDisplay(
                record.Source,
                record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                record.EntryId,
                record.TaskId,
                record.ActivityType,
                record.Detail,
                record.Result,
                record.RuleVersion,
                record.UserHashPrefix,
                record.RequestHashPrefix,
                record.OutputHashPrefix);
        }
    }

    private sealed record SettingsRowDisplay(
        string Section,
        string Name,
        string Value,
        string Meaning)
    {
        public static SettingsRowDisplay FromRow(SecuritySettingRow row)
        {
            return new SettingsRowDisplay(row.Section, row.Name, row.Value, row.Meaning);
        }
    }

    private sealed record PromotedExampleDisplay(
        string ExampleId,
        string FeedbackId,
        string TaskId,
        string ReviewStatus,
        string PromotionMode,
        string PromotedAtText)
    {
        public static PromotedExampleDisplay FromExample(PromotedExample example)
        {
            return new PromotedExampleDisplay(
                example.ExampleId,
                example.FeedbackId,
                example.TaskId,
                example.ReviewStatus,
                example.PromotionMode,
                example.PromotedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    private enum MainTabKey
    {
        Sql,
        Draft,
        Vba,
        Excel,
        Data,
        RiskDashboard,
        Report,
        Regulation,
        Feedback,
        History,
        Settings
    }
}

using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RiskManagementAI.Core.Assist;
using RiskManagementAI.Core.Assist.Providers;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Dashboard;
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
    private readonly ExcelFunctionHelper _excelFunctionHelper;
    private readonly ExcelReportBuilder _excelReportBuilder;
    private readonly DataProfiler _dataProfiler = new();
    private readonly LimitMonitor _limitMonitor = new();
    private readonly AuditLogReader _auditLogReader = new();
    private readonly SecuritySettingsSnapshotBuilder _settingsSnapshotBuilder = new();
    private readonly DashboardSnapshotBuilder _dashboardSnapshotBuilder = new();
    private readonly TaskLogWriter _taskLogWriter = new();
    private readonly SuggestionLogWriter _suggestionLogWriter = new();
    private readonly PolicyLoadResult _policyLoadResult = App.SecurityPolicyLoadResult;
    private readonly ILocalDraftService _draftService;
    private readonly DraftPipeline _draftPipeline;
    private readonly CompletionEngine _completionEngine;
    private readonly KbSearch? _kbSearch;
    private readonly SafetyFinding? _kbLoadFinding;
    private readonly ExamplePromotion _examplePromotion = new();
    private readonly PromotedExampleStore _promotedExampleStore = new();
    private IReadOnlyDictionary<MainTabKey, TabItem> _tabsByKey = new Dictionary<MainTabKey, TabItem>();
    private readonly Dictionary<TextBox, CompletionLanguage> _completionLanguages = new();
    private TextBox? _completionTargetBox;
    private CompletionLanguage _completionTargetLanguage;
    private CompletionResult? _completionResult;
    private IReadOnlyList<ExcelFunctionInfo> _excelFunctionHelperResults = Array.Empty<ExcelFunctionInfo>();
    private readonly DispatcherTimer _completionDebounceTimer;
    private TextBox? _pendingCompletionTextBox;
    private CompletionLanguage _pendingCompletionLanguage;
    private bool _suppressCompletionTextChanged;

    public MainWindow()
    {
        _ruleSet = RuleLoader.LoadDefault();
        _sqlChecker = new SqlSafetyChecker(_ruleSet);
        _vbaChecker = new VbaSafetyChecker(_ruleSet);
        _excelChecker = new Excel2021FunctionChecker(_ruleSet);
        _excelFunctionHelper = new ExcelFunctionHelper(_ruleSet);
        _excelReportBuilder = new ExcelReportBuilder(_ruleSet, _taskLogWriter);
        _completionEngine = new CompletionEngine(new CompletionProviderRegistry(StaticCompletionProviderFactory.CreateDefault(_ruleSet)));
        _completionDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _completionDebounceTimer.Tick += OnCompletionDebounceTimerTick;
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
        Loaded += OnMainWindowLoaded;
        Closing += OnMainWindowClosing;
        _tabsByKey = new Dictionary<MainTabKey, TabItem>
        {
            [MainTabKey.Dashboard] = DashboardTab,
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
        InitializeCompletionAssist();
        InitializeExcelFunctionHelper();
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

        // STAB-WP-03b: surface runtime integrity findings (non-blocking). FailClosed never reaches the
        // window (the app shuts down in App.OnStartup), so only Ok / DevFallback findings appear here.
        startupFindings.AddRange(App.IntegrityResult.Findings.Select(FindingDisplay.FromSafetyFinding));

        FindingList.ItemsSource = startupFindings;
        FindingSummaryText.Text = $"{startupFindings.Count} startup finding(s)";
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        ApplySavedLayout();
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        SaveCurrentLayout();
    }

    private void ApplySavedLayout()
    {
        var layout = UiLayoutStore.Load();
        Width = layout.WindowWidth;
        Height = layout.WindowHeight;
        EditorRow.Height = new GridLength(layout.EditorRowStar, GridUnitType.Star);
        ResultRow.Height = new GridLength(layout.ResultRowStar, GridUnitType.Star);
        SafetyPanelColumn.Width = new GridLength(layout.SafetyColumnWidth, GridUnitType.Pixel);
    }

    private void SaveCurrentLayout()
    {
        try
        {
            UiLayoutStore.Save(new UiLayout(
                WindowWidth: ActualWidth > 0 ? ActualWidth : Width,
                WindowHeight: ActualHeight > 0 ? ActualHeight : Height,
                EditorRowStar: StarOrFallback(EditorRow.Height, UiLayoutStore.Default.EditorRowStar),
                ResultRowStar: StarOrFallback(ResultRow.Height, UiLayoutStore.Default.ResultRowStar),
                SafetyColumnWidth: SafetyPanelColumn.ActualWidth > 0 ? SafetyPanelColumn.ActualWidth : SafetyPanelColumn.Width.Value,
                SchemaVersion: UiLayoutStore.CurrentSchemaVersion));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Layout persistence is convenience state only; app shutdown must remain non-blocking.
        }
    }

    private static double StarOrFallback(GridLength length, double fallback)
    {
        return length.IsStar && length.Value > 0
            ? length.Value
            : fallback;
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
                row.StatusCode,
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
        string BodyKind,
        string BodyLength,
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
                example.ExampleBodyKind,
                example.ExampleBodyLength.ToString("N0"),
                example.PromotedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    private sealed record DashboardMetricDisplay(
        string Metric,
        string Value,
        string Detail)
    {
        public static DashboardMetricDisplay FromRow(DashboardMetricRow row)
        {
            return new DashboardMetricDisplay(row.Metric, row.Value, row.Detail);
        }
    }

    private enum MainTabKey
    {
        Dashboard,
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

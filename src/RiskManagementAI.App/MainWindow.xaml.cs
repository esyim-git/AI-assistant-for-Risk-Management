using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private readonly SqlSafetyChecker _sqlChecker = new();
    private readonly VbaSafetyChecker _vbaChecker = new();
    private readonly Excel2021FunctionChecker _excelChecker = new();
    private readonly DataProfiler _dataProfiler = new();
    private readonly PolicyLoadResult _policyLoadResult = App.SecurityPolicyLoadResult;

    public MainWindow()
    {
        InitializeComponent();
        EnvironmentText.Text = BuildEnvironmentText(_policyLoadResult);
        SafetyStatusText.Text = _policyLoadResult.UsedFallback
            ? "Security policy fallback active"
            : "Security policy loaded";
        FindingList.ItemsSource = new[]
        {
            FindingDisplay.FromInfo("SYSTEM_READY", "Risk Management AI Assistant가 오프라인 모드로 시작되었습니다."),
            FindingDisplay.FromInfo("SECURITY_POLICY", BuildPolicySummary(_policyLoadResult.Policy))
        };
        FindingSummaryText.Text = "2 info";
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

    private void OnCheckSql(object sender, RoutedEventArgs e)
    {
        var findings = _sqlChecker.Check(SqlRequestBox.Text).ToList();
        ShowFindings("SQL Safety Check", findings);
    }

    private void OnCheckVba(object sender, RoutedEventArgs e)
    {
        var findings = _vbaChecker.Check(VbaRequestBox.Text).ToList();
        ShowFindings("VBA Safety Check", findings);
    }

    private void OnCheckExcel(object sender, RoutedEventArgs e)
    {
        var findings = _excelChecker.CheckFormula(ExcelRequestBox.Text).ToList();
        ShowFindings("Excel 2021 Function Check", findings);
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
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            DataPreviewBox.Text = ex.Message;
            var error = new SafetyFinding("DATA_PROFILE_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Data Profile", [error]);
        }
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
}

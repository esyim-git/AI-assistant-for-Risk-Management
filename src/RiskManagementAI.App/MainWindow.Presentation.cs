using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using RiskManagementAI.Core.Data;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
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
}

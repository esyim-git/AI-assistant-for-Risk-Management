using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Dashboard;
using RiskManagementAI.Core.Feedback;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
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
            var bodyInputs = string.IsNullOrWhiteSpace(FeedbackDraftBodyBox.Text)
                ? Array.Empty<FeedbackDraftBodyInput>()
                : [new FeedbackDraftBodyInput(
                    entry.FeedbackId,
                    entry.TaskId,
                    FeedbackDraftBodyBox.Text,
                    FeedbackBodyKindBox.Text.Trim())];
            var result = _examplePromotion.PromoteApproved([entry], bodyInputs, _ruleSet);
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

    private void OnRefreshDashboard(object sender, RoutedEventArgs e)
    {
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        try
        {
            var auditLog = _auditLogReader.Read("logs");
            var snapshot = _dashboardSnapshotBuilder.Build(new DashboardSnapshotRequest(
                _policyLoadResult,
                _ruleSet.RuleVersion,
                NoModelDraftService.ModeName,
                auditLog,
                _promotedExampleStore.ReadAll().Count,
                CountReports()));
            DashboardGrid.ItemsSource = snapshot.Rows.Select(DashboardMetricDisplay.FromRow).ToList();
            DashboardSummaryText.Text = BuildDashboardSummary(snapshot);
            ShowFindings("Dashboard", snapshot.Findings);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            DashboardSummaryText.Text = ex.Message;
            DashboardGrid.ItemsSource = Array.Empty<DashboardMetricDisplay>();
            var error = new SafetyFinding("DASHBOARD_ERROR", SafetySeverity.High, ex.Message);
            ShowFindings("Dashboard", [error]);
        }
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

}

using System.Windows;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
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
        SelectMainTab(
            MainTabKey.Dashboard,
            "Dashboard",
            "NAVIGATION_DASHBOARD",
            "Dashboard 탭으로 이동했습니다. 앱 상태를 read-only로 표시합니다.");
        RefreshDashboard();
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

}

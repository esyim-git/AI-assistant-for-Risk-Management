internal static class UiContractTests
{
    internal static void Run(SmokeTestContext context)
    {
        var loadedRuleSet = RuleLoader.LoadDefault();
var policyLoadResult = PolicyLoader.LoadDefault();
var missingPolicyResult = PolicyLoader.LoadFromFile("config/missing_policy_smoke.json");
var settingsSnapshot = new SecuritySettingsSnapshotBuilder().Build(policyLoadResult, loadedRuleSet.RuleVersion, NoModelDraftService.ModeName);
context.AssertTrue(!settingsSnapshot.UsedFallback, "SecuritySettingsSnapshot should preserve policy load state");
context.AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Environment" && row.Name == "RuleVersion" && row.Value == loadedRuleSet.RuleVersion), "SecuritySettingsSnapshot should include rule version");
context.AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Environment" && row.Name == "LocalModelMode" && row.Value == NoModelDraftService.ModeName), "SecuritySettingsSnapshot should include local model mode");
context.AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Network" && row.Name == "AllowExternalApi" && row.Value == "False" && row.Meaning == "Blocked"), "SecuritySettingsSnapshot should show external API blocked");
context.AssertTrue(settingsSnapshot.Rows.Any(row => row.Section == "Sql" && row.Name == "AllowAutoExecute" && row.Value == "False"), "SecuritySettingsSnapshot should show SQL auto execution blocked");
context.AssertTrue(settingsSnapshot.Findings.Any(f => f.Code == "SETTINGS_POLICY_LOADED" && f.Severity == SafetySeverity.Info), "SecuritySettingsSnapshot should emit loaded finding");
var fallbackSettingsSnapshot = new SecuritySettingsSnapshotBuilder().Build(missingPolicyResult, loadedRuleSet.RuleVersion, NoModelDraftService.ModeName);
context.AssertTrue(fallbackSettingsSnapshot.UsedFallback && fallbackSettingsSnapshot.Findings.Any(f => f.Code == "SETTINGS_POLICY_FALLBACK"), "SecuritySettingsSnapshot should report fallback state");

var dashboardSnapshot = new DashboardSnapshotBuilder().Build(new DashboardSnapshotRequest(
    policyLoadResult,
    loadedRuleSet.RuleVersion,
    NoModelDraftService.ModeName,
    new AuditLogReadResult(Array.Empty<AuditLogRecord>(), Array.Empty<SafetyFinding>()),
    PromotedExampleCount: 2,
    ReportCount: 3));
context.AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Offline Mode" && row.Value == "Enabled"), "DashboardSnapshot should show offline mode enabled");
context.AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Local Model" && row.Value == NoModelDraftService.ModeName), "DashboardSnapshot should show local model mode");
context.AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "RuleVersion" && row.Value == loadedRuleSet.RuleVersion), "DashboardSnapshot should show rule version");
context.AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Promoted Examples" && row.Value.Trim() == "2"), "DashboardSnapshot should show promoted example count");
context.AssertTrue(dashboardSnapshot.Rows.Any(row => row.Metric == "Reports" && row.Value.Trim() == "3"), "DashboardSnapshot should show report count");
context.AssertTrue(dashboardSnapshot.Findings.Any(f => f.Code == "DASHBOARD_READY" && f.Severity == SafetySeverity.Info), "DashboardSnapshot should emit ready finding");
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
context.AssertTrue(expectedMenuButtons.All(menuButtonClicks.ContainsKey), "UI shell should include all left menu buttons");
context.AssertTrue(menuButtonClicks.Values.All(click => !string.IsNullOrWhiteSpace(click)), "Left menu buttons should be wired to click handlers");

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
    context.AssertTrue(menuButtonClicks.TryGetValue(label, out var actualHandler) && actualHandler == handler, $"Left menu {label} should use {handler}");
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
context.AssertTrue(tabNamesByHeader.Count == expectedTabNames.Count && tabNamesByHeader.Values.All(name => !string.IsNullOrWhiteSpace(name)), "Main tabs should all have stable x:Name values");

foreach (var (header, tabName) in expectedTabNames)
{
    context.AssertTrue(tabNamesByHeader.TryGetValue(header, out var actualTabName) && actualTabName == tabName, $"Main tab {header} should be named {tabName}");
}

context.AssertTrue(!mainWindowCode.Contains("MainTabs.SelectedIndex", StringComparison.Ordinal), "UI navigation should not depend on TabControl indexes");
context.AssertTrue(mainWindowCode.Contains("MainTabs.SelectedItem = tab;", StringComparison.Ordinal), "UI navigation should select stable TabItem instances");
context.AssertTrue(!mainWindowCode.Contains("1.1m", StringComparison.Ordinal), "WP-01 should remove synthetic 1.1x limit formula from UI code");
context.AssertTrue(!mainWindowCode.Contains("PROFILE_TOTAL", StringComparison.Ordinal), "WP-01 should not emit aggregate synthetic limit rows");
context.AssertTrue(mainWindowCode.Contains("LIMIT_DATA_REQUIRED", StringComparison.Ordinal), "WP-01 should emit LIMIT_DATA_REQUIRED when real limit data is missing");
context.AssertTrue(mainWindowCode.Contains("DEMO_ONLY", StringComparison.Ordinal), "WP-01 should mark sample/demo report flows as DEMO_ONLY");
context.AssertTrue(!mainWindowCode.Contains("BuildUiLimitRows", StringComparison.Ordinal), "WP-07 should remove BuildUiLimitRows from UI code");
context.AssertTrue(!mainWindowCode.Contains("ExcelReportLimitRow", StringComparison.Ordinal), "WP-07 should remove ExcelReportLimitRow from UI code");
var retiredStubCodes = new[]
{
    "DASHBOARD_MVP_STATUS",
    "RISK_DASHBOARD_MVP_STATUS",
    "HISTORY_NOT_IMPLEMENTED",
    "SETTINGS_NOT_IMPLEMENTED"
};
context.AssertTrue(retiredStubCodes.All(code => !mainWindowCode.Contains(code, StringComparison.Ordinal)), "MVP-3 target screens should not use retired stub findings");
context.AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "한도 점검", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRunLimitMonitor", StringComparison.Ordinal)),
    "Risk Dashboard should expose a limit monitoring action");
context.AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "로그 새로고침", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRefreshHistory", StringComparison.Ordinal)),
    "History should expose a read-only refresh action");
context.AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "설정 새로고침", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnRefreshSettings", StringComparison.Ordinal)),
    "Settings should expose a view-only refresh action");
context.AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "승인 승격", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnPromoteFeedbackExample", StringComparison.Ordinal)),
    "Feedback Center should expose an approval promotion action");
context.AssertTrue(
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
    context.AssertTrue(mainWindowCode.Contains($"[MainTabKey.{tabKey}] = {tabName}", StringComparison.Ordinal), $"MainTabKey.{tabKey} should map to {tabName}");
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
    context.AssertTrue(methodBody.Contains("SelectMainTab", StringComparison.Ordinal) && methodBody.Contains(tabKey, StringComparison.Ordinal), $"Left menu handler {handler} should select {tabKey}");
}
    }
}
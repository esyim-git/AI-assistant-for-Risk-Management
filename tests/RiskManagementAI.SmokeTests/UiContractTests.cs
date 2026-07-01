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
    mainWindowCode.Contains("DuplicateLimitCount", StringComparison.Ordinal)
        && mainWindowCode.Contains("DUPLICATE_LIMIT", StringComparison.Ordinal),
    "Risk Dashboard result summary should expose duplicate limit count");
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

// STAB-UX-01: Resizable editor layout contract (XAML-level assertions, no WPF compile required).
var mainWindowXamlText = File.ReadAllText(Path.Combine("src", "RiskManagementAI.App", "MainWindow.xaml"));

bool ResizableEditorOk(string editorName)
{
    var box = mainWindowXaml.Descendants(wpf + "TextBox")
        .FirstOrDefault(t => (string?)t.Attribute(xaml + "Name") == editorName);
    return box != null
        && (string?)box.Attribute("HorizontalAlignment") == "Stretch"
        && (string?)box.Attribute("VerticalAlignment") == "Stretch"
        && (string?)box.Attribute("AcceptsTab") == "True"
        && (string?)box.Attribute("FontFamily") == "Consolas"
        && (string?)box.Attribute("FontSize") == "14";
}

context.AssertTrue(mainWindowXaml.Descendants(wpf + "GridSplitter").Count() >= 2, "UI layout should provide editor/result and workspace GridSplitters");
context.AssertTrue(!mainWindowXamlText.Contains("<RowDefinition Height=\"260\"", StringComparison.Ordinal) && mainWindowXamlText.Contains("Height=\"2*\"", StringComparison.Ordinal) && mainWindowXamlText.Contains("MinHeight=\"260\"", StringComparison.Ordinal), "UI layout editor region should be resizable not a fixed 260 height");
context.AssertTrue(mainWindowXamlText.Contains("MinWidth=\"1180\"", StringComparison.Ordinal) && mainWindowXamlText.Contains("MinHeight=\"720\"", StringComparison.Ordinal), "UI window should set MinWidth and MinHeight for the resizable shell");
context.AssertTrue(ResizableEditorOk("SqlRequestBox"), "UI SQL editor box should stretch with Consolas font");
context.AssertTrue(ResizableEditorOk("VbaRequestBox"), "UI VBA editor box should stretch with Consolas font");
context.AssertTrue(ResizableEditorOk("ExcelRequestBox"), "UI Excel editor box should stretch with Consolas font");
context.AssertTrue(
    new[] { "ExcelFunctionHelperQueryBox", "ExcelFunctionHelperList", "ExcelFunctionHelperDetailBox", "InsertExcelFunctionExampleButton" }
        .All(name => mainWindowXamlText.Contains($"x:Name=\"{name}\"", StringComparison.Ordinal)),
    "UI UX-WP-04 should expose stable Excel Function Helper controls in the Excel tab");
context.AssertTrue(
    mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute("Content"), "검색", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnSearchExcelFunctionHelper", StringComparison.Ordinal))
    && mainWindowXaml.Descendants(wpf + "Button").Any(button =>
        string.Equals((string?)button.Attribute(xaml + "Name"), "InsertExcelFunctionExampleButton", StringComparison.Ordinal)
        && string.Equals((string?)button.Attribute("Click"), "OnInsertExcelFunctionExample", StringComparison.Ordinal)),
    "UI UX-WP-04 Excel Function Helper should wire search and explicit insert buttons");
context.AssertTrue(
    mainWindowCode.Contains("_excelFunctionHelper = new ExcelFunctionHelper(_ruleSet)", StringComparison.Ordinal)
    && mainWindowCode.Contains("RefreshExcelFunctionHelper(showFindings: false)", StringComparison.Ordinal)
    && mainWindowCode.Contains("InsertExcelFunctionExample(ExcelRequestBox", StringComparison.Ordinal),
    "UI UX-WP-04 Excel Function Helper should be backed by Core helper and explicit user-selected insertion");
context.AssertTrue(
    !mainWindowXamlText.Contains("TextChanged=\"OnSearchExcelFunctionHelper\"", StringComparison.Ordinal)
    && mainWindowCode.Contains("검색어 원문은 로그에 저장하지 않았습니다.", StringComparison.Ordinal),
    "UI UX-WP-04 Excel Function Helper should avoid as-you-type search logging and avoid plaintext query logs");
context.AssertTrue(mainWindowXamlText.Contains("MinWidth=\"280\"", StringComparison.Ordinal) && mainWindowXamlText.Contains("MaxWidth=\"560\"", StringComparison.Ordinal), "UI Safety panel column should set MinWidth and MaxWidth bounds");
context.AssertTrue(mainWindowXamlText.Contains("x:Name=\"EditorRow\"", StringComparison.Ordinal) && mainWindowXamlText.Contains("x:Name=\"ResultRow\"", StringComparison.Ordinal), "UI layout should name editor/result rows for persistence");
context.AssertTrue(mainWindowXamlText.Contains("x:Name=\"SafetyPanelColumn\"", StringComparison.Ordinal), "UI layout should name Safety column for persistence");
context.AssertTrue(mainWindowCode.Contains("UiLayoutStore.Load()", StringComparison.Ordinal) && mainWindowCode.Contains("UiLayoutStore.Save", StringComparison.Ordinal), "UI layout should load and save persisted layout through Core store");

var originalCurrentDirectory = Directory.GetCurrentDirectory();
var layoutTempDirectory = Path.Combine(Path.GetTempPath(), "rmai_ui_layout_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(layoutTempDirectory);
try
{
    Directory.SetCurrentDirectory(layoutTempDirectory);
    var defaultLayout = UiLayoutStore.Load();
    context.AssertTrue(defaultLayout == UiLayoutStore.Default, "UI layout missing file should fallback to default layout");

    var savedLayout = new UiLayout(1280, 800, 3, 2, 420, UiLayoutStore.CurrentSchemaVersion);
    UiLayoutStore.Save(savedLayout);
    context.AssertTrue(UiLayoutStore.Load() == savedLayout, "UI layout store should round-trip saved layout values");

    File.WriteAllText(UiLayoutStore.DefaultPath, "{ broken json");
    context.AssertTrue(UiLayoutStore.Load() == UiLayoutStore.Default, "UI layout corrupt JSON should fallback to default layout");

    File.WriteAllText(
        UiLayoutStore.DefaultPath,
        System.Text.Json.JsonSerializer.Serialize(new UiLayout(1280, 800, 3, 2, 420, 999)));
    context.AssertTrue(UiLayoutStore.Load() == UiLayoutStore.Default, "UI layout schema mismatch should fallback to default layout");

    File.WriteAllText(
        UiLayoutStore.DefaultPath,
        System.Text.Json.JsonSerializer.Serialize(new UiLayout(100, 100, 0, -1, 9999, UiLayoutStore.CurrentSchemaVersion)));
    var clampedLayout = UiLayoutStore.Load();
    context.AssertTrue(clampedLayout.WindowWidth == 1180 && clampedLayout.WindowHeight == 720 && clampedLayout.EditorRowStar > 0 && clampedLayout.ResultRowStar > 0 && clampedLayout.SafetyColumnWidth == 560, "UI layout load should clamp window, star, and Safety column bounds");

    context.AssertTrue(context.Throws<ArgumentException>(() => UiLayoutStore.Load("../ui_layout.local.json")), "UI layout store should reject traversal paths");
    context.AssertTrue(context.Throws<ArgumentException>(() => UiLayoutStore.Load(Path.Combine(Path.GetTempPath(), "ui_layout.local.json"))), "UI layout store should reject rooted paths");
    context.AssertTrue(context.Throws<ArgumentException>(() => UiLayoutStore.Load("logs/ui_layout.local.json")), "UI layout store should reject non-config paths");
}
finally
{
    Directory.SetCurrentDirectory(originalCurrentDirectory);
    try
    {
        Directory.Delete(layoutTempDirectory, recursive: true);
    }
    catch (Exception cleanupEx) when (cleanupEx is IOException or UnauthorizedAccessException)
    {
        // Best-effort cleanup of temp layout files.
    }
}

context.AssertTrue(!IntegrityVerifier.MandatoryEntries.Contains(UiLayoutStore.DefaultPath, StringComparer.Ordinal) && !IntegrityVerifier.RequiredCriticalEntries.Contains(UiLayoutStore.DefaultPath, StringComparer.Ordinal), "UI layout local file should stay outside integrity mandatory and critical inventories");
context.AssertTrue(File.ReadAllText(".gitignore").Contains("config/ui_layout.local.json", StringComparison.Ordinal), "UI layout local file should be ignored by git");

// UX-WP-03: Smart Assist WPF popup contract.
var completionPopupXamlPath = Path.Combine("src", "RiskManagementAI.App", "Controls", "CompletionPopup.xaml");
var completionPopupCodePath = Path.Combine("src", "RiskManagementAI.App", "Controls", "CompletionPopup.xaml.cs");
var completionPopupXaml = XDocument.Load(completionPopupXamlPath);
var completionPopupXamlText = File.ReadAllText(completionPopupXamlPath);
var completionPopupCode = File.ReadAllText(completionPopupCodePath);
var completionEditorNames = new[] { "SqlRequestBox", "VbaRequestBox", "ExcelRequestBox", "RiskCommentRequestBox" };
context.AssertTrue(
    completionEditorNames.All(name => mainWindowXaml.Descendants(wpf + "TextBox").Any(t => (string?)t.Attribute(xaml + "Name") == name)),
    "Assist completion UI should expose SQL/VBA/Excel/RiskComment input boxes");
context.AssertTrue(ResizableEditorOk("RiskCommentRequestBox"), "Assist completion UI RiskComment box should stretch with Consolas font");
context.AssertTrue(
    mainWindowCode.Contains("RegisterCompletionTextBox(SqlRequestBox, CompletionLanguage.Sql)", StringComparison.Ordinal)
    && mainWindowCode.Contains("RegisterCompletionTextBox(VbaRequestBox, CompletionLanguage.Vba)", StringComparison.Ordinal)
    && mainWindowCode.Contains("RegisterCompletionTextBox(ExcelRequestBox, CompletionLanguage.Excel)", StringComparison.Ordinal)
    && mainWindowCode.Contains("RegisterCompletionTextBox(RiskCommentRequestBox, CompletionLanguage.RiskComment)", StringComparison.Ordinal),
    "Assist completion UI should map four input boxes to completion languages");
context.AssertTrue(
    completionPopupXaml.Descendants(wpf + "Popup").Any()
    && completionPopupXaml.Descendants(wpf + "ListBox").Any(),
    "Assist completion popup should use WPF Popup and ListBox only");
context.AssertTrue(
    completionPopupXamlText.Contains("{Binding Source}", StringComparison.Ordinal)
    && completionPopupXamlText.Contains("{Binding KindLabel}", StringComparison.Ordinal)
    && completionPopupXamlText.Contains("{Binding ReviewLabel}", StringComparison.Ordinal)
    && completionPopupXamlText.Contains("{Binding PreviewText}", StringComparison.Ordinal)
    && completionPopupXamlText.Contains("{Binding SafetyText}", StringComparison.Ordinal)
    && completionPopupXamlText.Contains("{Binding InsertabilityLabel}", StringComparison.Ordinal),
    "Assist completion popup should display Source KindLabel ReviewLabel PreviewText SafetyText and insertability fields");
context.AssertTrue(
    completionPopupCode.Contains("CompletionDisplayFormatter.FromItems", StringComparison.Ordinal)
    && completionPopupCode.Contains("CompletionDisplayInfo)?.Item", StringComparison.Ordinal),
    "Assist completion popup display should wrap items for rendering while accepting the original CompletionItem");
context.AssertTrue(
    mainWindowCode.Contains("Key.Space", StringComparison.Ordinal)
    && mainWindowCode.Contains("Key.Enter", StringComparison.Ordinal)
    && mainWindowCode.Contains("Key.Tab", StringComparison.Ordinal)
    && mainWindowCode.Contains("Key.Escape", StringComparison.Ordinal),
    "Assist completion UI should wire Ctrl+Space Enter Tab Esc keys");
context.AssertTrue(
    mainWindowCode.Contains("TextChanged += OnCompletionTextBoxTextChanged", StringComparison.Ordinal)
    && mainWindowCode.Contains("DispatcherTimer", StringComparison.Ordinal)
    && mainWindowCode.Contains("CompletionTriggerPolicy.EvaluateAsYouType", StringComparison.Ordinal),
    "Assist as-you-type UI should use debounced TextChanged and Core trigger policy");
context.AssertTrue(
    completionPopupCode.Contains("Show(TextBox placementTarget, IReadOnlyList<CompletionItem> items, bool grabFocus = true)", StringComparison.Ordinal)
    && completionPopupCode.Contains("if (RootPopup.IsOpen && grabFocus)", StringComparison.Ordinal)
    && mainWindowCode.Contains("grabFocus: false", StringComparison.Ordinal),
    "Assist as-you-type popup should preserve TextBox focus while Ctrl+Space can still grab focus");
context.AssertTrue(
    completionPopupCode.Contains("MoveSelection(int delta)", StringComparison.Ordinal)
    && mainWindowCode.Contains("CompletionPopupControl.MoveSelection", StringComparison.Ordinal)
    && mainWindowCode.Contains("Key.Down", StringComparison.Ordinal)
    && mainWindowCode.Contains("Key.Up", StringComparison.Ordinal),
    "Assist as-you-type UI should support TextBox-owned Up and Down popup selection");
context.AssertTrue(
    mainWindowCode.Contains("_suppressCompletionTextChanged = true", StringComparison.Ordinal)
    && mainWindowCode.Contains("InsertCompletionText(_completionTargetBox, item.InsertText)", StringComparison.Ordinal)
    && mainWindowCode.Contains("CompletionPopupControl.Close()", StringComparison.Ordinal),
    "Assist as-you-type UI should suppress programmatic edits and close stale popup states");
context.AssertTrue(
    mainWindowCode.Contains("if (!item.Insertable)", StringComparison.Ordinal)
    && mainWindowCode.Contains("안전 힌트는 정보 표시 전용", StringComparison.Ordinal),
    "Assist completion UI should not insert SafetyHint or BlockedHint items");
context.AssertTrue(
    mainWindowCode.Contains("InsertCompletionText(_completionTargetBox, item.InsertText)", StringComparison.Ordinal)
    && mainWindowCode.Contains("TryAcceptSelected", StringComparison.Ordinal)
    && mainWindowCode.Contains("OnCompletionPopupItemAccepted", StringComparison.Ordinal)
    && mainWindowCode.Contains("_suppressCompletionTextChanged = true", StringComparison.Ordinal),
    "Assist completion UI should insert only on explicit selected completion accept");
context.AssertTrue(
    mainWindowCode.Contains("SuggestionLogEntry.FromAcceptedItem", StringComparison.Ordinal)
    && mainWindowCode.Contains("LogHash.Sha256Hex(Environment.UserName)", StringComparison.Ordinal)
    && !completionPopupCode.Contains("File.AppendAllText", StringComparison.Ordinal),
    "Assist completion UI should audit accepted suggestions through hash-only SuggestionLogWriter");
context.AssertTrue(
    mainWindowCode.Contains("ShowFindings(\"Smart Assist\", _completionResult.Findings)", StringComparison.Ordinal)
    && mainWindowCode.Contains("item.Finding", StringComparison.Ordinal),
    "Assist completion UI should pass structured safety findings to the result panel");
    }
}

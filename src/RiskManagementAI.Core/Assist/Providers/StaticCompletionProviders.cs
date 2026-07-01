using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Assist.Providers;

public sealed class SqlCompletionProvider : ICompletionProvider
{
    public const string Id = "static-sql";

    private static readonly CompletionSeed[] Seeds =
    [
        new("SELECT", "SELECT ", CompletionItemKind.Keyword, 100),
        new("FROM", "FROM ", CompletionItemKind.Keyword, 110),
        new("WHERE", "WHERE ", CompletionItemKind.Keyword, 120),
        new("JOIN", "JOIN ", CompletionItemKind.Keyword, 130),
        new("ON", "ON ", CompletionItemKind.Keyword, 140),
        new("GROUP BY", "GROUP BY ", CompletionItemKind.Keyword, 150),
        new("HAVING", "HAVING ", CompletionItemKind.Keyword, 160),
        new("ORDER BY", "ORDER BY ", CompletionItemKind.Keyword, 170),
        new("AS", "AS ", CompletionItemKind.Keyword, 180),
        new("SELECT filtered rows", "SELECT\n    BASE_DT,\n    PORTFOLIO_ID,\n    RISK_FACTOR,\n    EXPOSURE_AMT\nFROM <TABLE_NAME>\nWHERE BASE_DT = :BASE_DT;\n", CompletionItemKind.Snippet, 300),
        new("SELECT grouped exposure", "SELECT\n    BASE_DT,\n    PORTFOLIO_ID,\n    RISK_FACTOR,\n    SUM(EXPOSURE_AMT) AS EXPOSURE_AMT\nFROM <TABLE_NAME>\nWHERE BASE_DT = :BASE_DT\nGROUP BY BASE_DT, PORTFOLIO_ID, RISK_FACTOR;\n", CompletionItemKind.Snippet, 310),
        new("SELECT limit usage ratio", "SELECT\n    e.BASE_DT,\n    e.PORTFOLIO_ID,\n    e.RISK_FACTOR,\n    e.EXPOSURE_AMT,\n    l.LIMIT_AMT,\n    ABS(e.EXPOSURE_AMT) / NULLIF(l.LIMIT_AMT, 0) AS USAGE_RATIO\nFROM <EXPOSURE_TABLE> e\nJOIN <LIMIT_TABLE> l\n  ON e.BASE_DT = l.BASE_DT\n AND e.PORTFOLIO_ID = l.PORTFOLIO_ID\n AND e.RISK_FACTOR = l.RISK_FACTOR\nWHERE e.BASE_DT = :BASE_DT\n  AND l.USE_YN = 'Y';\n", CompletionItemKind.Snippet, 320),
        new("SELECT prior-day exposure delta", "SELECT\n    cur.PORTFOLIO_ID,\n    cur.RISK_FACTOR,\n    cur.EXPOSURE_AMT AS CURRENT_EXPOSURE_AMT,\n    prev.EXPOSURE_AMT AS PRIOR_EXPOSURE_AMT,\n    cur.EXPOSURE_AMT - prev.EXPOSURE_AMT AS EXPOSURE_DELTA\nFROM <EXPOSURE_TABLE> cur\nJOIN <EXPOSURE_TABLE> prev\n  ON cur.PORTFOLIO_ID = prev.PORTFOLIO_ID\n AND cur.RISK_FACTOR = prev.RISK_FACTOR\nWHERE cur.BASE_DT = :CURRENT_BASE_DT\n  AND prev.BASE_DT = :PRIOR_BASE_DT;\n", CompletionItemKind.Snippet, 330),
        new("SELECT duplicate join keys", "SELECT\n    BASE_DT,\n    PORTFOLIO_ID,\n    RISK_FACTOR,\n    COUNT(*) AS ROW_COUNT\nFROM <TABLE_NAME>\nWHERE BASE_DT = :BASE_DT\nGROUP BY BASE_DT, PORTFOLIO_ID, RISK_FACTOR\nHAVING COUNT(*) > 1;\n", CompletionItemKind.Snippet, 340)
    ];

    private readonly SqlSafetyChecker checker;

    public SqlCompletionProvider()
        : this(RuleLoader.LoadDefault())
    {
    }

    public SqlCompletionProvider(SafetyRuleSet ruleSet)
    {
        checker = new SqlSafetyChecker(ruleSet);
    }

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language == CompletionLanguage.Sql;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        var items = new List<CompletionItem>();
        items.AddRange(checker.Check(context.Text)
            .Where(finding => finding.Severity == SafetySeverity.Blocker)
            .Select((finding, index) => CompletionProviderItems.Blocked(
                $"Blocked SQL: {finding.Code}",
                finding,
                "조회 전용 SQL만 추천합니다.",
                index)));

        items.AddRange(CompletionProviderItems.FromSeeds(Seeds, context.Prefix));
        return items;
    }
}

public sealed class VbaCompletionProvider : ICompletionProvider
{
    public const string Id = "static-vba";

    private static readonly CompletionSeed[] Seeds =
    [
        new("Option Explicit header", "Option Explicit\n\nPublic Sub Main()\n    On Error GoTo ErrHandler\n\nCleanExit:\n    Exit Sub\nErrHandler:\n    Resume CleanExit\nEnd Sub\n", CompletionItemKind.Snippet, 100),
        new("Application state restore", "Dim oldScreenUpdating As Boolean\nDim oldEnableEvents As Boolean\noldScreenUpdating = Application.ScreenUpdating\noldEnableEvents = Application.EnableEvents\nOn Error GoTo ErrHandler\nApplication.ScreenUpdating = False\nApplication.EnableEvents = False\n\nCleanExit:\n    Application.ScreenUpdating = oldScreenUpdating\n    Application.EnableEvents = oldEnableEvents\n    Exit Sub\nErrHandler:\n    Resume CleanExit\n", CompletionItemKind.Snippet, 110),
        new("Array row loop", "Dim values As Variant\nDim rowIndex As Long\nvalues = Range(\"A1\").CurrentRegion.Value\nFor rowIndex = 2 To UBound(values, 1)\n    ' Review each row before use.\nNext rowIndex\n", CompletionItemKind.Snippet, 120),
        new("Safe range to array", "Option Explicit\n\nPublic Sub ReviewRangeToArray()\n    On Error GoTo ErrHandler\n    Dim values As Variant\n    Dim rowIndex As Long\n    values = ActiveSheet.Range(\"A1\").CurrentRegion.Value\n    For rowIndex = 2 To UBound(values, 1)\n        ' 검토용 초안: 값 검증 후 사용합니다.\n    Next rowIndex\nCleanExit:\n    Exit Sub\nErrHandler:\n    Resume CleanExit\nEnd Sub\n", CompletionItemKind.Snippet, 130),
        new("Create review sheet", "Option Explicit\n\nPublic Sub CreateReviewSheet()\n    On Error GoTo ErrHandler\n    Dim reviewSheet As Worksheet\n    Set reviewSheet = ThisWorkbook.Worksheets.Add(After:=ThisWorkbook.Worksheets(ThisWorkbook.Worksheets.Count))\n    reviewSheet.Name = \"Review_Output\"\n    reviewSheet.Range(\"A1\").Value = \"검토용 초안\"\nCleanExit:\n    Exit Sub\nErrHandler:\n    Resume CleanExit\nEnd Sub\n", CompletionItemKind.Snippet, 140),
        new("Safe calculation state restore", "Option Explicit\n\nPublic Sub RunWithStateRestore()\n    Dim oldCalculation As XlCalculation\n    Dim oldScreenUpdating As Boolean\n    On Error GoTo ErrHandler\n    oldCalculation = Application.Calculation\n    oldScreenUpdating = Application.ScreenUpdating\n    Application.Calculation = xlCalculationManual\n    Application.ScreenUpdating = False\n\nCleanExit:\n    Application.Calculation = oldCalculation\n    Application.ScreenUpdating = oldScreenUpdating\n    Exit Sub\nErrHandler:\n    Resume CleanExit\nEnd Sub\n", CompletionItemKind.Snippet, 150)
    ];

    private readonly VbaSafetyChecker checker;

    public VbaCompletionProvider()
        : this(RuleLoader.LoadDefault())
    {
    }

    public VbaCompletionProvider(SafetyRuleSet ruleSet)
    {
        checker = new VbaSafetyChecker(ruleSet);
    }

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language == CompletionLanguage.Vba;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        var items = new List<CompletionItem>();
        items.AddRange(checker.Check(context.Text)
            .Where(finding => finding.Severity is SafetySeverity.High or SafetySeverity.Blocker)
            .Select((finding, index) => CompletionProviderItems.Blocked(
                $"Blocked VBA: {finding.Code}",
                finding,
                "Excel 2021 안전 VBA 패턴만 추천합니다.",
                index)));

        items.AddRange(CompletionProviderItems.FromSeeds(Seeds, context.Prefix));
        return items;
    }
}

public sealed class Excel2021CompletionProvider : ICompletionProvider, ICompletionProviderWarningSource
{
    public const string Id = "static-excel-2021";

    private readonly IReadOnlyList<string> allowFunctions;
    private readonly IReadOnlyList<string> skippedAllowFunctionLabels;

    public Excel2021CompletionProvider()
        : this(RuleLoader.LoadDefault())
    {
    }

    public Excel2021CompletionProvider(SafetyRuleSet ruleSet)
    {
        var validFunctions = new List<string>();
        var skippedLabels = new List<string>();
        foreach (var functionName in ruleSet.ExcelCompletionAllowFunctions)
        {
            if (IsWorksheetFunctionName(functionName))
            {
                validFunctions.Add(functionName);
            }
            else if (!string.IsNullOrWhiteSpace(functionName))
            {
                skippedLabels.Add(functionName.Trim());
            }
        }

        allowFunctions = validFunctions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(functionName => functionName, StringComparer.Ordinal)
            .ToArray();
        skippedAllowFunctionLabels = skippedLabels
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(label => label, StringComparer.Ordinal)
            .ToArray();
    }

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language == CompletionLanguage.Excel;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        return allowFunctions
            .Where(functionName => CompletionProviderItems.MatchesPrefix(functionName, context.Prefix))
            .Select((functionName, index) => new CompletionItem(
                functionName,
                functionName + "(",
                CompletionItemKind.Function,
                ProviderId,
                RequiresReview: true,
                Insertable: true,
                Finding: null,
                SafetyNote: "Excel 2021 호환 함수입니다.",
                SortKey: 100 + index))
            .ToArray();
    }

    public IReadOnlyList<string> GetWarnings(CompletionContext context)
    {
        return skippedAllowFunctionLabels
            .Select(label => $"Excel completion allow-function skipped: {label}")
            .ToArray();
    }

    private static bool IsWorksheetFunctionName(string functionName)
    {
        return !string.IsNullOrWhiteSpace(functionName)
            && functionName.All(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9' or '.');
    }
}

public sealed class Excel365BlockedHintProvider : ICompletionProvider
{
    public const string Id = "static-excel-365-blocked";

    private readonly Excel2021FunctionChecker checker;

    public Excel365BlockedHintProvider()
        : this(RuleLoader.LoadDefault())
    {
    }

    public Excel365BlockedHintProvider(SafetyRuleSet ruleSet)
    {
        checker = new Excel2021FunctionChecker(ruleSet);
    }

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language == CompletionLanguage.Excel;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        return checker.CheckFormula(context.Text)
            .Where(finding => finding.Code == "EXCEL_365_FUNCTION")
            .Select((finding, index) => CompletionProviderItems.Blocked(
                $"Blocked Excel 365 function: {finding.MatchedText?.TrimEnd('(') ?? finding.Code}",
                finding,
                "Excel 2021 대체안으로 재작성해야 합니다.",
                index))
            .ToArray();
    }
}

public sealed class SafetyHintProvider : ICompletionProvider
{
    public const string Id = "static-safety-hint";

    private readonly SqlSafetyChecker sqlChecker;
    private readonly VbaSafetyChecker vbaChecker;
    private readonly Excel2021FunctionChecker excelChecker;

    public SafetyHintProvider()
        : this(RuleLoader.LoadDefault())
    {
    }

    public SafetyHintProvider(SafetyRuleSet ruleSet)
    {
        sqlChecker = new SqlSafetyChecker(ruleSet);
        vbaChecker = new VbaSafetyChecker(ruleSet);
        excelChecker = new Excel2021FunctionChecker(ruleSet);
    }

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language is CompletionLanguage.Sql or CompletionLanguage.Vba or CompletionLanguage.Excel;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
    {
        var findings = context.Language switch
        {
            CompletionLanguage.Sql => sqlChecker.Check(context.Text),
            CompletionLanguage.Vba => vbaChecker.Check(context.Text),
            CompletionLanguage.Excel => excelChecker.CheckFormula(context.Text),
            _ => Array.Empty<SafetyFinding>()
        };

        return findings
            .Where(finding => finding.Severity >= SafetySeverity.Medium)
            .Select((finding, index) => new CompletionItem(
                $"Safety hint: {finding.Code}",
                string.Empty,
                CompletionItemKind.SafetyHint,
                ProviderId,
                RequiresReview: true,
                Insertable: false,
                finding,
                finding.Message,
                10 + index))
            .ToArray();
    }
}

public sealed class RiskPhraseProvider : ICompletionProvider
{
    public const string Id = "static-risk-phrase";

    private static readonly CompletionSeed[] Seeds =
    [
        new("기준일 기준 노출 합계", "기준일 기준 노출 합계와 한도 사용률을 함께 확인했습니다.", CompletionItemKind.Phrase, 100),
        new("한도 초과 후속 점검", "한도 초과 항목은 원천 데이터와 승인 한도를 재대사한 뒤 후속 조치가 필요합니다.", CompletionItemKind.Phrase, 110),
        new("전일 대비 변동 확인", "전일 대비 변동이 큰 항목은 원인 분류와 승인 이력을 함께 검토해야 합니다.", CompletionItemKind.Phrase, 120),
        new("검토용 초안 문구", "본 문구는 검토용 초안이며 최종 판단 전 담당자 확인이 필요합니다.", CompletionItemKind.Phrase, 130),
        new("집중도 상승 검토용 초안", "검토용 초안: 특정 포트폴리오 또는 리스크 팩터 집중도가 상승해 한도 여유와 분산 필요성을 함께 확인해야 합니다.", CompletionItemKind.Phrase, 140),
        new("데이터 품질 검토용 초안", "검토용 초안: 입력 데이터의 기준일, 중복 키, 누락 한도 여부를 재확인한 뒤 수치 해석을 확정해야 합니다.", CompletionItemKind.Phrase, 150),
        new("준법 확인 검토용 초안", "검토용 초안: 관련 공개 기준과 내부 승인 절차에 맞는지 담당자가 최종 확인해야 합니다.", CompletionItemKind.Phrase, 160),
        new("대사 예외 검토용 초안", "검토용 초안: 대사 예외는 원천합계, 행 증폭, 통화 및 단위 차이를 분리해 원인별로 확인해야 합니다.", CompletionItemKind.Phrase, 170)
    ];

    public string ProviderId => Id;

    public bool Supports(CompletionLanguage language)
        => language == CompletionLanguage.RiskComment;

    public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
        => CompletionProviderItems.FromSeeds(Seeds, context.Prefix).ToArray();
}

public static class StaticCompletionProviderFactory
{
    public static IReadOnlyList<ICompletionProvider> CreateDefault(SafetyRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        return
        [
            new Excel2021CompletionProvider(ruleSet),
            new Excel365BlockedHintProvider(ruleSet),
            new RiskPhraseProvider(),
            new SafetyHintProvider(ruleSet),
            new SqlCompletionProvider(ruleSet),
            new VbaCompletionProvider(ruleSet)
        ];
    }
}

internal sealed record CompletionSeed(string Label, string InsertText, CompletionItemKind Kind, int SortKey);

internal static class CompletionProviderItems
{
    public static IEnumerable<CompletionItem> FromSeeds(IEnumerable<CompletionSeed> seeds, string prefix)
    {
        return seeds
            .Where(seed => MatchesPrefix(seed.Label, prefix))
            .Select(seed => new CompletionItem(
                seed.Label,
                seed.InsertText,
                seed.Kind,
                string.Empty,
                RequiresReview: true,
                Insertable: true,
                Finding: null,
                SafetyNote: null,
                SortKey: seed.SortKey));
    }

    public static CompletionItem Blocked(string label, SafetyFinding finding, string safetyNote, int index)
    {
        return new CompletionItem(
            label,
            string.Empty,
            CompletionItemKind.BlockedHint,
            string.Empty,
            RequiresReview: true,
            Insertable: false,
            Finding: finding,
            SafetyNote: safetyNote,
            SortKey: 1 + index);
    }

    public static bool MatchesPrefix(string label, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return true;
        }

        return label.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

internal static class AssistTests
{
    internal static void Run(SmokeTestContext context)
    {
        var registry = new CompletionProviderRegistry([
            new StaticAssistProvider(
                "sql-provider",
                CompletionLanguage.Sql,
                new CompletionItem("WHERE", "WHERE ", CompletionItemKind.Keyword, "wrong-source", RequiresReview: false, Insertable: false, null, null, 30),
                new CompletionItem("SELECT", "SELECT ", CompletionItemKind.Keyword, "wrong-source", RequiresReview: false, Insertable: false, null, null, 10),
                new CompletionItem("FROM", "FROM ", CompletionItemKind.Keyword, "wrong-source", RequiresReview: false, Insertable: false, null, null, 20),
                new CompletionItem("SELECT", "SELECT duplicate", CompletionItemKind.Keyword, "wrong-source", RequiresReview: false, Insertable: false, null, null, 99)),
            new StaticAssistProvider(
                "vba-provider",
                CompletionLanguage.Vba,
                new CompletionItem("Option Explicit", "Option Explicit", CompletionItemKind.Snippet, "vba-provider", RequiresReview: false, Insertable: false, null, null, 10))
        ]);

        var engine = new CompletionEngine(registry, maxInsertableItems: 2);
        var sqlContext = new CompletionContext(CompletionLanguage.Sql, "SEL", CaretIndex: 3, Prefix: "SEL", Mode: CompletionEngine.NoModelMode);
        var firstSql = engine.GetCompletions(sqlContext);
        var secondSql = engine.GetCompletions(sqlContext);
        context.AssertTrue(CompletionSignature(firstSql) == CompletionSignature(secondSql), "Assist completion engine should be deterministic for same context");
        context.AssertTrue(firstSql.Mode == CompletionEngine.NoModelMode && firstSql.Warnings.Count == 0, "Assist completion engine should run in NoModel mode");
        context.AssertTrue(firstSql.Items.Count == 2 && firstSql.Items.All(item => item.Source == "sql-provider"), "Assist completion engine should cap insertable recommendations");
        context.AssertTrue(firstSql.Items.All(item => item.RequiresReview), "Assist completion items should always require review");
        context.AssertTrue(firstSql.Items.All(item => item.Insertable), "Assist completion normal items should be insertable");
        context.AssertTrue(firstSql.Items.Select(item => item.Label).SequenceEqual(["SELECT", "FROM"]), "Assist completion engine should sort and dedupe by provider label");
        context.AssertTrue(firstSql.Items[0].InsertText == "SELECT ", "Assist completion dedupe should keep deterministic first item");

        var vbaResult = engine.GetCompletions(new CompletionContext(CompletionLanguage.Vba, "Opt", 3, "Opt", CompletionEngine.NoModelMode));
        context.AssertTrue(vbaResult.Items.Count == 1 && vbaResult.Items[0].Source == "vba-provider", "Assist provider registry should route by language");
        context.AssertTrue(context.Throws<ArgumentException>(() => registry.Register(new StaticAssistProvider("sql-provider", CompletionLanguage.Excel))), "Assist provider registry should reject duplicate provider ids");

        var emptyResult = new CompletionEngine(new CompletionProviderRegistry()).GetCompletions(
            new CompletionContext(CompletionLanguage.RiskComment, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode));
        context.AssertTrue(emptyResult.Mode == CompletionEngine.NoModelMode && emptyResult.Items.Count == 0, "Assist completion engine should work without model or providers");

        context.AssertTrue(
            CompletionTriggerPolicy.EvaluateAsYouType(CompletionLanguage.Sql, "SE", "SE", matchCount: 1, suppressed: false).ShouldShow,
            "Assist as-you-type completion trigger should show when prefix threshold and matches are satisfied");
        context.AssertTrue(
            !CompletionTriggerPolicy.EvaluateAsYouType(CompletionLanguage.Sql, "S", "S", matchCount: 1, suppressed: false).ShouldShow,
            "Assist as-you-type completion trigger should close when prefix is below threshold");
        context.AssertTrue(
            !CompletionTriggerPolicy.EvaluateAsYouType(CompletionLanguage.Sql, "SE", "SE", matchCount: 0, suppressed: false).ShouldShow,
            "Assist as-you-type completion trigger should close when no matches remain");
        context.AssertTrue(
            !CompletionTriggerPolicy.EvaluateAsYouType(CompletionLanguage.Sql, "SE", "SE", matchCount: 1, suppressed: true).ShouldShow,
            "Assist as-you-type completion trigger should stay closed during programmatic edit suppression");
        context.AssertTrue(
            !CompletionTriggerPolicy.EvaluateAsYouType((CompletionLanguage)999, "SE", "SE", matchCount: 1, suppressed: false).ShouldShow
            && !CompletionTriggerPolicy.EvaluateAsYouType(CompletionLanguage.Excel, string.Empty, string.Empty, matchCount: 1, suppressed: false).ShouldShow,
            "Assist as-you-type completion trigger should reject unsupported language and empty text");
        context.AssertTrue(
            CompletionTriggerPolicy.ShouldShowExplicitInvocation(matchCount: 1)
            && !CompletionTriggerPolicy.ShouldShowExplicitInvocation(matchCount: 0),
            "Assist explicit Ctrl+Space completion trigger should ignore prefix threshold and depend only on matches");

        var displaySnippet = CompletionDisplayFormatter.FromItem(new CompletionItem(
            "safe snippet",
            "Line1\nLine2",
            CompletionItemKind.Snippet,
            "display-provider",
            RequiresReview: true,
            Insertable: true,
            null,
            "Review before use",
            10));
        context.AssertTrue(
            displaySnippet.KindLabel == "Snippet"
            && displaySnippet.PreviewText == "Line1 | Line2"
            && displaySnippet.SafetyText == "Review before use"
            && displaySnippet.InsertabilityLabel == "Insert on select",
            "Assist popup display formatter should expose snippet preview SafetyNote and insert-on-select label");

        var displayBlocked = CompletionDisplayFormatter.FromItem(new CompletionItem(
            "blocked delete",
            "DELETE FROM SAMPLE",
            CompletionItemKind.BlockedHint,
            "display-provider",
            RequiresReview: true,
            Insertable: false,
            new SafetyFinding("SQL_DML_DELETE", SafetySeverity.Blocker, "DELETE is blocked."),
            null,
            1));
        context.AssertTrue(
            displayBlocked.KindLabel == "Blocked Hint"
            && displayBlocked.PreviewText.Length == 0
            && displayBlocked.SafetyText.Contains("SQL_DML_DELETE", StringComparison.Ordinal)
            && displayBlocked.InsertabilityLabel == "Info only",
            "Assist popup display formatter should keep blocked hints non-insertable and surface structured finding text");

        var displayBlockedWithNote = CompletionDisplayFormatter.FromItem(new CompletionItem(
            "blocked delete with note",
            "DELETE FROM SAMPLE",
            CompletionItemKind.BlockedHint,
            "display-provider",
            RequiresReview: true,
            Insertable: false,
            new SafetyFinding("SQL_DML_DELETE", SafetySeverity.Blocker, "DELETE is blocked."),
            "조회 전용 SQL만 추천합니다.",
            1));
        context.AssertTrue(
            displayBlockedWithNote.SafetyText.StartsWith("SQL_DML_DELETE: DELETE is blocked.", StringComparison.Ordinal)
            && displayBlockedWithNote.SafetyText.Contains("조회 전용 SQL만 추천합니다.", StringComparison.Ordinal),
            "Assist popup display formatter should surface structured finding before generic SafetyNote for blocked hints");

        var blockedFinding = new SafetyFinding("SQL_BLOCKED_DELETE", SafetySeverity.Blocker, "Blocked statement", "DELETE", 0);
        var reviewFinding = new SafetyFinding("SQL_REVIEW_REQUIRED", SafetySeverity.Medium, "Review statement", null, 7);
        var safetyRegistry = new CompletionProviderRegistry([
            new StaticAssistProvider(
                "safety-provider",
                CompletionLanguage.Sql,
                new CompletionItem("normal-a", "A", CompletionItemKind.Keyword, "safety-provider", true, true, null, null, 10),
                new CompletionItem("normal-b", "B", CompletionItemKind.Keyword, "safety-provider", true, true, null, null, 20),
                new CompletionItem("blocked-delete", "DELETE FROM SAMPLE", CompletionItemKind.BlockedHint, "safety-provider", false, true, blockedFinding, "Blocked", 1),
                new CompletionItem("review-sql", "raw text should be cleared", CompletionItemKind.SafetyHint, "safety-provider", false, true, reviewFinding, "Review", 2))
        ]);
        var safetyResult = new CompletionEngine(safetyRegistry, maxInsertableItems: 1).GetCompletions(sqlContext);
        context.AssertTrue(safetyResult.Items.Count == 3 && safetyResult.Items.Count(item => item.Insertable) == 1, "Assist completion cap should preserve safety hints before insertable items");
        context.AssertTrue(safetyResult.Items.Take(2).All(item => !item.Insertable && item.InsertText.Length == 0 && item.Finding is not null), "Assist completion SafetyHint and BlockedHint should be non-insertable");
        context.AssertTrue(safetyResult.Findings.Count == 2 && safetyResult.Findings.Any(finding => finding.Code == blockedFinding.Code) && safetyResult.Findings.Any(finding => finding.Code == reviewFinding.Code), "Assist completion result findings should survive recommendation cap");
        context.AssertTrue(safetyResult.Items.All(item => item.RequiresReview), "Assist completion safety items should always require review");

        var kindDedupeFinding = new SafetyFinding("SQL_KIND_DEDUPE", SafetySeverity.Medium, "Review shared label completion.");
        var kindDedupeRegistry = new CompletionProviderRegistry([
            new StaticAssistProvider(
                "kind-dedupe-provider",
                CompletionLanguage.Sql,
                new CompletionItem("shared-label", "raw text should be cleared", CompletionItemKind.SafetyHint, "wrong-source", false, true, kindDedupeFinding, "Review", 1),
                new CompletionItem("shared-label", "SELECT ", CompletionItemKind.Keyword, "wrong-source", false, false, null, null, 2),
                new CompletionItem("shared-label", "SELECT duplicate", CompletionItemKind.Keyword, "wrong-source", false, false, null, null, 3))
        ]);
        var kindDedupeResult = new CompletionEngine(kindDedupeRegistry, maxInsertableItems: 10).GetCompletions(sqlContext);
        context.AssertTrue(
            kindDedupeResult.Items.Count(item => item.Label == "shared-label" && item.Kind == CompletionItemKind.SafetyHint) == 1
            && kindDedupeResult.Items.Count(item => item.Label == "shared-label" && item.Kind == CompletionItemKind.Keyword) == 1,
            "Assist completion dedupe should keep safety hint and insertable item when kind differs");
        context.AssertTrue(
            kindDedupeResult.Items.First().Kind == CompletionItemKind.SafetyHint
            && kindDedupeResult.Items.Count(item => item.Label == "shared-label" && item.Kind == CompletionItemKind.Keyword) == 1
            && kindDedupeResult.Findings.Any(finding => finding.Code == kindDedupeFinding.Code),
            "Assist completion dedupe should still collapse identical kind labels and preserve pinned findings");

        var samePinFinding = new SafetyFinding("SQL_SAME_PIN", SafetySeverity.Blocker, "Same finding should not render twice.", "DROP", 0);
        var safetyOnlyFinding = new SafetyFinding("SQL_SAFETY_ONLY", SafetySeverity.Medium, "Safety-only finding should remain pinned.", null, 4);
        var sameFindingPinRegistry = new CompletionProviderRegistry([
            new StaticAssistProvider(
                "same-finding-pin-provider",
                CompletionLanguage.Sql,
                new CompletionItem("blocked same finding", string.Empty, CompletionItemKind.BlockedHint, "wrong-source", false, true, samePinFinding, "Blocked", 1),
                new CompletionItem("safety same finding", "raw text should be cleared", CompletionItemKind.SafetyHint, "wrong-source", false, true, samePinFinding, "Review", 2),
                new CompletionItem("safety only finding", "raw text should be cleared", CompletionItemKind.SafetyHint, "wrong-source", false, true, safetyOnlyFinding, "Review", 3),
                new CompletionItem("insert-a", "A", CompletionItemKind.Keyword, "wrong-source", false, false, null, null, 10),
                new CompletionItem("insert-b", "B", CompletionItemKind.Keyword, "wrong-source", false, false, null, null, 20))
        ]);
        var sameFindingPinResult = new CompletionEngine(sameFindingPinRegistry, maxInsertableItems: 1).GetCompletions(sqlContext);
        context.AssertTrue(
            sameFindingPinResult.Items.Count(item => item.Finding == samePinFinding) == 1
            && sameFindingPinResult.Items.Any(item => item.Kind == CompletionItemKind.BlockedHint && item.Finding == samePinFinding)
            && sameFindingPinResult.Items.All(item => item.Finding != samePinFinding || item.Kind != CompletionItemKind.SafetyHint),
            "Assist completion should collapse duplicate SafetyHint when BlockedHint has the same finding");
        context.AssertTrue(
            sameFindingPinResult.Items.Any(item => item.Kind == CompletionItemKind.SafetyHint && item.Finding == safetyOnlyFinding)
            && sameFindingPinResult.Findings.Contains(samePinFinding)
            && sameFindingPinResult.Findings.Contains(safetyOnlyFinding),
            "Assist completion duplicate pin collapse should preserve safety-only pins and structured findings");
        context.AssertTrue(
            sameFindingPinResult.Items.Count == 3
            && sameFindingPinResult.Items.Count(item => item.Insertable) == 1
            && sameFindingPinResult.Items.Last().Label == "insert-a",
            "Assist completion duplicate pin collapse should preserve pinned priority and insertable cap");

        var nullFindingPinRegistry = new CompletionProviderRegistry([
            new StaticAssistProvider(
                "null-finding-pin-provider",
                CompletionLanguage.Sql,
                new CompletionItem("blocked fallback finding", string.Empty, CompletionItemKind.BlockedHint, "wrong-source", false, true, null, "Blocked", 1),
                new CompletionItem("safety fallback finding", string.Empty, CompletionItemKind.SafetyHint, "wrong-source", false, true, null, "Review", 2))
        ]);
        var nullFindingPinResult = new CompletionEngine(nullFindingPinRegistry, maxInsertableItems: 10).GetCompletions(sqlContext);
        context.AssertTrue(
            nullFindingPinResult.Items.Count(item => item.Kind == CompletionItemKind.BlockedHint) == 1
            && nullFindingPinResult.Items.Count(item => item.Kind == CompletionItemKind.SafetyHint) == 1
            && nullFindingPinResult.Findings.Count(finding => finding.Code == "COMPLETION_FINDING_REQUIRED") == 1,
            "Assist completion should keep null-finding guard fallback pins separate while deduping result findings");

        var ruleSet = RuleLoader.LoadDefault();
        var staticProviderRegistry = new CompletionProviderRegistry(StaticCompletionProviderFactory.CreateDefault(ruleSet));
        var staticProviderEngine = new CompletionEngine(staticProviderRegistry, maxInsertableItems: 50);

        var sqlProviderResult = staticProviderEngine.GetCompletions(new CompletionContext(
            CompletionLanguage.Sql,
            "DELETE FROM <TABLE_NAME> WHERE BASE_DT = :BASE_DT",
            46,
            "DEL",
            CompletionEngine.NoModelMode));
        context.AssertTrue(sqlProviderResult.Items.Any(item => item.Kind == CompletionItemKind.BlockedHint && item.Finding?.Code == "SQL_DML_DELETE"), "Assist SQL completion provider should return BlockedHint for DML");
        context.AssertTrue(sqlProviderResult.Items.Count(item => item.Finding?.Code == "SQL_DML_DELETE") == 1, "Assist SQL completion provider should collapse duplicate pinned finding rows for DML");
        context.AssertTrue(sqlProviderResult.Items.Where(item => item.Insertable).All(item => !ContainsAny(item.InsertText, "INSERT", "UPDATE", "DELETE", "MERGE", "CREATE", "ALTER", "DROP", "TRUNCATE", "GRANT", "REVOKE", "EXEC", "CALL", "COMMIT", "ROLLBACK")), "Assist SQL completion provider should not recommend DML or DDL text");
        context.AssertTrue(staticProviderEngine.GetCompletions(new CompletionContext(CompletionLanguage.Sql, "SEL", 3, "SEL", CompletionEngine.NoModelMode)).Items.Any(item => item.Label == "SELECT" && item.Insertable), "Assist SQL completion provider should recommend read-only SELECT keyword");

        var vbaProviderResult = staticProviderEngine.GetCompletions(new CompletionContext(
            CompletionLanguage.Vba,
            "Option Explicit\nSub Test()\nShell \"cmd.exe\"\nEnd Sub",
            53,
            "She",
            CompletionEngine.NoModelMode));
        context.AssertTrue(vbaProviderResult.Items.Any(item => item.Kind == CompletionItemKind.BlockedHint && item.Finding?.Code == "VBA_SHELL"), "Assist VBA completion provider should return BlockedHint for forbidden Shell API");
        context.AssertTrue(vbaProviderResult.Items.Where(item => item.Insertable).All(item => !ContainsAny(item.InsertText, "Shell", "WScript.Shell", "Kill", "FileSystemObject", "Declare PtrSafe", "Outlook.Application", "WinHttp", "MSXML2.XMLHTTP", "FollowHyperlink")), "Assist VBA completion provider should not recommend forbidden API text");

        var excelProvider = new Excel2021CompletionProvider(ruleSet);
        var excelItems = excelProvider.GetCompletions(new CompletionContext(CompletionLanguage.Excel, "=X", 2, "X", CompletionEngine.NoModelMode));
        context.AssertTrue(excelItems.Any(item => item.Label == "XLOOKUP" && item.InsertText == "XLOOKUP(" && item.Insertable), "Assist Excel completion provider should recommend Excel 2021 allowed functions");
        var nonFunctionLabels = new[] { "PivotTable", "HelperColumn", "VBA", "SQLAggregation" };
        context.AssertTrue(nonFunctionLabels.All(label => !ruleSet.ExcelCompletionAllowFunctions.Contains(label, StringComparer.OrdinalIgnoreCase)), "Assist Excel completion allow rules should exclude non-function labels");
        context.AssertTrue(excelProvider.GetCompletions(new CompletionContext(CompletionLanguage.Excel, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode)).All(item => !nonFunctionLabels.Contains(item.Label, StringComparer.OrdinalIgnoreCase)), "Assist Excel completion provider should not recommend non-function labels");
        var syntheticExcelRuleSet = ruleSet with { ExcelCompletionAllowFunctions = ["XLOOKUP", "HelperColumn"] };
        var syntheticExcelResult = new CompletionEngine(new CompletionProviderRegistry([new Excel2021CompletionProvider(syntheticExcelRuleSet)])).GetCompletions(
            new CompletionContext(CompletionLanguage.Excel, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode));
        context.AssertTrue(
            syntheticExcelResult.Items.Any(item => item.Label == "XLOOKUP" && item.Insertable)
            && syntheticExcelResult.Items.All(item => item.Label != "HelperColumn"),
            "Assist Excel completion provider should keep valid allow functions and skip invalid labels");
        context.AssertTrue(
            syntheticExcelResult.Warnings.Contains("Excel completion allow-function skipped: HelperColumn", StringComparer.Ordinal),
            "Assist Excel completion provider should surface invalid allow-function labels through result diagnostics");

        var excelBlockedProvider = new Excel365BlockedHintProvider(ruleSet);
        var blockedFunctionsCovered = ruleSet.ExcelBlockedFunctions
            .Where(functionName => excelBlockedProvider
                .GetCompletions(new CompletionContext(CompletionLanguage.Excel, $"={functionName}(A1:A3)", 0, functionName, CompletionEngine.NoModelMode))
                .Any(item => item.Kind == CompletionItemKind.BlockedHint && item.Finding?.Code == "EXCEL_365_FUNCTION"))
            .ToArray();
        context.AssertTrue(blockedFunctionsCovered.SequenceEqual(ruleSet.ExcelBlockedFunctions), "Assist Excel 365 blocked provider should stay in sync with RuleSet blocked functions");
        var excelBlockedResult = staticProviderEngine.GetCompletions(new CompletionContext(CompletionLanguage.Excel, "=TEXTSPLIT(A1,\",\")", 18, "TEXTSPLIT", CompletionEngine.NoModelMode));
        context.AssertTrue(excelBlockedResult.Items.Any(item => item.Kind == CompletionItemKind.BlockedHint && item.SafetyNote?.Contains("Excel 2021", StringComparison.OrdinalIgnoreCase) == true), "Assist Excel 365 blocked provider should emit Excel 2021 replacement guidance");

        var expectedSqlFinding = new SqlSafetyChecker(ruleSet)
            .Check("DELETE FROM <TABLE_NAME>")
            .First(finding => finding.Code == "SQL_DML_DELETE");
        var safetyHintProvider = new SafetyHintProvider(ruleSet);
        var safetyHintItems = safetyHintProvider.GetCompletions(new CompletionContext(CompletionLanguage.Sql, "DELETE FROM <TABLE_NAME>", 24, "DELETE", CompletionEngine.NoModelMode));
        context.AssertTrue(safetyHintItems.Any(item => item.Kind == CompletionItemKind.SafetyHint && !item.Insertable && item.InsertText.Length == 0 && item.Finding == expectedSqlFinding), "Assist SafetyHint provider should preserve checker structured finding");
        var safetyHintEngineResult = new CompletionEngine(new CompletionProviderRegistry([safetyHintProvider])).GetCompletions(new CompletionContext(CompletionLanguage.Sql, "DELETE FROM <TABLE_NAME>", 24, "DELETE", CompletionEngine.NoModelMode));
        context.AssertTrue(safetyHintEngineResult.Findings.Contains(expectedSqlFinding), "Assist SafetyHint provider should preserve finding in CompletionResult");
        context.AssertTrue(
            safetyHintProvider.GetCompletions(new CompletionContext(CompletionLanguage.Sql, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode)).Count == 0,
            "Assist SafetyHint provider should not pin low severity empty-input findings");
        context.AssertTrue(
            safetyHintProvider.GetCompletions(new CompletionContext(CompletionLanguage.Sql, "FROM <TABLE_NAME>", 17, "FROM", CompletionEngine.NoModelMode)).Any(item => item.Kind == CompletionItemKind.SafetyHint && item.Finding?.Code == "SQL_NO_SELECT"),
            "Assist SafetyHint provider should keep medium severity review findings pinned");

        foreach (var languageContext in new[]
        {
            new CompletionContext(CompletionLanguage.Sql, "DELETE FROM <TABLE_NAME>", 24, "DEL", CompletionEngine.NoModelMode),
            new CompletionContext(CompletionLanguage.Vba, "Option Explicit\nSub Test()\nShell \"cmd.exe\"\nEnd Sub", 53, "Opt", CompletionEngine.NoModelMode),
            new CompletionContext(CompletionLanguage.Excel, "=TEXTSPLIT(A1,\",\")", 18, "T", CompletionEngine.NoModelMode),
            new CompletionContext(CompletionLanguage.RiskComment, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode)
        })
        {
            context.AssertTrue(staticProviderEngine.GetCompletions(languageContext).Items.All(item => item.RequiresReview), $"Assist completion provider items should require review for {languageContext.Language}");
        }

        var riskPhraseItems = staticProviderEngine.GetCompletions(new CompletionContext(CompletionLanguage.RiskComment, string.Empty, 0, string.Empty, CompletionEngine.NoModelMode)).Items;
        context.AssertTrue(riskPhraseItems.Any(item => item.Kind == CompletionItemKind.Phrase && item.Insertable), "Assist RiskPhrase provider should recommend review-only risk phrases");
        var forbiddenRiskPhraseTerms = new[]
        {
            "pass" + "word",
            "Data" + " Source",
            "User" + " Id",
            "Initial" + " Catalog",
            "주민등록",
            "고객명",
            "계좌번호",
            "내부규정 원문"
        };
        context.AssertTrue(riskPhraseItems.All(item => !ContainsAny(item.Label + item.InsertText, forbiddenRiskPhraseTerms)), "Assist RiskPhrase provider should avoid real data and internal text seeds");

        var logPath = Path.Combine("logs", "smoke_assist_suggestion_log.jsonl");
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        var acceptedItem = new CompletionItem("SELECT", "SELECT BASE_DT", CompletionItemKind.Keyword, "sql-provider", true, true, null, null, 10);
        var rawUser = "assist-user";
        var userHash = LogHash.Sha256Hex(rawUser);
        var suggestionEntry = SuggestionLogEntry.FromAcceptedItem(
            acceptedItem,
            CompletionLanguage.Sql,
            userHash,
            new DateTime(2026, 06, 29, 0, 0, 0, DateTimeKind.Utc));
        var suggestionWriter = new SuggestionLogWriter("logs", "smoke_assist_suggestion_log.jsonl");
        suggestionWriter.Append(suggestionEntry);
        var suggestionLogText = File.ReadAllText(suggestionWriter.LogFilePath);
        context.AssertTrue(File.ReadAllLines(suggestionWriter.LogFilePath).Length == 1, "Assist suggestion audit should append one JSONL record");
        context.AssertTrue(suggestionEntry.SuggestionId == LogHash.Sha256Hex("sql-provider|SELECT"), "Assist suggestion audit should use provider label hash id");
        context.AssertTrue(suggestionLogText.Contains(suggestionEntry.InsertTextHash, StringComparison.Ordinal), "Assist suggestion audit should store InsertTextHash");
        context.AssertTrue(!suggestionLogText.Contains(acceptedItem.InsertText, StringComparison.Ordinal) && !suggestionLogText.Contains(rawUser, StringComparison.Ordinal), "Assist suggestion audit should not store raw insert text or user id");
        context.AssertTrue(context.Throws<ArgumentException>(() => suggestionWriter.Append(suggestionEntry with { UserHash = rawUser })), "Assist suggestion audit should reject raw user id");
        context.AssertTrue(context.Throws<ArgumentException>(() => suggestionWriter.Append(suggestionEntry with { InsertTextHash = acceptedItem.InsertText })), "Assist suggestion audit should reject raw insert text hash");
        context.AssertTrue(context.Throws<ArgumentException>(() => suggestionWriter.Append(suggestionEntry with { Kind = CompletionItemKind.BlockedHint })), "Assist suggestion audit should reject non-insertable hints");
        context.AssertTrue(context.Throws<ArgumentException>(() => SuggestionLogEntry.FromAcceptedItem(acceptedItem with { Insertable = false }, CompletionLanguage.Sql, userHash, DateTime.UtcNow)), "Assist suggestion audit should only accept insertable items");
        context.AssertTrue(context.Throws<ArgumentException>(() => new SuggestionLogWriter("logs/../reports", "bad.jsonl")), "Assist suggestion audit should reject paths outside logs");
    }

    private static string CompletionSignature(CompletionResult result)
    {
        return string.Join(
            "|",
            result.Items.Select(item => $"{item.Source}:{item.Label}:{item.InsertText}:{item.Kind}:{item.Insertable}:{item.RequiresReview}:{item.SortKey}"))
            + "::"
            + string.Join("|", result.Findings.Select(finding => $"{finding.Code}:{finding.Severity}:{finding.Position}"));
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StaticAssistProvider : ICompletionProvider
    {
        private readonly CompletionLanguage language;
        private readonly IReadOnlyList<CompletionItem> items;

        public StaticAssistProvider(string providerId, CompletionLanguage language, params CompletionItem[] items)
        {
            ProviderId = providerId;
            this.language = language;
            this.items = items;
        }

        public string ProviderId { get; }

        public bool Supports(CompletionLanguage language)
        {
            return this.language == language;
        }

        public IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
        {
            return items;
        }
    }
}

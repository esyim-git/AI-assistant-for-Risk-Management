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

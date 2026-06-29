using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Assist;

public enum CompletionLanguage
{
    Sql,
    Vba,
    Excel,
    RiskComment
}

public enum CompletionItemKind
{
    Keyword,
    Snippet,
    Function,
    Phrase,
    SafetyHint,
    BlockedHint
}

public sealed record CompletionContext(
    CompletionLanguage Language,
    string Text,
    int CaretIndex,
    string Prefix,
    string Mode);

public sealed record CompletionItem(
    string Label,
    string InsertText,
    CompletionItemKind Kind,
    string Source,
    bool RequiresReview,
    bool Insertable,
    SafetyFinding? Finding,
    string? SafetyNote,
    int SortKey);

public sealed record CompletionResult(
    IReadOnlyList<CompletionItem> Items,
    string Mode,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SafetyFinding> Findings);

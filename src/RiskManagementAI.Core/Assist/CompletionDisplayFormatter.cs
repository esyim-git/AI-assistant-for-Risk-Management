namespace RiskManagementAI.Core.Assist;

public sealed record CompletionDisplayInfo(
    CompletionItem Item,
    string Label,
    string Source,
    string KindLabel,
    string ReviewLabel,
    string InsertabilityLabel,
    string PreviewText,
    string SafetyText);

public static class CompletionDisplayFormatter
{
    private const int MaxPreviewLength = 120;

    public static IReadOnlyList<CompletionDisplayInfo> FromItems(IReadOnlyList<CompletionItem> items)
    {
        return items.Select(FromItem).ToArray();
    }

    public static CompletionDisplayInfo FromItem(CompletionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new CompletionDisplayInfo(
            item,
            item.Label,
            item.Source,
            KindLabel(item.Kind),
            item.RequiresReview ? "Review draft" : "Review state missing",
            item.Insertable ? "Insert on select" : "Info only",
            BuildPreviewText(item),
            BuildSafetyText(item));
    }

    private static string KindLabel(CompletionItemKind kind)
    {
        return kind switch
        {
            CompletionItemKind.Keyword => "Keyword",
            CompletionItemKind.Snippet => "Snippet",
            CompletionItemKind.Function => "Function",
            CompletionItemKind.Phrase => "Phrase",
            CompletionItemKind.SafetyHint => "Safety Hint",
            CompletionItemKind.BlockedHint => "Blocked Hint",
            _ => kind.ToString()
        };
    }

    private static string BuildPreviewText(CompletionItem item)
    {
        if (!item.Insertable || string.IsNullOrWhiteSpace(item.InsertText))
        {
            return string.Empty;
        }

        var normalized = NormalizeInline(item.InsertText);
        return normalized.Length <= MaxPreviewLength
            ? normalized
            : normalized[..MaxPreviewLength] + "...";
    }

    private static string BuildSafetyText(CompletionItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.SafetyNote))
        {
            return NormalizeInline(item.SafetyNote);
        }

        if (item.Finding is not null)
        {
            return NormalizeInline($"{item.Finding.Code}: {item.Finding.Message}");
        }

        return item.Insertable ? string.Empty : "Non-insertable safety hint.";
    }

    private static string NormalizeInline(string value)
    {
        return string.Join(" | ", (value ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}

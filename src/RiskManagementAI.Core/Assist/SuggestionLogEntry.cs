using RiskManagementAI.Core.Logging;

namespace RiskManagementAI.Core.Assist;

public sealed record SuggestionLogEntry(
    string SuggestionId,
    string ProviderId,
    CompletionLanguage Language,
    CompletionItemKind Kind,
    string Mode,
    string UserHash,
    string InsertTextHash,
    DateTime AcceptedAtUtc)
{
    public static SuggestionLogEntry FromAcceptedItem(
        CompletionItem item,
        CompletionLanguage language,
        string userHash,
        DateTime acceptedAtUtc,
        string mode = CompletionEngine.NoModelMode)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.Insertable)
        {
            throw new ArgumentException("Only insertable completion items can be audited.", nameof(item));
        }

        return new SuggestionLogEntry(
            LogHash.Sha256Hex(item.Source + "|" + item.Label),
            item.Source,
            language,
            item.Kind,
            mode,
            userHash,
            LogHash.Sha256Hex(item.InsertText),
            acceptedAtUtc);
    }
}

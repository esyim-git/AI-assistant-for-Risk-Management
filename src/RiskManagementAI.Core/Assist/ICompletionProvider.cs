namespace RiskManagementAI.Core.Assist;

public interface ICompletionProvider
{
    string ProviderId { get; }

    bool Supports(CompletionLanguage language);

    IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context);
}

public interface ICompletionProviderWarningSource
{
    IReadOnlyList<string> GetWarnings(CompletionContext context);
}

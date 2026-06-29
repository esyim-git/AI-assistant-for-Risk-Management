namespace RiskManagementAI.Core.Assist;

public sealed class CompletionProviderRegistry
{
    private readonly List<ICompletionProvider> providers = new();

    public CompletionProviderRegistry()
    {
    }

    public CompletionProviderRegistry(IEnumerable<ICompletionProvider> providers)
    {
        foreach (var provider in providers)
        {
            Register(provider);
        }
    }

    public void Register(ICompletionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (string.IsNullOrWhiteSpace(provider.ProviderId))
        {
            throw new ArgumentException("ProviderId is required.", nameof(provider));
        }

        if (providers.Any(existing => string.Equals(existing.ProviderId, provider.ProviderId, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Duplicate completion provider id: {provider.ProviderId}", nameof(provider));
        }

        providers.Add(provider);
    }

    public IReadOnlyList<ICompletionProvider> Resolve(CompletionLanguage language)
    {
        return providers
            .Where(provider => provider.Supports(language))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }
}

using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.Core.Assist;

public sealed class CompletionEngine
{
    public const string NoModelMode = "NoModel";
    public const int DefaultMaxInsertableItems = 50;

    private const string CompletionFindingRequiredCode = "COMPLETION_FINDING_REQUIRED";

    private readonly CompletionProviderRegistry registry;
    private readonly int maxInsertableItems;

    public CompletionEngine(CompletionProviderRegistry registry, int maxInsertableItems = DefaultMaxInsertableItems)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (maxInsertableItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInsertableItems), "Max insertable item count cannot be negative.");
        }

        this.registry = registry;
        this.maxInsertableItems = maxInsertableItems;
    }

    public CompletionResult GetCompletions(CompletionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var warnings = new List<string>();
        var mergedItems = new List<CompletionItem>();
        foreach (var provider in registry.Resolve(context.Language))
        {
            IReadOnlyList<CompletionItem> providerItems;
            try
            {
                providerItems = provider.GetCompletions(context) ?? Array.Empty<CompletionItem>();
            }
            catch (Exception ex)
            {
                warnings.Add($"Completion provider failed: {provider.ProviderId} ({ex.GetType().Name})");
                continue;
            }

            foreach (var item in providerItems)
            {
                mergedItems.Add(NormalizeItem(item, provider.ProviderId));
            }

            if (provider is ICompletionProviderWarningSource warningSource)
            {
                warnings.AddRange(warningSource
                    .GetWarnings(context)
                    .Where(warning => !string.IsNullOrWhiteSpace(warning)));
            }
        }

        var deduped = DedupeAndSort(mergedItems);
        var findings = deduped
            .Where(IsSafetyPinned)
            .Select(item => item.Finding)
            .OfType<SafetyFinding>()
            .Distinct()
            .ToArray();

        var pinnedItems = CollapseDuplicateFindingPins(deduped.Where(IsSafetyPinned).ToArray());
        var insertableItems = deduped
            .Where(item => !IsSafetyPinned(item))
            .Take(maxInsertableItems)
            .ToArray();

        return new CompletionResult(
            pinnedItems.Concat(insertableItems).ToArray(),
            NoModelMode,
            warnings.ToArray(),
            findings);
    }

    private static CompletionItem NormalizeItem(CompletionItem item, string providerId)
    {
        var isPinned = IsSafetyPinned(item);
        return item with
        {
            Source = providerId,
            RequiresReview = true,
            Insertable = !isPinned,
            InsertText = isPinned ? string.Empty : item.InsertText,
            Finding = isPinned
                ? item.Finding ?? new SafetyFinding(CompletionFindingRequiredCode, SafetySeverity.Medium, "Completion safety hint requires a structured finding.")
                : item.Finding,
            SafetyNote = string.IsNullOrWhiteSpace(item.SafetyNote) ? null : item.SafetyNote
        };
    }

    private static IReadOnlyList<CompletionItem> DedupeAndSort(IEnumerable<CompletionItem> items)
    {
        var sortedItems = items
            .OrderBy(item => IsSafetyPinned(item) ? 0 : 1)
            .ThenBy(item => item.SortKey)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ThenBy(item => item.Kind)
            .ToArray();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<CompletionItem>();
        foreach (var item in sortedItems)
        {
            var key = item.Source + "\u001f" + item.Label + "\u001f" + item.Kind;
            if (seen.Add(key))
            {
                deduped.Add(item);
            }
        }

        return deduped;
    }

    private static IReadOnlyList<CompletionItem> CollapseDuplicateFindingPins(IReadOnlyList<CompletionItem> pinnedItems)
    {
        var blockedFindings = new HashSet<SafetyFinding>();
        foreach (var item in pinnedItems)
        {
            if (item.Kind == CompletionItemKind.BlockedHint
                && item.Finding is { } finding
                && IsExplicitFinding(finding))
            {
                blockedFindings.Add(finding);
            }
        }

        var collapsed = new List<CompletionItem>(pinnedItems.Count);
        foreach (var item in pinnedItems)
        {
            if (item.Kind == CompletionItemKind.SafetyHint
                && item.Finding is { } finding
                && IsExplicitFinding(finding)
                && blockedFindings.Contains(finding))
            {
                continue;
            }

            collapsed.Add(item);
        }

        return collapsed;
    }

    private static bool IsSafetyPinned(CompletionItem item)
    {
        return item.Kind is CompletionItemKind.SafetyHint or CompletionItemKind.BlockedHint;
    }

    private static bool IsExplicitFinding(SafetyFinding? finding)
    {
        return finding is not null && !string.Equals(finding.Code, CompletionFindingRequiredCode, StringComparison.Ordinal);
    }
}

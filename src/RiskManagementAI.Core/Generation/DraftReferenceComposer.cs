using System.Text;

namespace RiskManagementAI.Core.Generation;

public static class DraftReferenceComposer
{
    public const int MaxReferenceCount = 5;
    public const int MaxReferenceTextChars = 2000;
    public const string TruncationMarker = "[reference truncated]";
    public const string ReferenceBlockHeader = "[Reviewed PromotedExample references | read-only | review draft | not auto-applied]";

    public static string? Compose(string? originalContext, IReadOnlyList<DraftReferenceExample> references)
    {
        ArgumentNullException.ThrowIfNull(references);

        var effectiveReferences = SelectEffectiveReferences(references).ToList();
        if (effectiveReferences.Count == 0)
        {
            return originalContext;
        }

        var referenceBlock = BuildReferenceBlock(effectiveReferences);
        if (string.IsNullOrWhiteSpace(originalContext))
        {
            return referenceBlock;
        }

        return NormalizeNewLines(originalContext) + "\n\n" + referenceBlock;
    }

    public static IReadOnlyList<string> SelectEffectiveExampleIds(IReadOnlyList<DraftReferenceExample>? references)
    {
        if (references is null || references.Count == 0)
        {
            return Array.Empty<string>();
        }

        return SelectEffectiveReferences(references)
            .Select(reference => reference.ExampleId)
            .ToList();
    }

    private static IEnumerable<ComposedReference> SelectEffectiveReferences(IReadOnlyList<DraftReferenceExample> references)
    {
        var selectedCount = 0;
        foreach (var reference in references)
        {
            if (selectedCount >= MaxReferenceCount)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(reference.ReferenceText))
            {
                continue;
            }

            var normalizedText = NormalizeNewLines(reference.ReferenceText);
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                continue;
            }

            var truncatedText = Truncate(normalizedText);
            selectedCount++;
            yield return new ComposedReference(NormalizeExampleId(reference.ExampleId), truncatedText, normalizedText.Length);
        }
    }

    private static string BuildReferenceBlock(IReadOnlyList<ComposedReference> references)
    {
        var builder = new StringBuilder();
        builder.Append(ReferenceBlockHeader);

        for (var index = 0; index < references.Count; index++)
        {
            var reference = references[index];
            builder.Append('\n');
            builder.Append("- (");
            builder.Append(index + 1);
            builder.Append(") [");
            builder.Append(reference.ExampleId);
            builder.Append("] chars=");
            builder.Append(reference.OriginalCharCount);

            foreach (var line in reference.Text.Split('\n'))
            {
                builder.Append('\n');
                builder.Append("| ");
                builder.Append(line);
            }
        }

        return builder.ToString();
    }

    private static string Truncate(string value)
    {
        if (value.Length <= MaxReferenceTextChars)
        {
            return value;
        }

        return value[..MaxReferenceTextChars] + "\n" + TruncationMarker;
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static string NormalizeExampleId(string? exampleId)
    {
        var normalized = NormalizeNewLines(exampleId ?? string.Empty)
            .Replace('\n', ' ')
            .Trim();
        return normalized.Length == 0 ? "(missing-example-id)" : normalized;
    }

    private sealed record ComposedReference(string ExampleId, string Text, int OriginalCharCount);
}

using System.Text;

namespace RiskManagementAI.Core.Kb;

public sealed class KbIndex
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<RegulationCatalogEntry>> postings;

    private KbIndex(
        IReadOnlyList<RegulationCatalogEntry> entries,
        IReadOnlyDictionary<string, IReadOnlyList<RegulationCatalogEntry>> postings)
    {
        Entries = entries;
        this.postings = postings;
        IndexedTermCount = postings.Count;
        PostingCount = postings.Values.Sum(value => value.Count);
    }

    public IReadOnlyList<RegulationCatalogEntry> Entries { get; }

    public int IndexedTermCount { get; }

    public int PostingCount { get; }

    public static KbIndex Build(IEnumerable<RegulationCatalogEntry> entries)
    {
        var orderedEntries = entries
            .OrderBy(entry => entry.SourceId, StringComparer.Ordinal)
            .ToList();
        var mutablePostings = new SortedDictionary<string, SortedDictionary<string, RegulationCatalogEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in orderedEntries)
        {
            foreach (var key in EntryKeys(entry))
            {
                if (!mutablePostings.TryGetValue(key, out var posting))
                {
                    posting = new SortedDictionary<string, RegulationCatalogEntry>(StringComparer.Ordinal);
                    mutablePostings[key] = posting;
                }

                posting[entry.SourceId] = entry;
            }
        }

        var frozenPostings = mutablePostings.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<RegulationCatalogEntry>)item.Value.Values.ToList(),
            StringComparer.OrdinalIgnoreCase);
        return new KbIndex(orderedEntries, frozenPostings);
    }

    public IReadOnlyList<RegulationCatalogEntry> FindCandidates(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var candidates = new SortedDictionary<string, RegulationCatalogEntry>(StringComparer.Ordinal);
        foreach (var key in QueryKeys(query))
        {
            if (!postings.TryGetValue(key, out var entries))
            {
                continue;
            }

            foreach (var entry in entries)
            {
                candidates[entry.SourceId] = entry;
            }
        }

        return candidates.Values.ToList();
    }

    public string DeterministicSignature()
    {
        var signature = new StringBuilder();
        foreach (var posting in postings.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            signature.Append(posting.Key);
            signature.Append(':');
            signature.AppendJoin('|', posting.Value.Select(entry => entry.SourceId));
            signature.Append('\n');
        }

        return signature.ToString();
    }

    private static IEnumerable<string> EntryKeys(RegulationCatalogEntry entry)
    {
        return SearchFields(entry)
            .SelectMany(field => TextKeys(field))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> QueryKeys(string query)
    {
        return TextKeys(query.Trim());
    }

    private static IEnumerable<string> TextKeys(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            yield break;
        }

        yield return normalized;

        foreach (var token in Tokenize(normalized))
        {
            yield return token;
        }

        foreach (var substring in Substrings(normalized))
        {
            yield return substring;
        }
    }

    private static IEnumerable<string> Tokenize(string value)
    {
        var current = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                current.Append(ch);
                continue;
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static IEnumerable<string> Substrings(string value)
    {
        for (var start = 0; start < value.Length; start++)
        {
            for (var length = 1; start + length <= value.Length; length++)
            {
                yield return value.Substring(start, length);
            }
        }
    }

    private static IEnumerable<string> SearchFields(RegulationCatalogEntry entry)
    {
        yield return entry.SourceId;
        yield return entry.Title;
        yield return entry.Category;
        yield return entry.SourceOrg;
        yield return entry.SourceType;
        yield return entry.Status;
        yield return entry.Note;
    }
}

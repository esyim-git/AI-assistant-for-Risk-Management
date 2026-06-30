namespace RiskManagementAI.Core.Kb;

internal sealed class KbClauseIndex
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<RegulationClause>> postings;

    private KbClauseIndex(
        IReadOnlyList<RegulationClause> clauses,
        IReadOnlyDictionary<string, IReadOnlyList<RegulationClause>> postings)
    {
        Clauses = clauses;
        this.postings = postings;
    }

    internal IReadOnlyList<RegulationClause> Clauses { get; }

    internal static KbClauseIndex Build(IEnumerable<RegulationClause> clauses)
    {
        var orderedClauses = clauses
            .OrderBy(clause => clause.ChunkId, StringComparer.Ordinal)
            .ToList();
        var mutablePostings = new SortedDictionary<string, SortedDictionary<string, RegulationClause>>(StringComparer.OrdinalIgnoreCase);

        foreach (var clause in orderedClauses)
        {
            foreach (var key in ClauseKeys(clause))
            {
                if (!mutablePostings.TryGetValue(key, out var posting))
                {
                    posting = new SortedDictionary<string, RegulationClause>(StringComparer.Ordinal);
                    mutablePostings[key] = posting;
                }

                posting[clause.ChunkId] = clause;
            }
        }

        var frozenPostings = mutablePostings.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<RegulationClause>)item.Value.Values.ToList(),
            StringComparer.OrdinalIgnoreCase);
        return new KbClauseIndex(orderedClauses, frozenPostings);
    }

    internal IReadOnlyList<RegulationClause> FindCandidates(string query)
    {
        var normalized = query.Trim();
        if (normalized.Length == 0)
        {
            return [];
        }

        if (KbKeying.RequiresLinearContainsFallback(normalized))
        {
            return Clauses
                .Where(clause => ContainsSearchField(clause, normalized))
                .OrderBy(clause => clause.ChunkId, StringComparer.Ordinal)
                .ToList();
        }

        var candidates = new SortedDictionary<string, RegulationClause>(StringComparer.Ordinal);
        foreach (var key in KbKeying.TextKeys(normalized))
        {
            if (!postings.TryGetValue(key, out var clauses))
            {
                continue;
            }

            foreach (var clause in clauses)
            {
                candidates[clause.ChunkId] = clause;
            }
        }

        return candidates.Values.ToList();
    }

    private static IEnumerable<string> ClauseKeys(RegulationClause clause)
    {
        return SearchFields(clause)
            .SelectMany(KbKeying.TextKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsSearchField(RegulationClause clause, string value)
    {
        return clause.ClauseText.Contains(value, StringComparison.OrdinalIgnoreCase)
            || clause.ClauseRef.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SearchFields(RegulationClause clause)
    {
        yield return clause.ClauseText;
        yield return clause.ClauseRef;
    }
}

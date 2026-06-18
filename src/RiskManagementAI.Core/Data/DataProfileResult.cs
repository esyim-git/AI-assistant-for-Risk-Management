namespace RiskManagementAI.Core.Data;

public sealed record DataProfileResult(
    string SourceName,
    int RowCount,
    int ColumnCount,
    IReadOnlyList<string> Columns,
    IReadOnlyDictionary<string, int> NullCounts
);

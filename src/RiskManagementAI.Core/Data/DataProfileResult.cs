namespace RiskManagementAI.Core.Data;

public sealed record DataProfileResult(
    string SourceName,
    int RowCount,
    int ColumnCount,
    IReadOnlyList<string> Columns,
    IReadOnlyDictionary<string, int> NullCounts,
    int DuplicateRowCount,
    IReadOnlyDictionary<string, int> BaseDateDistribution,
    IReadOnlyDictionary<string, NumericColumnProfile> NumericColumns,
    IReadOnlyList<string> Warnings
);

public sealed record NumericColumnProfile(
    string ColumnName,
    int NonNullCount,
    decimal Sum,
    decimal Min,
    decimal Max,
    int OutlierCount
);

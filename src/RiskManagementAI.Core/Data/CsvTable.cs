namespace RiskManagementAI.Core.Data;

public sealed record CsvTable(
    string SourcePath,
    string SourceName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<CsvRow> Rows,
    CsvReadMetadata Metadata)
{
    public int RowCount => Rows.Count;

    public int ColumnCount => Columns.Count;
}

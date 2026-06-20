namespace RiskManagementAI.Core.Data;

public sealed class CsvRow
{
    private readonly Dictionary<string, string> values;

    public CsvRow(IReadOnlyList<string> columns, IReadOnlyList<string> rowValues, int lineNumber)
    {
        LineNumber = lineNumber;
        RawFieldCount = rowValues.Count;
        Values = rowValues.ToArray();
        values = columns
            .Select((column, index) => new
            {
                Column = column,
                Value = index < rowValues.Count ? rowValues[index].Trim() : string.Empty
            })
            .ToDictionary(item => item.Column, item => item.Value, StringComparer.OrdinalIgnoreCase);
    }

    public int LineNumber { get; }

    public int RawFieldCount { get; }

    public IReadOnlyList<string> Values { get; }

    public string GetValue(string columnName)
    {
        if (values.TryGetValue(columnName, out var value))
        {
            return value;
        }

        throw new InvalidDataException($"{columnName} 컬럼이 없습니다.");
    }
}

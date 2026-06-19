using System.Globalization;
using System.Text;

namespace RiskManagementAI.Core.Data;

public sealed class DataProfiler
{
    private const string BaseDateColumnName = "BASE_DT";

    public DataProfileResult ProfileCsv(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
        {
            throw new ArgumentException("CSV 파일 경로가 비어 있습니다.", nameof(csvPath));
        }

        if (!string.Equals(Path.GetExtension(csvPath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("현재 MVP-1 DataProfiler는 CSV 파일만 지원합니다.", nameof(csvPath));
        }

        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("CSV 파일을 찾을 수 없습니다.", csvPath);
        }

        using var reader = new StreamReader(csvPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            throw new InvalidDataException("CSV 파일에 헤더가 없습니다.");
        }

        var columns = ParseCsvLine(headerLine)
            .Select((column, index) => index == 0 ? column.TrimStart('\uFEFF').Trim() : column.Trim())
            .ToArray();

        if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("CSV 헤더에 빈 컬럼명이 있습니다.");
        }

        var nullCounts = columns.ToDictionary(column => column, _ => 0, StringComparer.OrdinalIgnoreCase);
        var baseDateDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var duplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        var duplicateRowCount = 0;
        var warnings = new List<string>();
        var numericCandidates = columns.ToDictionary(column => column, column => new NumericAccumulator(column), StringComparer.OrdinalIgnoreCase);
        var baseDateIndex = Array.FindIndex(columns, column => string.Equals(column, BaseDateColumnName, StringComparison.OrdinalIgnoreCase));
        var rowCount = 0;
        var lineNumber = 1;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);
            if (values.Count != columns.Length)
            {
                warnings.Add($"Line {lineNumber}: 컬럼 수가 헤더와 다릅니다. expected={columns.Length}, actual={values.Count}");
            }

            var normalizedValues = NormalizeValues(values, columns.Length);
            rowCount++;

            var rowKey = string.Join('\u001F', normalizedValues);
            if (!duplicateKeys.Add(rowKey))
            {
                duplicateRowCount++;
            }

            for (var index = 0; index < columns.Length; index++)
            {
                var column = columns[index];
                var value = normalizedValues[index].Trim();
                if (IsNullValue(value))
                {
                    nullCounts[column]++;
                    numericCandidates[column].RegisterNull();
                    continue;
                }

                numericCandidates[column].RegisterValue(value);
            }

            if (baseDateIndex >= 0)
            {
                var baseDate = normalizedValues[baseDateIndex].Trim();
                if (!IsNullValue(baseDate))
                {
                    baseDateDistribution[baseDate] = baseDateDistribution.GetValueOrDefault(baseDate) + 1;
                }
            }
        }

        var numericColumns = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .ToDictionary(candidate => candidate.ColumnName, candidate => candidate.ToProfile(), StringComparer.OrdinalIgnoreCase);

        if (baseDateIndex < 0)
        {
            warnings.Add($"{BaseDateColumnName} 컬럼이 없어 기준일 분포를 산출하지 못했습니다.");
        }

        return new DataProfileResult(
            Path.GetFileName(csvPath),
            rowCount,
            columns.Length,
            columns,
            nullCounts,
            duplicateRowCount,
            baseDateDistribution,
            numericColumns,
            warnings);
    }

    private static IReadOnlyList<string> NormalizeValues(IReadOnlyList<string> values, int columnCount)
    {
        var normalized = new string[columnCount];
        for (var index = 0; index < columnCount; index++)
        {
            normalized[index] = index < values.Count ? values[index] : string.Empty;
        }

        return normalized;
    }

    private static bool IsNullValue(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "NA", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private sealed class NumericAccumulator
    {
        private readonly List<decimal> values = [];
        private bool sawTextValue;

        public NumericAccumulator(string columnName)
        {
            ColumnName = columnName;
        }

        public string ColumnName { get; }

        public bool IsNumericColumn => values.Count > 0 && !sawTextValue;

        public void RegisterNull()
        {
        }

        public void RegisterValue(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
            else
            {
                sawTextValue = true;
            }
        }

        public NumericColumnProfile ToProfile()
        {
            var sum = values.Sum();
            return new NumericColumnProfile(
                ColumnName,
                values.Count,
                sum,
                values.Min(),
                values.Max(),
                CountSimpleOutliers());
        }

        private int CountSimpleOutliers()
        {
            if (values.Count < 4)
            {
                return 0;
            }

            var average = values.Average(value => (double)value);
            var variance = values.Sum(value =>
            {
                var delta = (double)value - average;
                return delta * delta;
            }) / values.Count;
            var standardDeviation = Math.Sqrt(variance);
            if (standardDeviation == 0)
            {
                return 0;
            }

            var threshold = standardDeviation * 3;
            return values.Count(value => Math.Abs((double)value - average) > threshold);
        }
    }
}

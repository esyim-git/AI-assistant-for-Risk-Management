using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using RiskManagementAI.Core.Mapping;

namespace RiskManagementAI.Core.Data;

public sealed class DataProfiler
{
    private readonly ColumnMappingLoadResult columnMappingLoadResult;

    public DataProfiler()
        : this(ColumnMappingLoader.LoadDefault())
    {
    }

    public DataProfiler(ColumnMapping mapping)
        : this(new ColumnMappingLoadResult(mapping, UsedFallback: false, Warnings: Array.Empty<string>()))
    {
    }

    public DataProfiler(ColumnMappingLoadResult columnMappingLoadResult)
    {
        ArgumentNullException.ThrowIfNull(columnMappingLoadResult);
        this.columnMappingLoadResult = columnMappingLoadResult;
    }

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

        return ProfileTable(CsvReader.Read(csvPath));
    }

    public DataProfileResult ProfileCsvStreaming(string csvPath, CsvEncoding encoding = CsvEncoding.Auto)
    {
        var header = CsvReader.ReadHeader(csvPath, encoding);
        return ProfileRows(
            header.SourceName,
            header.Columns,
            () => CsvReader.StreamDataRows(header).Select(row => new ProfileInputRow(row.Values, row.RawFieldCount, row.LineNumber)));
    }

    public DataProfileResult ProfileTable(CsvTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return ProfileRows(
            table.SourceName,
            table.Columns,
            () => table.Rows.Select(row => new ProfileInputRow(row.Values, row.RawFieldCount, row.LineNumber)));
    }

    private DataProfileResult ProfileRows(
        string sourceName,
        IReadOnlyList<string> inputColumns,
        Func<IEnumerable<ProfileInputRow>> rowFactory)
    {
        var columns = inputColumns.ToArray();

        if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("CSV 헤더에 빈 컬럼명이 있습니다.");
        }

        var nullCounts = columns.ToDictionary(column => column, _ => 0, StringComparer.OrdinalIgnoreCase);
        var baseDateDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var duplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        var duplicateRowCount = 0;
        var warnings = new List<string>(columnMappingLoadResult.Warnings);
        var numericCandidates = columns.ToDictionary(column => column, column => new NumericAccumulator(column), StringComparer.OrdinalIgnoreCase);
        var baseDateColumnName = columnMappingLoadResult.Mapping.Physical(LogicalColumn.BaseDate);
        var baseDateIndex = Array.FindIndex(columns, column => string.Equals(column, baseDateColumnName, StringComparison.OrdinalIgnoreCase));
        var rowCount = 0;
        foreach (var row in rowFactory())
        {
            if (row.RawFieldCount != columns.Length)
            {
                warnings.Add($"Line {row.LineNumber}: 컬럼 수가 헤더와 다릅니다. expected={columns.Length}, actual={row.RawFieldCount}");
            }

            var normalizedValues = NormalizeValues(row.Values, columns.Length);
            rowCount++;

            var rowKey = HashNormalizedRow(normalizedValues);
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

        var outlierBoundaries = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .Select(candidate => new { candidate.ColumnName, Boundary = candidate.CreateOutlierBoundary() })
            .Where(item => item.Boundary is not null)
            .ToDictionary(item => item.ColumnName, item => item.Boundary!, StringComparer.OrdinalIgnoreCase);
        var outlierCounts = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .ToDictionary(candidate => candidate.ColumnName, _ => 0, StringComparer.OrdinalIgnoreCase);

        if (outlierBoundaries.Count > 0)
        {
            foreach (var row in rowFactory())
            {
                var normalizedValues = NormalizeValues(row.Values, columns.Length);
                for (var index = 0; index < columns.Length; index++)
                {
                    var column = columns[index];
                    if (!outlierBoundaries.TryGetValue(column, out var boundary))
                    {
                        continue;
                    }

                    var value = normalizedValues[index].Trim();
                    if (IsNullValue(value))
                    {
                        continue;
                    }

                    if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed)
                        && boundary.IsOutlier(parsed))
                    {
                        outlierCounts[column]++;
                    }
                }
            }
        }

        var numericColumns = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .ToDictionary(
                candidate => candidate.ColumnName,
                candidate => candidate.ToProfile(outlierCounts.GetValueOrDefault(candidate.ColumnName)),
                StringComparer.OrdinalIgnoreCase);

        if (baseDateIndex < 0)
        {
            warnings.Add($"{baseDateColumnName} 컬럼이 없어 기준일 분포를 산출하지 못했습니다.");
        }

        return new DataProfileResult(
            sourceName,
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

    private static string HashNormalizedRow(IReadOnlyList<string> normalizedValues)
    {
        var rowKey = string.Join('\u001F', normalizedValues);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rowKey))).ToLowerInvariant();
    }

    private readonly record struct ProfileInputRow(IReadOnlyList<string> Values, int RawFieldCount, int LineNumber);

    private sealed record OutlierBoundary(double Mean, double Threshold)
    {
        public bool IsOutlier(decimal value)
            => Math.Abs((double)value - Mean) > Threshold;
    }

    private sealed class NumericAccumulator
    {
        private int count;
        private decimal sum;
        private decimal min;
        private decimal max;
        private double mean;
        private double m2;
        private bool sawTextValue;

        public NumericAccumulator(string columnName)
        {
            ColumnName = columnName;
        }

        public string ColumnName { get; }

        public bool IsNumericColumn => count > 0 && !sawTextValue;

        public void RegisterNull()
        {
        }

        public void RegisterValue(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed))
            {
                count++;
                sum += parsed;
                if (count == 1)
                {
                    min = parsed;
                    max = parsed;
                    mean = (double)parsed;
                    m2 = 0;
                    return;
                }

                if (parsed < min)
                {
                    min = parsed;
                }

                if (parsed > max)
                {
                    max = parsed;
                }

                var sample = (double)parsed;
                var delta = sample - mean;
                mean += delta / count;
                var deltaAfterMean = sample - mean;
                m2 += delta * deltaAfterMean;
            }
            else
            {
                sawTextValue = true;
            }
        }

        public OutlierBoundary? CreateOutlierBoundary()
        {
            if (count < 4)
            {
                return null;
            }

            var variance = m2 / count;
            var standardDeviation = Math.Sqrt(variance);
            if (standardDeviation == 0)
            {
                return null;
            }

            return new OutlierBoundary(mean, standardDeviation * 3);
        }

        public NumericColumnProfile ToProfile(int outlierCount)
        {
            return new NumericColumnProfile(
                ColumnName,
                count,
                sum,
                min,
                max,
                outlierCount);
        }
    }
}

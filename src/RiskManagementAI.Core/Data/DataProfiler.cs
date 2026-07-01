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

        var outlierWork = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .Select(candidate => new { candidate.ColumnName, Work = candidate.CreateOutlierWork() })
            .Where(item => item.Work is not null)
            .ToDictionary(item => item.ColumnName, item => item.Work!, StringComparer.OrdinalIgnoreCase);
        var outlierCounts = numericCandidates.Values
            .Where(candidate => candidate.IsNumericColumn)
            .ToDictionary(candidate => candidate.ColumnName, _ => 0, StringComparer.OrdinalIgnoreCase);

        if (outlierWork.Count > 0)
        {
            foreach (var row in rowFactory())
            {
                var normalizedValues = NormalizeValues(row.Values, columns.Length);
                for (var index = 0; index < columns.Length; index++)
                {
                    var column = columns[index];
                    if (!outlierWork.TryGetValue(column, out var work))
                    {
                        continue;
                    }

                    var value = normalizedValues[index].Trim();
                    if (IsNullValue(value))
                    {
                        continue;
                    }

                    if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed))
                    {
                        work.AddVariance(parsed);
                    }
                }
            }

            var outlierBoundaries = outlierWork
                .Select(item => new { item.Key, Boundary = item.Value.CreateBoundary() })
                .Where(item => item.Boundary is not null)
                .ToDictionary(item => item.Key, item => item.Boundary!, StringComparer.OrdinalIgnoreCase);

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

    private sealed class OutlierWork
    {
        private readonly int count;
        private readonly double mean;
        private double varianceSum;

        public OutlierWork(int count, double mean)
        {
            this.count = count;
            this.mean = mean;
        }

        public void AddVariance(decimal value)
        {
            var delta = (double)value - mean;
            varianceSum += delta * delta;
        }

        public OutlierBoundary? CreateBoundary()
        {
            var standardDeviation = Math.Sqrt(varianceSum / count);
            if (standardDeviation == 0)
            {
                return null;
            }

            return new OutlierBoundary(mean, standardDeviation * 3);
        }
    }

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
        private double legacyDoubleSum;
        private bool sumOverflowed;
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
            if (sawTextValue)
            {
                return;
            }

            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var parsed))
            {
                count++;
                legacyDoubleSum += (double)parsed;
                try
                {
                    sum += parsed;
                }
                catch (OverflowException)
                {
                    sumOverflowed = true;
                }

                if (count == 1)
                {
                    min = parsed;
                    max = parsed;
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
            }
            else
            {
                sawTextValue = true;
            }
        }

        public OutlierWork? CreateOutlierWork()
        {
            if (count < 4)
            {
                return null;
            }

            return new OutlierWork(count, legacyDoubleSum / count);
        }

        public NumericColumnProfile ToProfile(int outlierCount)
        {
            if (sumOverflowed)
            {
                throw new OverflowException("Numeric column sum exceeded decimal range.");
            }

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

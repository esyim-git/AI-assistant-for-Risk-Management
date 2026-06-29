using System.Text;

namespace RiskManagementAI.Core.Data;

public static class CsvReader
{
    public const int MaxRowCount = 200_000;
    public const long MaxByteSize = 50L * 1024L * 1024L;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static CsvTable Read(string path, CsvEncoding encoding = CsvEncoding.Auto)
    {
        var header = ReadHeader(path, encoding);
        var rows = StreamDataRows(header)
            .Select(row => new CsvRow(header.Columns, row.Values, row.LineNumber))
            .ToList();

        return new CsvTable(
            header.SourcePath,
            header.SourceName,
            header.Columns,
            rows,
            header.Metadata);
    }

    public static CsvTable ReadStreaming(string path, CsvEncoding encoding = CsvEncoding.Auto)
        => Read(path, encoding);

    internal static CsvStreamHeader ReadHeader(string path, CsvEncoding encoding = CsvEncoding.Auto)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("CSV 파일 경로가 비어 있습니다.", nameof(path));
        }

        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("CSV 파일만 지원합니다.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("CSV 파일을 찾을 수 없습니다.", path);
        }

        var file = new FileInfo(path);
        if (file.Length > MaxByteSize)
        {
            throw new InvalidDataException($"CSV 파일 크기가 안전 상한을 초과했습니다. max={MaxByteSize}, actual={file.Length}");
        }

        var hadUtf8Bom = HasUtf8Bom(path);
        var detectedEncoding = DetectEncoding(path, encoding, hadUtf8Bom);
        var firstRecord = ParseRecords(ReadLines(path, detectedEncoding, hadUtf8Bom)).FirstOrDefault();
        if (firstRecord is null)
        {
            throw new InvalidDataException("CSV 파일에 헤더가 없습니다.");
        }

        var header = firstRecord.Values
            .Select((column, index) => index == 0 ? column.TrimStart('\uFEFF').Trim() : column.Trim())
            .ToArray();
        if (header.Length == 0 || header.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("CSV 헤더에 빈 컬럼명이 있습니다.");
        }

        if (header.Distinct(StringComparer.OrdinalIgnoreCase).Count() != header.Length)
        {
            throw new InvalidDataException("CSV 헤더에 중복 컬럼명이 있습니다.");
        }

        return new CsvStreamHeader(
            Path.GetFullPath(path),
            Path.GetFileName(path),
            header,
            new CsvReadMetadata(
                encoding,
                detectedEncoding,
                hadUtf8Bom,
                detectedEncoding == CsvEncoding.Cp949 ? Cp949Decoder.MappingSha256 : null,
                detectedEncoding == CsvEncoding.Cp949 ? Cp949Decoder.MappingEntryCount : null));
    }

    internal static IEnumerable<CsvStreamRow> StreamDataRows(CsvStreamHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        var skippedHeader = false;
        var rowCount = 0;
        foreach (var record in ParseRecords(ReadLines(header.SourcePath, header.Metadata.DetectedEncoding, header.Metadata.HadUtf8Bom)))
        {
            if (!skippedHeader)
            {
                skippedHeader = true;
                continue;
            }

            if (record.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rowCount++;
            if (rowCount > MaxRowCount)
            {
                throw new InvalidDataException($"CSV 행 수가 안전 상한을 초과했습니다. max={MaxRowCount}, actual={rowCount}");
            }

            yield return new CsvStreamRow(record.Values, record.LineNumber);
        }
    }

    private static CsvEncoding DetectEncoding(string path, CsvEncoding requestedEncoding, bool hadUtf8Bom)
    {
        return requestedEncoding switch
        {
            CsvEncoding.Utf8 => EnsureUtf8Readable(path, hadUtf8Bom),
            CsvEncoding.Cp949 => CsvEncoding.Cp949,
            CsvEncoding.Auto => DetectAutoEncoding(path, hadUtf8Bom),
            _ => throw new ArgumentOutOfRangeException(nameof(requestedEncoding), requestedEncoding, "지원하지 않는 CSV 인코딩입니다.")
        };
    }

    private static CsvEncoding DetectAutoEncoding(string path, bool hadUtf8Bom)
    {
        if (hadUtf8Bom)
        {
            return EnsureUtf8Readable(path, hadUtf8Bom);
        }

        return TryReadUtf8(path, hadUtf8Bom) ? CsvEncoding.Utf8 : CsvEncoding.Cp949;
    }

    private static CsvEncoding EnsureUtf8Readable(string path, bool hadUtf8Bom)
    {
        try
        {
            _ = TryReadUtf8(path, hadUtf8Bom, throwOnFailure: true);
            return CsvEncoding.Utf8;
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("UTF-8 CSV 디코딩에 실패했습니다.", exception);
        }
    }

    private static bool TryReadUtf8(string path, bool hadUtf8Bom, bool throwOnFailure = false)
    {
        try
        {
            using var reader = OpenUtf8Reader(path, hadUtf8Bom);
            var buffer = new char[4096];
            while (reader.Read(buffer, 0, buffer.Length) > 0)
            {
            }

            return true;
        }
        catch (DecoderFallbackException) when (!throwOnFailure)
        {
            return false;
        }
    }

    private static StreamReader OpenUtf8Reader(string path, bool hadUtf8Bom)
    {
        var stream = File.OpenRead(path);
        if (hadUtf8Bom)
        {
            stream.Position = 3;
        }

        return new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: false);
    }

    private static bool HasUtf8Bom(string path)
    {
        Span<byte> bytes = stackalloc byte[3];
        using var stream = File.OpenRead(path);
        return stream.Read(bytes) == 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

    private static IEnumerable<string> ReadLines(string path, CsvEncoding detectedEncoding, bool hadUtf8Bom)
    {
        if (detectedEncoding == CsvEncoding.Cp949)
        {
            return ReadCp949Lines(path);
        }

        return ReadUtf8Lines(path, hadUtf8Bom);
    }

    private static IEnumerable<string> ReadUtf8Lines(string path, bool hadUtf8Bom)
    {
        using var reader = OpenUtf8Reader(path, hadUtf8Bom);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    private static IEnumerable<string> ReadCp949Lines(string path)
    {
        using var stream = File.OpenRead(path);
        foreach (var line in Cp949Decoder.DecodeLines(stream))
        {
            yield return line;
        }
    }

    private static IEnumerable<CsvRecord> ParseRecords(IEnumerable<string> lines)
    {
        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            yield return new CsvRecord(ParseCsvLine(line), lineNumber);
        }
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

    private sealed record CsvRecord(IReadOnlyList<string> Values, int LineNumber);
}

internal sealed record CsvStreamHeader(
    string SourcePath,
    string SourceName,
    IReadOnlyList<string> Columns,
    CsvReadMetadata Metadata);

internal sealed record CsvStreamRow(IReadOnlyList<string> Values, int LineNumber)
{
    public int RawFieldCount => Values.Count;
}

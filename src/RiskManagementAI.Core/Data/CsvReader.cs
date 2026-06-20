using System.Text;

namespace RiskManagementAI.Core.Data;

public static class CsvReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static CsvTable Read(string path, CsvEncoding encoding = CsvEncoding.Auto)
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

        var bytes = File.ReadAllBytes(path);
        var decoded = Decode(bytes, encoding);
        var records = ParseRecords(decoded.Text);
        if (records.Count == 0)
        {
            throw new InvalidDataException("CSV 파일에 헤더가 없습니다.");
        }

        var header = records[0].Values
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

        var rows = records
            .Skip(1)
            .Where(record => !record.Values.All(string.IsNullOrWhiteSpace))
            .Select(record => new CsvRow(header, record.Values, record.LineNumber))
            .ToList();

        return new CsvTable(
            Path.GetFullPath(path),
            Path.GetFileName(path),
            header,
            rows,
            new CsvReadMetadata(
                encoding,
                decoded.DetectedEncoding,
                decoded.HadUtf8Bom,
                decoded.DetectedEncoding == CsvEncoding.Cp949 ? Cp949Decoder.MappingSha256 : null,
                decoded.DetectedEncoding == CsvEncoding.Cp949 ? Cp949Decoder.MappingEntryCount : null));
    }

    private static DecodedCsv Decode(byte[] bytes, CsvEncoding requestedEncoding)
    {
        var hadUtf8Bom = HasUtf8Bom(bytes);
        return requestedEncoding switch
        {
            CsvEncoding.Utf8 => new DecodedCsv(DecodeUtf8OrThrow(bytes, hadUtf8Bom), CsvEncoding.Utf8, hadUtf8Bom),
            CsvEncoding.Cp949 => new DecodedCsv(Cp949Decoder.Decode(bytes), CsvEncoding.Cp949, hadUtf8Bom),
            CsvEncoding.Auto => DecodeAuto(bytes, hadUtf8Bom),
            _ => throw new ArgumentOutOfRangeException(nameof(requestedEncoding), requestedEncoding, "지원하지 않는 CSV 인코딩입니다.")
        };
    }

    private static DecodedCsv DecodeAuto(byte[] bytes, bool hadUtf8Bom)
    {
        if (hadUtf8Bom)
        {
            return new DecodedCsv(DecodeUtf8OrThrow(bytes, hadUtf8Bom), CsvEncoding.Utf8, hadUtf8Bom);
        }

        if (TryDecodeUtf8(bytes, hadUtf8Bom, out var utf8Text))
        {
            return new DecodedCsv(utf8Text, CsvEncoding.Utf8, hadUtf8Bom);
        }

        return new DecodedCsv(Cp949Decoder.Decode(bytes), CsvEncoding.Cp949, hadUtf8Bom);
    }

    private static bool TryDecodeUtf8(byte[] bytes, bool hadUtf8Bom, out string text)
    {
        try
        {
            text = DecodeUtf8Strict(bytes, hadUtf8Bom);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static string DecodeUtf8OrThrow(byte[] bytes, bool hadUtf8Bom)
    {
        try
        {
            return DecodeUtf8Strict(bytes, hadUtf8Bom);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("UTF-8 CSV 디코딩에 실패했습니다.", exception);
        }
    }

    private static string DecodeUtf8Strict(byte[] bytes, bool hadUtf8Bom)
    {
        var offset = hadUtf8Bom ? 3 : 0;
        return StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
    }

    private static bool HasUtf8Bom(byte[] bytes)
        => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

    private static IReadOnlyList<CsvRecord> ParseRecords(string text)
    {
        var records = new List<CsvRecord>();
        using var reader = new StringReader(text);
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            records.Add(new CsvRecord(ParseCsvLine(line), lineNumber));
        }

        return records;
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

    private sealed record DecodedCsv(string Text, CsvEncoding DetectedEncoding, bool HadUtf8Bom);

    private sealed record CsvRecord(IReadOnlyList<string> Values, int LineNumber);
}

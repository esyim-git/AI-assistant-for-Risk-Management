using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RiskManagementAI.Core.Data;

public static class Cp949Decoder
{
    // SHA256 of the LF-normalized resource bytes. The resource is pinned to LF
    // via .gitattributes (text eol=lf) so this hash is identical on every
    // platform/checkout (Windows autocrlf would otherwise change it).
    public const string ExpectedMappingSha256 = "ca2d8cb6296b659c227237955dd87ba2d212ebc6e18cfc218bacee6c232db67d";
    public const int ExpectedMappingEntryCount = 17236;

    private const string ResourceName = "RiskManagementAI.Core.Data.Resources.cp949-uhc-map.txt";
    private static readonly Lazy<MappingData> Mapping = new(LoadMapping, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string MappingSha256 => Mapping.Value.Sha256;

    public static int MappingEntryCount => Mapping.Value.Entries.Count;

    public static void VerifyMapping()
    {
        _ = Mapping.Value;
    }

    public static string Decode(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var mapping = Mapping.Value.Entries;
        var builder = new StringBuilder(bytes.Length);
        var invalidSequences = 0;

        for (var index = 0; index < bytes.Length; index++)
        {
            var current = bytes[index];
            if (current <= 0x7F)
            {
                builder.Append((char)current);
                continue;
            }

            if (current == 0x80)
            {
                builder.Append('\u0080');
                continue;
            }

            if (index + 1 >= bytes.Length)
            {
                invalidSequences++;
                continue;
            }

            var trail = bytes[++index];
            var key = (ushort)((current << 8) | trail);
            if (mapping.TryGetValue(key, out var mapped))
            {
                builder.Append(mapped);
            }
            else
            {
                invalidSequences++;
            }
        }

        if (invalidSequences > 0)
        {
            throw new InvalidDataException($"CP949 CSV 디코딩 중 유효하지 않은 바이트 시퀀스가 있습니다. count={invalidSequences}");
        }

        return builder.ToString();
    }

    public static IEnumerable<string> DecodeLines(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var mapping = Mapping.Value.Entries;
        var line = new StringBuilder();
        var invalidSequences = 0;
        var skipNextLf = false;

        while (true)
        {
            var next = stream.ReadByte();
            if (next < 0)
            {
                break;
            }

            string decoded;
            var current = (byte)next;
            if (current <= 0x7F)
            {
                decoded = ((char)current).ToString();
            }
            else if (current == 0x80)
            {
                decoded = "\u0080";
            }
            else
            {
                var trail = stream.ReadByte();
                if (trail < 0)
                {
                    invalidSequences++;
                    break;
                }

                var key = (ushort)((current << 8) | (byte)trail);
                if (!mapping.TryGetValue(key, out var mapped))
                {
                    invalidSequences++;
                    continue;
                }

                decoded = mapped;
            }

            foreach (var ch in decoded)
            {
                if (skipNextLf && ch == '\n')
                {
                    skipNextLf = false;
                    continue;
                }

                skipNextLf = false;
                if (ch == '\r')
                {
                    yield return line.ToString();
                    line.Clear();
                    skipNextLf = true;
                    continue;
                }

                if (ch == '\n')
                {
                    yield return line.ToString();
                    line.Clear();
                    continue;
                }

                line.Append(ch);
            }
        }

        if (invalidSequences > 0)
        {
            throw new InvalidDataException($"CP949 CSV 디코딩 중 유효하지 않은 바이트 시퀀스가 있습니다. count={invalidSequences}");
        }

        if (line.Length > 0)
        {
            yield return line.ToString();
        }
    }

    private static MappingData LoadMapping()
    {
        var assembly = typeof(Cp949Decoder).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"CP949 매핑 리소스를 찾을 수 없습니다. resource={ResourceName}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(sha256, ExpectedMappingSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"CP949 매핑 리소스 해시가 일치하지 않습니다. expected={ExpectedMappingSha256}, actual={sha256}");
        }

        var text = Encoding.ASCII.GetString(bytes);
        var entries = new Dictionary<ushort, string>();
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == trimmed.Length - 1)
            {
                throw new InvalidDataException("CP949 매핑 리소스 형식이 올바르지 않습니다.");
            }

            var byteKey = ushort.Parse(trimmed[..separatorIndex], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var codePoint = int.Parse(trimmed[(separatorIndex + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            entries[byteKey] = char.ConvertFromUtf32(codePoint);
        }

        if (entries.Count != ExpectedMappingEntryCount)
        {
            throw new InvalidDataException($"CP949 매핑 리소스 항목 수가 일치하지 않습니다. expected={ExpectedMappingEntryCount}, actual={entries.Count}");
        }

        return new MappingData(entries, sha256);
    }

    private sealed record MappingData(IReadOnlyDictionary<ushort, string> Entries, string Sha256);
}

using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace RiskManagementAI.Core.Data;

public static class XlsxReader
{
    public const int MaxZipEntries = 512;
    public const long MaxUncompressedBytes = 25L * 1024L * 1024L;
    public const long MaxEntryBytes = 10L * 1024L * 1024L;
    public const int MaxWorksheetRows = 5000;
    public const int MaxWorksheetColumns = 256;

    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static CsvTable Read(string path, string? sheetName = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("XLSX 파일 경로가 비어 있습니다.", nameof(path));
        }

        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("XlsxReader는 .xlsx 파일만 지원합니다.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("XLSX 파일을 찾을 수 없습니다.", path);
        }

        try
        {
            using var archive = ZipFile.OpenRead(path);
            ValidateArchiveSafety(archive);

            var sharedStrings = ReadSharedStrings(archive);
            var sheet = ResolveWorksheet(archive, sheetName);
            var records = ReadWorksheetRows(archive, sheet, sharedStrings);
            if (records.Count == 0)
            {
                throw new InvalidDataException("XLSX 워크시트에 헤더가 없습니다.");
            }

            var headerRecord = records[0];
            var columns = TrimTrailingEmpty(headerRecord.Values)
                .Select(column => column.Trim())
                .ToArray();
            if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException("XLSX 헤더에 빈 컬럼명이 있습니다.");
            }

            if (columns.Length > MaxWorksheetColumns)
            {
                throw new InvalidDataException($"XLSX 컬럼 수가 안전 상한을 초과했습니다. max={MaxWorksheetColumns}, actual={columns.Length}");
            }

            if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Length)
            {
                throw new InvalidDataException("XLSX 헤더에 중복 컬럼명이 있습니다.");
            }

            var rows = records
                .Skip(1)
                .Where(record => !record.Values.All(string.IsNullOrWhiteSpace))
                .Select(record => new CsvRow(columns, NormalizeValues(record.Values, columns.Length), record.RowNumber))
                .ToList();

            return new CsvTable(
                Path.GetFullPath(path),
                Path.GetFileName(path),
                columns,
                rows,
                new CsvReadMetadata(CsvEncoding.Utf8, CsvEncoding.Utf8, HadUtf8Bom: false, Cp949MappingSha256: null, Cp949MappingEntryCount: null));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            throw new InvalidDataException("XLSX 파일을 읽을 수 없습니다. 파일 형식 또는 권한을 확인하세요.", ex);
        }
    }

    private static void ValidateArchiveSafety(ZipArchive archive)
    {
        if (archive.Entries.Count > MaxZipEntries)
        {
            throw new InvalidDataException($"XLSX ZIP 엔트리 수가 안전 상한을 초과했습니다. max={MaxZipEntries}, actual={archive.Entries.Count}");
        }

        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(entry.FullName))
            {
                throw new InvalidDataException($"XLSX ZIP 엔트리 경로가 안전하지 않습니다. entry={entry.FullName}");
            }

            if (entry.Length > MaxEntryBytes)
            {
                throw new InvalidDataException($"XLSX ZIP 엔트리 크기가 안전 상한을 초과했습니다. entry={entry.FullName}, max={MaxEntryBytes}, actual={entry.Length}");
            }

            totalBytes += entry.Length;
            if (totalBytes > MaxUncompressedBytes)
            {
                throw new InvalidDataException($"XLSX ZIP 압축해제 크기가 안전 상한을 초과했습니다. max={MaxUncompressedBytes}, actual={totalBytes}");
            }
        }
    }

    private static WorksheetPart ResolveWorksheet(ZipArchive archive, string? requestedSheetName)
    {
        var workbook = LoadXmlEntry(archive, "xl/workbook.xml");
        var workbookRels = LoadXmlEntry(archive, "xl/_rels/workbook.xml.rels");
        var relationshipTargets = workbookRels
            .Root?
            .Elements(PackageRelationshipsNs + "Relationship")
            .Where(element => string.Equals((string?)element.Attribute("Type"), "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet", StringComparison.Ordinal))
            .ToDictionary(
                element => (string?)element.Attribute("Id") ?? string.Empty,
                element => NormalizeWorksheetTarget((string?)element.Attribute("Target") ?? string.Empty),
                StringComparer.Ordinal)
            ?? throw new InvalidDataException("XLSX workbook 관계 파일이 비어 있습니다.");

        var sheetElements = workbook.Root?
            .Element(SpreadsheetNs + "sheets")?
            .Elements(SpreadsheetNs + "sheet")
            .ToList()
            ?? throw new InvalidDataException("XLSX workbook에 시트 목록이 없습니다.");
        if (sheetElements.Count == 0)
        {
            throw new InvalidDataException("XLSX workbook에 시트가 없습니다.");
        }

        var selected = string.IsNullOrWhiteSpace(requestedSheetName)
            ? sheetElements[0]
            : sheetElements.FirstOrDefault(element => string.Equals((string?)element.Attribute("name"), requestedSheetName, StringComparison.Ordinal));
        if (selected is null)
        {
            throw new InvalidDataException($"요청한 시트를 찾을 수 없습니다. sheetName={requestedSheetName}");
        }

        var name = (string?)selected.Attribute("name") ?? string.Empty;
        var relationshipId = (string?)selected.Attribute(RelationshipsNs + "id") ?? string.Empty;
        if (!relationshipTargets.TryGetValue(relationshipId, out var target))
        {
            throw new InvalidDataException($"XLSX 시트 관계를 찾을 수 없습니다. sheetName={name}, rId={relationshipId}");
        }

        return new WorksheetPart(name, target);
    }

    private static string NormalizeWorksheetTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target) || target.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidDataException("XLSX worksheet target 경로가 안전하지 않습니다.");
        }

        var normalized = target.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "xl/" + normalized;
        }

        if (!normalized.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"XLSX worksheet target이 worksheets 하위가 아닙니다. target={target}");
        }

        return normalized;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return Array.Empty<string>();
        }

        var document = LoadXmlEntry(entry);
        return document
            .Descendants(SpreadsheetNs + "si")
            .Select(ReadStringItem)
            .ToArray();
    }

    private static IReadOnlyList<WorksheetRecord> ReadWorksheetRows(
        ZipArchive archive,
        WorksheetPart sheet,
        IReadOnlyList<string> sharedStrings)
    {
        var document = LoadXmlEntry(archive, sheet.Target);
        var records = new List<WorksheetRecord>();
        foreach (var rowElement in document.Descendants(SpreadsheetNs + "row"))
        {
            if (records.Count >= MaxWorksheetRows)
            {
                throw new InvalidDataException($"XLSX 워크시트 행 수가 안전 상한을 초과했습니다. max={MaxWorksheetRows}");
            }

            var rowNumber = ParseRowNumber(rowElement);
            var values = new List<string>();
            foreach (var cell in rowElement.Elements(SpreadsheetNs + "c"))
            {
                var columnIndex = ParseColumnIndex((string?)cell.Attribute("r"));
                if (columnIndex > MaxWorksheetColumns)
                {
                    throw new InvalidDataException($"XLSX 워크시트 컬럼 수가 안전 상한을 초과했습니다. max={MaxWorksheetColumns}, actual={columnIndex}");
                }

                while (values.Count < columnIndex)
                {
                    values.Add(string.Empty);
                }

                values[columnIndex - 1] = ReadCellValue(cell, sharedStrings);
            }

            if (values.Count > 0 && !values.All(string.IsNullOrWhiteSpace))
            {
                records.Add(new WorksheetRecord(rowNumber, values));
            }
        }

        return records;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (string.Equals(type, "s", StringComparison.Ordinal))
        {
            var indexText = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
            if (!int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out var sharedStringIndex)
                || sharedStringIndex < 0
                || sharedStringIndex >= sharedStrings.Count)
            {
                throw new InvalidDataException($"XLSX shared string index가 올바르지 않습니다. index={indexText}");
            }

            return sharedStrings[sharedStringIndex];
        }

        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
        {
            var inlineString = cell.Element(SpreadsheetNs + "is");
            return inlineString is null ? string.Empty : ReadStringItem(inlineString);
        }

        return cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
    }

    private static string ReadStringItem(XElement stringItem)
    {
        var directText = stringItem.Element(SpreadsheetNs + "t");
        if (directText is not null)
        {
            return directText.Value;
        }

        return string.Concat(stringItem.Descendants(SpreadsheetNs + "t").Select(text => text.Value));
    }

    private static XDocument LoadXmlEntry(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidDataException($"XLSX 필수 엔트리를 찾을 수 없습니다. entry={entryName}");
        return LoadXmlEntry(entry);
    }

    private static XDocument LoadXmlEntry(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxEntryBytes)
        {
            throw new InvalidDataException($"XLSX XML 엔트리 크기가 안전 상한을 초과했습니다. entry={entry.FullName}");
        }

        using var stream = entry.Open();
        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxEntryBytes
        });

        return XDocument.Load(xmlReader, LoadOptions.None);
    }

    private static int ParseRowNumber(XElement row)
    {
        var reference = (string?)row.Attribute("r");
        return int.TryParse(reference, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static int ParseColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            throw new InvalidDataException("XLSX 셀 참조가 비어 있습니다.");
        }

        var index = 0;
        foreach (var ch in cellReference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        if (index <= 0)
        {
            throw new InvalidDataException($"XLSX 셀 참조가 올바르지 않습니다. reference={cellReference}");
        }

        return index;
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

    private static IReadOnlyList<string> TrimTrailingEmpty(IReadOnlyList<string> values)
    {
        var count = values.Count;
        while (count > 0 && string.IsNullOrWhiteSpace(values[count - 1]))
        {
            count--;
        }

        return values.Take(count).ToArray();
    }

    private sealed record WorksheetPart(string Name, string Target);

    private sealed record WorksheetRecord(int RowNumber, IReadOnlyList<string> Values);
}

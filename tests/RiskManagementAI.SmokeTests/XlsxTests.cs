internal static class XlsxTests
{
    internal static void Run(SmokeTestContext context)
    {
        var profiler = new DataProfiler();
var xlsxSmokeDirectory = Path.Combine("artifacts", "smoke-xlsx-input-wp03");
Directory.CreateDirectory(xlsxSmokeDirectory);
var xlsxSmokePath = Path.Combine(xlsxSmokeDirectory, "risk_input_relationship_order.xlsx");
CreateSmokeXlsx(xlsxSmokePath);
var defaultXlsxTable = XlsxReader.Read(xlsxSmokePath);
context.AssertTrue(defaultXlsxTable.Columns.SequenceEqual(["Marker", "Value"]), "XlsxReader should read the first visible sheet by workbook order");
context.AssertTrue(defaultXlsxTable.Rows.Single().GetValue("Marker") == "FIRST", "XlsxReader default sheet should use workbook relationship target, not sheet file order");
var namedXlsxTable = XlsxReader.Read(xlsxSmokePath, "위험데이터");
context.AssertTrue(namedXlsxTable.Columns.SequenceEqual(["BASE_DT", "DESK_CD", "한글", "AMT"]), "XlsxReader should read named non-first sheet headers");
context.AssertTrue(namedXlsxTable.Rows.Single().GetValue("BASE_DT") == "20260617", "XlsxReader should read shared string cell values");
context.AssertTrue(namedXlsxTable.Rows.Single().GetValue("한글") == "값힣", "XlsxReader should read Korean rich shared strings");
context.AssertTrue(namedXlsxTable.Rows.Single().GetValue("AMT") == "10.5", "XlsxReader should read numeric cells as invariant text");
context.AssertTrue(namedXlsxTable.SourceName == "risk_input_relationship_order.xlsx", "XlsxReader should expose source name through CsvTable");
context.AssertTrue(profiler.ProfileTable(namedXlsxTable).NumericColumns["AMT"].Sum == 10.5m, "XlsxReader CsvTable should flow through DataProfiler pipeline");
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(xlsxSmokePath, "없는시트")), "XlsxReader should fail gracefully for missing sheet names");

var unsafeTargetXlsxPath = Path.Combine(xlsxSmokeDirectory, "unsafe_target.xlsx");
CreateUnsafeTargetXlsx(unsafeTargetXlsxPath);
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(unsafeTargetXlsxPath)), "XlsxReader should reject unsafe worksheet relationship targets");

var corruptXlsxPath = Path.Combine(xlsxSmokeDirectory, "corrupt.xlsx");
File.WriteAllText(corruptXlsxPath, "not a zip file");
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(corruptXlsxPath)), "XlsxReader should fail gracefully for corrupt xlsx files");

var tooManyRowsXlsxPath = Path.Combine(xlsxSmokeDirectory, "too_many_rows.xlsx");
CreateSmokeXlsx(tooManyRowsXlsxPath, tooManyRows: true);
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(tooManyRowsXlsxPath)), "XlsxReader should enforce worksheet row safety cap");
context.AssertTrue(context.Throws<ArgumentException>(() => XlsxReader.Read(Path.Combine(xlsxSmokeDirectory, "not_xlsx.csv"))), "XlsxReader should reject non-xlsx extensions");
    }

    private static void CreateUnsafeTargetXlsx(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteZipEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""");
        WriteZipEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");
        WriteZipEntry(archive, "xl/workbook.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Sheet1" sheetId="1" r:id="rIdUnsafe"/>
  </sheets>
</workbook>
""");
        WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rIdUnsafe" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="../worksheets/sheet1.xml"/>
</Relationships>
""");
        WriteZipEntry(archive, "xl/worksheets/sheet1.xml", """
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    <row r="1"><c r="A1" t="inlineStr"><is><t>BASE_DT</t></is></c></row>
  </sheetData>
</worksheet>
""");
    }
}

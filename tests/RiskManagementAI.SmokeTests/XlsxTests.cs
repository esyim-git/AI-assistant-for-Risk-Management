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

var corruptXlsxPath = Path.Combine(xlsxSmokeDirectory, "corrupt.xlsx");
File.WriteAllText(corruptXlsxPath, "not a zip file");
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(corruptXlsxPath)), "XlsxReader should fail gracefully for corrupt xlsx files");

var tooManyRowsXlsxPath = Path.Combine(xlsxSmokeDirectory, "too_many_rows.xlsx");
CreateSmokeXlsx(tooManyRowsXlsxPath, tooManyRows: true);
context.AssertTrue(context.Throws<InvalidDataException>(() => XlsxReader.Read(tooManyRowsXlsxPath)), "XlsxReader should enforce worksheet row safety cap");
context.AssertTrue(context.Throws<ArgumentException>(() => XlsxReader.Read(Path.Combine(xlsxSmokeDirectory, "not_xlsx.csv"))), "XlsxReader should reject non-xlsx extensions");
    }
}
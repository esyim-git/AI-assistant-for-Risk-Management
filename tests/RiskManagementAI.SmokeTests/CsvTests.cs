internal static class CsvTests
{
    internal static void Run(SmokeTestContext context)
    {
        var profiler = new DataProfiler();
var limitMonitor = new LimitMonitor();
context.AssertTrue(Cp949Decoder.MappingSha256 == Cp949Decoder.ExpectedMappingSha256, "CsvReader CP949 mapping hash should match pinned SHA256");
context.AssertTrue(Cp949Decoder.MappingEntryCount == Cp949Decoder.ExpectedMappingEntryCount, "CsvReader CP949 mapping should include full Windows-949/UHC entries");

var encodingSmokeDirectory = Path.Combine("artifacts", "smoke-csv-encoding-wp02");
Directory.CreateDirectory(encodingSmokeDirectory);
var utf8NoBomCsv = Path.Combine(encodingSmokeDirectory, "utf8_no_bom.csv");
File.WriteAllText(utf8NoBomCsv, "BASE_DT,DESK_CD,확장힣,AMT\n20260617,EQD,힣값,10\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
var utf8NoBomTable = CsvReader.Read(utf8NoBomCsv);
context.AssertTrue(utf8NoBomTable.Metadata.DetectedEncoding == CsvEncoding.Utf8 && !utf8NoBomTable.Metadata.HadUtf8Bom, "CsvReader should auto-detect UTF-8 without BOM");
context.AssertTrue(utf8NoBomTable.Rows.Single().GetValue("확장힣") == "힣값", "CsvReader should roundtrip UTF-8 UHC syllable text");

var utf8BomCsv = Path.Combine(encodingSmokeDirectory, "utf8_bom.csv");
File.WriteAllText(utf8BomCsv, "BASE_DT,DESK_CD,AMT\n20260617,EQD,10\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
var utf8BomTable = CsvReader.Read(utf8BomCsv);
context.AssertTrue(utf8BomTable.Metadata.DetectedEncoding == CsvEncoding.Utf8 && utf8BomTable.Metadata.HadUtf8Bom, "CsvReader should honor UTF-8 BOM");

var cp949UhcCsv = Path.Combine("samples", "dummy_data", "cp949_uhc_sample_cp949.csv");
var cp949UhcTable = CsvReader.Read(cp949UhcCsv);
context.AssertTrue(cp949UhcTable.Metadata.DetectedEncoding == CsvEncoding.Cp949, "CsvReader should auto-detect CP949 when UTF-8 decoding fails");
context.AssertTrue(cp949UhcTable.Columns.Contains("확장힣", StringComparer.Ordinal), "CsvReader CP949 should decode UHC extension syllable in header");
context.AssertTrue(cp949UhcTable.Rows.Single().GetValue("확장힣") == "힣값", "CsvReader CP949 should decode UHC extension syllable in value");
context.AssertTrue(cp949UhcTable.Metadata.Cp949MappingSha256 == Cp949Decoder.ExpectedMappingSha256, "CsvReader CP949 metadata should expose mapping SHA256");
context.AssertTrue(cp949UhcTable.Metadata.Cp949MappingEntryCount == Cp949Decoder.ExpectedMappingEntryCount, "CsvReader CP949 metadata should expose mapping entry count");
context.AssertTrue(CsvReader.Read(cp949UhcCsv, CsvEncoding.Cp949).Rows.Single().GetValue("확장힣") == "힣값", "CsvReader should support explicit CP949");
context.AssertTrue(CsvReader.ReadStreaming(cp949UhcCsv, CsvEncoding.Cp949).Rows.Single().GetValue("확장힣") == "힣값", "CsvReader CP949 streaming should reuse UHC decoder");
context.AssertTrue(context.Throws<InvalidDataException>(() => CsvReader.Read(cp949UhcCsv, CsvEncoding.Utf8)), "CsvReader explicit UTF-8 should reject CP949 bytes");

var cp949Profile = profiler.ProfileCsv(cp949UhcCsv);
context.AssertTrue(cp949Profile.SourceName == "cp949_uhc_sample_cp949.csv" && cp949Profile.RowCount == 1, "DataProfiler should use common CsvReader for CP949 files");
context.AssertTrue(cp949Profile.NumericColumns["AMT"].Sum == 10m, "DataProfiler should preserve numeric profiling through common CsvReader");
var cp949StreamingProfile = profiler.ProfileCsvStreaming(cp949UhcCsv, CsvEncoding.Cp949);
context.AssertTrue(cp949StreamingProfile.NumericColumns["AMT"].Sum == 10m && cp949StreamingProfile.BaseDateDistribution["20260617"] == 1, "DataProfiler CP949 streaming should preserve BASE_DT numeric profile");

var cp949LimitResult = limitMonitor.Analyze(
    Path.Combine("samples", "dummy_data", "risk_exposure_sample_cp949.csv"),
    Path.Combine("samples", "dummy_data", "risk_limit_sample_cp949.csv"),
    "20260617");
context.AssertTrue(cp949LimitResult.Rows.Count == 1, "LimitMonitor should use common CsvReader for CP949 files");
context.AssertTrue(cp949LimitResult.Rows.Single().RiskFactor == "KOSPI200힣", "LimitMonitor should preserve CP949 UHC join key text");
context.AssertTrue(cp949LimitResult.Rows.Single().Status == LimitMonitorStatus.Warning, "LimitMonitor should classify CP949 sample after common CsvReader join");

var cp949Catalog = RegulationCatalog.LoadFromFile(Path.Combine("samples", "dummy_data", "regulation_catalog_cp949.csv"));
context.AssertTrue(cp949Catalog.Entries.Single().Title == "힣 규정", "RegulationCatalog should use common CsvReader for CP949 catalog files");
    }
}

internal static class DataProfileTests
{
    internal static void Run(SmokeTestContext context)
    {
        var profiler = new DataProfiler();
var limitMonitor = new LimitMonitor();
var customColumnMappingResult = LoadCompleteCustomColumnMapping();
var profileSmokeDirectory = Path.Combine("artifacts", "smoke-profile-b05");
Directory.CreateDirectory(profileSmokeDirectory);
var profileSmokeCsv = Path.Combine(profileSmokeDirectory, "profile_sample.csv");
File.WriteAllText(profileSmokeCsv, "BASE_DT,DESK_CD,AMT\n20260617,EQD,10\n20260617,EQD,10\n20260618,,20\n");
var smallProfile = profiler.ProfileCsv(profileSmokeCsv);
context.AssertTrue(smallProfile.RowCount == 3, "Small profile sample should have 3 data rows");
context.AssertTrue(smallProfile.NullCounts["DESK_CD"] == 1, "DataProfiler should count null values");
context.AssertTrue(smallProfile.DuplicateRowCount == 1, "DataProfiler should count duplicate rows");
context.AssertTrue(smallProfile.BaseDateDistribution["20260617"] == 2 && smallProfile.BaseDateDistribution["20260618"] == 1, "DataProfiler should count BASE_DT values");
context.AssertTrue(smallProfile.NumericColumns["AMT"].Sum == 40m, "DataProfiler should compute small numeric sum");
var smallProfileStreaming = profiler.ProfileCsvStreaming(profileSmokeCsv);
AssertProfilesEqual(context, smallProfile, smallProfileStreaming, "DataProfiler streaming should match ProfileTable for BASE_DT duplicate numeric profile");

var welfordCsv = Path.Combine(profileSmokeDirectory, "profile_welford_outlier_streaming.csv");
using (var writer = new StreamWriter(welfordCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    writer.WriteLine("BASE_DT,AMT");
    for (var index = 0; index < 10; index++)
    {
        writer.WriteLine("20260617,0");
    }

    writer.WriteLine("20260617,1000");
}
var welfordProfile = profiler.ProfileCsvStreaming(welfordCsv);
var welfordAmount = welfordProfile.NumericColumns["AMT"];
context.AssertTrue(
    welfordAmount.NonNullCount == 11 && welfordAmount.Sum == 1000m && welfordAmount.Min == 0m && welfordAmount.Max == 1000m && welfordAmount.OutlierCount == 1,
    "DataProfiler streaming welford numeric profile should preserve 3-sigma OutlierCount");

var largeNumberOutlierCsv = Path.Combine(profileSmokeDirectory, "profile_welford_legacy_outlier_large_numbers.csv");
using (var writer = new StreamWriter(largeNumberOutlierCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    writer.WriteLine("BASE_DT,AMT");
    for (var index = 0; index < 9; index++)
    {
        writer.WriteLine("20260617,1000000000000000");
    }

    writer.WriteLine("20260617,1000000000000001");
}
var largeNumberLegacyProfile = profiler.ProfileCsv(largeNumberOutlierCsv);
var largeNumberStreamingProfile = profiler.ProfileCsvStreaming(largeNumberOutlierCsv);
context.AssertTrue(
    largeNumberLegacyProfile.NumericColumns["AMT"].OutlierCount == 1
    && largeNumberStreamingProfile.NumericColumns["AMT"].OutlierCount == 1,
    "DataProfiler streaming welford should preserve legacy large-number OutlierCount");

var mixedNonnumericOverflowCsv = Path.Combine(profileSmokeDirectory, "profile_mixed_nonnumeric_overflow.csv");
File.WriteAllText(
    mixedNonnumericOverflowCsv,
    $"BASE_DT,MIXED_ID\n20260617,{decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n20260617,{decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n20260617,TEXT_ID\n",
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
var mixedNonnumericLegacyProfile = profiler.ProfileCsv(mixedNonnumericOverflowCsv);
var mixedNonnumericStreamingProfile = profiler.ProfileCsvStreaming(mixedNonnumericOverflowCsv);
context.AssertTrue(
    !mixedNonnumericLegacyProfile.NumericColumns.ContainsKey("MIXED_ID") && !mixedNonnumericStreamingProfile.NumericColumns.ContainsKey("MIXED_ID"),
    "DataProfiler streaming should avoid overflow for duplicate mixed nonnumeric identifiers");

var largeStreamingCsv = Path.Combine(profileSmokeDirectory, "profile_large_streaming_within_cap.csv");
using (var writer = new StreamWriter(largeStreamingCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    writer.WriteLine("BASE_DT,DESK_CD,AMT");
    for (var index = 0; index < 4096; index++)
    {
        writer.WriteLine($"20260617,DESK_{index % 4},{index % 10}");
    }
}
var largeStreamingProfile = profiler.ProfileCsvStreaming(largeStreamingCsv);
context.AssertTrue(
    largeStreamingProfile.RowCount == 4096 && largeStreamingProfile.BaseDateDistribution["20260617"] == 4096 && largeStreamingProfile.NumericColumns["AMT"].Sum == 18420m,
    "DataProfiler streaming should process large BASE_DT input within MaxRowCount deterministically");

var byteCapCsv = Path.Combine(profileSmokeDirectory, "profile_streaming_byte_cap.csv");
using (var stream = File.Create(byteCapCsv))
{
    stream.SetLength(CsvReader.MaxByteSize + 1);
}
context.AssertTrue(
    ThrowsInvalidDataWithMaxActual(() => profiler.ProfileCsvStreaming(byteCapCsv)),
    "DataProfiler streaming MaxByteSize should throw InvalidDataException with max actual");

var rowCapCsv = Path.Combine(profileSmokeDirectory, "profile_streaming_row_cap.csv");
using (var writer = new StreamWriter(rowCapCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    writer.WriteLine("BASE_DT,AMT");
    for (var index = 0; index <= CsvReader.MaxRowCount; index++)
    {
        writer.WriteLine("20260617,1");
    }
}
context.AssertTrue(
    ThrowsInvalidDataWithMaxActual(() => profiler.ProfileCsvStreaming(rowCapCsv)),
    "DataProfiler streaming MaxRowCount should throw InvalidDataException with max actual");

var noBaseDateCsv = Path.Combine(profileSmokeDirectory, "profile_no_base_dt.csv");
File.WriteAllText(noBaseDateCsv, "DESK_CD,AMT\nEQD,10\nFIC,20\n");
var noBaseDateProfile = profiler.ProfileCsv(noBaseDateCsv);
context.AssertTrue(noBaseDateProfile.Warnings.Any(w => w.Contains("BASE_DT", StringComparison.OrdinalIgnoreCase)), "DataProfiler should warn when BASE_DT is missing");

var customMappingProfileCsv = Path.Combine(profileSmokeDirectory, "profile_custom_mapping_wp04.csv");
File.WriteAllText(customMappingProfileCsv, "BASE_DATE,DESK_CD,EXPOSURE\n20260617,EQD,10\n20260618,FIC,20\n");
var customMappingProfile = new DataProfiler(customColumnMappingResult).ProfileCsv(customMappingProfileCsv);
context.AssertTrue(customMappingProfile.BaseDateDistribution["20260617"] == 1 && customMappingProfile.BaseDateDistribution["20260618"] == 1, "DataProfiler should use ColumnMapping for BASE_DT distribution");

var customMappingExposureCsv = Path.Combine(profileSmokeDirectory, "limit_custom_mapping_exposure_wp04.csv");
var customMappingLimitCsv = Path.Combine(profileSmokeDirectory, "limit_custom_mapping_limit_wp04.csv");
File.WriteAllText(customMappingExposureCsv, "BASE_DATE,DESK_CD,PORT_ID,PRODUCT_TYPE,RISK_NM,CCY_CD,EXPOSURE\n20260617,EQD,PF_CUSTOM,Derivative,KOSPI200,KRW,95\n");
File.WriteAllText(customMappingLimitCsv, "BASE_DATE,PORT_ID,RISK_NM,LIMIT,ACTIVE_YN\n20260617,PF_CUSTOM,KOSPI200,100,Y\n");
var customMappingLimitResult = new LimitMonitor(customColumnMappingResult).Analyze(customMappingExposureCsv, customMappingLimitCsv, "20260617");
context.AssertTrue(customMappingLimitResult.Rows.Count == 1, "LimitMonitor should use ColumnMapping for renamed join columns");
context.AssertTrue(customMappingLimitResult.Rows.Single().Status == LimitMonitorStatus.Warning, "LimitMonitor should classify custom mapped rows with renamed amount columns");

    }

    private static void AssertProfilesEqual(SmokeTestContext context, DataProfileResult expected, DataProfileResult actual, string name)
    {
        context.AssertTrue(
            expected.SourceName == actual.SourceName
            && expected.RowCount == actual.RowCount
            && expected.ColumnCount == actual.ColumnCount
            && expected.Columns.SequenceEqual(actual.Columns, StringComparer.Ordinal)
            && DictionaryEquals(expected.NullCounts, actual.NullCounts)
            && expected.DuplicateRowCount == actual.DuplicateRowCount
            && DictionaryEquals(expected.BaseDateDistribution, actual.BaseDateDistribution)
            && NumericProfilesEqual(expected.NumericColumns, actual.NumericColumns)
            && expected.Warnings.SequenceEqual(actual.Warnings, StringComparer.Ordinal),
            name);
    }

    private static bool ThrowsInvalidDataWithMaxActual(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (InvalidDataException exception)
        {
            return exception.Message.Contains("max=", StringComparison.OrdinalIgnoreCase)
                && exception.Message.Contains("actual=", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool DictionaryEquals(IReadOnlyDictionary<string, int> left, IReadOnlyDictionary<string, int> right)
    {
        return left.Count == right.Count
            && left.All(item => right.TryGetValue(item.Key, out var value) && value == item.Value);
    }

    private static bool NumericProfilesEqual(
        IReadOnlyDictionary<string, NumericColumnProfile> left,
        IReadOnlyDictionary<string, NumericColumnProfile> right)
    {
        return left.Count == right.Count
            && left.All(item =>
                right.TryGetValue(item.Key, out var value)
                && item.Value.NonNullCount == value.NonNullCount
                && item.Value.Sum == value.Sum
                && item.Value.Min == value.Min
                && item.Value.Max == value.Max
                && item.Value.OutlierCount == value.OutlierCount);
    }
}

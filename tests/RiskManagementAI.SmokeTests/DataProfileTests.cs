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
}
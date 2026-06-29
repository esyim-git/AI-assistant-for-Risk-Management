internal sealed class TestRunner
{
    private readonly SmokeTestContext context = new();

    public void Run()
    {
        SafetyTests.Run(context);
        UiContractTests.Run(context);
        MappingTests.Run(context);
        GenerationTests.Run(context);
        KbTests.Run(context);
        NcrTests.Run(context);
        AssistTests.Run(context);
        AuditTests.Run(context);
        DataProfileTests.Run(context);
        LimitReconciliationTests.Run(context);
        ReportTests.Run(context);
        CsvTests.Run(context);
        XlsxTests.Run(context);
        PackagingTests.Run(context);
    }

    public int PrintSummaryAndGetExitCode()
    {
        return context.PrintSummaryAndGetExitCode();
    }
}

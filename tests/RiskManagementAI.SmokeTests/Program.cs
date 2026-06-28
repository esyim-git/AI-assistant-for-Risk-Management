var runner = new TestRunner();
runner.Run();
Environment.ExitCode = runner.PrintSummaryAndGetExitCode();
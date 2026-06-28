using System.Windows;
using RiskManagementAI.Core.Config;
using RiskManagementAI.Core.Integrity;

namespace RiskManagementAI.App;

public partial class App : Application
{
    public static PolicyLoadResult SecurityPolicyLoadResult { get; private set; } =
        new(SecurityPolicy.SafeDefaults(), UsedFallback: true, Warnings: ["Policy has not been loaded yet."]);

    public static IntegrityResult IntegrityResult { get; private set; } =
        RiskManagementAI.Core.Integrity.IntegrityResult.NotVerified();

    protected override void OnStartup(StartupEventArgs e)
    {
        // STAB-WP-03b: runtime fail-closed integrity gate runs BEFORE anything else (policy load,
        // base.OnStartup, window creation). Operational mode blocks startup on any manifest tamper.
        var devAllow = DevSwitchActive();
        IntegrityResult = IntegrityVerifier.VerifyPackage(AppContext.BaseDirectory, strict: !devAllow);
        if (IntegrityGate.Decide(IntegrityResult, devAllow) == GateDecision.Block)
        {
            // Audit-friendly message (hash prefixes only; no file contents).
            var detail = IntegrityResult.Warnings.Count > 0
                ? string.Join(Environment.NewLine, IntegrityResult.Warnings)
                : "approved_manifest.json verification failed.";
            MessageBox.Show(
                "패키지 무결성 검증에 실패하여 애플리케이션을 종료합니다.\n" +
                "Integrity verification failed; the application will not start.\n\n" +
                detail,
                "무결성 검증 실패 (Integrity verification failed)",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(2);
            Environment.Exit(2);
            return;
        }

        SecurityPolicyLoadResult = PolicyLoader.LoadDefault();
        base.OnStartup(e);
    }

    /// <summary>
    /// Hardened developer bypass (STAB-WP-03b). Requires BOTH an explicit environment switch AND an
    /// attached debugger, so a stray environment variable on an operational PC can never disable the
    /// gate during normal execution. Neither condition is present in a release package run.
    /// </summary>
    private static bool DevSwitchActive()
    {
        var envSet = string.Equals(
            Environment.GetEnvironmentVariable("RMAI_DEV_ALLOW_UNVERIFIED"),
            "1",
            StringComparison.Ordinal);
        return envSet && System.Diagnostics.Debugger.IsAttached;
    }
}

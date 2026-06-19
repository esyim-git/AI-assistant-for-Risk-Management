using System.Windows;
using RiskManagementAI.Core.Config;

namespace RiskManagementAI.App;

public partial class App : Application
{
    public static PolicyLoadResult SecurityPolicyLoadResult { get; private set; } =
        new(SecurityPolicy.SafeDefaults(), UsedFallback: true, Warnings: ["Policy has not been loaded yet."]);

    protected override void OnStartup(StartupEventArgs e)
    {
        SecurityPolicyLoadResult = PolicyLoader.LoadDefault();
        base.OnStartup(e);
    }
}

using System.Linq;
using System.Text;
using System.Windows;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private readonly SqlSafetyChecker _sqlChecker = new();
    private readonly VbaSafetyChecker _vbaChecker = new();
    private readonly Excel2021FunctionChecker _excelChecker = new();

    public MainWindow()
    {
        InitializeComponent();
        ResultBox.Text = "Risk Management AI Assistant가 오프라인 모드로 시작되었습니다.\nLocal LLM 모델이 없어도 룰 검사와 샘플 분석은 동작합니다.";
    }

    private void OnCheckSql(object sender, RoutedEventArgs e)
    {
        var findings = _sqlChecker.Check(RequestBox.Text).ToList();
        ShowFindings("SQL Safety Check", findings);
    }

    private void OnCheckVba(object sender, RoutedEventArgs e)
    {
        var findings = _vbaChecker.Check(RequestBox.Text).ToList();
        ShowFindings("VBA Safety Check", findings);
    }

    private void OnCheckExcel(object sender, RoutedEventArgs e)
    {
        var findings = _excelChecker.CheckFormula(RequestBox.Text).ToList();
        ShowFindings("Excel 2021 Function Check", findings);
    }

    private void ShowFindings(string title, System.Collections.Generic.IReadOnlyList<SafetyFinding> findings)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{title}]");
        sb.AppendLine($"Finding Count: {findings.Count}");
        sb.AppendLine();

        if (findings.Count == 0)
        {
            sb.AppendLine("위험 또는 호환성 경고가 탐지되지 않았습니다.");
        }
        else
        {
            foreach (var f in findings)
            {
                sb.AppendLine($"- {f.Severity} / {f.Code}: {f.Message}");
            }
        }

        ResultBox.Text = sb.ToString();
        SafetyStatusText.Text = findings.Any(f => f.Severity == SafetySeverity.Blocker || f.Severity == SafetySeverity.High)
            ? "High risk finding detected"
            : "No high risk finding";
    }
}

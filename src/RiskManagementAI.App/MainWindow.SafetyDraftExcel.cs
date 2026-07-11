using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using RiskManagementAI.Core.Excel;
using RiskManagementAI.Core.Generation;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private void OnCheckSql(object sender, RoutedEventArgs e)
    {
        var findings = _sqlChecker.Check(SqlRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("SqlSafetyCheck", nameof(SqlSafetyChecker), SqlRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("SQL Safety Check", findings);
    }

    private void OnCheckVba(object sender, RoutedEventArgs e)
    {
        var findings = _vbaChecker.Check(VbaRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("VbaSafetyCheck", nameof(VbaSafetyChecker), VbaRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("VBA Safety Check", findings);
    }

    private void OnGenerateSqlDraft(object sender, RoutedEventArgs e)
    {
        RunDraftPipeline(DraftRequestKind.Sql);
    }

    private void OnGenerateVbaDraft(object sender, RoutedEventArgs e)
    {
        RunDraftPipeline(DraftRequestKind.Vba);
    }

    private void RunDraftPipeline(DraftRequestKind kind)
    {
        var result = _draftPipeline.Generate(new DraftPipelineRequest(
            kind,
            DraftRequestBox.Text,
            Environment.UserName));
        DraftResultBox.Text = BuildDraftPipelineSummary(result);
        ShowFindings($"{kind} Draft Pipeline", result.Findings);
    }

    private void OnCheckExcel(object sender, RoutedEventArgs e)
    {
        var findings = _excelChecker.CheckFormula(ExcelRequestBox.Text).ToList();
        var auditFinding = AppendAuditLog("Excel2021FunctionCheck", nameof(Excel2021FunctionChecker), ExcelRequestBox.Text, findings);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        ShowFindings("Excel 2021 Function Check", findings);
    }

    private void InitializeExcelFunctionHelper()
    {
        RefreshExcelFunctionHelper(showFindings: false);
    }

    private void OnSearchExcelFunctionHelper(object sender, RoutedEventArgs e)
    {
        RefreshExcelFunctionHelper(showFindings: true);
    }

    private void RefreshExcelFunctionHelper(bool showFindings)
    {
        _excelFunctionHelperResults = _excelFunctionHelper.Search(ExcelFunctionHelperQueryBox.Text);
        ExcelFunctionHelperList.ItemsSource = _excelFunctionHelperResults;
        ExcelFunctionHelperList.SelectedIndex = _excelFunctionHelperResults.Count > 0 ? 0 : -1;
        if (_excelFunctionHelperResults.Count == 0)
        {
            ExcelFunctionHelperDetailBox.Text = _excelFunctionHelper.Warnings.Count > 0
                ? string.Join(Environment.NewLine, _excelFunctionHelper.Warnings)
                : "검색 결과가 없습니다.";
        }

        if (showFindings)
        {
            var findings = _excelFunctionHelper.Warnings
                .Select(warning => new SafetyFinding("EXCEL_HELPER_WARNING", SafetySeverity.Low, warning))
                .ToList();
            findings.Add(new SafetyFinding(
                "EXCEL_HELPER_SEARCH_RESULT",
                SafetySeverity.Info,
                $"Excel Function Helper 결과 {_excelFunctionHelperResults.Count:N0}건. 검색어 원문은 로그에 저장하지 않았습니다."));
            ShowFindings("Excel Function Helper", findings);
        }
    }

    private void OnExcelFunctionHelperSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ExcelFunctionHelperList.SelectedItem is ExcelFunctionInfo selected)
        {
            ExcelFunctionHelperDetailBox.Text = FormatExcelFunctionInfo(selected);
        }
    }

    private void OnInsertExcelFunctionExample(object sender, RoutedEventArgs e)
    {
        if (ExcelFunctionHelperList.SelectedItem is not ExcelFunctionInfo selected)
        {
            ShowFindings("Excel Function Helper", [
                new SafetyFinding("EXCEL_HELPER_NO_SELECTION", SafetySeverity.Info, "삽입할 함수 예시를 먼저 선택하세요.")
            ]);
            return;
        }

        _completionDebounceTimer.Stop();
        _suppressCompletionTextChanged = true;
        try
        {
            InsertExcelFunctionExample(ExcelRequestBox, _excelFunctionHelper.BuildFormulaInsertion(selected));
        }
        finally
        {
            _suppressCompletionTextChanged = false;
        }

        ExcelRequestBox.Focus();
        ShowFindings("Excel Function Helper", [
            new SafetyFinding(
                "EXCEL_HELPER_FORMULA_INSERTED",
                SafetySeverity.Info,
                $"{selected.Name} 예시 수식을 사용자 선택으로 삽입했습니다. 자동삽입은 수행하지 않습니다.")
        ]);
    }

    private static void InsertExcelFunctionExample(TextBox textBox, string formulaExample)
    {
        var safeFormula = formulaExample ?? string.Empty;
        if (textBox.SelectionLength > 0)
        {
            var selectionStart = textBox.SelectionStart;
            textBox.SelectedText = safeFormula;
            textBox.CaretIndex = selectionStart + safeFormula.Length;
            return;
        }

        var caretIndex = Math.Clamp(textBox.CaretIndex, 0, textBox.Text.Length);
        textBox.Text = textBox.Text.Insert(caretIndex, safeFormula);
        textBox.CaretIndex = caretIndex + safeFormula.Length;
    }

    private static string FormatExcelFunctionInfo(ExcelFunctionInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Function: {info.Name}");
        sb.AppendLine($"Excel 2021: {(info.Is365Only ? "Microsoft 365 only / blocked by active rules" : "Compatible with active rules")}");
        sb.AppendLine($"Recommended: {(info.Recommended ? "Yes" : "No")}");
        sb.AppendLine($"Args: {info.Args}");
        sb.AppendLine();
        sb.AppendLine(info.Description);
        sb.AppendLine();
        sb.AppendLine($"Risk example: {info.RiskMgmtExample}");
        sb.AppendLine($"Formula example: {info.FormulaExample}");
        sb.AppendLine($"Excel 2021 alternative: {info.Excel2021Alternative}");
        return sb.ToString();
    }

}

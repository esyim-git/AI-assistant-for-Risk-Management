using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RiskManagementAI.Core.Assist;
using RiskManagementAI.Core.Logging;
using RiskManagementAI.Core.Safety;

namespace RiskManagementAI.App;

public partial class MainWindow : Window
{
    private void InitializeCompletionAssist()
    {
        CompletionPopupControl.ItemAccepted += OnCompletionPopupItemAccepted;
        RegisterCompletionTextBox(SqlRequestBox, CompletionLanguage.Sql);
        RegisterCompletionTextBox(VbaRequestBox, CompletionLanguage.Vba);
        RegisterCompletionTextBox(ExcelRequestBox, CompletionLanguage.Excel);
        RegisterCompletionTextBox(RiskCommentRequestBox, CompletionLanguage.RiskComment);
    }

    private void RegisterCompletionTextBox(TextBox textBox, CompletionLanguage language)
    {
        _completionLanguages[textBox] = language;
        textBox.PreviewKeyDown += OnCompletionTextBoxPreviewKeyDown;
        textBox.TextChanged += OnCompletionTextBoxTextChanged;
    }

    private void OnCompletionTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || !_completionLanguages.TryGetValue(textBox, out var language))
        {
            return;
        }

        if (CompletionPopupControl.IsCompletionOpen && (e.Key is Key.Enter or Key.Tab))
        {
            e.Handled = CompletionPopupControl.TryAcceptSelected();
            return;
        }

        if (CompletionPopupControl.IsCompletionOpen && (e.Key is Key.Down or Key.Up))
        {
            e.Handled = CompletionPopupControl.MoveSelection(e.Key == Key.Down ? 1 : -1);
            return;
        }

        if (CompletionPopupControl.IsCompletionOpen && e.Key == Key.Escape)
        {
            CompletionPopupControl.Close();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Space)
        {
            ShowCompletionPopup(textBox, language, grabFocus: true, enforceAsYouTypePolicy: false);
            e.Handled = true;
        }
    }

    private void OnCompletionTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || !_completionLanguages.TryGetValue(textBox, out var language))
        {
            return;
        }

        var prefix = ExtractCompletionPrefix(textBox.Text, textBox.CaretIndex);
        var queryDecision = CompletionTriggerPolicy.EvaluateAsYouType(
            language,
            textBox.Text,
            prefix,
            matchCount: 1,
            _suppressCompletionTextChanged);
        if (!queryDecision.ShouldShow)
        {
            _completionDebounceTimer.Stop();
            if (_completionTargetBox == textBox)
            {
                CompletionPopupControl.Close();
            }

            return;
        }

        _pendingCompletionTextBox = textBox;
        _pendingCompletionLanguage = language;
        _completionDebounceTimer.Stop();
        _completionDebounceTimer.Start();
    }

    private void OnCompletionDebounceTimerTick(object? sender, EventArgs e)
    {
        _completionDebounceTimer.Stop();
        if (_pendingCompletionTextBox is null)
        {
            return;
        }

        ShowCompletionPopup(
            _pendingCompletionTextBox,
            _pendingCompletionLanguage,
            grabFocus: false,
            enforceAsYouTypePolicy: true);
    }

    private void ShowCompletionPopup(TextBox textBox, CompletionLanguage language, bool grabFocus = true, bool enforceAsYouTypePolicy = false)
    {
        var prefix = ExtractCompletionPrefix(textBox.Text, textBox.CaretIndex);
        if (enforceAsYouTypePolicy)
        {
            var queryDecision = CompletionTriggerPolicy.EvaluateAsYouType(
                language,
                textBox.Text,
                prefix,
                matchCount: 1,
                _suppressCompletionTextChanged);
            if (!queryDecision.ShouldShow)
            {
                if (_completionTargetBox == textBox)
                {
                    CompletionPopupControl.Close();
                }

                return;
            }
        }

        var context = new CompletionContext(
            language,
            textBox.Text,
            textBox.CaretIndex,
            prefix,
            CompletionEngine.NoModelMode);
        _completionResult = _completionEngine.GetCompletions(context);
        _completionTargetBox = textBox;
        _completionTargetLanguage = language;

        if (_completionResult.Findings.Count > 0)
        {
            ShowFindings("Smart Assist", _completionResult.Findings);
        }

        if (_completionResult.Items.Count == 0)
        {
            CompletionPopupControl.Close();
            if (!enforceAsYouTypePolicy)
            {
                ShowFindings("Smart Assist", [
                    new SafetyFinding("COMPLETION_NO_ITEMS", SafetySeverity.Info, "현재 입력 위치에 표시할 추천이 없습니다.")
                ]);
            }

            return;
        }

        if (enforceAsYouTypePolicy)
        {
            var showDecision = CompletionTriggerPolicy.EvaluateAsYouType(
                language,
                textBox.Text,
                prefix,
                _completionResult.Items.Count,
                _suppressCompletionTextChanged);
            if (!showDecision.ShouldShow)
            {
                CompletionPopupControl.Close();
                return;
            }
        }

        CompletionPopupControl.Show(textBox, _completionResult.Items, grabFocus);
    }

    private void OnCompletionPopupItemAccepted(object? sender, CompletionItem item)
    {
        AcceptCompletionItem(item);
    }

    private void AcceptCompletionItem(CompletionItem item)
    {
        if (_completionTargetBox is null)
        {
            CompletionPopupControl.Close();
            return;
        }

        var findings = (_completionResult?.Findings ?? Array.Empty<SafetyFinding>()).ToList();
        if (item.Finding is not null && !findings.Contains(item.Finding))
        {
            findings.Add(item.Finding);
        }

        if (!item.Insertable)
        {
            ShowFindings("Smart Assist", findings.Count == 0
                ? [new SafetyFinding("COMPLETION_HINT_SELECTED", SafetySeverity.Info, "안전 힌트는 정보 표시 전용이며 텍스트를 삽입하지 않습니다.")]
                : findings);
            _completionTargetBox.Focus();
            return;
        }

        _completionDebounceTimer.Stop();
        _suppressCompletionTextChanged = true;
        try
        {
            InsertCompletionText(_completionTargetBox, item.InsertText);
        }
        finally
        {
            _suppressCompletionTextChanged = false;
        }

        var auditFinding = AppendSuggestionAudit(item);
        if (auditFinding is not null)
        {
            findings.Add(auditFinding);
        }

        findings.Add(new SafetyFinding(
            "COMPLETION_ACCEPTED",
            SafetySeverity.Info,
            $"추천 항목을 삽입했습니다. Source={item.Source}, Kind={item.Kind}, RequiresReview={item.RequiresReview}."));
        ShowFindings("Smart Assist", findings);
        CompletionPopupControl.Close(restoreFocus: false);
        _completionTargetBox.Focus();
    }

    private SafetyFinding? AppendSuggestionAudit(CompletionItem item)
    {
        try
        {
            _suggestionLogWriter.Append(SuggestionLogEntry.FromAcceptedItem(
                item,
                _completionTargetLanguage,
                LogHash.Sha256Hex(Environment.UserName),
                DateTime.UtcNow,
                CompletionEngine.NoModelMode));
            return null;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return new SafetyFinding("SUGGESTION_LOG_WRITE_FAILED", SafetySeverity.High, ex.Message);
        }
    }

    private static void InsertCompletionText(TextBox textBox, string insertText)
    {
        var caretIndex = textBox.CaretIndex;
        var prefix = ExtractCompletionPrefix(textBox.Text, caretIndex);
        var replaceStart = Math.Max(0, caretIndex - prefix.Length);
        textBox.Text = textBox.Text.Remove(replaceStart, prefix.Length).Insert(replaceStart, insertText);
        textBox.CaretIndex = replaceStart + insertText.Length;
    }

    private static string ExtractCompletionPrefix(string text, int caretIndex)
    {
        if (string.IsNullOrEmpty(text) || caretIndex <= 0)
        {
            return string.Empty;
        }

        var safeCaretIndex = Math.Min(caretIndex, text.Length);
        var start = safeCaretIndex;
        while (start > 0 && IsCompletionPrefixChar(text[start - 1]))
        {
            start--;
        }

        return text[start..safeCaretIndex];
    }

    private static bool IsCompletionPrefixChar(char value)
    {
        return char.IsLetterOrDigit(value) || value is '_' or '.';
    }

}

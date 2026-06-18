using System.Text.RegularExpressions;

namespace RiskManagementAI.Core.Safety;

public sealed class VbaSafetyChecker
{
    private static readonly RulePattern[] DenyRules =
    [
        new("VBA_SHELL", @"\bShell\b", SafetySeverity.Blocker, "외부 프로그램 실행 가능성이 있는 Shell 호출은 금지됩니다."),
        new("VBA_WSCRIPT", @"WScript\.Shell", SafetySeverity.Blocker, "WScript.Shell 호출은 금지됩니다."),
        new("VBA_KILL", @"\bKill\b", SafetySeverity.Blocker, "파일 삭제 명령 Kill은 금지됩니다."),
        new("VBA_WINAPI", @"Declare\s+PtrSafe", SafetySeverity.High, "WinAPI 호출 가능성이 있어 검토가 필요합니다."),
        new("VBA_FSO", @"Scripting\.FileSystemObject", SafetySeverity.High, "FileSystemObject 사용은 파일 삭제/이동 가능성이 있어 검토가 필요합니다."),
        new("VBA_OUTLOOK", @"Outlook\.Application", SafetySeverity.High, "Outlook 자동 발송 가능성이 있어 검토가 필요합니다."),
        new("VBA_HTTP", @"(WinHttp|MSXML2\.XMLHTTP)", SafetySeverity.Blocker, "외부 네트워크 호출 가능성이 있는 HTTP 객체는 금지됩니다.")
    ];

    public IEnumerable<SafetyFinding> Check(string vbaText)
    {
        if (string.IsNullOrWhiteSpace(vbaText))
        {
            yield return new SafetyFinding("VBA_EMPTY", SafetySeverity.Low, "VBA 텍스트가 비어 있습니다.");
            yield break;
        }

        foreach (var rule in DenyRules)
        {
            var match = Regex.Match(vbaText, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(rule.Code, rule.Severity, rule.Message, match.Value, match.Index);
            }
        }

        if (!Regex.IsMatch(vbaText, @"^\s*Option\s+Explicit\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant))
        {
            yield return new SafetyFinding("VBA_OPTION_EXPLICIT_MISSING", SafetySeverity.Medium, "Option Explicit이 누락되었습니다.");
        }
    }
}

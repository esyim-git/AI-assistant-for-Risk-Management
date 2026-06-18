using System.Text.RegularExpressions;

namespace RiskManagementAI.Core.Safety;

public sealed class SqlSafetyChecker
{
    private static readonly RulePattern[] DenyRules =
    [
        new("SQL_DML_INSERT", @"\bINSERT\b", SafetySeverity.Blocker, "조회 전용 원칙에 위배되는 INSERT가 포함되어 있습니다."),
        new("SQL_DML_UPDATE", @"\bUPDATE\b", SafetySeverity.Blocker, "조회 전용 원칙에 위배되는 UPDATE가 포함되어 있습니다."),
        new("SQL_DML_DELETE", @"\bDELETE\b", SafetySeverity.Blocker, "조회 전용 원칙에 위배되는 DELETE가 포함되어 있습니다."),
        new("SQL_DML_MERGE", @"\bMERGE\b", SafetySeverity.Blocker, "조회 전용 원칙에 위배되는 MERGE가 포함되어 있습니다."),
        new("SQL_DDL", @"\b(CREATE|ALTER|DROP|TRUNCATE)\b", SafetySeverity.Blocker, "DDL 명령은 초기 MVP에서 금지됩니다."),
        new("SQL_PRIVILEGE", @"\b(GRANT|REVOKE)\b", SafetySeverity.Blocker, "권한 변경 명령은 금지됩니다."),
        new("SQL_EXEC", @"\b(EXEC|CALL)\b", SafetySeverity.Blocker, "프로시저 실행 명령은 금지됩니다."),
        new("SQL_TX", @"\b(COMMIT|ROLLBACK)\b", SafetySeverity.High, "트랜잭션 제어 명령은 초기 MVP에서 허용하지 않습니다.")
    ];

    public IEnumerable<SafetyFinding> Check(string sqlText)
    {
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            yield return new SafetyFinding("SQL_EMPTY", SafetySeverity.Low, "SQL 텍스트가 비어 있습니다.");
            yield break;
        }

        foreach (var rule in DenyRules)
        {
            var match = Regex.Match(sqlText, rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                yield return new SafetyFinding(rule.Code, rule.Severity, rule.Message, match.Value, match.Index);
            }
        }

        if (Regex.IsMatch(sqlText, @"SELECT\s+\*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return new SafetyFinding("SQL_SELECT_STAR", SafetySeverity.Medium, "SELECT * 사용은 컬럼 변경 및 성능 리스크가 있으므로 명시 컬럼을 권장합니다.");
        }

        if (!Regex.IsMatch(sqlText, @"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return new SafetyFinding("SQL_NO_SELECT", SafetySeverity.Medium, "조회 SQL로 보이지 않습니다. 초기 MVP는 SELECT 계열만 권장합니다.");
        }
    }
}

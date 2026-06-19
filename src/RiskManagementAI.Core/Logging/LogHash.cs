using System.Security.Cryptography;
using System.Text;

namespace RiskManagementAI.Core.Logging;

public static class LogHash
{
    public static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool IsSha256Hex(string? value)
    {
        return value is { Length: 64 } && value.All(IsHexCharacter);
    }

    private static bool IsHexCharacter(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}

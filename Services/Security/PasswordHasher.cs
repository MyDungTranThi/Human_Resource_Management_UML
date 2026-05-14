using System.Security.Cryptography;
using System.Text;

namespace HRM.Web.Services.Security;

public static class PasswordHasher
{
    public static string Hash(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string plainText, string storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return false;
        }

        // Backward compatibility for legacy plain-text passwords.
        if (string.Equals(plainText, storedValue, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(
            Hash(plainText),
            storedValue,
            StringComparison.OrdinalIgnoreCase);
    }
}

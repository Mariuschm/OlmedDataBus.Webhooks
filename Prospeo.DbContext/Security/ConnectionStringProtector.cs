using System.Security.Cryptography;
using System.Text;

namespace Prospeo.DbContext.Security;

internal static class ConnectionStringProtector
{
    private const string EncryptedPrefix = "ENC:";

    public static string DecryptConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        var passwordPattern = "Password=";
        var passwordIndex = connectionString.IndexOf(passwordPattern, StringComparison.OrdinalIgnoreCase);

        if (passwordIndex == -1)
            return connectionString;

        var startIndex = passwordIndex + passwordPattern.Length;
        var endIndex = connectionString.IndexOf(';', startIndex);

        if (endIndex == -1)
            endIndex = connectionString.Length;

        var passwordValue = connectionString.Substring(startIndex, endIndex - startIndex);

        if (!IsEncrypted(passwordValue))
            return connectionString;

        var decryptedPassword = Decrypt(passwordValue);

        return connectionString.Substring(0, startIndex) +
               decryptedPassword +
               connectionString.Substring(endIndex);
    }

    private static byte[] GetEncryptionKey()
    {
        var machineKey = Environment.MachineName + Environment.UserName;
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
    }

    private static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        if (!encryptedText.StartsWith(EncryptedPrefix))
            return encryptedText;

        try
        {
            var cipherText = encryptedText.Substring(EncryptedPrefix.Length);
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();

            var iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new CryptographicException(
                $"Nie można odszyfrować wartości. Upewnij się, że zaszyfrowano ją na tej samej maszynie i koncie użytkownika. Błąd: {ex.Message}",
                ex);
        }
    }

    private static bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptedPrefix);
    }
}
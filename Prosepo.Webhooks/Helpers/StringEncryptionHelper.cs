using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Prosepo.Webhooks.Helpers
{
    /// <summary>
    /// Helper do szyfrowania i odszyfrowywania stringów u¿ywaj¹c AES-256
    /// </summary>
    public static class StringEncryptionHelper
    {
        private const int KeySize = 256;
        private const int IvSize = 128;
        
        /// <summary>
        /// Generuje nowy klucz szyfrowania (Base64)
        /// </summary>
        /// <returns>Klucz szyfrowania w formacie Base64</returns>
        public static string GenerateKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        /// <summary>
        /// Szyfruje tekst przy u¿yciu klucza AES-256
        /// </summary>
        /// <param name="plainText">Tekst do zaszyfrowania</param>
        /// <param name="key">Klucz szyfrowania (Base64)</param>
        /// <returns>Zaszyfrowany tekst (Base64)</returns>
        /// <exception cref="ArgumentException">Gdy tekst lub klucz jest pusty</exception>
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Tekst do zaszyfrowania nie mo¿e byæ pusty", nameof(plainText));
            
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Klucz szyfrowania nie mo¿e byæ pusty", nameof(key));

            byte[] keyBytes = Convert.FromBase64String(key);
            
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Key = keyBytes;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            
            // Zapisz IV na pocz¹tku zaszyfrowanych danych
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            
            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        /// <summary>
        /// Odszyfrowuje tekst przy u¿yciu klucza AES-256
        /// </summary>
        /// <param name="cipherText">Zaszyfrowany tekst (Base64)</param>
        /// <param name="key">Klucz szyfrowania (Base64)</param>
        /// <returns>Odszyfrowany tekst</returns>
        /// <exception cref="ArgumentException">Gdy zaszyfrowany tekst lub klucz jest pusty</exception>
        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("Zaszyfrowany tekst nie mo¿e byæ pusty", nameof(cipherText));
            
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Klucz szyfrowania nie mo¿e byæ pusty", nameof(key));

            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            
            using var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Key = keyBytes;
            
            // Odczytaj IV z pocz¹tku zaszyfrowanych danych
            byte[] iv = new byte[IvSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }

        /// <summary>
        /// Sprawdza, czy tekst jest w formacie zaszyfrowanym (Base64)
        /// </summary>
        /// <param name="text">Tekst do sprawdzenia</param>
        /// <returns>True jeœli tekst wygl¹da na zaszyfrowany</returns>
        public static bool IsEncrypted(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                // SprawdŸ czy to poprawny Base64
                byte[] data = Convert.FromBase64String(text);
                // Zaszyfrowane dane powinny byæ d³u¿sze ni¿ sam IV
                return data.Length > IvSize / 8;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Odszyfrowuje tekst tylko jeœli jest zaszyfrowany, w przeciwnym razie zwraca oryginalny tekst
        /// </summary>
        /// <param name="text">Tekst (zaszyfrowany lub niezaszyfrowany)</param>
        /// <param name="key">Klucz szyfrowania (Base64)</param>
        /// <returns>Odszyfrowany tekst lub oryginalny tekst</returns>
        public static string DecryptIfEncrypted(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (!IsEncrypted(text))
                return text;

            try
            {
                return Decrypt(text, key);
            }
            catch
            {
                // Jeœli nie uda³o siê odszyfrowaæ, zwróæ oryginalny tekst
                return text;
            }
        }
    }
}

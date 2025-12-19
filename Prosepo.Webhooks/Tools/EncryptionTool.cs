using System;
using Prosepo.Webhooks.Helpers;

namespace Prosepo.Webhooks.Tools
{
    /// <summary>
    /// Narzêdzie CLI do szyfrowania i odszyfrowywania wartoœci konfiguracyjnych
    /// U¿ycie:
    /// - Generowanie klucza: dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key
    /// - Szyfrowanie: dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt "tekst" --key "klucz_base64"
    /// - Odszyfrowywanie: dotnet run --project Prosepo.Webhooks -- encrypt-tool --decrypt "zaszyfrowany_tekst" --key "klucz_base64"
    /// </summary>
    public static class EncryptionTool
    {
        public static void Run(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            try
            {
                if (HasArg(args, "--generate-key"))
                {
                    GenerateKey();
                }
                else if (HasArg(args, "--encrypt"))
                {
                    string text = GetArgValue(args, "--encrypt");
                    string key = GetArgValue(args, "--key");
                    EncryptText(text, key);
                }
                else if (HasArg(args, "--decrypt"))
                {
                    string text = GetArgValue(args, "--decrypt");
                    string key = GetArgValue(args, "--key");
                    DecryptText(text, key);
                }
                else
                {
                    ShowHelp();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"? B³¹d: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void GenerateKey()
        {
            string key = StringEncryptionHelper.GenerateKey();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?? Wygenerowany klucz szyfrowania:");
            Console.ResetColor();
            Console.WriteLine(key);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("??  WA¯NE: Zapisz ten klucz w bezpiecznym miejscu!");
            Console.WriteLine("   Dodaj go do User Secrets lub zmiennych œrodowiskowych jako 'Encryption:Key'");
            Console.ResetColor();
        }

        private static void EncryptText(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Tekst do zaszyfrowania nie mo¿e byæ pusty");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Klucz szyfrowania nie mo¿e byæ pusty. U¿yj --generate-key aby wygenerowaæ nowy klucz.");
            }

            string encrypted = StringEncryptionHelper.Encrypt(text, key);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?? Zaszyfrowany tekst:");
            Console.ResetColor();
            Console.WriteLine(encrypted);
        }

        private static void DecryptText(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Tekst do odszyfrowania nie mo¿e byæ pusty");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Klucz szyfrowania nie mo¿e byæ pusty");
            }

            string decrypted = StringEncryptionHelper.Decrypt(text, key);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("?? Odszyfrowany tekst:");
            Console.ResetColor();
            Console.WriteLine(decrypted);
        }

        private static bool HasArg(string[] args, string arg)
        {
            return Array.IndexOf(args, arg) >= 0;
        }

        private static string GetArgValue(string[] args, string arg)
        {
            int index = Array.IndexOf(args, arg);
            if (index >= 0 && index + 1 < args.Length)
            {
                return args[index + 1];
            }
            return string.Empty;
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("??????????????????????????????????????????????????????????????");
            Console.WriteLine("?     ?? Narzêdzie do szyfrowania danych konfiguracyjnych   ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("U¿ycie:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  1. Generowanie klucza szyfrowania:");
            Console.ResetColor();
            Console.WriteLine("     dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  2. Szyfrowanie tekstu:");
            Console.ResetColor();
            Console.WriteLine("     dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt \"moj_sekret\" --key \"klucz_base64\"");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  3. Odszyfrowywanie tekstu:");
            Console.ResetColor();
            Console.WriteLine("     dotnet run --project Prosepo.Webhooks -- encrypt-tool --decrypt \"zaszyfrowany_tekst\" --key \"klucz_base64\"");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Przyk³ad:");
            Console.ResetColor();
            Console.WriteLine("  # Wygeneruj klucz");
            Console.WriteLine("  dotnet run --project Prosepo.Webhooks -- encrypt-tool --generate-key");
            Console.WriteLine();
            Console.WriteLine("  # Zaszyfruj has³o API");
            Console.WriteLine("  dotnet run --project Prosepo.Webhooks -- encrypt-tool --encrypt \"moje_haslo_api\" --key \"ABC123...\"");
            Console.WriteLine();
        }
    }
}

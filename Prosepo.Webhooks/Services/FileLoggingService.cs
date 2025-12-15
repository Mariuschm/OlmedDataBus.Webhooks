using System.Collections.Concurrent;
using System.Text.Json;

namespace Prosepo.Webhooks.Services
{
    /// <summary>
    /// Serwis do logowania aplikacji do plików z rotacj¹ dzienn¹.
    /// Zapewnia thread-safe zapisywanie logów z kategoryzacj¹ i formatowaniem.
    /// Wspiera tryb debug (wszystkie poziomy) i produkcyjny (tylko b³êdy).
    /// </summary>
    public class FileLoggingService
    {
        private readonly string _logsDirectory;
        private readonly IConfiguration _configuration;
        private readonly bool _isDebug;
        private static readonly object _lock = new object();
        private static readonly ConcurrentDictionary<string, string> _fileCache = new();

        /// <summary>
        /// Inicjalizuje now¹ instancjê FileLoggingService.
        /// </summary>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        public FileLoggingService(IConfiguration configuration)
        {
            _configuration = configuration;
            _logsDirectory = _configuration["Logging:File:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            _isDebug = _configuration.GetValue<bool>("Logging:File:IsDebug", false);
            EnsureLogsDirectoryExists();
        }

        /// <summary>
        /// Sprawdza czy log powinien byæ zapisany na podstawie poziomu i trybu debug.
        /// W trybie debug logowane s¹ wszystkie poziomy.
        /// W trybie produkcyjnym logowane s¹ tylko Error i Critical.
        /// </summary>
        /// <param name="level">Poziom logowania</param>
        /// <returns>True jeœli log powinien byæ zapisany</returns>
        private bool ShouldLog(LogLevel level)
        {
            // W trybie debug logujemy wszystkie poziomy
            if (_isDebug)
            {
                return true;
            }

            // W trybie produkcyjnym logujemy tylko Error i Critical
            return level >= LogLevel.Error;
        }

        /// <summary>
        /// Zapisuje log do pliku z okreœlon¹ kategori¹ i poziomem.
        /// </summary>
        /// <param name="category">Kategoria logu (np. "webhook", "cron", "scheduler")</param>
        /// <param name="level">Poziom logowania</param>
        /// <param name="message">Wiadomoœæ do zalogowania</param>
        /// <param name="exception">Opcjonalny wyj¹tek do zalogowania</param>
        /// <param name="additionalData">Dodatkowe dane do za³¹czenia</param>
        public async Task LogAsync(string category, LogLevel level, string message, Exception? exception = null, object? additionalData = null)
        {
            // SprawdŸ czy log powinien byæ zapisany
            if (!ShouldLog(level))
            {
                return;
            }

            try
            {
                var logEntry = CreateLogEntry(category, level, message, exception, additionalData);
                var fileName = GetLogFileName(category);
                var filePath = Path.Combine(_logsDirectory, fileName);

                // Thread-safe zapis do pliku
                using var semaphore = new SemaphoreSlim(1, 1);
                await semaphore.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(filePath, logEntry + Environment.NewLine);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch
            {
                // Nie logujemy b³êdów logowania aby unikn¹æ nieskoñczonych pêtli
            }
        }

        /// <summary>
        /// Zapisuje strukturalny log w formacie JSON.
        /// </summary>
        /// <param name="category">Kategoria logu</param>
        /// <param name="level">Poziom logowania</param>
        /// <param name="message">Wiadomoœæ</param>
        /// <param name="data">Dane strukturalne do zapisania jako JSON</param>
        public async Task LogStructuredAsync(string category, LogLevel level, string message, object data)
        {
            // SprawdŸ czy log powinien byæ zapisany
            if (!ShouldLog(level))
            {
                return;
            }

            try
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    Category = category,
                    Level = level.ToString(),
                    Message = message,
                    Data = data
                };

                var jsonLine = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });

                var fileName = GetStructuredLogFileName(category);
                var filePath = Path.Combine(_logsDirectory, fileName);

                // Thread-safe zapis do pliku
                using var semaphore = new SemaphoreSlim(1, 1);
                await semaphore.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync(filePath, jsonLine + Environment.NewLine);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch
            {
                // Nie logujemy b³êdów logowania
            }
        }

        /// <summary>
        /// Tworzy wpis logu w standardowym formacie tekstowym.
        /// </summary>
        private string CreateLogEntry(string category, LogLevel level, string message, Exception? exception, object? additionalData)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] [{category}] {message}";

            if (exception != null)
            {
                logEntry += $"{Environment.NewLine}Exception: {exception}";
            }

            if (additionalData != null)
            {
                try
                {
                    var dataJson = JsonSerializer.Serialize(additionalData, new JsonSerializerOptions { WriteIndented = true });
                    logEntry += $"{Environment.NewLine}AdditionalData: {dataJson}";
                }
                catch
                {
                    logEntry += $"{Environment.NewLine}AdditionalData: {additionalData}";
                }
            }

            return logEntry;
        }

        /// <summary>
        /// Generuje nazwê pliku logu na podstawie kategorii i daty.
        /// </summary>
        private string GetLogFileName(string category)
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var cacheKey = $"{category}_{date}";
            
            return _fileCache.GetOrAdd(cacheKey, key => $"app_{category}_{date}.log");
        }

        /// <summary>
        /// Generuje nazwê pliku dla logów strukturalnych (JSON).
        /// </summary>
        private string GetStructuredLogFileName(string category)
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var cacheKey = $"structured_{category}_{date}";
            
            return _fileCache.GetOrAdd(cacheKey, key => $"app_structured_{category}_{date}.json");
        }

        /// <summary>
        /// Zapewnia istnienie katalogu logów.
        /// </summary>
        private void EnsureLogsDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logsDirectory))
                {
                    Directory.CreateDirectory(_logsDirectory);
                }
            }
            catch
            {
                // Jeœli nie mo¿na utworzyæ katalogu, logi bêd¹ zapisywane w katalogu aplikacji
            }
        }

        /// <summary>
        /// Czyœci stare pliki logów zgodnie z konfiguracj¹ retencji.
        /// </summary>
        public async Task CleanupOldLogsAsync()
        {
            try
            {
                var retentionDays = _configuration.GetValue<int>("Logging:File:RetentionDays", 30);
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                var logFiles = Directory.GetFiles(_logsDirectory, "app_*.log")
                    .Concat(Directory.GetFiles(_logsDirectory, "app_*.json"))
                    .Where(file => File.GetCreationTime(file) < cutoffDate);

                foreach (var file in logFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Kontynuuj z nastêpnym plikiem jeœli nie mo¿na usun¹æ aktualnego
                    }
                }
            }
            catch
            {
                // B³êdy czyszczenia nie powinny wp³ywaæ na dzia³anie aplikacji
            }
        }

        /// <summary>
        /// Pobiera listê dostêpnych plików logów.
        /// </summary>
        /// <returns>Lista informacji o plikach logów</returns>
        public async Task<List<LogFileInfo>> GetLogFilesAsync()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logsDirectory, "app_*")
                    .Select(filePath => new LogFileInfo
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        Size = new FileInfo(filePath).Length,
                        CreatedAt = File.GetCreationTime(filePath),
                        LastModified = File.GetLastWriteTime(filePath),
                        IsStructured = filePath.EndsWith(".json")
                    })
                    .OrderByDescending(f => f.LastModified)
                    .ToList();

                return logFiles;
            }
            catch
            {
                return new List<LogFileInfo>();
            }
        }
    }

    /// <summary>
    /// Informacje o pliku logu.
    /// </summary>
    public class LogFileInfo
    {
        /// <summary>
        /// Nazwa pliku.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Pe³na œcie¿ka do pliku.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Rozmiar pliku w bajtach.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Data utworzenia pliku.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data ostatniej modyfikacji.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Czy plik zawiera logi strukturalne (JSON).
        /// </summary>
        public bool IsStructured { get; set; }
    }
}
using System.Text.Json;
using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Services
{
    /// <summary>
    /// Serwis zarz¹dzaj¹cy konfiguracj¹ synchronizacji produktów.
    /// £aduje konfiguracje z pliku JSON lub bazy danych i udostêpnia je do tworzenia zadañ cyklicznych.
    /// </summary>
    public class ProductSyncConfigurationService
    {
        private readonly ILogger<ProductSyncConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _configurationFilePath;
        private ProductSyncConfigurationCollection? _cachedConfigurations;
        private DateTime _lastFileCheck = DateTime.MinValue;
        private readonly TimeSpan _cacheValidityTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Inicjalizuje now¹ instancjê ProductSyncConfigurationService.
        /// </summary>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        public ProductSyncConfigurationService(ILogger<ProductSyncConfigurationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Œcie¿ka do pliku konfiguracji z mo¿liwoœci¹ nadpisania przez appsettings.json
            _configurationFilePath = _configuration["ProductSync:ConfigurationFile"] 
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "product-sync-config.json");
            
            EnsureConfigurationFileExists();
        }

        /// <summary>
        /// Pobiera wszystkie aktywne konfiguracje synchronizacji produktów.
        /// U¿ywa cache z automatycznym odœwie¿aniem co 5 minut.
        /// </summary>
        /// <returns>Lista aktywnych konfiguracji synchronizacji</returns>
        public async Task<List<ProductSyncConfiguration>> GetActiveConfigurationsAsync()
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                return configurations.Configurations.Where(c => c.IsActive).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji synchronizacji produktów");
                return new List<ProductSyncConfiguration>();
            }
        }

        /// <summary>
        /// Pobiera wszystkie konfiguracje synchronizacji produktów (aktywne i nieaktywne).
        /// </summary>
        /// <returns>Lista wszystkich konfiguracji synchronizacji</returns>
        public async Task<List<ProductSyncConfiguration>> GetAllConfigurationsAsync()
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                return configurations.Configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania wszystkich konfiguracji synchronizacji produktów");
                return new List<ProductSyncConfiguration>();
            }
        }

        /// <summary>
        /// Pobiera konkretn¹ konfiguracjê synchronizacji po ID.
        /// </summary>
        /// <param name="configurationId">Identyfikator konfiguracji</param>
        /// <returns>Konfiguracja jeœli istnieje, null w przeciwnym przypadku</returns>
        public async Task<ProductSyncConfiguration?> GetConfigurationByIdAsync(string configurationId)
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                return configurations.Configurations.FirstOrDefault(c => c.Id == configurationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji synchronizacji: {ConfigurationId}", configurationId);
                return null;
            }
        }

        /// <summary>
        /// Dodaje lub aktualizuje konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configuration">Konfiguracja do dodania/aktualizacji</param>
        /// <returns>True jeœli operacja siê powiod³a</returns>
        public async Task<bool> SaveConfigurationAsync(ProductSyncConfiguration configuration)
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                
                // ZnajdŸ istniej¹c¹ konfiguracjê lub dodaj now¹
                var existingIndex = configurations.Configurations.FindIndex(c => c.Id == configuration.Id);
                if (existingIndex >= 0)
                {
                    configurations.Configurations[existingIndex] = configuration;
                    _logger.LogInformation("Aktualizowano konfiguracjê synchronizacji: {ConfigurationId}", configuration.Id);
                }
                else
                {
                    configurations.Configurations.Add(configuration);
                    _logger.LogInformation("Dodano now¹ konfiguracjê synchronizacji: {ConfigurationId}", configuration.Id);
                }

                configurations.LastModified = DateTime.UtcNow;
                await SaveConfigurationsToFileAsync(configurations);
                _cachedConfigurations = null; // Wyczyœæ cache

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania konfiguracji synchronizacji: {ConfigurationId}", configuration.Id);
                return false;
            }
        }

        /// <summary>
        /// Usuwa konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <param name="configurationId">Identyfikator konfiguracji do usuniêcia</param>
        /// <returns>True jeœli konfiguracja zosta³a usuniêta</returns>
        public async Task<bool> DeleteConfigurationAsync(string configurationId)
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                var removed = configurations.Configurations.RemoveAll(c => c.Id == configurationId) > 0;
                
                if (removed)
                {
                    configurations.LastModified = DateTime.UtcNow;
                    await SaveConfigurationsToFileAsync(configurations);
                    _cachedConfigurations = null; // Wyczyœæ cache
                    _logger.LogInformation("Usuniêto konfiguracjê synchronizacji: {ConfigurationId}", configurationId);
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas usuwania konfiguracji synchronizacji: {ConfigurationId}", configurationId);
                return false;
            }
        }

        /// <summary>
        /// £aduje konfiguracje z pliku z obs³ug¹ cache.
        /// </summary>
        /// <returns>Kolekcja konfiguracji synchronizacji</returns>
        private async Task<ProductSyncConfigurationCollection> LoadConfigurationsAsync()
        {
            // SprawdŸ cache
            if (_cachedConfigurations != null && DateTime.UtcNow - _lastFileCheck < _cacheValidityTime)
            {
                return _cachedConfigurations;
            }

            try
            {
                if (!File.Exists(_configurationFilePath))
                {
                    _logger.LogWarning("Plik konfiguracji nie istnieje: {FilePath}", _configurationFilePath);
                    return CreateDefaultConfiguration();
                }

                var jsonContent = await File.ReadAllTextAsync(_configurationFilePath);
                var configurations = JsonSerializer.Deserialize<ProductSyncConfigurationCollection>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });

                if (configurations == null)
                {
                    _logger.LogWarning("Nie mo¿na deserializowaæ konfiguracji z pliku: {FilePath}", _configurationFilePath);
                    return CreateDefaultConfiguration();
                }

                _cachedConfigurations = configurations;
                _lastFileCheck = DateTime.UtcNow;
                
                _logger.LogInformation("Za³adowano {Count} konfiguracji synchronizacji produktów", configurations.Configurations.Count);
                return configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas ³adowania konfiguracji z pliku: {FilePath}", _configurationFilePath);
                return CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Zapisuje konfiguracje do pliku JSON.
        /// </summary>
        /// <param name="configurations">Konfiguracje do zapisania</param>
        private async Task SaveConfigurationsToFileAsync(ProductSyncConfigurationCollection configurations)
        {
            try
            {
                var directory = Path.GetDirectoryName(_configurationFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(configurations, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(_configurationFilePath, jsonContent);
                _logger.LogInformation("Zapisano konfiguracje synchronizacji do pliku: {FilePath}", _configurationFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas zapisywania konfiguracji do pliku: {FilePath}", _configurationFilePath);
                throw;
            }
        }

        /// <summary>
        /// Tworzy domyœln¹ konfiguracjê synchronizacji produktów.
        /// </summary>
        /// <returns>Domyœlna kolekcja konfiguracji</returns>
        private ProductSyncConfigurationCollection CreateDefaultConfiguration()
        {
            return new ProductSyncConfigurationCollection
            {
                Version = "1.0",
                LastModified = DateTime.UtcNow,
                Configurations = new List<ProductSyncConfiguration>
                {
                    new ProductSyncConfiguration
                    {
                        Id = "olmed-sync-products",
                        Name = "Synchronizacja produktów Olmed",
                        Description = "Pobieranie produktów z API Olmed co 2 godziny - synchronizacja produktów",
                        IsActive = true,
                        IntervalSeconds = 7200, // 2 godziny
                        Method = "POST",
                        Url = "https://draft-csm-connector.grupaolmed.pl/erp-api/products/get-products",
                        UseOlmedAuth = true,
                        Headers = new Dictionary<string, string>
                        {
                            { "accept", "application/json" },
                            { "Content-Type", "application/json" },
                            { "X-CSRF-TOKEN", "" }
                        },
                        Body = "{\"marketplace\": \"APTEKA_OLMED\"}",
                        Marketplace = "APTEKA_OLMED"
                    }
                }
            };
        }

        /// <summary>
        /// Zapewnia istnienie pliku konfiguracji.
        /// Tworzy domyœlny plik jeœli nie istnieje.
        /// </summary>
        private void EnsureConfigurationFileExists()
        {
            try
            {
                if (!File.Exists(_configurationFilePath))
                {
                    var directory = Path.GetDirectoryName(_configurationFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var defaultConfig = CreateDefaultConfiguration();
                    var jsonContent = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    File.WriteAllText(_configurationFilePath, jsonContent);
                    _logger.LogInformation("Utworzono domyœlny plik konfiguracji: {FilePath}", _configurationFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas tworzenia pliku konfiguracji: {FilePath}", _configurationFilePath);
            }
        }

        /// <summary>
        /// Wymusza odœwie¿enie cache konfiguracji.
        /// U¿yteczne po zewnêtrznych zmianach w pliku konfiguracji.
        /// </summary>
        public void RefreshCache()
        {
            _cachedConfigurations = null;
            _lastFileCheck = DateTime.MinValue;
            _logger.LogInformation("Cache konfiguracji synchronizacji zosta³ odœwie¿ony");
        }
    }
}
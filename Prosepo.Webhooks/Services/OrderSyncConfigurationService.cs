using System.Text.Json;
using Prosepo.Webhooks.Models;

namespace Prosepo.Webhooks.Services
{
    /// <summary>
    /// Serwis zarz¹dzaj¹cy konfiguracj¹ synchronizacji zamówieñ.
    /// £aduje konfiguracje z pliku JSON lub bazy danych i udostêpnia je do tworzenia zadañ cyklicznych.
    /// Automatycznie zarz¹dza dynamicznymi zakresami dat (dateFrom/dateTo) dla zapytañ.
    /// </summary>
    public class OrderSyncConfigurationService
    {
        private readonly ILogger<OrderSyncConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _configurationFilePath;
        private OrderSyncConfigurationCollection? _cachedConfigurations;
        private DateTime _lastFileCheck = DateTime.MinValue;
        private readonly TimeSpan _cacheValidityTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Inicjalizuje now¹ instancjê OrderSyncConfigurationService.
        /// </summary>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        public OrderSyncConfigurationService(ILogger<OrderSyncConfigurationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Œcie¿ka do pliku konfiguracji z mo¿liwoœci¹ nadpisania przez appsettings.json
            _configurationFilePath = _configuration["OrderSync:ConfigurationFile"] 
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "order-sync-config.json");
            
            EnsureConfigurationFileExists();
        }

        /// <summary>
        /// Pobiera wszystkie aktywne konfiguracje synchronizacji zamówieñ.
        /// U¿ywa cache z automatycznym odœwie¿aniem co 5 minut.
        /// </summary>
        /// <returns>Lista aktywnych konfiguracji synchronizacji</returns>
        public async Task<List<OrderSyncConfiguration>> GetActiveConfigurationsAsync()
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                return configurations.Configurations.Where(c => c.IsActive).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania konfiguracji synchronizacji zamówieñ");
                return new List<OrderSyncConfiguration>();
            }
        }

        /// <summary>
        /// Pobiera wszystkie konfiguracje synchronizacji zamówieñ (aktywne i nieaktywne).
        /// </summary>
        /// <returns>Lista wszystkich konfiguracji synchronizacji</returns>
        public async Task<List<OrderSyncConfiguration>> GetAllConfigurationsAsync()
        {
            try
            {
                var configurations = await LoadConfigurationsAsync();
                return configurations.Configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas pobierania wszystkich konfiguracji synchronizacji zamówieñ");
                return new List<OrderSyncConfiguration>();
            }
        }

        /// <summary>
        /// Pobiera konkretn¹ konfiguracjê synchronizacji po ID.
        /// </summary>
        /// <param name="configurationId">Identyfikator konfiguracji</param>
        /// <returns>Konfiguracja jeœli istnieje, null w przeciwnym przypadku</returns>
        public async Task<OrderSyncConfiguration?> GetConfigurationByIdAsync(string configurationId)
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
        /// Generuje body ¿¹dania dla synchronizacji zamówieñ z dynamicznie obliczonymi datami.
        /// </summary>
        /// <param name="configuration">Konfiguracja synchronizacji</param>
        /// <returns>JSON string z body ¿¹dania zawieraj¹cy marketplace, dateFrom i dateTo</returns>
        public string GenerateRequestBody(OrderSyncConfiguration configuration)
        {
            try
            {
                // Oblicz datê koñcow¹
                var dateTo = configuration.UseCurrentDateAsEndDate 
                    ? DateTime.Now.Date 
                    : DateTime.Now.Date.AddDays(-1);

                // Oblicz datê pocz¹tkow¹ (2 dni wstecz od dateTo)
                var dateFrom = dateTo.AddDays(-configuration.DateRangeDays);

                // Sformatuj daty zgodnie z okreœlonym formatem
                var dateFromStr = dateFrom.ToString(configuration.DateFormat);
                var dateToStr = dateTo.ToString(configuration.DateFormat);

                // Przygotuj obiekt do serializacji
                var requestBody = new Dictionary<string, object>
                {
                    { "marketplace", configuration.Marketplace },
                    { "dateFrom", dateFromStr },
                    { "dateTo", dateToStr }
                };

                // Dodaj dodatkowe parametry jeœli istniej¹
                if (configuration.AdditionalParameters != null && configuration.AdditionalParameters.Any())
                {
                    foreach (var param in configuration.AdditionalParameters)
                    {
                        requestBody[param.Key] = param.Value;
                    }
                }

                var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogDebug("Wygenerowano body ¿¹dania dla {ConfigurationId}: {Body}", 
                    configuration.Id, jsonBody);

                return jsonBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas generowania body ¿¹dania dla {ConfigurationId}", 
                    configuration.Id);
                throw;
            }
        }

        /// <summary>
        /// Tworzy ¿¹danie CronJob z konfiguracji synchronizacji zamówieñ.
        /// Automatycznie generuje body z dynamicznymi datami.
        /// </summary>
        /// <param name="configuration">Konfiguracja synchronizacji</param>
        /// <returns>Obiekt CronJobRequest gotowy do wykonania</returns>
        public CronJobRequest CreateCronJobRequest(OrderSyncConfiguration configuration)
        {
            var request = new CronJobRequest
            {
                Method = configuration.Method,
                Url = configuration.Url,
                UseOlmedAuth = configuration.UseOlmedAuth,
                Headers = configuration.Headers ?? new Dictionary<string, string>(),
                Body = GenerateRequestBody(configuration)
            };

            return request;
        }

        /// <summary>
        /// Dodaje lub aktualizuje konfiguracjê synchronizacji zamówieñ.
        /// </summary>
        /// <param name="configuration">Konfiguracja do dodania/aktualizacji</param>
        /// <returns>True jeœli operacja siê powiod³a</returns>
        public async Task<bool> SaveConfigurationAsync(OrderSyncConfiguration configuration)
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
        /// Usuwa konfiguracjê synchronizacji zamówieñ.
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
        private async Task<OrderSyncConfigurationCollection> LoadConfigurationsAsync()
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
                var configurations = JsonSerializer.Deserialize<OrderSyncConfigurationCollection>(jsonContent, new JsonSerializerOptions
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
                
                _logger.LogInformation("Za³adowano {Count} konfiguracji synchronizacji zamówieñ", configurations.Configurations.Count);
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
        private async Task SaveConfigurationsToFileAsync(OrderSyncConfigurationCollection configurations)
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
        /// Tworzy domyœln¹ konfiguracjê synchronizacji zamówieñ.
        /// </summary>
        /// <returns>Domyœlna kolekcja konfiguracji</returns>
        private OrderSyncConfigurationCollection CreateDefaultConfiguration()
        {
            return new OrderSyncConfigurationCollection
            {
                Version = "1.0",
                LastModified = DateTime.UtcNow,
                Configurations = new List<OrderSyncConfiguration>
                {
                    new OrderSyncConfiguration
                    {
                        Id = "olmed-sync-orders",
                        Name = "Synchronizacja zamówieñ Olmed",
                        Description = "Pobieranie zamówieñ z API Olmed co 2 godziny - synchronizacja zamówieñ z dynamicznym zakresem dat (2 dni)",
                        IsActive = true,
                        IntervalSeconds = 7200, // 2 godziny
                        Method = "POST",
                        Url = "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
                        UseOlmedAuth = true,
                        Headers = new Dictionary<string, string>
                        {
                            { "accept", "application/json" },
                            { "Content-Type", "application/json" },
                            { "X-CSRF-TOKEN", "" }
                        },
                        Marketplace = "APTEKA_OLMED",
                        DateRangeDays = 2,
                        UseCurrentDateAsEndDate = true,
                        DateFormat = "yyyy-MM-dd"
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

        /// <summary>
        /// Pobiera podgl¹d ¿¹dania dla danej konfiguracji (bez wykonywania).
        /// Przydatne do debugowania i weryfikacji konfiguracji.
        /// </summary>
        /// <param name="configurationId">Identyfikator konfiguracji</param>
        /// <returns>Obiekt z podgl¹dem ¿¹dania</returns>
        public async Task<object?> GetRequestPreviewAsync(string configurationId)
        {
            try
            {
                var configuration = await GetConfigurationByIdAsync(configurationId);
                if (configuration == null)
                {
                    return null;
                }

                var body = GenerateRequestBody(configuration);
                
                return new
                {
                    ConfigurationId = configuration.Id,
                    ConfigurationName = configuration.Name,
                    Method = configuration.Method,
                    Url = configuration.Url,
                    Headers = configuration.Headers,
                    Body = body,
                    BodyParsed = JsonSerializer.Deserialize<Dictionary<string, object>>(body),
                    UseOlmedAuth = configuration.UseOlmedAuth
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas generowania podgl¹du ¿¹dania dla {ConfigurationId}", configurationId);
                return null;
            }
        }
    }
}

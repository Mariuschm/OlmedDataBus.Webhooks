using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Attributes;
using Prosepo.Webhooks.Services;
using Prospeo.DbContext.Interfaces;

namespace Prosepo.Webhooks.Controllers
{
    /// <summary>
    /// Controller do zarz¹dzania kolejk¹ zadañ webhook.
    /// Wymaga autoryzacji API Key dla wszystkich endpointów.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiKeyAuth]
    public class QueueController : ControllerBase
    {
        private readonly IQueueService? _queueService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QueueController> _logger;
        private readonly FileLoggingService _fileLoggingService;

        /// <summary>
        /// Inicjalizuje now¹ instancjê QueueController.
        /// </summary>
        /// <param name="queueService">Serwis obs³ugi kolejki (opcjonalny)</param>
        /// <param name="configuration">Konfiguracja aplikacji</param>
        /// <param name="logger">Logger do rejestrowania zdarzeñ</param>
        /// <param name="fileLoggingService">Serwis logowania do plików</param>
        public QueueController(
            IQueueService? queueService, 
            IConfiguration configuration, 
            ILogger<QueueController> logger,
            FileLoggingService fileLoggingService)
        {
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileLoggingService = fileLoggingService ?? throw new ArgumentNullException(nameof(fileLoggingService));
        }

        /// <summary>
        /// Pobiera listê zadañ z kolejki dla produktów przetworzonych przez webhook.
        /// </summary>
        /// <param name="limit">Maksymalna liczba zadañ do pobrania (domyœlnie 100)</param>
        /// <returns>Lista zadañ z kolejki dla produktów</returns>
        /// <response code="200">Lista zadañ zosta³a pomyœlnie pobrana</response>
        /// <response code="401">Brak autoryzacji - wymagany API Key</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        /// <response code="500">B³¹d serwera podczas pobierania zadañ</response>
        [HttpGet("products")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductQueueItems([FromQuery] int limit = 100)
        {
            try
            {
                if (_queueService == null)
                {
                    _logger.LogWarning("QueueService is not available");
                    await _fileLoggingService.LogAsync("queue", LogLevel.Warning, 
                        "Attempted to access product queue but service is unavailable");

                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Success = false,
                        Message = "Queue service nie jest dostêpny"
                    });
                }

                // Walidacja limitu
                if (limit < 1 || limit > 1000)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Limit musi byæ w zakresie 1-1000"
                    });
                }

                var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                _logger.LogDebug("Fetching product queue items with scope: {Scope}, flag: {Flag}, limit: {Limit}", 
                    productScope, webhookProcessingFlag, limit);

                // Pobierz zadania z odpowiednim scope (produkty)
                var queueItems = await _queueService.GetByScopeAsync(productScope);

                // Filtruj tylko zadania z webhook (po flagach)
                var webhookItems = queueItems
                    .Where(q => q.Flg == webhookProcessingFlag)
                    .OrderByDescending(q => q.DateAddDateTime)
                    .Take(limit)
                    .ToList();

                var result = webhookItems.Select(item => new
                {
                    Id = item.Id,
                    RowID = item.RowID,
                    FirmaId = item.FirmaId,
                    Scope = item.Scope,
                    Description = item.Description,
                    TargetID = item.TargetID,
                    Flg = item.Flg,
                    DateAdd = item.DateAddDateTime,
                    DateMod = item.DateModDateTime,
                    RequestPreview = item.Request?.Length > 200 
                        ? item.Request.Substring(0, 200) + "..." 
                        : item.Request
                }).ToList();

                _logger.LogInformation("Retrieved {Count} product queue items", result.Count);

                await _fileLoggingService.LogAsync("queue", LogLevel.Information,
                    "Product queue items retrieved", null, new
                    {
                        ItemCount = result.Count,
                        ProductScope = productScope,
                        WebhookFlag = webhookProcessingFlag,
                        Limit = limit
                    });

                return Ok(new
                {
                    Success = true,
                    TotalItems = result.Count,
                    ProductScope = productScope,
                    WebhookFlag = webhookProcessingFlag,
                    RequestedLimit = limit,
                    QueueItems = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching product queue items");
                
                await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                    "Error fetching product queue items", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Wyst¹pi³ b³¹d podczas pobierania zadañ z kolejki",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Pobiera listê zadañ z kolejki dla zamówieñ przetworzonych przez webhook.
        /// </summary>
        /// <param name="limit">Maksymalna liczba zadañ do pobrania (domyœlnie 100)</param>
        /// <returns>Lista zadañ z kolejki dla zamówieñ</returns>
        /// <response code="200">Lista zadañ zosta³a pomyœlnie pobrana</response>
        /// <response code="401">Brak autoryzacji - wymagany API Key</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        /// <response code="500">B³¹d serwera podczas pobierania zadañ</response>
        [HttpGet("orders")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOrderQueueItems([FromQuery] int limit = 100)
        {
            try
            {
                if (_queueService == null)
                {
                    _logger.LogWarning("QueueService is not available");
                    await _fileLoggingService.LogAsync("queue", LogLevel.Warning,
                        "Attempted to access order queue but service is unavailable");

                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Success = false,
                        Message = "Queue service nie jest dostêpny"
                    });
                }

                // Walidacja limitu
                if (limit < 1 || limit > 1000)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Limit musi byæ w zakresie 1-1000"
                    });
                }

                var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                _logger.LogDebug("Fetching order queue items with scope: {Scope}, flag: {Flag}, limit: {Limit}",
                    orderScope, webhookProcessingFlag, limit);

                // Pobierz zadania z odpowiednim scope (zamówienia)
                var queueItems = await _queueService.GetByScopeAsync(orderScope);

                // Filtruj tylko zadania z webhook (po flagach)
                var webhookItems = queueItems
                    .Where(q => q.Flg == webhookProcessingFlag)
                    .OrderByDescending(q => q.DateAddDateTime)
                    .Take(limit)
                    .ToList();

                var result = webhookItems.Select(item => new
                {
                    Id = item.Id,
                    RowID = item.RowID,
                    FirmaId = item.FirmaId,
                    Scope = item.Scope,
                    Description = item.Description,
                    TargetID = item.TargetID,
                    Flg = item.Flg,
                    DateAdd = item.DateAddDateTime,
                    DateMod = item.DateModDateTime,
                    RequestPreview = item.Request?.Length > 200
                        ? item.Request.Substring(0, 200) + "..."
                        : item.Request
                }).ToList();

                _logger.LogInformation("Retrieved {Count} order queue items", result.Count);

                await _fileLoggingService.LogAsync("queue", LogLevel.Information,
                    "Order queue items retrieved", null, new
                    {
                        ItemCount = result.Count,
                        OrderScope = orderScope,
                        WebhookFlag = webhookProcessingFlag,
                        Limit = limit
                    });

                return Ok(new
                {
                    Success = true,
                    TotalItems = result.Count,
                    OrderScope = orderScope,
                    WebhookFlag = webhookProcessingFlag,
                    RequestedLimit = limit,
                    QueueItems = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching order queue items");

                await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                    "Error fetching order queue items", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Wyst¹pi³ b³¹d podczas pobierania zadañ z kolejki",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Pobiera szczegó³y konkretnego zadania z kolejki.
        /// </summary>
        /// <param name="id">Identyfikator zadania w kolejce</param>
        /// <returns>Szczegó³owe informacje o zadaniu</returns>
        /// <response code="200">Zadanie zosta³o znalezione</response>
        /// <response code="401">Brak autoryzacji - wymagany API Key</response>
        /// <response code="404">Zadanie nie zosta³o znalezione</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        /// <response code="500">B³¹d serwera podczas pobierania zadania</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueItemById(int id)
        {
            try
            {
                if (_queueService == null)
                {
                    _logger.LogWarning("QueueService is not available");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Success = false,
                        Message = "Queue service nie jest dostêpny"
                    });
                }

                _logger.LogDebug("Fetching queue item with ID: {Id}", id);

                var queueItem = await _queueService.GetByIdAsync(id);

                if (queueItem == null)
                {
                    _logger.LogWarning("Queue item with ID {Id} not found", id);
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Zadanie z ID {id} nie zosta³o znalezione"
                    });
                }

                _logger.LogInformation("Retrieved queue item with ID: {Id}", id);

                return Ok(new
                {
                    Success = true,
                    QueueItem = new
                    {
                        Id = queueItem.Id,
                        RowID = queueItem.RowID,
                        FirmaId = queueItem.FirmaId,
                        Scope = queueItem.Scope,
                        Description = queueItem.Description,
                        TargetID = queueItem.TargetID,
                        Flg = queueItem.Flg,
                        DateAdd = queueItem.DateAddDateTime,
                        DateMod = queueItem.DateModDateTime,
                        Request = queueItem.Request
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching queue item with ID: {Id}", id);

                await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                    "Error fetching queue item by ID", ex, new { QueueItemId = id });

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Wyst¹pi³ b³¹d podczas pobierania zadania",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Pobiera statystyki kolejki zadañ.
        /// </summary>
        /// <returns>Statystyki kolejki zawieraj¹ce liczby zadañ wed³ug scope i flag</returns>
        /// <response code="200">Statystyki zosta³y pomyœlnie pobrane</response>
        /// <response code="401">Brak autoryzacji - wymagany API Key</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        /// <response code="500">B³¹d serwera podczas pobierania statystyk</response>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQueueStatistics()
        {
            try
            {
                if (_queueService == null)
                {
                    _logger.LogWarning("QueueService is not available");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Success = false,
                        Message = "Queue service nie jest dostêpny"
                    });
                }

                var productScope = _configuration.GetValue<int>("Queue:ProductScope", 16);
                var orderScope = _configuration.GetValue<int>("Queue:OrderScope", 17);
                var webhookProcessingFlag = _configuration.GetValue<int>("Queue:WebhookProcessingFlag", 0);

                _logger.LogDebug("Calculating queue statistics");

                // Pobierz wszystkie zadania
                var allItems = await _queueService.GetAllAsync();
                var allItemsList = allItems.ToList();

                // Statystyki dla produktów
                var productItems = allItemsList.Where(q => q.Scope == productScope).ToList();
                var productWebhookItems = productItems.Where(q => q.Flg == webhookProcessingFlag).ToList();

                // Statystyki dla zamówieñ
                var orderItems = allItemsList.Where(q => q.Scope == orderScope).ToList();
                var orderWebhookItems = orderItems.Where(q => q.Flg == webhookProcessingFlag).ToList();

                var statistics = new
                {
                    TotalItems = allItemsList.Count,
                    ProductItems = new
                    {
                        Total = productItems.Count,
                        FromWebhook = productWebhookItems.Count,
                        ByFlag = productItems.GroupBy(q => q.Flg)
                            .Select(g => new { Flag = g.Key, Count = g.Count() })
                            .ToList()
                    },
                    OrderItems = new
                    {
                        Total = orderItems.Count,
                        FromWebhook = orderWebhookItems.Count,
                        ByFlag = orderItems.GroupBy(q => q.Flg)
                            .Select(g => new { Flag = g.Key, Count = g.Count() })
                            .ToList()
                    },
                    OtherScopes = allItemsList
                        .Where(q => q.Scope != productScope && q.Scope != orderScope)
                        .GroupBy(q => q.Scope)
                        .Select(g => new { Scope = g.Key, Count = g.Count() })
                        .ToList()
                };

                _logger.LogInformation("Queue statistics calculated: Total={Total}, Products={Products}, Orders={Orders}",
                    statistics.TotalItems, statistics.ProductItems.Total, statistics.OrderItems.Total);

                await _fileLoggingService.LogAsync("queue", LogLevel.Information,
                    "Queue statistics retrieved", null, statistics);

                return Ok(new
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    Statistics = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calculating queue statistics");

                await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                    "Error calculating queue statistics", ex);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Wyst¹pi³ b³¹d podczas pobierania statystyk kolejki",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Ponawia przetwarzanie zadania z kolejki.
        /// Resetuje status zadania, aby mog³o zostaæ ponownie przetworzone.
        /// </summary>
        /// <param name="id">Identyfikator zadania w kolejce</param>
        /// <returns>Status operacji ponowienia zadania</returns>
        /// <remarks>
        /// Zadania ze statusem Completed (Flg=1) lub Pending (Flg=0) nie mog¹ byæ ponawiane.
        /// Tylko zadania z b³êdami (Flg=-1) lub w trakcie przetwarzania (Flg=5) mog¹ byæ ponawiane.
        /// </remarks>
        /// <response code="200">Zadanie zosta³o pomyœlnie zresetowane do ponownego przetworzenia</response>
        /// <response code="400">Zadanie nie mo¿e byæ ponowione ze wzglêdu na aktualny status</response>
        /// <response code="401">Brak autoryzacji - wymagany API Key</response>
        /// <response code="404">Zadanie nie zosta³o znalezione</response>
        /// <response code="503">Serwis kolejki nie jest dostêpny</response>
        /// <response code="500">B³¹d serwera podczas ponowienia zadania</response>
        [HttpPost("{id}/retry")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RetryQueueItem(int id)
        {
            try
            {
                if (_queueService == null)
                {
                    _logger.LogWarning("QueueService is not available for retry operation");
                    await _fileLoggingService.LogAsync("queue", LogLevel.Warning,
                        "Attempted to retry queue item but service is unavailable", null, new { QueueItemId = id });

                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        Success = false,
                        Message = "Queue service nie jest dostêpny"
                    });
                }

                _logger.LogDebug("Attempting to retry queue item with ID: {Id}", id);

                // Pobierz zadanie z kolejki
                var queueItem = await _queueService.GetByIdAsync(id);

                if (queueItem == null)
                {
                    _logger.LogWarning("Queue item with ID {Id} not found for retry", id);
                    await _fileLoggingService.LogAsync("queue", LogLevel.Warning,
                        "Queue item not found for retry", null, new { QueueItemId = id });

                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Zadanie z ID {id} nie zosta³o znalezione"
                    });
                }

                // SprawdŸ status zadania (Flg)
                // Flg = 0: Pending (oczekuje) - NIE MO¯NA ponawiaæ
                // Flg = 1: Completed (zakoñczone pomyœlnie) - NIE MO¯NA ponawiaæ
                // Flg = -1: Error (b³¹d) - MO¯NA ponawiaæ
                // Flg = 5: Processing (w trakcie) - MO¯NA ponawiaæ (np. jeœli zawieszone)
                if (queueItem.Flg == 0 || queueItem.Flg == 1)
                {
                    var statusName = queueItem.Flg == 0 ? "Pending (oczekuje)" : "Completed (zakoñczone)";
                    
                    _logger.LogWarning("Cannot retry queue item {Id} with status Flg={Flg} ({Status})", 
                        id, queueItem.Flg, statusName);

                    await _fileLoggingService.LogAsync("queue", LogLevel.Warning,
                        "Attempted to retry queue item with invalid status", null, new
                        {
                            QueueItemId = id,
                            CurrentFlg = queueItem.Flg,
                            StatusName = statusName,
                            Scope = queueItem.Scope,
                            FirmaId = queueItem.FirmaId
                        });

                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Nie mo¿na ponowiæ zadania ze statusem {statusName}",
                        Details = new
                        {
                            CurrentStatus = queueItem.Flg,
                            StatusName = statusName,
                            Reason = queueItem.Flg == 0 
                                ? "Zadanie ju¿ oczekuje na przetworzenie" 
                                : "Zadanie zosta³o ju¿ pomyœlnie zakoñczone",
                            AllowedStatuses = new[]
                            {
                                new { Flg = -1, Name = "Error (b³¹d)" },
                                new { Flg = 5, Name = "Processing (w trakcie)" }
                            }
                        }
                    });
                }

                // Zapisz stary status dla logowania
                var oldFlg = queueItem.Flg;
                var oldDescription = queueItem.Description;

                // Resetuj status zadania do Pending (0)
                queueItem.Flg = 0;
                queueItem.DateModDateTime = DateTime.UtcNow;
                
                // Opcjonalnie: wyczyœæ opis z poprzednich b³êdów
                // lub dodaj informacjê o ponowieniu
                if (!string.IsNullOrEmpty(queueItem.Description))
                {
                    queueItem.Description = $"[RETRY at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Previous: {queueItem.Description}";
                    
                    // Ogranicz d³ugoœæ opisu do 1024 znaków
                    if (queueItem.Description.Length > 1024)
                    {
                        queueItem.Description = queueItem.Description.Substring(0, 1024);
                    }
                }

                // Zapisz zmienione zadanie
                var updateResult = await _queueService.UpdateAsync(queueItem);

                if (!updateResult)
                {
                    _logger.LogError("Failed to update queue item {Id} for retry", id);
                    await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                        "Failed to update queue item for retry", null, new { QueueItemId = id });

                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        Success = false,
                        Message = "Nie uda³o siê zaktualizowaæ zadania w kolejce"
                    });
                }

                _logger.LogInformation("Queue item {Id} successfully reset for retry. Status changed from Flg={OldFlg} to Flg=0", 
                    id, oldFlg);

                await _fileLoggingService.LogAsync("queue", LogLevel.Information,
                    "Queue item reset for retry", null, new
                    {
                        QueueItemId = id,
                        RowID = queueItem.RowID,
                        Scope = queueItem.Scope,
                        FirmaId = queueItem.FirmaId,
                        OldFlg = oldFlg,
                        NewFlg = queueItem.Flg,
                        OldDescription = oldDescription,
                        NewDescription = queueItem.Description,
                        RetryTimestamp = DateTime.UtcNow
                    });

                return Ok(new
                {
                    Success = true,
                    Message = "Zadanie zosta³o pomyœlnie zresetowane do ponownego przetworzenia",
                    QueueItem = new
                    {
                        Id = queueItem.Id,
                        RowID = queueItem.RowID,
                        FirmaId = queueItem.FirmaId,
                        Scope = queueItem.Scope,
                        PreviousStatus = oldFlg,
                        CurrentStatus = queueItem.Flg,
                        DateMod = queueItem.DateModDateTime,
                        Description = queueItem.Description
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrying queue item with ID: {Id}", id);

                await _fileLoggingService.LogAsync("queue", LogLevel.Error,
                    "Error retrying queue item", ex, new { QueueItemId = id });

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "Wyst¹pi³ b³¹d podczas ponowienia zadania",
                    Error = ex.Message
                });
            }
        }
    }
}

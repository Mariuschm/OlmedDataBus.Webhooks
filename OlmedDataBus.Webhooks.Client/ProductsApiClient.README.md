# ProductsApiClient - Klient API do Komunikacji z ProductsController

## ?? Opis

`ProductsApiClient` to dedykowana klasa klienta do komunikacji z `ProductsController` w projekcie Prosepo.Webhooks. Zapewnia typowane metody do:
- Aktualizacji stanÛw magazynowych produktÛw
- Aktualizacji úrednich cen zakupu
- Weryfikacji autoryzacji API Key

## ?? Jak UøyÊ

### Opcja 1: Dodaj kod bezpoúrednio do swojego projektu

1. Skopiuj plik `ProductsApiClient.cs` (poniøej) do swojego projektu
2. Dodaj wymagane referencje NuGet:
   ```xml
   <PackageReference Include="System.Text.Json" Version="8.0.0" />
   ```
3. Dodaj referencjÍ do projektu z DTOs:
   ```xml
   <ProjectReference Include="..\Prospeo.DTOs\Prospeo.DTOs.csproj" />
   ```

### Opcja 2: UtwÛrz osobnπ bibliotekÍ Client

Moøesz utworzyÊ oddzielny projekt biblioteki klasy (np. `YourProject.ProductsClient`) targetujπcy .NET 8 lub nowszy.

## ?? Wymagane Zaleønoúci

### Pakiety NuGet:
- `System.Text.Json` >= 8.0.0
- .NET 8.0 lub nowszy (dla pe≥nej kompatybilnoúci z Prospeo.DTOs)

### Referencje Projektowe:
- `Prospeo.DTOs` - zawiera DTOs dla produktÛw (`StockUpdateRequest`, `StockUpdateResponse`, etc.)

## ?? Kod èrÛd≥owy - ProductsApiClient.cs

```csharp
using Prospeo.DTOs.Product;
using System.Net.Http.Json;
using System.Text.Json;

namespace YourProject.ProductsClient // ZmieÒ namespace na swÛj
{
    /// <summary>
    /// Klient API do komunikacji z kontrolerem produktÛw (ProductsController).
    /// Zapewnia typowane metody do aktualizacji stanÛw magazynowych.
    /// </summary>
    public class ProductsApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;
        private readonly bool _disposeHttpClient;

        /// <summary>
        /// Inicjalizuje nowπ instancjÍ klasy ProductsApiClient z w≥asnym HttpClient.
        /// </summary>
        public ProductsApiClient(string baseUrl, string apiKey, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _disposeHttpClient = true;

            _httpClient = new HttpClient
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            };

            ConfigureHttpClient();
            ConfigureJsonOptions();
        }

        /// <summary>
        /// Inicjalizuje nowπ instancjÍ z istniejπcym HttpClient (zarzπdzanym zewnÍtrznie).
        /// </summary>
        public ProductsApiClient(HttpClient httpClient, string baseUrl, string apiKey)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _disposeHttpClient = false;

            ConfigureHttpClient();
            ConfigureJsonOptions();
        }

        private void ConfigureHttpClient()
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProductsApiClient/1.0");
        }

        private void ConfigureJsonOptions()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Aktualizuje stany magazynowe produktÛw w systemie Olmed.
        /// </summary>
        /// <param name="request">Øπdanie aktualizacji stanÛw magazynowych</param>
        /// <param name="cancellationToken">Token anulowania operacji</param>
        /// <returns>Odpowiedü z wynikiem operacji aktualizacji</returns>
        /// <exception cref="ArgumentNullException">Gdy request jest null</exception>
        /// <exception cref="HttpRequestException">Gdy øπdanie HTTP zakoÒczy siÍ niepowodzeniem</exception>
        /// <remarks>
        /// Endpoint: POST /api/products/update-product-stocks
        /// Wymaga: X-API-Key w nag≥Ûwku
        /// 
        /// <para>
        /// Przyk≥ad uøycia:
        /// <code>
        /// var request = new StockUpdateRequest
        /// {
        ///     Marketplace = "APTEKA_OLMED",
        ///     Skus = new Dictionary&lt;string, StockUpdateItemDto&gt;
        ///     {
        ///         ["SKU001"] = new StockUpdateItemDto 
        ///         { 
        ///             Stock = 100, 
        ///             AveragePurchasePrice = 25.50m 
        ///         },
        ///         ["SKU002"] = new StockUpdateItemDto 
        ///         { 
        ///             Stock = 0, 
        ///             AveragePurchasePrice = 15.00m 
        ///         }
        ///     },
        ///     Notes = "Aktualizacja po inwentaryzacji",
        ///     UpdateDate = DateTime.UtcNow
        /// };
        /// 
        /// var result = await client.UpdateProductStocksAsync(request);
        /// </code>
        /// </para>
        /// </remarks>
        public async Task<ProductsApiResult<StockUpdateResponse>> UpdateProductStocksAsync(
            StockUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = $"{_baseUrl}/api/products/update-product-stocks";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);
                return await ProcessResponseAsync<StockUpdateResponse>(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return ProductsApiResult<StockUpdateResponse>.Failure($"B≥πd HTTP: {ex.Message}", 0, ex);
            }
            catch (TaskCanceledException ex)
            {
                return ProductsApiResult<StockUpdateResponse>.Failure("Øπdanie anulowane lub timeout", 0, ex);
            }
        }

        /// <summary>
        /// Pomocnicza metoda do aktualizacji pojedynczego SKU.
        /// </summary>
        /// <param name="marketplace">Identyfikator marketplace</param>
        /// <param name="sku">SKU produktu</param>
        /// <param name="stock">Nowy stan magazynowy</param>
        /// <param name="averagePurchasePrice">årednia cena zakupu</param>
        /// <param name="notes">Opcjonalne notatki</param>
        /// <param name="cancellationToken">Token anulowania</param>
        /// <returns>Wynik operacji aktualizacji</returns>
        public async Task<ProductsApiResult<StockUpdateResponse>> UpdateSingleProductStockAsync(
            string marketplace,
            string sku,
            decimal stock,
            decimal averagePurchasePrice,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(marketplace))
                throw new ArgumentNullException(nameof(marketplace));
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentNullException(nameof(sku));
            if (stock < 0)
                throw new ArgumentException("Stan magazynowy nie moøe byÊ ujemny", nameof(stock));
            if (averagePurchasePrice < 0)
                throw new ArgumentException("Cena zakupu nie moøe byÊ ujemna", nameof(averagePurchasePrice));

            var request = new StockUpdateRequest
            {
                Marketplace = marketplace,
                Skus = new Dictionary<string, StockUpdateItemDto>
                {
                    [sku] = new StockUpdateItemDto
                    {
                        Stock = stock,
                        AveragePurchasePrice = averagePurchasePrice
                    }
                },
                Notes = notes,
                UpdateDate = DateTime.UtcNow
            };

            return await UpdateProductStocksAsync(request, cancellationToken);
        }

        /// <summary>
        /// Pomocnicza metoda do aktualizacji wielu SKU jednoczeúnie.
        /// </summary>
        /// <param name="marketplace">Identyfikator marketplace</param>
        /// <param name="stockUpdates">S≥ownik z SKU i danymi do aktualizacji</param>
        /// <param name="notes">Opcjonalne notatki</param>
        /// <param name="cancellationToken">Token anulowania</param>
        /// <returns>Wynik operacji aktualizacji</returns>
        public async Task<ProductsApiResult<StockUpdateResponse>> UpdateMultipleProductStocksAsync(
            string marketplace,
            Dictionary<string, (decimal stock, decimal averagePurchasePrice)> stockUpdates,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(marketplace))
                throw new ArgumentNullException(nameof(marketplace));
            if (stockUpdates == null || stockUpdates.Count == 0)
                throw new ArgumentException("Lista SKU nie moøe byÊ pusta", nameof(stockUpdates));

            var skus = new Dictionary<string, StockUpdateItemDto>();
            foreach (var update in stockUpdates)
            {
                if (update.Value.stock < 0)
                    throw new ArgumentException($"Stan magazynowy dla SKU '{update.Key}' nie moøe byÊ ujemny");
                if (update.Value.averagePurchasePrice < 0)
                    throw new ArgumentException($"Cena zakupu dla SKU '{update.Key}' nie moøe byÊ ujemna");

                skus[update.Key] = new StockUpdateItemDto
                {
                    Stock = update.Value.stock,
                    AveragePurchasePrice = update.Value.averagePurchasePrice
                };
            }

            var request = new StockUpdateRequest
            {
                Marketplace = marketplace,
                Skus = skus,
                Notes = notes,
                UpdateDate = DateTime.UtcNow
            };

            return await UpdateProductStocksAsync(request, cancellationToken);
        }

        private async Task<ProductsApiResult<T>> ProcessResponseAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken) where T : class
        {
            var statusCode = (int)response.StatusCode;
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
                    return ProductsApiResult<T>.Success(data!, statusCode, responseContent);
                }
                catch (JsonException ex)
                {
                    return ProductsApiResult<T>.Failure($"B≥πd deserializacji: {ex.Message}", statusCode, ex, responseContent);
                }
            }
            else
            {
                string errorMessage = $"HTTP {statusCode}";
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
                    if (errorResponse != null)
                    {
                        if (errorResponse.TryGetValue("message", out var msg))
                            errorMessage = $"{errorMessage}: {msg}";
                        else if (errorResponse.TryGetValue("error", out var err))
                            errorMessage = $"{errorMessage}: {err}";
                    }
                }
                catch
                {
                    errorMessage = $"{errorMessage}: {responseContent}";
                }

                return ProductsApiResult<T>.Failure(errorMessage, statusCode, null, responseContent);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_disposeHttpClient)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Reprezentuje wynik wywo≥ania metody API produktÛw.
    /// </summary>
    public class ProductsApiResult<T> where T : class
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? ErrorMessage { get; init; }
        public int StatusCode { get; init; }
        public string? RawResponse { get; init; }
        public Exception? Exception { get; init; }
        public bool IsFailure => !IsSuccess;

        public static ProductsApiResult<T> Success(T data, int statusCode, string? rawResponse = null)
            => new() { IsSuccess = true, Data = data, StatusCode = statusCode, RawResponse = rawResponse };

        public static ProductsApiResult<T> Failure(string errorMessage, int statusCode, Exception? exception = null, string? rawResponse = null)
            => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode, Exception = exception, RawResponse = rawResponse };
    }
}
```

## ?? Przyk≥ady Uøycia

### 1. Podstawowa Konfiguracja

```csharp
// Prosty klient (zarzπdza w≥asnym HttpClient)
using var client = new ProductsApiClient(
    baseUrl: "https://your-webhook-service.com",
    apiKey: "your-api-key-from-database"
);
```

### 2. Z IHttpClientFactory (ASP.NET Core - zalecane)

```csharp
// W Program.cs / Startup.cs
services.AddHttpClient<ProductsApiClient>();

services.AddScoped<ProductsApiClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    
    return new ProductsApiClient(
        httpClient,
        config["ProductsApi:BaseUrl"]!,
        config["ProductsApi:ApiKey"]!
    );
});
```

### 3. Aktualizacja Pojedynczego Produktu

```csharp
var result = await client.UpdateSingleProductStockAsync(
    marketplace: "APTEKA_OLMED",
    sku: "PROD-001",
    stock: 150,
    averagePurchasePrice: 25.50m,
    notes: "Aktualizacja po dostawie"
);

if (result.IsSuccess)
{
    Console.WriteLine($"? Zaktualizowano {result.Data.UpdatedCount} produktÛw");
    Console.WriteLine($"  SKU: {string.Join(", ", result.Data.UpdatedSkus)}");
    Console.WriteLine($"  Marketplace: {result.Data.Marketplace}");
    Console.WriteLine($"  Czas: {result.Data.ProcessedAt}");
}
else
{
    Console.WriteLine($"? B≥πd: {result.ErrorMessage}");
    Console.WriteLine($"  HTTP Status: {result.StatusCode}");
}
```

### 4. Aktualizacja Wielu ProduktÛw Jednoczeúnie

```csharp
var stockUpdates = new Dictionary<string, (decimal stock, decimal price)>
{
    ["PROD-001"] = (150, 25.50m),
    ["PROD-002"] = (0, 30.00m),      // Brak na stanie
    ["PROD-003"] = (75, 18.99m),
    ["PROD-004"] = (200, 42.75m)
};

var result = await client.UpdateMultipleProductStocksAsync(
    marketplace: "APTEKA_OLMED",
    stockUpdates: stockUpdates,
    notes: "Codzienna synchronizacja stanÛw"
);

if (result.IsSuccess)
{
    Console.WriteLine($"? Zaktualizowano {result.Data.UpdatedCount} z {stockUpdates.Count} produktÛw");
    
    // Sprawdü czy wszystkie produkty zosta≥y zaktualizowane
    if (result.Data.UpdatedCount < stockUpdates.Count)
    {
        var failedSkus = stockUpdates.Keys.Except(result.Data.UpdatedSkus);
        Console.WriteLine($"? Nie zaktualizowano: {string.Join(", ", failedSkus)}");
    }
}
```

### 5. Aktualizacja z Pe≥nπ Kontrolπ (Manual Request)

```csharp
var request = new StockUpdateRequest
{
    Marketplace = "APTEKA_OLMED",
    Skus = new Dictionary<string, StockUpdateItemDto>
    {
        ["14978"] = new StockUpdateItemDto
        {
            Stock = 35,
            AveragePurchasePrice = 10.04m
        },
        ["111714"] = new StockUpdateItemDto
        {
            Stock = 120,
            AveragePurchasePrice = 15.14m
        }
    },
    Notes = "Aktualizacja po inwentaryzacji 2024-01-15",
    UpdateDate = DateTime.UtcNow
};

var result = await client.UpdateProductStocksAsync(request);

if (result.IsSuccess)
{
    Console.WriteLine($"? Sukces!");
    Console.WriteLine($"  Message: {result.Data.Message}");
    Console.WriteLine($"  Zaktualizowano: {result.Data.UpdatedCount} SKU");
    Console.WriteLine($"  Lista: {string.Join(", ", result.Data.UpdatedSkus)}");
    Console.WriteLine($"  Przetworzone: {result.Data.ProcessedAt:yyyy-MM-dd HH:mm:ss}");
}
```

### 6. Batch Update (Duøa Liczba ProduktÛw)

```csharp
// Przyk≥ad przetwarzania duøej liczby produktÛw w partiach
var allProducts = GetAllProductsToUpdate(); // Pobierz wszystkie produkty
var batchSize = 100; // Przetwarzaj po 100 produktÛw

var batches = allProducts
    .Select((product, index) => new { product, index })
    .GroupBy(x => x.index / batchSize)
    .Select(g => g.Select(x => x.product).ToList())
    .ToList();

int totalUpdated = 0;
int totalFailed = 0;

foreach (var (batch, batchNumber) in batches.Select((b, i) => (b, i + 1)))
{
    Console.WriteLine($"Przetwarzanie partii {batchNumber}/{batches.Count}...");
    
    var stockUpdates = batch.ToDictionary(
        p => p.Sku,
        p => (p.Stock, p.AveragePurchasePrice)
    );
    
    var result = await client.UpdateMultipleProductStocksAsync(
        marketplace: "APTEKA_OLMED",
        stockUpdates: stockUpdates,
        notes: $"Batch {batchNumber} - automatyczna synchronizacja"
    );
    
    if (result.IsSuccess)
    {
        totalUpdated += result.Data.UpdatedCount;
        Console.WriteLine($"  ? Zaktualizowano {result.Data.UpdatedCount} produktÛw");
    }
    else
    {
        totalFailed += stockUpdates.Count;
        Console.WriteLine($"  ? B≥πd: {result.ErrorMessage}");
    }
    
    // Opcjonalne opÛünienie miÍdzy partiami
    await Task.Delay(1000);
}

Console.WriteLine($"\nPodsumowanie:");
Console.WriteLine($"  Zaktualizowano: {totalUpdated}");
Console.WriteLine($"  B≥Ídy: {totalFailed}");
```

### 7. Aktualizacja z Retry Logic (Polly)

```csharp
using Polly;

var retryPolicy = Policy
    .HandleResult<ProductsApiResult<StockUpdateResponse>>(r => 
        !r.IsSuccess && r.StatusCode >= 500)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"PrÛba {retryCount} po {timespan.TotalSeconds}s. B≥πd: {outcome.Result.ErrorMessage}");
        });

var result = await retryPolicy.ExecuteAsync(async () =>
    await client.UpdateSingleProductStockAsync(
        marketplace: "APTEKA_OLMED",
        sku: "PROD-001",
        stock: 100,
        averagePurchasePrice: 25.00m
    ));

if (result.IsSuccess)
{
    Console.WriteLine("? Sukces po retry");
}
```

### 8. Integracja z Loggingiem

```csharp
public class StockSyncService
{
    private readonly ProductsApiClient _client;
    private readonly ILogger<StockSyncService> _logger;
    
    public StockSyncService(ProductsApiClient client, ILogger<StockSyncService> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<bool> SyncStockAsync(string marketplace, Dictionary<string, (decimal, decimal)> updates)
    {
        _logger.LogInformation(
            "RozpoczÍcie synchronizacji stanÛw dla {Marketplace}, produktÛw: {Count}",
            marketplace, updates.Count);
        
        var result = await _client.UpdateMultipleProductStocksAsync(
            marketplace, updates, "Automatyczna synchronizacja");
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Pomyúlnie zsynchronizowano {UpdatedCount} produktÛw w {Marketplace}",
                result.Data.UpdatedCount, marketplace);
            return true;
        }
        else
        {
            _logger.LogError(
                "B≥πd synchronizacji stanÛw dla {Marketplace}: {Error}",
                marketplace, result.ErrorMessage);
            return false;
        }
    }
}
```

## ?? Autoryzacja

Wszystkie metody wymagajπ nag≥Ûwka **X-API-Key** ktÛry jest automatycznie dodawany przez klienta. API Key musi byÊ skonfigurowany w bazie danych w tabeli `Firmy`.

## ?? Obs≥uga B≥ÍdÛw

### Walidacja po stronie klienta

Klient automatycznie waliduje:
- ? Marketplace nie moøe byÊ pusty
- ? Lista SKU nie moøe byÊ pusta
- ? Stan magazynowy nie moøe byÊ ujemny
- ? Cena zakupu nie moøe byÊ ujemna

### Kody b≥ÍdÛw HTTP

| Kod | Opis | Dzia≥anie |
|-----|------|-----------|
| **200** | Sukces | Stany zaktualizowane |
| **400** | B≥Ídne dane | Sprawdü parametry øπdania |
| **401** | Brak autoryzacji | Sprawdü API Key |
| **500** | B≥πd serwera | SprÛbuj ponownie pÛüniej |
| **503** | Serwis niedostÍpny | Sprawdü czy API dzia≥a |

### Przyk≥ad kompleksowej obs≥ugi b≥ÍdÛw

```csharp
try
{
    var result = await client.UpdateSingleProductStockAsync(
        marketplace: "APTEKA_OLMED",
        sku: "PROD-001",
        stock: 100,
        averagePurchasePrice: 25.00m
    );
    
    if (result.IsSuccess)
    {
        // Sukces
        Console.WriteLine($"? Zaktualizowano: {result.Data.Message}");
    }
    else
    {
        // B≥πd API
        Console.WriteLine($"? B≥πd API: {result.ErrorMessage}");
        
        switch (result.StatusCode)
        {
            case 400:
                Console.WriteLine("  ? Nieprawid≥owe dane wejúciowe");
                Console.WriteLine($"  ? Odpowiedü: {result.RawResponse}");
                break;
            case 401:
                Console.WriteLine("  ? Brak autoryzacji - sprawdü API Key");
                break;
            case 500:
                Console.WriteLine("  ? B≥πd serwera - sprÛbuj ponownie");
                break;
        }
        
        if (result.Exception != null)
        {
            Console.WriteLine($"  Wyjπtek: {result.Exception.Message}");
        }
    }
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"? Brak wymaganego parametru: {ex.ParamName}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"? Nieprawid≥owy parametr: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"? Nieoczekiwany b≥πd: {ex.Message}");
}
```

## ?? Testowanie

### Test jednostkowy z Moq

```csharp
using Moq;
using Moq.Protected;
using Xunit;

public class ProductsApiClientTests
{
    [Fact]
    public async Task UpdateProductStocks_Success_ReturnsSuccessResult()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{
                    ""success"": true,
                    ""message"": ""Stany zaktualizowane"",
                    ""marketplace"": ""APTEKA_OLMED"",
                    ""updatedCount"": 2,
                    ""updatedSkus"": [""SKU001"", ""SKU002""],
                    ""processedAt"": ""2024-01-15T10:30:00Z""
                }")
            });
        
        var httpClient = new HttpClient(mockHandler.Object);
        var client = new ProductsApiClient(httpClient, "https://test.com", "test-key");
        
        var request = new StockUpdateRequest
        {
            Marketplace = "APTEKA_OLMED",
            Skus = new Dictionary<string, StockUpdateItemDto>
            {
                ["SKU001"] = new() { Stock = 100, AveragePurchasePrice = 25.00m },
                ["SKU002"] = new() { Stock = 50, AveragePurchasePrice = 30.00m }
            }
        };
        
        // Act
        var result = await client.UpdateProductStocksAsync(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.UpdatedCount);
        Assert.Equal("APTEKA_OLMED", result.Data.Marketplace);
        Assert.Contains("SKU001", result.Data.UpdatedSkus);
        Assert.Contains("SKU002", result.Data.UpdatedSkus);
    }
    
    [Theory]
    [InlineData(-10, 25.00)] // Ujemny stan
    [InlineData(100, -25.00)] // Ujemna cena
    public void UpdateSingleProductStock_InvalidValues_ThrowsArgumentException(
        decimal stock, decimal price)
    {
        // Arrange
        var client = new ProductsApiClient("https://test.com", "test-key");
        
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.UpdateSingleProductStockAsync(
                "APTEKA_OLMED", "SKU001", stock, price));
    }
}
```

## ?? Best Practices

### 1. **Uøywaj Batch Updates**
Dla duøej liczby produktÛw, grupuj aktualizacje w partie po 50-100 SKU.

### 2. **Implementuj Retry Logic**
Uøywaj Polly lub podobnej biblioteki dla b≥ÍdÛw sieciowych (5xx).

### 3. **Loguj Wszystkie Operacje**
Zawsze loguj wywo≥ania API dla audytu i troubleshootingu.

### 4. **Waliduj Dane Przed Wys≥aniem**
Sprawdü dane przed wywo≥aniem API (klient robi to automatycznie).

### 5. **Obs≥uguj Timeouty**
Ustaw odpowiedni timeout dla duøych batch'y (domyúlnie 30s).

### 6. **Monitoruj WydajnoúÊ**
åledü czasy odpowiedzi i success rate.

### 7. **Uøywaj CancellationToken**
Pozwala na anulowanie d≥ugotrwa≥ych operacji.

### 8. **Przechowuj API Key Bezpiecznie**
Uøywaj User Secrets, Azure Key Vault, lub podobnych.

## ?? Powiπzane Dokumenty

- [ProductsController.cs](../Prosepo.Webhooks/Controllers/ProductsController.cs) - Implementacja serwera
- [StockUpdateRequest.cs](../Prospeo.DTOs/Product/StockUpdateRequest.cs) - DTO øπdania
- [StockUpdateResponse.cs](../Prospeo.DTOs/Product/StockUpdateResponse.cs) - DTO odpowiedzi
- [ORDERS_API_CLIENT.md](../ORDERS_API_CLIENT.md) - Analogiczny klient dla zamÛwieÒ

## ? Checklist Integracji

- [ ] Dodaj kod ProductsApiClient.cs do projektu
- [ ] Dodaj pakiet NuGet: System.Text.Json
- [ ] Dodaj referencjÍ do Prospeo.DTOs
- [ ] Skonfiguruj base URL i API Key
- [ ] Zarejestruj w DI (jeúli ASP.NET Core)
- [ ] Zaimplementuj obs≥ugÍ b≥ÍdÛw
- [ ] Dodaj logowanie
- [ ] Zaimplementuj retry logic
- [ ] Napisz testy jednostkowe
- [ ] Przetestuj z prawdziwym API
- [ ] Zoptymalizuj batch size
- [ ] Dodaj monitoring wydajnoúci

## ?? Rozwiπzywanie ProblemÛw

### Problem: "Stan magazynowy nie moøe byÊ ujemny"
**Rozwiπzanie:** Sprawdü dane ürÛd≥owe, upewnij siÍ øe stock >= 0

### Problem: "HTTP 401 Unauthorized"
**Rozwiπzanie:** Sprawdü czy API Key jest prawid≥owy i aktywny w bazie (tabela Firmy)

### Problem: "HTTP 400 - Marketplace jest wymagany"
**Rozwiπzanie:** Upewnij siÍ øe pole Marketplace nie jest puste

### Problem: Timeout przy duøej liczbie SKU
**Rozwiπzanie:** Podziel aktualizacjÍ na mniejsze partie (50-100 SKU)

### Problem: "Brak SKU do aktualizacji"
**Rozwiπzanie:** S≥ownik Skus musi zawieraÊ co najmniej jeden element

## ?? Wsparcie

W razie problemÛw skontaktuj siÍ z zespo≥em Prospeo lub sprawdü dokumentacjÍ w repozytorium projektu.

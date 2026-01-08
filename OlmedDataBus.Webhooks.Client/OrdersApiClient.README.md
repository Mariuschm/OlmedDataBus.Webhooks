# OrdersApiClient - Klient API do Komunikacji z OrdersController

## ?? Opis

`OrdersApiClient` to dedykowana klasa klienta do komunikacji z `OrdersController` w projekcie Prosepo.Webhooks. Zapewnia typowane metody do:
- Aktualizacji statusÛw zamÛwieÒ
- Przesy≥ania wynikÛw realizacji zamÛwieÒ
- Weryfikacji autoryzacji API Key

## ?? Jak UøyÊ

### Opcja 1: Dodaj kod bezpoúrednio do swojego projektu

1. Skopiuj plik `OrdersApiClient.cs` (poniøej) do swojego projektu
2. Dodaj wymagane referencje NuGet:
   ```xml
   <PackageReference Include="System.Text.Json" Version="8.0.0" />
   ```
3. Dodaj referencjÍ do projektu z DTOs:
   ```xml
   <ProjectReference Include="..\Prospeo.DTOs\Prospeo.DTOs.csproj" />
   ```

### Opcja 2: UtwÛrz osobnπ bibliotekÍ Client

Moøesz utworzyÊ oddzielny projekt biblioteki klasy (np. `YourProject.OrdersClient`) targetujπcy .NET 8 lub nowszy.

## ?? Wymagane Zaleønoúci

### Pakiety NuGet:
- `System.Text.Json` >= 8.0.0
- .NET 8.0 lub nowszy (dla pe≥nej kompatybilnoúci z Prospeo.DTOs)

### Referencje Projektowe:
- `Prospeo.DTOs` - zawiera DTOs dla zamÛwieÒ (`UpdateOrderStatusRequest`, `UploadOrderRealizationRequest`, etc.)

## ?? Kod èrÛd≥owy - OrdersApiClient.cs

```csharp
using Prospeo.DTOs.Order;
using Prospeo.DTOs.Product;
using System.Net.Http.Json;
using System.Text.Json;

namespace YourProject.OrdersClient // ZmieÒ namespace na swÛj
{
    /// <summary>
    /// Klient API do komunikacji z kontrolerem zamÛwieÒ (OrdersController).
    /// Zapewnia typowane metody do aktualizacji statusÛw zamÛwieÒ i przesy≥ania wynikÛw realizacji.
    /// </summary>
    public class OrdersApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;
        private readonly bool _disposeHttpClient;

        /// <summary>
        /// Inicjalizuje nowπ instancjÍ klasy OrdersApiClient z w≥asnym HttpClient.
        /// </summary>
        public OrdersApiClient(string baseUrl, string apiKey, TimeSpan? timeout = null)
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
        public OrdersApiClient(HttpClient httpClient, string baseUrl, string apiKey)
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
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "OrdersApiClient/1.0");
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
        /// Aktualizuje status zamÛwienia w systemie Olmed.
        /// </summary>
        public async Task<OrdersApiResult<UpdateOrderStatusResponse>> UpdateOrderStatusAsync(
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = $"{_baseUrl}/api/orders/update-status";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);
                return await ProcessResponseAsync<UpdateOrderStatusResponse>(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return OrdersApiResult<UpdateOrderStatusResponse>.Failure($"B≥πd HTTP: {ex.Message}", 0, ex);
            }
            catch (TaskCanceledException ex)
            {
                return OrdersApiResult<UpdateOrderStatusResponse>.Failure("Øπdanie anulowane lub timeout", 0, ex);
            }
        }

        /// <summary>
        /// Przesy≥a wyniki realizacji zamÛwienia do systemu Olmed.
        /// </summary>
        public async Task<OrdersApiResult<UploadOrderRealizationResponse>> UploadOrderRealizationAsync(
            UploadOrderRealizationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var url = $"{_baseUrl}/api/orders/upload-order-realization-result";

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);
                return await ProcessResponseAsync<UploadOrderRealizationResponse>(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return OrdersApiResult<UploadOrderRealizationResponse>.Failure($"B≥πd HTTP: {ex.Message}", 0, ex);
            }
            catch (TaskCanceledException ex)
            {
                return OrdersApiResult<UploadOrderRealizationResponse>.Failure("Øπdanie anulowane lub timeout", 0, ex);
            }
        }

        /// <summary>
        /// Pobiera informacje o zalogowanej firmie na podstawie API Key.
        /// </summary>
        public async Task<OrdersApiResult<AuthenticatedFirmaResponse>> GetAuthenticatedFirmaAsync(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/api/orders/authenticated-firma";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                return await ProcessResponseAsync<AuthenticatedFirmaResponse>(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return OrdersApiResult<AuthenticatedFirmaResponse>.Failure($"B≥πd HTTP: {ex.Message}", 0, ex);
            }
            catch (TaskCanceledException ex)
            {
                return OrdersApiResult<AuthenticatedFirmaResponse>.Failure("Øπdanie anulowane lub timeout", 0, ex);
            }
        }

        private async Task<OrdersApiResult<T>> ProcessResponseAsync<T>(
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
                    return OrdersApiResult<T>.Success(data!, statusCode, responseContent);
                }
                catch (JsonException ex)
                {
                    return OrdersApiResult<T>.Failure($"B≥πd deserializacji: {ex.Message}", statusCode, ex, responseContent);
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

                return OrdersApiResult<T>.Failure(errorMessage, statusCode, null, responseContent);
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
    /// Reprezentuje wynik wywo≥ania metody API zamÛwieÒ.
    /// </summary>
    public class OrdersApiResult<T> where T : class
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? ErrorMessage { get; init; }
        public int StatusCode { get; init; }
        public string? RawResponse { get; init; }
        public Exception? Exception { get; init; }
        public bool IsFailure => !IsSuccess;

        public static OrdersApiResult<T> Success(T data, int statusCode, string? rawResponse = null)
            => new() { IsSuccess = true, Data = data, StatusCode = statusCode, RawResponse = rawResponse };

        public static OrdersApiResult<T> Failure(string errorMessage, int statusCode, Exception? exception = null, string? rawResponse = null)
            => new() { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode, Exception = exception, RawResponse = rawResponse };
    }

    /// <summary>
    /// Reprezentuje odpowiedü z informacjami o zalogowanej firmie.
    /// </summary>
    public class AuthenticatedFirmaResponse
    {
        public int? FirmaId { get; set; }
        public string? FirmaNazwa { get; set; }
        public string? Message { get; set; }
    }
}
```

## ?? Przyk≥ady Uøycia

### 1. Podstawowa Konfiguracja

```csharp
// Prosty klient (zarzπdza w≥asnym HttpClient)
using var client = new OrdersApiClient(
    baseUrl: "https://your-webhook-service.com",
    apiKey: "your-api-key-from-database"
);
```

### 2. Z IHttpClientFactory (ASP.NET Core - zalecane)

```csharp
// W Program.cs / Startup.cs
services.AddHttpClient<OrdersApiClient>();

services.AddScoped<OrdersApiClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var config = sp.GetRequiredService<IConfiguration>();
    
    return new OrdersApiClient(
        httpClient,
        config["OrdersApi:BaseUrl"]!,
        config["OrdersApi:ApiKey"]!
    );
});
```

### 3. Aktualizacja Statusu ZamÛwienia

```csharp
var request = new UpdateOrderStatusRequest
{
    Marketplace = "APTEKA_OLMED",
    OrderNumber = "ORD/2024/01/0001",
    Status = "1", // PrzyjÍto do realizacji
    Note = "ZamÛwienie przyjÍte do realizacji"
};

var result = await client.UpdateOrderStatusAsync(request);

if (result.IsSuccess)
{
    Console.WriteLine($"? Status: {result.Data.NewStatus}");
    Console.WriteLine($"  Message: {result.Data.Message}");
}
else
{
    Console.WriteLine($"? B≥πd: {result.ErrorMessage}");
    Console.WriteLine($"  HTTP Status: {result.StatusCode}");
}
```

### 4. Przes≥anie WynikÛw Realizacji

```csharp
var request = new UploadOrderRealizationRequest
{
    Marketplace = "APTEKA_OLMED",
    OrderNumber = "ORD/2024/01/0001",
    Items = new List<OrderRealizationItemDto>
    {
        new()
        {
            Sku = "PROD-001",
            Quantity = 2,
            ExpirationDate = "2025-12-31",
            SeriesNumber = "LOT2024-001"
        }
    }
};

var result = await client.UploadOrderRealizationAsync(request);

if (result.IsSuccess)
{
    Console.WriteLine($"? Przetworzono {result.Data.ItemsProcessed} pozycji");
}
```

### 5. Weryfikacja API Key

```csharp
var result = await client.GetAuthenticatedFirmaAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Firma: {result.Data.FirmaNazwa}");
    Console.WriteLine($"ID: {result.Data.FirmaId}");
}
else if (result.StatusCode == 401)
{
    Console.WriteLine("API Key nieprawid≥owy lub nieaktywny");
}
```

## ?? Autoryzacja

Wszystkie metody wymagajπ nag≥Ûwka **X-API-Key** ktÛry jest automatycznie dodawany przez klienta. API Key musi byÊ skonfigurowany w bazie danych w tabeli `Firmy`.

## ?? Statusy ZamÛwieÒ

| Status | Nazwa | Opis |
|--------|-------|------|
| `-1` | Infolinia | Wymaga interwencji |
| `0` | Oczekuje na przyjÍcie | Nowe zamÛwienie |
| `1` | PrzyjÍto do realizacji | W trakcie kompletacji |
| `5` | Gotowe do wysy≥ki | Spakowane |
| `8` | Anulowano | Anulowane |
| `9` | Przekazane do kuriera | W transporcie |
| `100` | Zg≥oszono braki | Brak towaru |

## ?? Obs≥uga B≥ÍdÛw

Klient zwraca `OrdersApiResult<T>` ktÛry zawiera:
- `IsSuccess` - czy operacja siÍ powiod≥a
- `Data` - dane odpowiedzi (jeúli sukces)
- `ErrorMessage` - komunikat b≥Ídu (jeúli niepowodzenie)
- `StatusCode` - kod HTTP
- `RawResponse` - surowa odpowiedü JSON
- `Exception` - wyjπtek ktÛry wystπpi≥ (jeúli dotyczy)

### Przyk≥ad z retry logic (Polly)

```csharp
using Polly;

var retryPolicy = Policy
    .HandleResult<OrdersApiResult<UpdateOrderStatusResponse>>(r => 
        !r.IsSuccess && r.StatusCode >= 500)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} po {timespan.TotalSeconds}s");
        });

var result = await retryPolicy.ExecuteAsync(async () =>
    await client.UpdateOrderStatusAsync(request));
```

## ?? Testowanie

### Test jednostkowy z Moq

```csharp
[Fact]
public async Task UpdateOrderStatus_Success_ReturnsSuccessResult()
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
            Content = new StringContent(/*...*/)
        });
    
    var httpClient = new HttpClient(mockHandler.Object);
    var client = new OrdersApiClient(httpClient, "https://test.com", "key");
    
    // Act
    var result = await client.UpdateOrderStatusAsync(new UpdateOrderStatusRequest { /*...*/ });
    
    // Assert
    Assert.True(result.IsSuccess);
}
```

## ?? WiÍcej Informacji

- Dokumentacja API: [README_ORDER_INVOICE_API.md](../Prosepo.Webhooks/README_ORDER_INVOICE_API.md)
- DTO Models: [Prospeo.DTOs/Order/](../Prospeo.DTOs/Order/)
- OrdersController: [Prosepo.Webhooks/Controllers/OrdersController.cs](../Prosepo.Webhooks/Controllers/OrdersController.cs)

## ? Checklist Integracji

- [ ] Dodaj kod OrdersApiClient.cs do swojego projektu
- [ ] Dodaj pakiet NuGet: System.Text.Json
- [ ] Dodaj referencjÍ do Prospeo.DTOs
- [ ] Skonfiguruj base URL i API Key
- [ ] Zarejestruj w DI (jeúli uøywasz ASP.NET Core)
- [ ] Zaimplementuj obs≥ugÍ b≥ÍdÛw
- [ ] Dodaj logowanie wywo≥aÒ
- [ ] Napisz testy jednostkowe
- [ ] Przetestuj z prawdziwym API

## ?? Rozwiπzywanie ProblemÛw

### Problem: "CS0246: The type or namespace name 'Prospeo' could not be found"
**Rozwiπzanie:** Dodaj referencjÍ do projektu Prospeo.DTOs

### Problem: "CS0234: The type or namespace name 'Json' does not exist"
**Rozwiπzanie:** Dodaj pakiet NuGet: `System.Text.Json`

### Problem: "HTTP 401 Unauthorized"
**Rozwiπzanie:** Sprawdü czy API Key jest prawid≥owy i aktywny w bazie danych (tabela Firmy)

### Problem: "HTTP 503 Service Unavailable"
**Rozwiπzanie:** Sprawdü czy serwis webhook jest uruchomiony i dostÍpny

## ?? Wsparcie

W razie problemÛw skontaktuj siÍ z zespo≥em Prospeo lub sprawdü dokumentacjÍ w repozytorium projektu.

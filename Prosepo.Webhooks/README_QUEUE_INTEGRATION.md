# Integracja WebhookController z systemem kolejki (Queue)

## Opis funkcjonalnoœci

WebhookController zosta³ rozszerzony o mo¿liwoœæ automatycznego dodawania danych produktów (`ProductDto`) do kolejki zadañ gdy webhook zostanie pomyœlnie przetworzony.

## Jak to dzia³a

1. **Odbiór webhook** - Controller odbiera i weryfikuje webhook z API Olmed
2. **Deszyfracja danych** - Dane s¹ deszyfrowane i zapisywane do pliku
3. **Analiza zawartoœci** - System sprawdza czy odszyfrowane dane zawieraj¹ informacje o produkcie
4. **Dodanie do kolejki** - Jeœli znaleziono `ProductDto`, zostaje on automatycznie dodany do kolejki zadañ

## Konfiguracja

W `appsettings.json` dodano sekcjê konfiguracji kolejki:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProspeoDb;Trusted_Connection=true;MultipleActiveResultSets=true;"
  },
  "Queue": {
    "ProductScope": 1001,
    "OrderScope": 1002,
    "DefaultFirmaId": 1,
    "WebhookProcessingFlag": 100
  }
}
```

## Wykrywanie ProductDto

System wykrywa dane produktu w dwóch scenariuszach:

1. **Zagnie¿d¿one productData** - gdy JSON zawiera sekcjê `productData`:
   ```json
   {
     "accountName": "APTEKA_OLMED",
     "marketplace": "APTEKA_OLMED",
     "productData": {
       "id": 12345,
       "sku": "PROD-001",
       "name": "Przyk³adowy produkt",
       ...
     }
   }
   ```

2. **Bezpoœredni ProductDto** - gdy ca³oœæ JSON to ProductDto (dla webhooków typu "product"):
   ```json
   {
     "id": 12345,
     "sku": "PROD-001", 
     "name": "Przyk³adowy produkt",
     ...
   }
   ```

## Struktura zadania w kolejce

Ka¿dy ProductDto dodany do kolejki tworzy zadanie z nastêpuj¹cymi parametrami:

- **Scope**: `1001` (ProductScope z konfiguracji)
- **FirmaId**: `1` (DefaultFirmaId z konfiguracji)  
- **TargetID**: `ID` produktu z ProductDto
- **Description**: `"Webhook Product Update - SKU: {sku}, Name: {name}"`
- **Flg**: `100` (WebhookProcessingFlag - do identyfikacji zadañ pochodz¹cych z webhook)
- **Request**: Pe³ny JSON ProductDto (do póŸniejszego przetwarzania)

## Endpointy

### 1. Podstawowy webhook endpoint
```
POST /api/webhook
```
- Odbiera webhook, weryfikuje, deszyfruje i automatycznie dodaje ProductDto do kolejki

### 2. Pobranie zadañ produktowych z kolejki  
```
GET /api/webhook/queue/products
```
- Zwraca listê wszystkich zadañ w kolejce pochodz¹cych z webhook (ProductDto)
- Filtruje po scope = 1001 i flag = 100

### 3. Health check
```
GET /api/webhook/health  
```
- Sprawdza status controllera i dostêpnoœæ katalogu webhook

### 4. Logi webhook
```
GET /api/webhook/logs
```
- Zwraca listê plików logów webhook

## Logowanie

Wszystkie operacje zwi¹zane z kolejk¹ s¹ logowane:

- ? Pomyœlne dodanie ProductDto do kolejki
- ?? Ostrze¿enia gdy QueueService nie jest dostêpny
- ? B³êdy podczas dodawania do kolejki
- ?? Informacje o mapowaniu productData do ProductDto

## Wymagania

1. **Baza danych** - Wymagane jest po³¹czenie do bazy danych SQL Server z tabelami:
   - `ProRWS.Queue` - tabela kolejki zadañ
   - `ProRWS.Firmy` - tabela firm

2. **NuGet packages** - Automatycznie dodawane przez `Prospeo.DbContext`:
   - Microsoft.EntityFrameworkCore.SqlServer
   - Microsoft.EntityFrameworkCore

3. **Connection String** - Musi byæ skonfigurowany w `appsettings.json`

## Przyk³ad u¿ycia

```csharp
// Webhook automatycznie przetworzy i doda ProductDto do kolejki
POST /api/webhook
Content-Type: application/json
X-OLMED-ERP-API-SIGNATURE: [signature]

{
  "guid": "12345678-1234-1234-1234-123456789012",
  "webhookType": "ProductUpdated", 
  "webhookData": "[encrypted_data]"
}

// Sprawdzenie co zosta³o dodane do kolejki
GET /api/webhook/queue/products
```

## Uwagi

- System jest odporny na brak dostêpu do bazy - jeœli `IQueueService` nie jest dostêpny, webhook nadal dzia³a ale bez funkcjonalnoœci kolejki
- Wszystkie b³êdy s¹ logowane zarówno do konsoli jak i do plików
- ProductDto musi zawieraæ prawid³owe pola (ID, SKU, Name) aby zostaæ dodany do kolejki
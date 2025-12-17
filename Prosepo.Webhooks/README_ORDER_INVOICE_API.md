# Dokumentacja Kontrolerów Order i Invoice

## Przegl¹d

Dodano dwa nowe kontrolery API z autentykacj¹ tokenow¹ do komunikacji z systemem Olmed:

1. **OrderController** - zarz¹dzanie statusami zamówieñ
2. **InvoiceController** - zarz¹dzanie fakturami

## Autentykacja

### Mechanizm ApiKeyAuth

Oba kontrolery wykorzystuj¹ mechanizm autoryzacji `ApiKeyAuthAttribute`, który:
- Wymaga nag³ówka `X-API-Key` w ka¿dym ¿¹daniu
- Weryfikuje klucz API z baz¹ danych (tabela `Firmy`)
- Umo¿liwia identyfikacjê firmy wysy³aj¹cej ¿¹danie
- Zwraca b³¹d 401 Unauthorized przy nieprawid³owym kluczu

### Konfiguracja API Key

Klucze API s¹ przechowywane w bazie danych w tabeli `ProRWS.Firmy`:
```sql
SELECT Id, NazwaFirmy, ApiKey FROM ProRWS.Firmy WHERE ApiKey IS NOT NULL;
```

## 1. OrderController

Endpoint: `/api/order`

### 1.1. GET /api/order/update-status

Aktualizuje status zamówienia w systemie Olmed.

**Parametry zapytania:**
- `orderId` (string, wymagany) - identyfikator zamówienia
- `orderStatus` (int, wymagany) - nowy status zamówienia

**Nag³ówki:**
- `X-API-Key` (wymagany) - klucz API firmy

**Przyk³ad ¿¹dania:**
```http
GET /api/order/update-status?orderId=ORD-12345&orderStatus=2 HTTP/1.1
Host: localhost:5000
X-API-Key: your-api-key-here
```

**Przyk³ad odpowiedzi (200 OK):**
```json
{
  "success": true,
  "message": "Status zamówienia zosta³ pomyœlnie zaktualizowany",
  "orderId": "ORD-12345",
  "newStatus": 2
}
```

**Przyk³ad odpowiedzi (400 Bad Request):**
```json
{
  "success": false,
  "error": "OrderId jest wymagany",
  "message": "Parametr orderId nie mo¿e byæ pusty"
}
```

**Przyk³ad odpowiedzi (401 Unauthorized):**
```json
{
  "error": "Brak nag³ówka X-API-Key",
  "message": "API Key jest wymagany do autoryzacji tego endpointa"
}
```

### 1.2. GET /api/order/authenticated-firma

Zwraca informacje o zalogowanej firmie na podstawie API Key.

**Nag³ówki:**
- `X-API-Key` (wymagany) - klucz API firmy

**Przyk³ad ¿¹dania:**
```http
GET /api/order/authenticated-firma HTTP/1.1
Host: localhost:5000
X-API-Key: your-api-key-here
```

**Przyk³ad odpowiedzi (200 OK):**
```json
{
  "firmaId": "1002",
  "firmaNazwa": "Przyk³adowa Firma Sp. z o.o.",
  "message": "Pomyœlnie zautoryzowano"
}
```

## 2. InvoiceController

Endpoint: `/api/invoice`

### 2.1. POST /api/invoice/sent

Zg³asza wys³anie faktury do systemu Olmed.

**Nag³ówki:**
- `X-API-Key` (wymagany) - klucz API firmy
- `Content-Type: application/json`

**Body (JSON):**
```json
{
  "invoiceNumber": "FV/2024/001",
  "orderId": "ORD-12345",
  "sentDate": "2024-01-15T10:30:00Z",
  "recipientEmail": "customer@example.com",
  "additionalData": {
    "deliveryMethod": "email",
    "notes": "Faktura wys³ana mailem"
  }
}
```

**Parametry body:**
- `invoiceNumber` (string, wymagany) - numer faktury
- `orderId` (string, opcjonalny) - identyfikator powi¹zanego zamówienia
- `sentDate` (DateTime, opcjonalny) - data wys³ania (domyœlnie: czas serwera)
- `recipientEmail` (string, opcjonalny) - email odbiorcy
- `additionalData` (object, opcjonalny) - dodatkowe dane

**Przyk³ad ¿¹dania:**
```http
POST /api/invoice/sent HTTP/1.1
Host: localhost:5000
X-API-Key: your-api-key-here
Content-Type: application/json

{
  "invoiceNumber": "FV/2024/001",
  "orderId": "ORD-12345",
  "sentDate": "2024-01-15T10:30:00Z",
  "recipientEmail": "customer@example.com"
}
```

**Przyk³ad odpowiedzi (200 OK):**
```json
{
  "success": true,
  "message": "Wys³anie faktury zosta³o pomyœlnie zg³oszone",
  "invoiceNumber": "FV/2024/001",
  "processedAt": "2024-01-15T10:30:15.123Z"
}
```

**Przyk³ad odpowiedzi (400 Bad Request):**
```json
{
  "success": false,
  "error": "InvoiceNumber jest wymagany",
  "message": "Numer faktury nie mo¿e byæ pusty"
}
```

### 2.2. GET /api/invoice/list

Pobiera listê faktur z systemu Olmed.

**Parametry zapytania:**
- `orderId` (string, opcjonalny) - filtrowanie po identyfikatorze zamówienia

**Nag³ówki:**
- `X-API-Key` (wymagany) - klucz API firmy

**Przyk³ad ¿¹dania:**
```http
GET /api/invoice/list?orderId=ORD-12345 HTTP/1.1
Host: localhost:5000
X-API-Key: your-api-key-here
```

**Przyk³ad odpowiedzi (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "invoiceNumber": "FV/2024/001",
      "orderId": "ORD-12345",
      "sentDate": "2024-01-15T10:30:00Z",
      "status": "sent"
    }
  ]
}
```

### 2.3. GET /api/invoice/authenticated-firma

Zwraca informacje o zalogowanej firmie na podstawie API Key.

**Nag³ówki:**
- `X-API-Key` (wymagany) - klucz API firmy

**Przyk³ad ¿¹dania:**
```http
GET /api/invoice/authenticated-firma HTTP/1.1
Host: localhost:5000
X-API-Key: your-api-key-here
```

**Przyk³ad odpowiedzi (200 OK):**
```json
{
  "firmaId": "1002",
  "firmaNazwa": "Przyk³adowa Firma Sp. z o.o.",
  "message": "Pomyœlnie zautoryzowano"
}
```

## Komunikacja z Olmed API

### OlmedApiService

Serwis `OlmedApiService` obs³uguje komunikacjê z API Olmed:
- Automatyczne zarz¹dzanie tokenem autoryzacyjnym
- Token jest cachowany przez 50 minut (wygasa po 60)
- Automatyczne logowanie przy wygaœniêciu tokena
- Wsparcie dla ¿¹dañ GET i POST

### Konfiguracja

Konfiguracja w `appsettings.json`:
```json
{
  "OlmedAuth": {
    "BaseUrl": "https://draft-csm-connector.grupaolmed.pl",
    "Username": "test_prospeo",
    "Password": "pvRGowxF&6J*M$"
  }
}
```

### Endpointy Olmed

Kontrolery wysy³aj¹ ¿¹dania do nastêpuj¹cych endpointów Olmed:
- `/erp-api/orders/update-status` - aktualizacja statusu zamówienia
- `/erp-api/invoices/sent` - zg³oszenie wys³ania faktury
- `/erp-api/invoices/list` - pobranie listy faktur

## Testowanie

### U¿ywanie cURL

**Test OrderController:**
```bash
curl -X GET "http://localhost:5000/api/order/update-status?orderId=ORD-12345&orderStatus=2" \
  -H "X-API-Key: your-api-key-here"
```

**Test InvoiceController:**
```bash
curl -X POST "http://localhost:5000/api/invoice/sent" \
  -H "X-API-Key: your-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{
    "invoiceNumber": "FV/2024/001",
    "orderId": "ORD-12345",
    "sentDate": "2024-01-15T10:30:00Z",
    "recipientEmail": "customer@example.com"
  }'
```

### U¿ywanie PowerShell

**Test OrderController:**
```powershell
$headers = @{
    "X-API-Key" = "your-api-key-here"
}

Invoke-RestMethod -Uri "http://localhost:5000/api/order/update-status?orderId=ORD-12345&orderStatus=2" `
    -Method Get `
    -Headers $headers
```

**Test InvoiceController:**
```powershell
$headers = @{
    "X-API-Key" = "your-api-key-here"
    "Content-Type" = "application/json"
}

$body = @{
    invoiceNumber = "FV/2024/001"
    orderId = "ORD-12345"
    sentDate = "2024-01-15T10:30:00Z"
    recipientEmail = "customer@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/invoice/sent" `
    -Method Post `
    -Headers $headers `
    -Body $body
```

## Logowanie

Wszystkie ¿¹dania s¹ logowane z nastêpuj¹cymi informacjami:
- Firma wykonuj¹ca ¿¹danie (ID i nazwa)
- Parametry ¿¹dania
- OdpowiedŸ z systemu Olmed
- B³êdy i wyj¹tki

Logi mo¿na znaleŸæ w:
- Konsoli aplikacji (poziom Information)
- Plikach w folderze `Logs/` (jeœli w³¹czone)

## Bezpieczeñstwo

### Zalecenia

1. **API Keys:**
   - Przechowuj klucze API w bezpieczny sposób
   - U¿ywaj ró¿nych kluczy dla ró¿nych firm/partnerów
   - Regularnie rotuj klucze API

2. **HTTPS:**
   - Zawsze u¿ywaj HTTPS w œrodowisku produkcyjnym
   - Nie wysy³aj API Keys przez niezabezpieczone po³¹czenia

3. **Rate Limiting:**
   - Rozwa¿ implementacjê rate limiting dla endpointów
   - Monitoruj nietypowe wzorce u¿ycia

4. **Logowanie:**
   - Nie loguj wra¿liwych danych (has³a, pe³ne API keys)
   - Monitoruj nieudane próby autoryzacji

## Struktura plików

```
Prosepo.Webhooks/
??? Attributes/
?   ??? ApiKeyAuthAttribute.cs          # Atrybut autoryzacji API Key
??? Controllers/
?   ??? OrderController.cs              # Kontroler zamówieñ
?   ??? InvoiceController.cs            # Kontroler faktur
??? Services/
    ??? OlmedApiService.cs               # Serwis komunikacji z Olmed

Prospeo.DTOs/
??? UpdateOrderStatusDto.cs             # DTOs dla statusu zamówienia
??? InvoiceSentDto.cs                   # DTOs dla faktury
```

## Status Zamówienia

Mo¿liwe wartoœci statusu zamówienia (przyk³adowe):
- `0` - Nowe
- `1` - W trakcie realizacji
- `2` - Wys³ane
- `3` - Dostarczone
- `4` - Anulowane
- `5` - Zwrócone

*Uwaga: Dok³adne wartoœci statusów powinny byæ zgodne z dokumentacj¹ API Olmed.*

## Troubleshooting

### Problem: 401 Unauthorized

**Przyczyna:** Nieprawid³owy lub brakuj¹cy API Key
**Rozwi¹zanie:**
1. SprawdŸ czy nag³ówek `X-API-Key` jest obecny
2. Zweryfikuj czy klucz istnieje w bazie danych: `SELECT * FROM ProRWS.Firmy WHERE ApiKey = 'your-key'`
3. SprawdŸ czy klucz nie wygas³

### Problem: 500 Internal Server Error podczas autoryzacji

**Przyczyna:** Brak po³¹czenia z baz¹ danych
**Rozwi¹zanie:**
1. SprawdŸ connection string w `appsettings.json`
2. Zweryfikuj czy serwer SQL jest dostêpny
3. SprawdŸ logi aplikacji

### Problem: B³¹d podczas komunikacji z Olmed

**Przyczyna:** Problem z tokenem lub endpoint Olmed niedostêpny
**Rozwi¹zanie:**
1. SprawdŸ konfiguracjê `OlmedAuth` w `appsettings.json`
2. Zweryfikuj czy endpoint Olmed jest dostêpny
3. SprawdŸ logi - token powinien byæ automatycznie odœwie¿any
4. Rêcznie zresetuj token wywo³uj¹c `OlmedApiService.ResetToken()`

## Changelog

### 2024-01-15 - Wersja 1.0
- ? Dodano `ApiKeyAuthAttribute` - autentykacja tokenem
- ? Dodano `OrderController` z metod¹ `update-status`
- ? Dodano `InvoiceController` z metod¹ `sent`
- ? Dodano `OlmedApiService` - serwis komunikacji z Olmed
- ? Dodano DTOs: `UpdateOrderStatusDto`, `InvoiceSentDto`
- ? Integracja z baz¹ danych (tabela Firmy)
- ? Automatyczne zarz¹dzanie tokenem Olmed

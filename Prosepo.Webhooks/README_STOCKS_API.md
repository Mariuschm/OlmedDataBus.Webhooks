# Stocks API - Dokumentacja

## Przegl¹d

Kontroler `StocksController` zapewnia API do zarz¹dzania stanami magazynowymi produktów w systemie Olmed. API wymaga autentykacji za pomoc¹ klucza API (nag³ówek `X-API-Key`).

## Format danych

Stany magazynowe s¹ organizowane wed³ug marketplace z s³ownikiem SKU:

```json
{
  "marketplace": "APTEKA_OLMED",
  "skus": {
    "14978": {
      "stock": 35,
      "average_purchase_price": 10.04
    },
    "111714": {
      "stock": 120,
      "average_purchase_price": 15.14
    }
  }
}
```

## Autentykacja

Wszystkie endpointy wymagaj¹ nag³ówka autoryzacyjnego:

```
X-API-Key: {your-api-key}
```

Klucz API jest przypisany do konkretnej firmy w bazie danych (tabela `ProRWS.Firmy`).

## Endpointy

### 1. Aktualizacja stanów magazynowych

**Endpoint:** `POST /api/stocks/update`

**Opis:** Aktualizuje stany magazynowe wielu produktów w systemie Olmed.

**Nag³ówki:**
```
Content-Type: application/json
X-API-Key: {your-api-key}
```

**Body (JSON):**
```json
{
  "marketplace": "APTEKA_OLMED",
  "skus": {
    "14978": {
      "stock": 35,
      "average_purchase_price": 10.04
    },
    "111714": {
      "stock": 120,
      "average_purchase_price": 15.14
    }
  },
  "notes": "Aktualizacja stanów magazynowych",
  "updateDate": "2024-01-15T10:30:00Z"
}
```

**Parametry:**

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `marketplace` | string | Tak | Nazwa marketplace (np. "APTEKA_OLMED") |
| `skus` | object | Tak | S³ownik SKU do aktualizacji |
| `skus[sku].stock` | decimal | Tak | Stan magazynowy (musi byæ >= 0) |
| `skus[sku].average_purchase_price` | decimal | Tak | Œrednia cena zakupu (musi byæ >= 0) |
| `notes` | string | Nie | Dodatkowe notatki |
| `updateDate` | datetime | Nie | Data aktualizacji (domyœlnie: bie¿¹ca) |

**OdpowiedŸ sukcesu (200 OK):**
```json
{
  "success": true,
  "message": "Stany magazynowe zosta³y pomyœlnie zaktualizowane",
  "marketplace": "APTEKA_OLMED",
  "updatedCount": 2,
  "updatedSkus": ["14978", "111714"],
  "processedAt": "2024-01-15T10:30:00Z"
}
```

**Mo¿liwe b³êdy:**
- `400 Bad Request` - nieprawid³owe parametry (brak marketplace, puste SKU, ujemne wartoœci)
- `401 Unauthorized` - brak lub nieprawid³owy klucz API
- `500 Internal Server Error` - b³¹d serwera lub API Olmed

---

### 2. Pobranie stanów magazynowych

**Endpoint:** `GET /api/stocks`

**Opis:** Pobiera stany magazynowe wszystkich produktów w marketplace.

**Nag³ówki:**
```
X-API-Key: {your-api-key}
```

**Parametry query:**

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `marketplace` | string | Tak | Nazwa marketplace |

**Przyk³ad:**
```
GET /api/stocks?marketplace=APTEKA_OLMED
```

**OdpowiedŸ sukcesu (200 OK):**
```json
{
  "success": true,
  "data": {
    "marketplace": "APTEKA_OLMED",
    "skus": {
      "14978": {
        "stock": 35,
        "average_purchase_price": 10.04
      },
      "111714": {
        "stock": 120,
        "average_purchase_price": 15.14
      }
    }
  }
}
```

**Mo¿liwe b³êdy:**
- `400 Bad Request` - brak parametru marketplace
- `401 Unauthorized` - brak lub nieprawid³owy klucz API
- `404 Not Found` - nie znaleziono danych dla marketplace
- `500 Internal Server Error` - b³¹d serwera lub API Olmed

---

### 3. Pobranie stanu konkretnego SKU

**Endpoint:** `GET /api/stocks/sku`

**Opis:** Pobiera stan magazynowy konkretnego produktu.

**Nag³ówki:**
```
X-API-Key: {your-api-key}
```

**Parametry query:**

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `marketplace` | string | Tak | Nazwa marketplace |
| `sku` | string | Tak | SKU produktu |

**Przyk³ad:**
```
GET /api/stocks/sku?marketplace=APTEKA_OLMED&sku=14978
```

**OdpowiedŸ sukcesu (200 OK):**
```json
{
  "success": true,
  "marketplace": "APTEKA_OLMED",
  "sku": "14978",
  "data": {
    "stock": 35,
    "average_purchase_price": 10.04
  }
}
```

**Mo¿liwe b³êdy:**
- `400 Bad Request` - brak parametru marketplace lub sku
- `401 Unauthorized` - brak lub nieprawid³owy klucz API
- `404 Not Found` - nie znaleziono produktu
- `500 Internal Server Error` - b³¹d serwera lub API Olmed

---

### 4. Informacje o zalogowanej firmie

**Endpoint:** `GET /api/stocks/authenticated-firma`

**Opis:** Zwraca informacje o firmie przypisanej do u¿ytego klucza API (endpoint testowy).

**Nag³ówki:**
```
X-API-Key: {your-api-key}
```

**OdpowiedŸ sukcesu (200 OK):**
```json
{
  "firmaId": 1,
  "firmaNazwa": "Przyk³adowa Firma Sp. z o.o.",
  "message": "Pomyœlnie zautoryzowano"
}
```

---

## Przyk³ady u¿ycia

### PowerShell

#### Aktualizacja stanów magazynowych
```powershell
$apiKey = "your-api-key-here"
$baseUrl = "https://localhost:5001"

$body = @{
    marketplace = "APTEKA_OLMED"
    skus = @{
        "14978" = @{
            stock = 35
            average_purchase_price = 10.04
        }
        "111714" = @{
            stock = 120
            average_purchase_price = 15.14
        }
    }
    notes = "Aktualizacja stanów magazynowych"
} | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod `
    -Uri "$baseUrl/api/stocks/update" `
    -Method Post `
    -Headers @{ "X-API-Key" = $apiKey } `
    -ContentType "application/json" `
    -Body $body

$response | ConvertTo-Json -Depth 10
```

#### Pobranie wszystkich stanów
```powershell
$response = Invoke-RestMethod `
    -Uri "$baseUrl/api/stocks?marketplace=APTEKA_OLMED" `
    -Method Get `
    -Headers @{ "X-API-Key" = $apiKey }

$response | ConvertTo-Json -Depth 10
```

#### Pobranie stanu konkretnego SKU
```powershell
$response = Invoke-RestMethod `
    -Uri "$baseUrl/api/stocks/sku?marketplace=APTEKA_OLMED&sku=14978" `
    -Method Get `
    -Headers @{ "X-API-Key" = $apiKey }

$response | ConvertTo-Json -Depth 10
```

---

### cURL

#### Aktualizacja stanów
```bash
curl -X POST "https://localhost:5001/api/stocks/update" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{
    "marketplace": "APTEKA_OLMED",
    "skus": {
      "14978": {
        "stock": 35,
        "average_purchase_price": 10.04
      },
      "111714": {
        "stock": 120,
        "average_purchase_price": 15.14
      }
    }
  }'
```

#### Pobranie stanów
```bash
curl -X GET "https://localhost:5001/api/stocks?marketplace=APTEKA_OLMED" \
  -H "X-API-Key: your-api-key-here"
```

#### Pobranie konkretnego SKU
```bash
curl -X GET "https://localhost:5001/api/stocks/sku?marketplace=APTEKA_OLMED&sku=14978" \
  -H "X-API-Key: your-api-key-here"
```

---

## Integracja z systemem Olmed

Wszystkie ¿¹dania s¹ przekazywane do API Olmed przez `OlmedApiService`, który obs³uguje:
- Automatyczn¹ autoryzacjê do API Olmed
- Cachowanie tokenów (50 minut)
- Obs³ugê b³êdów i retry
- Logowanie szczegó³owe dla diagnostyki

### Endpointy Olmed API
- `POST /erp-api/stocks/update` - aktualizacja stanów
- `GET /erp-api/stocks` - pobranie stanów marketplace
- `GET /erp-api/stocks/sku` - pobranie stanu konkretnego SKU

---

## Bezpieczeñstwo

1. **Autentykacja API Key**
   - Ka¿de ¿¹danie wymaga nag³ówka `X-API-Key`
   - Klucz jest weryfikowany w bazie danych (tabela `ProRWS.Firmy`)
   - Ka¿dy klucz jest przypisany do konkretnej firmy

2. **Walidacja danych**
   - Marketplace nie mo¿e byæ puste
   - S³ownik SKU musi zawieraæ co najmniej jeden element
   - Stan magazynowy nie mo¿e byæ ujemny
   - Œrednia cena zakupu nie mo¿e byæ ujemna

3. **Logowanie**
   - Wszystkie ¿¹dania s¹ logowane z informacj¹ o firmie
   - B³êdy s¹ szczegó³owo rejestrowane dla diagnostyki
   - Odpowiedzi z API Olmed s¹ logowane

---

## Konfiguracja

Konfiguracja API Olmed w `appsettings.json` lub User Secrets:

```json
{
  "OlmedApi": {
    "BaseUrl": "https://olmed-api.example.com",
    "Username": "api-user",
    "Password": "encrypted-password"
  }
}
```

**UWAGA:** U¿ywaj User Secrets dla wra¿liwych danych w œrodowisku Development!

```powershell
# Konfiguracja User Secrets
dotnet user-secrets set "OlmedApi:Username" "your-username"
dotnet user-secrets set "OlmedApi:Password" "your-password"
```

---

## Testy

Skrypt testowy PowerShell znajduje siê w: `test-stocks-api.ps1`

```powershell
# Uruchom testy
.\test-stocks-api.ps1 -ApiKey "your-api-key" -SkipCertificateCheck
```

---

## Rozwi¹zywanie problemów

### Problem: 401 Unauthorized
- SprawdŸ czy klucz API jest poprawny
- SprawdŸ czy klucz istnieje w tabeli `ProRWS.Firmy`
- SprawdŸ czy kolumna `ApiKey` jest wype³niona

### Problem: 500 Internal Server Error
- SprawdŸ logi aplikacji
- SprawdŸ po³¹czenie z API Olmed
- SprawdŸ konfiguracjê User Secrets (`OlmedApi:BaseUrl`, `Username`, `Password`)

### Problem: Dane nie s¹ aktualizowane
- SprawdŸ czy API Olmed dzia³a poprawnie
- SprawdŸ logi w `OlmedApiService` (tokeny, odpowiedzi)
- Zweryfikuj parametry ¿¹dania (marketplace, skus)
- SprawdŸ format JSON w body ¿¹dania

---

## Swagger / OpenAPI

Po uruchomieniu aplikacji, dokumentacja Swagger jest dostêpna pod adresem:
```
https://localhost:5001/swagger
```

Swagger UI umo¿liwia testowanie wszystkich endpointów bezpoœrednio z przegl¹darki.

---

## Kontakt i wsparcie

W razie problemów sprawdŸ:
1. Logi aplikacji (katalog `Logs/`)
2. Output Window w Visual Studio (zak³adka Debug)
3. Dokumentacjê g³ówn¹ projektu: `README.md`

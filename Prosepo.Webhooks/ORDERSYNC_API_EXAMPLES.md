# Przyk³ady u¿ycia OrderSyncController API

Ten dokument zawiera przyk³ady wywo³añ API dla kontrolera `OrderSyncController`.

## Podstawowe URL
```
http://localhost:5000/api/ordersync
```

---

## 1. Pobierz wszystkie aktywne konfiguracje

**Endpoint:** `GET /api/ordersync/active`

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/active" -H "accept: application/json"
```

**OdpowiedŸ:**
```json
[
  {
    "id": "olmed-sync-orders",
    "name": "Synchronizacja zamówieñ Olmed",
    "description": "Pobieranie zamówieñ z API Olmed co 2 godziny",
    "isActive": true,
    "intervalSeconds": 7200,
    "method": "POST",
    "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
    "useOlmedAuth": true,
    "headers": {
      "accept": "application/json",
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": ""
    },
    "marketplace": "APTEKA_OLMED",
    "dateRangeDays": 2,
    "useCurrentDateAsEndDate": true,
    "dateFormat": "yyyy-MM-dd",
    "additionalParameters": {}
  }
]
```

---

## 2. Pobierz wszystkie konfiguracje (aktywne i nieaktywne)

**Endpoint:** `GET /api/ordersync/all`

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/all" -H "accept: application/json"
```

---

## 3. Pobierz konkretn¹ konfiguracjê po ID

**Endpoint:** `GET /api/ordersync/{id}`

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/olmed-sync-orders" -H "accept: application/json"
```

**OdpowiedŸ 200:**
```json
{
  "id": "olmed-sync-orders",
  "name": "Synchronizacja zamówieñ Olmed",
  "description": "Pobieranie zamówieñ z API Olmed co 2 godziny",
  "isActive": true,
  "intervalSeconds": 7200,
  "method": "POST",
  "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
  "useOlmedAuth": true,
  "marketplace": "APTEKA_OLMED",
  "dateRangeDays": 2,
  "useCurrentDateAsEndDate": true,
  "dateFormat": "yyyy-MM-dd"
}
```

**OdpowiedŸ 404:**
```json
{
  "error": "Konfiguracja o ID 'nieistniejacy-id' nie zosta³a znaleziona"
}
```

---

## 4. Podgl¹d ¿¹dania HTTP (preview)

**Endpoint:** `GET /api/ordersync/{id}/preview`

**Opis:** Pokazuje jak bêdzie wygl¹daæ ¿¹danie HTTP z dynamicznie wygenerowanymi datami, bez faktycznego wysy³ania.

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/olmed-sync-orders/preview" -H "accept: application/json"
```

**OdpowiedŸ:**
```json
{
  "configurationId": "olmed-sync-orders",
  "configurationName": "Synchronizacja zamówieñ Olmed",
  "method": "POST",
  "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
  "headers": {
    "accept": "application/json",
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": ""
  },
  "body": "{\"marketplace\":\"APTEKA_OLMED\",\"dateFrom\":\"2025-01-18\",\"dateTo\":\"2025-01-20\"}",
  "bodyParsed": {
    "marketplace": "APTEKA_OLMED",
    "dateFrom": "2025-01-18",
    "dateTo": "2025-01-20"
  },
  "useOlmedAuth": true
}
```

---

## 5. Wygeneruj tylko body ¿¹dania

**Endpoint:** `GET /api/ordersync/{id}/body`

**Opis:** Generuje tylko body ¿¹dania JSON z dynamicznie obliczonymi datami.

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/olmed-sync-orders/body" -H "accept: application/json"
```

**OdpowiedŸ:**
```json
{
  "configurationId": "olmed-sync-orders",
  "body": "{\"marketplace\":\"APTEKA_OLMED\",\"dateFrom\":\"2025-01-18\",\"dateTo\":\"2025-01-20\"}",
  "bodyParsed": {
    "marketplace": "APTEKA_OLMED",
    "dateFrom": "2025-01-18",
    "dateTo": "2025-01-20"
  }
}
```

---

## 6. Dodaj lub zaktualizuj konfiguracjê

**Endpoint:** `POST /api/ordersync`

**Przyk³ad - dodanie nowej konfiguracji:**
```bash
curl -X POST "http://localhost:5000/api/ordersync" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "custom-sync-orders",
    "name": "Niestandardowa synchronizacja zamówieñ",
    "description": "Synchronizacja co 1 godzinê z zakresem 7 dni",
    "isActive": true,
    "intervalSeconds": 3600,
    "method": "POST",
    "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
    "useOlmedAuth": true,
    "headers": {
      "accept": "application/json",
      "Content-Type": "application/json"
    },
    "marketplace": "APTEKA_OLMED",
    "dateRangeDays": 7,
    "useCurrentDateAsEndDate": false,
    "dateFormat": "yyyy-MM-dd",
    "additionalParameters": {}
  }'
```

**OdpowiedŸ 200:**
```json
{
  "success": true,
  "message": "Konfiguracja zosta³a zapisana pomyœlnie",
  "configurationId": "custom-sync-orders"
}
```

**OdpowiedŸ 400 (b³¹d walidacji):**
```json
{
  "error": "ID konfiguracji nie mo¿e byæ puste"
}
```

---

## 7. Usuñ konfiguracjê

**Endpoint:** `DELETE /api/ordersync/{id}`

**Przyk³ad:**
```bash
curl -X DELETE "http://localhost:5000/api/ordersync/custom-sync-orders" -H "accept: application/json"
```

**OdpowiedŸ 200:**
```json
{
  "success": true,
  "message": "Konfiguracja 'custom-sync-orders' zosta³a usuniêta"
}
```

**OdpowiedŸ 404:**
```json
{
  "error": "Konfiguracja o ID 'nieistniejacy-id' nie zosta³a znaleziona"
}
```

---

## 8. Odœwie¿ cache konfiguracji

**Endpoint:** `POST /api/ordersync/refresh-cache`

**Opis:** Wymusza ponowne wczytanie pliku konfiguracyjnego. Przydatne po rêcznej edycji pliku `order-sync-config.json`.

**Przyk³ad:**
```bash
curl -X POST "http://localhost:5000/api/ordersync/refresh-cache" -H "accept: application/json"
```

**OdpowiedŸ:**
```json
{
  "success": true,
  "message": "Cache konfiguracji zosta³ odœwie¿ony"
}
```

---

## 9. Pobierz przyk³adow¹ konfiguracjê (template)

**Endpoint:** `GET /api/ordersync/example`

**Opis:** Zwraca przyk³adow¹ konfiguracjê, któr¹ mo¿na u¿yæ jako szablon.

**Przyk³ad:**
```bash
curl -X GET "http://localhost:5000/api/ordersync/example" -H "accept: application/json"
```

**OdpowiedŸ:**
```json
{
  "id": "example-sync-orders",
  "name": "Przyk³adowa synchronizacja zamówieñ",
  "description": "Konfiguracja demonstracyjna - pobieranie zamówieñ co 4 godziny",
  "isActive": false,
  "intervalSeconds": 14400,
  "method": "POST",
  "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
  "useOlmedAuth": true,
  "headers": {
    "accept": "application/json",
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": ""
  },
  "marketplace": "APTEKA_OLMED",
  "dateRangeDays": 3,
  "useCurrentDateAsEndDate": true,
  "dateFormat": "yyyy-MM-dd",
  "additionalParameters": {}
}
```

---

## Przyk³ady zaawansowane

### Konfiguracja z dodatkowymi parametrami

```json
{
  "id": "advanced-sync-orders",
  "name": "Zaawansowana synchronizacja",
  "isActive": true,
  "intervalSeconds": 7200,
  "method": "POST",
  "url": "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders",
  "useOlmedAuth": true,
  "marketplace": "APTEKA_OLMED",
  "dateRangeDays": 2,
  "useCurrentDateAsEndDate": true,
  "dateFormat": "yyyy-MM-dd",
  "additionalParameters": {
    "orderStatus": "PENDING",
    "includeDetails": true,
    "maxResults": 100
  }
}
```

Body wygenerowane przez serwis:
```json
{
  "marketplace": "APTEKA_OLMED",
  "dateFrom": "2025-01-18",
  "dateTo": "2025-01-20",
  "orderStatus": "PENDING",
  "includeDetails": true,
  "maxResults": 100
}
```

---

## PowerShell przyk³ady

### Test podgl¹du ¿¹dania
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/ordersync/olmed-sync-orders/preview" -Method Get
Write-Host "URL: $($response.url)"
Write-Host "Body: $($response.body)"
Write-Host "DateFrom: $($response.bodyParsed.dateFrom)"
Write-Host "DateTo: $($response.bodyParsed.dateTo)"
```

### Dodanie nowej konfiguracji
```powershell
$config = @{
    id = "test-sync-orders"
    name = "Test synchronizacja"
    isActive = $true
    intervalSeconds = 7200
    method = "POST"
    url = "https://draft-csm-connector.grupaolmed.pl/erp-api/orders/get-orders"
    useOlmedAuth = $true
    marketplace = "APTEKA_OLMED"
    dateRangeDays = 2
    useCurrentDateAsEndDate = $true
    dateFormat = "yyyy-MM-dd"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/ordersync" -Method Post -Body $config -ContentType "application/json"
```

---

## Testowanie dynamicznych dat

Aby zobaczyæ jak zmieniaj¹ siê daty w zale¿noœci od konfiguracji:

### Test 1: Dzisiejsza data jako dateTo (useCurrentDateAsEndDate = true)
```bash
curl -X GET "http://localhost:5000/api/ordersync/olmed-sync-orders/body"
```
Wynik (dla 2025-01-20):
```json
{
  "dateFrom": "2025-01-18",
  "dateTo": "2025-01-20"
}
```

### Test 2: Wczorajsza data jako dateTo (useCurrentDateAsEndDate = false)
Najpierw zaktualizuj konfiguracjê:
```bash
curl -X POST "http://localhost:5000/api/ordersync" -H "Content-Type: application/json" -d '{
  "id": "olmed-sync-orders",
  "useCurrentDateAsEndDate": false,
  ... (pozosta³e pola)
}'
```

Nastêpnie sprawdŸ body:
```bash
curl -X GET "http://localhost:5000/api/ordersync/olmed-sync-orders/body"
```
Wynik (dla 2025-01-20):
```json
{
  "dateFrom": "2025-01-17",
  "dateTo": "2025-01-19"
}
```

---

## Swagger UI

Po uruchomieniu aplikacji otwórz przegl¹darkê i przejdŸ do:
```
http://localhost:5000/swagger
```

Tam znajdziesz interaktywn¹ dokumentacjê wszystkich endpointów z mo¿liwoœci¹ testowania.

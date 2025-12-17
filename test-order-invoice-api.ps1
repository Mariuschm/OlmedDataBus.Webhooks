# Test Order and Invoice Controllers
# Skrypt testowy dla nowych endpointów API

# Konfiguracja
$baseUrl = "http://localhost:5000"
$apiKey = "your-api-key-here"  # Zmieñ na w³aœciwy API Key z bazy danych

# Kolory dla lepszej czytelnoœci
function Write-Success {
    param([string]$message)
    Write-Host "? $message" -ForegroundColor Green
}

function Write-Error {
    param([string]$message)
    Write-Host "? $message" -ForegroundColor Red
}

function Write-Info {
    param([string]$message)
    Write-Host "??  $message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$message)
    Write-Host "??  $message" -ForegroundColor Yellow
}

# Wspólne nag³ówki
$headers = @{
    "X-API-Key" = $apiKey
    "Content-Type" = "application/json"
}

Write-Info "=== Test Order and Invoice Controllers ==="
Write-Info "Base URL: $baseUrl"
Write-Info "API Key: $apiKey"
Write-Host ""

# ============================================
# TEST 1: Sprawdzenie autoryzacji
# ============================================
Write-Info "TEST 1: Sprawdzenie autoryzacji (Order Controller)"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/order/authenticated-firma" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "Autoryzacja pomyœlna"
    Write-Host "Firma ID: $($response.firmaId)"
    Write-Host "Firma Nazwa: $($response.firmaNazwa)"
} catch {
    Write-Error "B³¹d autoryzacji: $($_.Exception.Message)"
    Write-Warning "SprawdŸ czy API Key jest poprawny i istnieje w bazie danych"
    exit 1
}
Write-Host ""

# ============================================
# TEST 2: Aktualizacja statusu zamówienia
# ============================================
Write-Info "TEST 2: Aktualizacja statusu zamówienia"
$orderId = "TEST-ORD-" + (Get-Date -Format "yyyyMMdd-HHmmss")
$orderStatus = 2

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/order/update-status?orderId=$orderId&orderStatus=$orderStatus" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "Status zamówienia zaktualizowany"
    Write-Host "Order ID: $($response.orderId)"
    Write-Host "Nowy status: $($response.newStatus)"
    Write-Host "Komunikat: $($response.message)"
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Error "B³¹d podczas aktualizacji statusu"
    Write-Host "Szczegó³y: $($errorDetails.message)"
}
Write-Host ""

# ============================================
# TEST 3: Zg³oszenie wys³ania faktury
# ============================================
Write-Info "TEST 3: Zg³oszenie wys³ania faktury"
$invoiceData = @{
    invoiceNumber = "FV/TEST/" + (Get-Date -Format "yyyyMMdd-HHmmss")
    orderId = $orderId
    sentDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    recipientEmail = "test@example.com"
    additionalData = @{
        deliveryMethod = "email"
        notes = "Test invoice sent via PowerShell script"
        testMode = $true
    }
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/invoice/sent" `
        -Method Post `
        -Headers $headers `
        -Body $invoiceData `
        -ErrorAction Stop
    
    Write-Success "Faktura zg³oszona pomyœlnie"
    Write-Host "Invoice Number: $($response.invoiceNumber)"
    Write-Host "Processed At: $($response.processedAt)"
    Write-Host "Komunikat: $($response.message)"
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Error "B³¹d podczas zg³aszania faktury"
    Write-Host "Szczegó³y: $($errorDetails.message)"
}
Write-Host ""

# ============================================
# TEST 4: Pobranie listy faktur
# ============================================
Write-Info "TEST 4: Pobranie listy faktur"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/invoice/list" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "Lista faktur pobrana"
    Write-Host "Success: $($response.success)"
    Write-Host "Data: $($response.data | ConvertTo-Json -Depth 3)"
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Error "B³¹d podczas pobierania listy faktur"
    Write-Host "Szczegó³y: $($errorDetails.message)"
}
Write-Host ""

# ============================================
# TEST 5: Pobranie listy faktur z filtrem
# ============================================
Write-Info "TEST 5: Pobranie listy faktur z filtrem (orderId)"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/invoice/list?orderId=$orderId" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "Lista faktur (z filtrem) pobrana"
    Write-Host "Success: $($response.success)"
    Write-Host "Data: $($response.data | ConvertTo-Json -Depth 3)"
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Error "B³¹d podczas pobierania listy faktur"
    Write-Host "Szczegó³y: $($errorDetails.message)"
}
Write-Host ""

# ============================================
# TEST 6: Sprawdzenie autoryzacji (Invoice Controller)
# ============================================
Write-Info "TEST 6: Sprawdzenie autoryzacji (Invoice Controller)"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/invoice/authenticated-firma" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "Autoryzacja pomyœlna"
    Write-Host "Firma ID: $($response.firmaId)"
    Write-Host "Firma Nazwa: $($response.firmaNazwa)"
} catch {
    Write-Error "B³¹d autoryzacji: $($_.Exception.Message)"
}
Write-Host ""

# ============================================
# TEST 7: Nieprawid³owy API Key (oczekiwany b³¹d 401)
# ============================================
Write-Info "TEST 7: Test nieprawid³owego API Key (oczekiwany b³¹d 401)"
$invalidHeaders = @{
    "X-API-Key" = "invalid-key-12345"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/order/authenticated-firma" `
        -Method Get `
        -Headers $invalidHeaders `
        -ErrorAction Stop
    
    Write-Warning "Nieoczekiwany sukces - powinien byæ b³¹d 401"
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Success "Poprawnie zwrócono b³¹d 401 dla nieprawid³owego API Key"
    } else {
        Write-Warning "Otrzymano inny kod b³êdu: $($_.Exception.Response.StatusCode)"
    }
}
Write-Host ""

# ============================================
# TEST 8: Brak API Key (oczekiwany b³¹d 401)
# ============================================
Write-Info "TEST 8: Test braku API Key (oczekiwany b³¹d 401)"
$noKeyHeaders = @{
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/order/authenticated-firma" `
        -Method Get `
        -Headers $noKeyHeaders `
        -ErrorAction Stop
    
    Write-Warning "Nieoczekiwany sukces - powinien byæ b³¹d 401"
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Success "Poprawnie zwrócono b³¹d 401 dla braku API Key"
    } else {
        Write-Warning "Otrzymano inny kod b³êdu: $($_.Exception.Response.StatusCode)"
    }
}
Write-Host ""

# ============================================
# TEST 9: Nieprawid³owe parametry (oczekiwany b³¹d 400)
# ============================================
Write-Info "TEST 9: Test nieprawid³owych parametrów (oczekiwany b³¹d 400)"
try {
    # Pusty orderId
    $response = Invoke-RestMethod -Uri "$baseUrl/api/order/update-status?orderId=&orderStatus=2" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Warning "Nieoczekiwany sukces - powinien byæ b³¹d 400"
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Success "Poprawnie zwrócono b³¹d 400 dla pustego orderId"
    } else {
        Write-Warning "Otrzymano inny kod b³êdu: $($_.Exception.Response.StatusCode)"
    }
}
Write-Host ""

# ============================================
# Podsumowanie
# ============================================
Write-Info "=== Testy zakoñczone ==="
Write-Host ""
Write-Info "Uwagi:"
Write-Host "- Zmieñ zmienn¹ `$apiKey na w³aœciwy klucz API z bazy danych"
Write-Host "- Zmieñ zmienn¹ `$baseUrl jeœli aplikacja dzia³a na innym porcie"
Write-Host "- Niektóre testy mog¹ zakoñczyæ siê b³êdem jeœli endpoint Olmed nie jest dostêpny"
Write-Host "- SprawdŸ logi aplikacji aby zobaczyæ szczegó³y komunikacji z Olmed"
Write-Host ""

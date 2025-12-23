# Test Stocks API
# Skrypt do testowania API zarz¹dzania stanami magazynowymi

param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$ApiKey = "",
    [switch]$SkipCertificateCheck
)

# Kolory dla output
$ColorSuccess = "Green"
$ColorError = "Red"
$ColorInfo = "Cyan"
$ColorWarning = "Yellow"

# Ignoruj b³êdy certyfikatu SSL dla testów lokalnych
if ($SkipCertificateCheck) {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        # PowerShell Core
        $PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
        $PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true
    }
    else {
        # Windows PowerShell
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    }
}

Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "  Test Stocks API" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

# SprawdŸ czy podano klucz API
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "UWAGA: Nie podano klucza API!" -ForegroundColor $ColorWarning
    Write-Host "U¿yj parametru -ApiKey lub ustaw zmienn¹ œrodowiskow¹:" -ForegroundColor $ColorWarning
    Write-Host '  $env:TEST_API_KEY = "your-api-key-here"' -ForegroundColor $ColorWarning
    Write-Host ""
    
    # Spróbuj pobraæ z zmiennej œrodowiskowej
    $ApiKey = $env:TEST_API_KEY
    
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Host "Nie znaleziono klucza API. Przerywam." -ForegroundColor $ColorError
        exit 1
    }
    else {
        Write-Host "U¿yto klucza z zmiennej œrodowiskowej TEST_API_KEY" -ForegroundColor $ColorSuccess
    }
}

Write-Host "Base URL: $BaseUrl" -ForegroundColor $ColorInfo
Write-Host "API Key: $($ApiKey.Substring(0, [Math]::Min(10, $ApiKey.Length)))..." -ForegroundColor $ColorInfo
Write-Host ""

# Funkcja do wysy³ania ¿¹dañ
function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body = $null,
        [string]$Description
    )
    
    Write-Host "----------------------------------------" -ForegroundColor $ColorInfo
    Write-Host "Test: $Description" -ForegroundColor $ColorInfo
    Write-Host "Endpoint: $Method $Endpoint" -ForegroundColor $ColorInfo
    
    $headers = @{
        "X-API-Key" = $ApiKey
    }
    
    try {
        $uri = "$BaseUrl$Endpoint"
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            Write-Host "Body:" -ForegroundColor $ColorInfo
            Write-Host $jsonBody -ForegroundColor Gray
            
            $response = Invoke-RestMethod `
                -Uri $uri `
                -Method $Method `
                -Headers $headers `
                -ContentType "application/json" `
                -Body $jsonBody
        }
        else {
            $response = Invoke-RestMethod `
                -Uri $uri `
                -Method $Method `
                -Headers $headers
        }
        
        Write-Host "? Sukces!" -ForegroundColor $ColorSuccess
        Write-Host "OdpowiedŸ:" -ForegroundColor $ColorInfo
        $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
        
        return @{ Success = $true; Response = $response }
    }
    catch {
        Write-Host "? B³¹d!" -ForegroundColor $ColorError
        Write-Host "Status: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor $ColorError
        Write-Host "Wiadomoœæ: $($_.Exception.Message)" -ForegroundColor $ColorError
        
        if ($_.ErrorDetails.Message) {
            Write-Host "Szczegó³y:" -ForegroundColor $ColorError
            Write-Host $_.ErrorDetails.Message -ForegroundColor $ColorError
        }
        
        return @{ Success = $false; Error = $_.Exception.Message }
    }
    finally {
        Write-Host ""
    }
}

# Test 1: Weryfikacja autentykacji
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "1. TEST AUTENTYKACJI" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$result = Invoke-ApiRequest `
    -Method "GET" `
    -Endpoint "/api/stocks/authenticated-firma" `
    -Description "Weryfikacja autentykacji i pobranie danych firmy"

if (-not $result.Success) {
    Write-Host "Autentykacja nie powiod³a siê. SprawdŸ klucz API!" -ForegroundColor $ColorError
    exit 1
}

# Test 2: Aktualizacja stanów magazynowych
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "2. TEST AKTUALIZACJI STANÓW MAGAZYNOWYCH" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$updateBody = @{
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
        "999999" = @{
            stock = 50
            average_purchase_price = 25.50
        }
    }
    notes = "Test aktualizacji stanów magazynowych"
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $updateBody `
    -Description "Aktualizacja stanów dla 3 SKU"

# Test 3: Pobranie wszystkich stanów
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "3. TEST POBRANIA WSZYSTKICH STANÓW" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$result = Invoke-ApiRequest `
    -Method "GET" `
    -Endpoint "/api/stocks?marketplace=APTEKA_OLMED" `
    -Description "Pobranie wszystkich stanów magazynowych dla marketplace"

# Test 4: Pobranie stanu konkretnego SKU
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "4. TEST POBRANIA STANU KONKRETNEGO SKU" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$result = Invoke-ApiRequest `
    -Method "GET" `
    -Endpoint "/api/stocks/sku?marketplace=APTEKA_OLMED&sku=14978" `
    -Description "Pobranie stanu dla SKU 14978"

# Test 5: Aktualizacja pojedynczego SKU
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "5. TEST AKTUALIZACJI POJEDYNCZEGO SKU" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$singleSkuBody = @{
    marketplace = "APTEKA_OLMED"
    skus = @{
        "14978" = @{
            stock = 100
            average_purchase_price = 12.50
        }
    }
    notes = "Aktualizacja pojedynczego SKU"
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $singleSkuBody `
    -Description "Aktualizacja pojedynczego SKU"

# Test 6: Walidacja - brak marketplace
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "6. TEST WALIDACJI - BRAK MARKETPLACE" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$invalidBody = @{
    marketplace = ""
    skus = @{
        "14978" = @{
            stock = 100
            average_purchase_price = 10.00
        }
    }
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $invalidBody `
    -Description "Próba aktualizacji bez marketplace (oczekiwany b³¹d 400)"

# Test 7: Walidacja - puste SKU
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "7. TEST WALIDACJI - PUSTE SKU" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$invalidBody = @{
    marketplace = "APTEKA_OLMED"
    skus = @{}
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $invalidBody `
    -Description "Próba aktualizacji z pustym s³ownikiem SKU (oczekiwany b³¹d 400)"

# Test 8: Walidacja - ujemny stan
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "8. TEST WALIDACJI - UJEMNY STAN" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$invalidBody = @{
    marketplace = "APTEKA_OLMED"
    skus = @{
        "14978" = @{
            stock = -10
            average_purchase_price = 10.00
        }
    }
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $invalidBody `
    -Description "Próba aktualizacji z ujemnym stanem (oczekiwany b³¹d 400)"

# Test 9: Walidacja - ujemna cena
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "9. TEST WALIDACJI - UJEMNA CENA ZAKUPU" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$invalidBody = @{
    marketplace = "APTEKA_OLMED"
    skus = @{
        "14978" = @{
            stock = 100
            average_purchase_price = -5.00
        }
    }
}

$result = Invoke-ApiRequest `
    -Method "POST" `
    -Endpoint "/api/stocks/update" `
    -Body $invalidBody `
    -Description "Próba aktualizacji z ujemn¹ cen¹ zakupu (oczekiwany b³¹d 400)"

# Test 10: Pobranie nieistniej¹cego marketplace
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "10. TEST NIEISTNIEJ¥CEGO MARKETPLACE" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$result = Invoke-ApiRequest `
    -Method "GET" `
    -Endpoint "/api/stocks?marketplace=NONEXISTENT_MARKETPLACE" `
    -Description "Próba pobrania stanów dla nieistniej¹cego marketplace (oczekiwany b³¹d 404)"

# Test 11: Pobranie nieistniej¹cego SKU
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "11. TEST NIEISTNIEJ¥CEGO SKU" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""

$result = Invoke-ApiRequest `
    -Method "GET" `
    -Endpoint "/api/stocks/sku?marketplace=APTEKA_OLMED&sku=NONEXISTENT-SKU-999999" `
    -Description "Próba pobrania stanu nieistniej¹cego SKU (oczekiwany b³¹d 404)"

# Podsumowanie
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host "  TESTY ZAKOÑCZONE" -ForegroundColor $ColorInfo
Write-Host "================================================" -ForegroundColor $ColorInfo
Write-Host ""
Write-Host "SprawdŸ wyniki powy¿ej." -ForegroundColor $ColorInfo
Write-Host "? - Test zakoñczony sukcesem" -ForegroundColor $ColorSuccess
Write-Host "? - Test zakoñczony b³êdem" -ForegroundColor $ColorError
Write-Host ""
Write-Host "UWAGA: Niektóre testy (6-11) celowo zwracaj¹ b³êdy," -ForegroundColor $ColorWarning
Write-Host "aby zweryfikowaæ poprawnoœæ walidacji." -ForegroundColor $ColorWarning
Write-Host ""

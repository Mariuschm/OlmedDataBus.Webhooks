# ================================================================================================
# ?? TEST KONFIGURACJI BEZPIECZEÑSTWA
# ================================================================================================
# Ten skrypt testuje czy User Secrets s¹ poprawnie skonfigurowane
# i czy aplikacja mo¿e siê uruchomiæ
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? TEST KONFIGURACJI BEZPIECZEÑSTWA - Prosepo.Webhooks" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

# PrzejdŸ do katalogu projektu
$projectPath = Join-Path $PSScriptRoot "Prosepo.Webhooks"
Set-Location $projectPath

Write-Host "?? Katalog projektu: $projectPath" -ForegroundColor Yellow
Write-Host ""

# ================================================================================================
# TEST 1: SprawdŸ czy User Secrets s¹ zainicjalizowane
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 1: Sprawdzanie inicjalizacji User Secrets" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

$userSecretsOutput = dotnet user-secrets list 2>&1
$userSecretsExitCode = $LASTEXITCODE

if ($userSecretsExitCode -ne 0) {
    Write-Host "? FAILED: User Secrets nie s¹ zainicjalizowane!" -ForegroundColor Red
    Write-Host "   Rozwi¹zanie: Uruchom .\setup-user-secrets.ps1" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: User Secrets s¹ zainicjalizowane" -ForegroundColor Green
    Write-Host ""
}

# ================================================================================================
# TEST 2: SprawdŸ czy ConnectionString jest skonfigurowany
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 2: Sprawdzanie ConnectionString" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

$connectionStringOutput = dotnet user-secrets list | Select-String "ConnectionStrings:DefaultConnection"

if ($null -eq $connectionStringOutput -or $connectionStringOutput -eq "") {
    Write-Host "? FAILED: ConnectionString nie jest skonfigurowany!" -ForegroundColor Red
    Write-Host "   Rozwi¹zanie: Uruchom .\setup-user-secrets.ps1" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: ConnectionString jest skonfigurowany" -ForegroundColor Green
    Write-Host "   $connectionStringOutput" -ForegroundColor Gray
    Write-Host ""
}

# ================================================================================================
# TEST 3: SprawdŸ czy klucze OlmedDataBus s¹ skonfigurowane
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 3: Sprawdzanie kluczy OlmedDataBus" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

$encryptionKeyOutput = dotnet user-secrets list | Select-String "OlmedDataBus:WebhookKeys:EncryptionKey"
$hmacKeyOutput = dotnet user-secrets list | Select-String "OlmedDataBus:WebhookKeys:HmacKey"

$encryptionKeyOk = $null -ne $encryptionKeyOutput -and $encryptionKeyOutput -ne ""
$hmacKeyOk = $null -ne $hmacKeyOutput -and $hmacKeyOutput -ne ""

if (-not $encryptionKeyOk -or -not $hmacKeyOk) {
    Write-Host "? FAILED: Klucze OlmedDataBus nie s¹ skonfigurowane!" -ForegroundColor Red
    Write-Host "   Rozwi¹zanie: Uruchom .\setup-user-secrets.ps1" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: Klucze OlmedDataBus s¹ skonfigurowane" -ForegroundColor Green
    Write-Host "   EncryptionKey: OK" -ForegroundColor Gray
    Write-Host "   HmacKey: OK" -ForegroundColor Gray
    Write-Host ""
}

# ================================================================================================
# TEST 4: SprawdŸ czy dane OlmedAuth s¹ skonfigurowane
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 4: Sprawdzanie danych OlmedAuth" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

$authUsernameOutput = dotnet user-secrets list | Select-String "OlmedAuth:Username"
$authPasswordOutput = dotnet user-secrets list | Select-String "OlmedAuth:Password"

$authUsernameOk = $null -ne $authUsernameOutput -and $authUsernameOutput -ne ""
$authPasswordOk = $null -ne $authPasswordOutput -and $authPasswordOutput -ne ""

if (-not $authUsernameOk -or -not $authPasswordOk) {
    Write-Host "? FAILED: Dane OlmedAuth nie s¹ skonfigurowane!" -ForegroundColor Red
    Write-Host "   Rozwi¹zanie: Uruchom .\setup-user-secrets.ps1" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: Dane OlmedAuth s¹ skonfigurowane" -ForegroundColor Green
    Write-Host "   Username: OK" -ForegroundColor Gray
    Write-Host "   Password: OK" -ForegroundColor Gray
    Write-Host ""
}

# ================================================================================================
# TEST 5: SprawdŸ czy appsettings.json NIE zawiera wra¿liwych danych
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 5: Sprawdzanie appsettings.json" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

$appsettingsContent = Get-Content "appsettings.json" -Raw

$hasPlaintextPassword = $appsettingsContent -match '"Password"\s*:\s*"(?!CONFIGURE_VIA_USER_SECRETS_OR_ENVIRONMENT_VARIABLES)'
$hasPlaintextKey = $appsettingsContent -match '"EncryptionKey"\s*:\s*"(?!CONFIGURE_VIA_USER_SECRETS_OR_ENVIRONMENT_VARIABLES)'
$hasPlaintextConnection = $appsettingsContent -match '"DefaultConnection"\s*:\s*"Server='

if ($hasPlaintextPassword -or $hasPlaintextKey -or $hasPlaintextConnection) {
    Write-Host "? FAILED: appsettings.json zawiera wra¿liwe dane w plain text!" -ForegroundColor Red
    Write-Host "   To jest powa¿ne zagro¿enie bezpieczeñstwa!" -ForegroundColor Red
    Write-Host "   Rozwi¹zanie: Przywróæ appsettings.json z Git i uruchom .\setup-user-secrets.ps1" -ForegroundColor Yellow
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: appsettings.json NIE zawiera wra¿liwych danych" -ForegroundColor Green
    Write-Host ""
}

# ================================================================================================
# TEST 6: Test kompilacji
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "TEST 6: Test kompilacji projektu" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Kompilowanie projektu..." -ForegroundColor Yellow
$buildOutput = dotnet build --configuration Debug 2>&1
$buildExitCode = $LASTEXITCODE

if ($buildExitCode -ne 0) {
    Write-Host "? FAILED: Projekt nie kompiluje siê!" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Red
    Write-Host ""
    Set-Location $PSScriptRoot
    exit 1
} else {
    Write-Host "? PASSED: Projekt kompiluje siê poprawnie" -ForegroundColor Green
    Write-Host ""
}

# ================================================================================================
# PODSUMOWANIE
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "? WSZYSTKIE TESTY PRZESZ£Y POMYŒLNIE!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Podsumowanie:" -ForegroundColor Yellow
Write-Host "   ? User Secrets zainicjalizowane" -ForegroundColor Green
Write-Host "   ? ConnectionString skonfigurowany" -ForegroundColor Green
Write-Host "   ? Klucze OlmedDataBus skonfigurowane" -ForegroundColor Green
Write-Host "   ? Dane OlmedAuth skonfigurowane" -ForegroundColor Green
Write-Host "   ? appsettings.json bezpieczny" -ForegroundColor Green
Write-Host "   ? Projekt kompiluje siê" -ForegroundColor Green
Write-Host ""

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? Aplikacja jest gotowa do uruchomienia!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Nastêpne kroki:" -ForegroundColor Yellow
Write-Host "   1. Uruchom aplikacjê: dotnet run" -ForegroundColor White
Write-Host "   2. Przetestuj endpoint: https://localhost:5001/api/webhook" -ForegroundColor White
Write-Host "   3. SprawdŸ logi w katalogu Logs/" -ForegroundColor White
Write-Host ""

# Powrót do g³ównego katalogu
Set-Location $PSScriptRoot

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "? TEST ZAKOÑCZONY" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan

# ================================================================================================
# ?? KONFIGURACJA USER SECRETS DLA PROJEKTU Prosepo.Webhooks
# ================================================================================================
# Ten skrypt automatycznie konfiguruje User Secrets dla œrodowiska deweloperskiego
# User Secrets przechowuje wra¿liwe dane poza kodem Ÿród³owym
#
# Lokalizacja User Secrets:
# Windows: %APPDATA%\Microsoft\UserSecrets\olmedatabus-webhooks-2024\secrets.json
# Linux/macOS: ~/.microsoft/usersecrets/olmedatabus-webhooks-2024/secrets.json
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? KONFIGURACJA USER SECRETS - Prosepo.Webhooks" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

# PrzejdŸ do katalogu projektu
$projectPath = Join-Path $PSScriptRoot "Prosepo.Webhooks"
Set-Location $projectPath

Write-Host "?? Katalog projektu: $projectPath" -ForegroundColor Yellow
Write-Host ""

# SprawdŸ czy User Secrets s¹ ju¿ zainicjalizowane
Write-Host "?? Sprawdzanie czy User Secrets s¹ zainicjalizowane..." -ForegroundColor Yellow
$secretsId = dotnet user-secrets list 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "??  User Secrets nie s¹ zainicjalizowane. Inicjalizacja..." -ForegroundColor Yellow
    dotnet user-secrets init --id "olmedatabus-webhooks-2024"
    Write-Host "? User Secrets zainicjalizowane!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "? User Secrets ju¿ zainicjalizowane" -ForegroundColor Green
    Write-Host ""
}

# ================================================================================================
# KONFIGURACJA WRA¯LIWYCH DANYCH
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? KONFIGURACJA WRA¯LIWYCH DANYCH" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Connection String do bazy danych
Write-Host "1??  Konfiguracja Connection String do bazy danych SQL Server..." -ForegroundColor Yellow
$connectionString = "Server=192.168.88.210;Database=PROSWB;User Id=sa;Password=zaq12wsX;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString
Write-Host "   ? Connection String skonfigurowany" -ForegroundColor Green
Write-Host ""

# 2. OlmedDataBus - EncryptionKey
Write-Host "2??  Konfiguracja OlmedDataBus EncryptionKey..." -ForegroundColor Yellow
$encryptionKey = "yw523eo6PwCKdEKHC61ocrwhTfceSZgV"
dotnet user-secrets set "OlmedDataBus:WebhookKeys:EncryptionKey" $encryptionKey
Write-Host "   ? EncryptionKey skonfigurowany" -ForegroundColor Green
Write-Host ""

# 3. OlmedDataBus - HmacKey
Write-Host "3??  Konfiguracja OlmedDataBus HmacKey..." -ForegroundColor Yellow
$hmacKey = "yryOvM0rNbhiuzbpF3WKcTcKWIluZ7ki"
dotnet user-secrets set "OlmedDataBus:WebhookKeys:HmacKey" $hmacKey
Write-Host "   ? HmacKey skonfigurowany" -ForegroundColor Green
Write-Host ""

# 4. OlmedAuth - Username
Write-Host "4??  Konfiguracja OlmedAuth Username..." -ForegroundColor Yellow
$authUsername = "test_prospeo"
dotnet user-secrets set "OlmedAuth:Username" $authUsername
Write-Host "   ? Username skonfigurowany" -ForegroundColor Green
Write-Host ""

# 5. OlmedAuth - Password
Write-Host "5??  Konfiguracja OlmedAuth Password..." -ForegroundColor Yellow
$authPassword = "pvRGowxF&6J*M$"
dotnet user-secrets set "OlmedAuth:Password" $authPassword
Write-Host "   ? Password skonfigurowany" -ForegroundColor Green
Write-Host ""

# ================================================================================================
# PODSUMOWANIE
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "? KONFIGURACJA ZAKOÑCZONA POMYŒLNIE!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Lista skonfigurowanych sekretów:" -ForegroundColor Yellow
Write-Host ""
dotnet user-secrets list
Write-Host ""

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? LOKALIZACJA PLIKU SECRETS.JSON:" -ForegroundColor Yellow
Write-Host "   Windows: %APPDATA%\Microsoft\UserSecrets\olmedatabus-webhooks-2024\secrets.json" -ForegroundColor White
Write-Host "   Pe³na œcie¿ka: $env:APPDATA\Microsoft\UserSecrets\olmedatabus-webhooks-2024\secrets.json" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? DODATKOWE INFORMACJE:" -ForegroundColor Yellow
Write-Host "   • User Secrets dzia³aj¹ tylko w œrodowisku DEVELOPMENT" -ForegroundColor White
Write-Host "   • Dla produkcji u¿yj zmiennych œrodowiskowych (zobacz: setup-production-env.ps1)" -ForegroundColor White
Write-Host "   • User Secrets NIE s¹ commitowane do repozytorium Git" -ForegroundColor White
Write-Host "   • Ka¿dy deweloper musi uruchomiæ ten skrypt lokalnie" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? Aplikacja jest gotowa do uruchomienia w trybie deweloperskim!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan

# Powrót do g³ównego katalogu
Set-Location $PSScriptRoot

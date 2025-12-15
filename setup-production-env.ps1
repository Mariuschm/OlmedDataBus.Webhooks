# ================================================================================================
# ?? KONFIGURACJA ZMIENNYCH ŒRODOWISKOWYCH DLA PRODUKCJI - IIS
# ================================================================================================
# Ten skrypt konfiguruje zmienne œrodowiskowe na poziomie IIS Application Pool
# dla bezpiecznego przechowywania wra¿liwych danych w œrodowisku produkcyjnym
#
# UWAGA: Wymaga uprawnieñ administratora!
# ================================================================================================

#Requires -RunAsAdministrator

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? KONFIGURACJA ZMIENNYCH ŒRODOWISKOWYCH - PRODUKCJA (IIS)" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

# Import modu³u WebAdministration
Import-Module WebAdministration -ErrorAction Stop

# ================================================================================================
# PARAMETRY KONFIGURACYJNE
# ================================================================================================

$appPoolName = "ProsepoWebhooks"
$siteName = "ProsepoWebhooks"

Write-Host "?? Parametry konfiguracji:" -ForegroundColor Yellow
Write-Host "   Application Pool: $appPoolName" -ForegroundColor White
Write-Host "   Site Name: $siteName" -ForegroundColor White
Write-Host ""

# SprawdŸ czy Application Pool istnieje
if (-not (Test-Path "IIS:\AppPools\$appPoolName")) {
    Write-Host "? Application Pool '$appPoolName' nie istnieje!" -ForegroundColor Red
    Write-Host "   Uruchom najpierw skrypt: publish-to-iis.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "? Application Pool '$appPoolName' znaleziony" -ForegroundColor Green
Write-Host ""

# ================================================================================================
# FUNKCJA DO USTAWIANIA ZMIENNYCH ŒRODOWISKOWYCH
# ================================================================================================

function Set-AppPoolEnvironmentVariable {
    param(
        [string]$AppPoolName,
        [string]$Name,
        [string]$Value
    )
    
    $appPoolPath = "IIS:\AppPools\$AppPoolName"
    $envVars = Get-ItemProperty $appPoolPath -Name environmentVariables
    
    # Usuñ istniej¹c¹ zmienn¹ o tej samej nazwie (jeœli istnieje)
    $filteredVars = $envVars.Collection | Where-Object { $_.name -ne $Name }
    
    # Dodaj now¹ zmienn¹
    Clear-ItemProperty $appPoolPath -Name environmentVariables
    
    foreach ($var in $filteredVars) {
        Add-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.applicationHost/applicationPools/add[@name='$AppPoolName']/environmentVariables" -name "." -value @{name=$var.name;value=$var.value}
    }
    
    Add-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' -filter "system.applicationHost/applicationPools/add[@name='$AppPoolName']/environmentVariables" -name "." -value @{name=$Name;value=$Value}
}

# ================================================================================================
# KONFIGURACJA WRA¯LIWYCH DANYCH
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? KONFIGURACJA WRA¯LIWYCH DANYCH" -ForegroundColor Cyan
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "??  UWAGA: Poni¿ej znajduj¹ siê przyk³adowe wartoœci!" -ForegroundColor Yellow
Write-Host "   Przed u¿yciem w produkcji, zmieñ je na w³aœciwe wartoœci produkcyjne!" -ForegroundColor Yellow
Write-Host ""

$continueSetup = Read-Host "Czy chcesz kontynuowaæ? (T/N)"
if ($continueSetup -ne "T" -and $continueSetup -ne "t") {
    Write-Host "Anulowano konfiguracjê" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# 1. Ustaw ASPNETCORE_ENVIRONMENT na Production
Write-Host "1??  Ustawianie ASPNETCORE_ENVIRONMENT=Production..." -ForegroundColor Yellow
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "ASPNETCORE_ENVIRONMENT" -Value "Production"
Write-Host "   ? ASPNETCORE_ENVIRONMENT skonfigurowany" -ForegroundColor Green
Write-Host ""

# 2. Connection String do bazy danych
Write-Host "2??  Konfiguracja Connection String do bazy danych SQL Server..." -ForegroundColor Yellow
$connectionString = Read-Host "   Podaj Connection String (Enter = u¿yj przyk³adowej wartoœci DEV)"
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $connectionString = "Server=192.168.88.210;Database=PROSWB;User Id=sa;Password=zaq12wsX;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;"
    Write-Host "   ??  U¿ywam przyk³adowej wartoœci DEV!" -ForegroundColor Yellow
}
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "ConnectionStrings__DefaultConnection" -Value $connectionString
Write-Host "   ? Connection String skonfigurowany" -ForegroundColor Green
Write-Host ""

# 3. OlmedDataBus - EncryptionKey
Write-Host "3??  Konfiguracja OlmedDataBus EncryptionKey..." -ForegroundColor Yellow
$encryptionKey = Read-Host "   Podaj EncryptionKey (Enter = u¿yj przyk³adowej wartoœci DEV)"
if ([string]::IsNullOrWhiteSpace($encryptionKey)) {
    $encryptionKey = "yw523eo6PwCKdEKHC61ocrwhTfceSZgV"
    Write-Host "   ??  U¿ywam przyk³adowej wartoœci DEV!" -ForegroundColor Yellow
}
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "OlmedDataBus__WebhookKeys__EncryptionKey" -Value $encryptionKey
Write-Host "   ? EncryptionKey skonfigurowany" -ForegroundColor Green
Write-Host ""

# 4. OlmedDataBus - HmacKey
Write-Host "4??  Konfiguracja OlmedDataBus HmacKey..." -ForegroundColor Yellow
$hmacKey = Read-Host "   Podaj HmacKey (Enter = u¿yj przyk³adowej wartoœci DEV)"
if ([string]::IsNullOrWhiteSpace($hmacKey)) {
    $hmacKey = "yryOvM0rNbhiuzbpF3WKcTcKWIluZ7ki"
    Write-Host "   ??  U¿ywam przyk³adowej wartoœci DEV!" -ForegroundColor Yellow
}
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "OlmedDataBus__WebhookKeys__HmacKey" -Value $hmacKey
Write-Host "   ? HmacKey skonfigurowany" -ForegroundColor Green
Write-Host ""

# 5. OlmedAuth - Username
Write-Host "5??  Konfiguracja OlmedAuth Username..." -ForegroundColor Yellow
$authUsername = Read-Host "   Podaj Username (Enter = u¿yj przyk³adowej wartoœci DEV)"
if ([string]::IsNullOrWhiteSpace($authUsername)) {
    $authUsername = "test_prospeo"
    Write-Host "   ??  U¿ywam przyk³adowej wartoœci DEV!" -ForegroundColor Yellow
}
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "OlmedAuth__Username" -Value $authUsername
Write-Host "   ? Username skonfigurowany" -ForegroundColor Green
Write-Host ""

# 6. OlmedAuth - Password
Write-Host "6??  Konfiguracja OlmedAuth Password..." -ForegroundColor Yellow
$authPassword = Read-Host "   Podaj Password (Enter = u¿yj przyk³adowej wartoœci DEV)" -AsSecureString
if ($authPassword.Length -eq 0) {
    $authPasswordPlain = "pvRGowxF&6J*M$"
    Write-Host "   ??  U¿ywam przyk³adowej wartoœci DEV!" -ForegroundColor Yellow
} else {
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($authPassword)
    $authPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
}
Set-AppPoolEnvironmentVariable -AppPoolName $appPoolName -Name "OlmedAuth__Password" -Value $authPasswordPlain
Write-Host "   ? Password skonfigurowany" -ForegroundColor Green
Write-Host ""

# ================================================================================================
# RESTART APPLICATION POOL
# ================================================================================================

Write-Host "?? Restartowanie Application Pool..." -ForegroundColor Yellow
Restart-WebAppPool -Name $appPoolName
Start-Sleep -Seconds 2
Write-Host "? Application Pool zrestartowany" -ForegroundColor Green
Write-Host ""

# ================================================================================================
# PODSUMOWANIE
# ================================================================================================

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "? KONFIGURACJA ZAKOÑCZONA POMYŒLNIE!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Skonfigurowane zmienne œrodowiskowe:" -ForegroundColor Yellow
$appPoolPath = "IIS:\AppPools\$appPoolName"
$envVars = Get-ItemProperty $appPoolPath -Name environmentVariables
foreach ($var in $envVars.Collection) {
    $displayValue = $var.value
    # Ukryj wra¿liwe dane w wyœwietlaniu
    if ($var.name -like "*Password*" -or $var.name -like "*Key*" -or $var.name -like "*ConnectionString*") {
        $displayValue = "***HIDDEN***"
    }
    Write-Host "   • $($var.name) = $displayValue" -ForegroundColor White
}
Write-Host ""

Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? DODATKOWE INFORMACJE:" -ForegroundColor Yellow
Write-Host "   • Zmienne œrodowiskowe s¹ bezpiecznie przechowywane w konfiguracji IIS" -ForegroundColor White
Write-Host "   • Dostêp do zmiennych ma tylko Application Pool: $appPoolName" -ForegroundColor White
Write-Host "   • Zmienne NIE s¹ widoczne w plikach konfiguracyjnych" -ForegroundColor White
Write-Host "   • W razie zmian, uruchom ponownie ten skrypt" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================================" -ForegroundColor Cyan
Write-Host "?? Aplikacja jest gotowa do uruchomienia w œrodowisku produkcyjnym!" -ForegroundColor Green
Write-Host "================================================================================================" -ForegroundColor Cyan

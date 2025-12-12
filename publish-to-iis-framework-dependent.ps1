# Skrypt publikacji aplikacji Prosepo.Webhooks do IIS (Framework-Dependent)
# Wymaga zainstalowanego .NET 10 Runtime na serwerze
# Wersja: 1.0
# Data: 2024-12-12

param(
    [string]$OutputPath = "C:\inetpub\wwwroot\ProspeoWebhooks",
    [string]$Configuration = "Release"
)

Write-Host "=== Publikacja Prosepo.Webhooks do IIS (Framework-Dependent) ===" -ForegroundColor Green
Write-Host "Œcie¿ka docelowa: $OutputPath" -ForegroundColor Cyan
Write-Host "Konfiguracja: $Configuration" -ForegroundColor Cyan
Write-Host ""

# SprawdŸ czy .NET 10 Runtime jest zainstalowany
Write-Host "Sprawdzanie .NET Runtime..." -ForegroundColor Yellow
$runtimes = dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 10"
if ($runtimes) {
    Write-Host "   ? Znaleziono .NET 10 Runtime:" -ForegroundColor Green
    $runtimes | ForEach-Object { Write-Host "     $_" -ForegroundColor Gray }
} else {
    Write-Host "   ? B£¥D: Nie znaleziono .NET 10 Runtime!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Aby zainstalowaæ .NET 10 Runtime, pobierz go z:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Lub u¿yj skryptu publish-to-iis.ps1 dla self-contained deployment" -ForegroundColor Yellow
    exit 1
}

# PrzejdŸ do katalogu projektu
$projectPath = "Prosepo.Webhooks"
Set-Location $projectPath

Write-Host ""
Write-Host "1. Czyszczenie poprzedniej publikacji..." -ForegroundColor Yellow
if (Test-Path $OutputPath) {
    # Zatrzymaj aplikacjê w IIS przed usuniêciem
    Write-Host "   Zatrzymywanie puli aplikacji..." -ForegroundColor Yellow
    try {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        Stop-WebAppPool -Name "ProspeoWebhooksPool" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    } catch {
        Write-Host "   Pula aplikacji nie istnieje lub nie mo¿na jej zatrzymaæ" -ForegroundColor Gray
    }
    
    Remove-Item -Path $OutputPath\* -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "   ? Wyczyszczono katalog docelowy" -ForegroundColor Green
}

Write-Host ""
Write-Host "2. Publikacja aplikacji (framework-dependent)..." -ForegroundColor Yellow

# Usuñ tymczasowo SelfContained i RuntimeIdentifier z csproj
$csprojPath = "Prosepo.Webhooks.csproj"
$csprojContent = Get-Content $csprojPath -Raw
$originalContent = $csprojContent

# Usuñ RuntimeIdentifier i SelfContained jeœli istniej¹
$csprojContent = $csprojContent -replace '<RuntimeIdentifier>.*?</RuntimeIdentifier>', ''
$csprojContent = $csprojContent -replace '<SelfContained>.*?</SelfContained>', ''
Set-Content -Path $csprojPath -Value $csprojContent

try {
    dotnet publish -c $Configuration -o $OutputPath

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ? Publikacja zakoñczona sukcesem" -ForegroundColor Green
    } else {
        Write-Host "   ? B³¹d podczas publikacji" -ForegroundColor Red
        Set-Content -Path $csprojPath -Value $originalContent
        exit 1
    }
} finally {
    # Przywróæ oryginaln¹ zawartoœæ csproj
    Set-Content -Path $csprojPath -Value $originalContent
}

Write-Host ""
Write-Host "3. Konfiguracja uprawnieñ..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $OutputPath
    $permission = "IIS_IUSRS","FullControl","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    Set-Acl $OutputPath $acl
    Write-Host "   ? Ustawiono uprawnienia dla IIS_IUSRS" -ForegroundColor Green
} catch {
    Write-Host "   ? Nie uda³o siê ustawiæ uprawnieñ: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "4. Tworzenie katalogów roboczych..." -ForegroundColor Yellow
$workingDirs = @("Logs", "WebhookData", "CronJobLogs")
foreach ($dir in $workingDirs) {
    $dirPath = Join-Path $OutputPath $dir
    if (!(Test-Path $dirPath)) {
        New-Item -Path $dirPath -ItemType Directory -Force | Out-Null
        Write-Host "   ? Utworzono katalog: $dir" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "5. Konfiguracja IIS..." -ForegroundColor Yellow

# Import modu³u WebAdministration
Import-Module WebAdministration -ErrorAction SilentlyContinue

# Utwórz pulê aplikacji jeœli nie istnieje
$appPoolName = "ProspeoWebhooksPool"
if (!(Test-Path "IIS:\AppPools\$appPoolName")) {
    New-WebAppPool -Name $appPoolName
    Write-Host "   ? Utworzono pulê aplikacji: $appPoolName" -ForegroundColor Green
} else {
    Write-Host "   ? Pula aplikacji ju¿ istnieje: $appPoolName" -ForegroundColor Gray
}

# Skonfiguruj pulê aplikacji dla .NET Core
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "enable32BitAppOnWin64" -Value $false
Write-Host "   ? Skonfigurowano pulê aplikacji (No Managed Code)" -ForegroundColor Green

# Utwórz aplikacjê w IIS jeœli nie istnieje
$siteName = "Default Web Site"
$appName = "ProspeoWebhooks"
$appPath = "/$appName"

if (!(Test-Path "IIS:\Sites\$siteName\$appName")) {
    New-WebApplication -Name $appName -Site $siteName -PhysicalPath $OutputPath -ApplicationPool $appPoolName
    Write-Host "   ? Utworzono aplikacjê IIS: $appPath" -ForegroundColor Green
} else {
    Set-ItemProperty "IIS:\Sites\$siteName\$appName" -Name "physicalPath" -Value $OutputPath
    Set-ItemProperty "IIS:\Sites\$siteName\$appName" -Name "applicationPool" -Value $appPoolName
    Write-Host "   ? Zaktualizowano aplikacjê IIS: $appPath" -ForegroundColor Green
}

Write-Host ""
Write-Host "6. Uruchamianie aplikacji..." -ForegroundColor Yellow
Start-WebAppPool -Name $appPoolName
Start-Sleep -Seconds 2
Write-Host "   ? Uruchomiono pulê aplikacji" -ForegroundColor Green

Write-Host ""
Write-Host "=== Publikacja zakoñczona ===" -ForegroundColor Green
Write-Host ""
Write-Host "Aplikacja dostêpna pod adresem:" -ForegroundColor Cyan
Write-Host "  http://localhost/$appName" -ForegroundColor White
Write-Host "  http://localhost/$appName/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Informacje o wersji:" -ForegroundColor Cyan
Write-Host "  .NET Runtime: 10.0 (Framework-Dependent)" -ForegroundColor White
Write-Host "  Wymaga: .NET 10 Runtime zainstalowany na serwerze" -ForegroundColor White
Write-Host ""
Write-Host "Katalogi robocze:" -ForegroundColor Cyan
foreach ($dir in $workingDirs) {
    Write-Host "  - $OutputPath\$dir" -ForegroundColor White
}
Write-Host ""

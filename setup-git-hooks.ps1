# Instalator Git Hooks dla automatycznego CI/CD
# Ten skrypt konfiguruje Git hooks do automatycznego uruchamiania CI/CD pipeline
# Wersja: 1.0
# Data: 2024-12-12

Write-Host "=== Instalator Git Hooks CI/CD ===" -ForegroundColor Green
Write-Host ""

# Sprawdź czy jesteśmy w repozytorium Git
if (-not (Test-Path ".git")) {
    Write-Host "? Błąd: Nie jesteś w katalogu głównym repozytorium Git!" -ForegroundColor Red
    exit 1
}

# Ścieżka do katalogu hooks
$hooksDir = ".git\hooks"
if (-not (Test-Path $hooksDir)) {
    New-Item -Path $hooksDir -ItemType Directory -Force | Out-Null
}

# === PRE-COMMIT HOOK ===
Write-Host "1. Konfiguracja pre-commit hook..." -ForegroundColor Yellow

$preCommitContent = @'
#!/usr/bin/env pwsh
# Pre-commit hook - sprawdza build przed commitem

Write-Host "`n=== Pre-Commit Hook ===" -ForegroundColor Cyan

# Uruchom build
Write-Host "Sprawdzanie buildu..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed! Commit zablokowany." -ForegroundColor Red
    Write-Host "Napraw błędy i spróbuj ponownie." -ForegroundColor Yellow
    Write-Host "Aby pominąć ten hook, użyj: git commit --no-verify`n" -ForegroundColor Gray
    exit 1
}

Write-Host "? Build sukces" -ForegroundColor Green
Write-Host ""
exit 0
'@

$preCommitPath = Join-Path $hooksDir "pre-commit"
Set-Content -Path $preCommitPath -Value $preCommitContent -Encoding UTF8
Write-Host "   ? Utworzono pre-commit hook" -ForegroundColor Green

# === POST-COMMIT HOOK ===
Write-Host ""
Write-Host "2. Konfiguracja post-commit hook..." -ForegroundColor Yellow

$postCommitContent = @'
#!/usr/bin/env pwsh
# Post-commit hook - loguje commit i generuje metryki

$commitHash = git rev-parse --short HEAD
$commitMessage = git log -1 --pretty=%B
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

Write-Host "`n=== Post-Commit Hook ===" -ForegroundColor Cyan
Write-Host "Commit: $commitHash" -ForegroundColor Gray
Write-Host "Czas: $timestamp" -ForegroundColor Gray

# Zapisz do logu commits
$logDir = "build-logs"
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
}

$logEntry = @"
[$timestamp] Commit: $commitHash
Message: $commitMessage

"@

Add-Content -Path "$logDir\commits.log" -Value $logEntry

Write-Host "? Commit zarejestrowany`n" -ForegroundColor Green
'@

$postCommitPath = Join-Path $hooksDir "post-commit"
Set-Content -Path $postCommitPath -Value $postCommitContent -Encoding UTF8
Write-Host "   ? Utworzono post-commit hook" -ForegroundColor Green

# === PRE-PUSH HOOK ===
Write-Host ""
Write-Host "3. Konfiguracja pre-push hook..." -ForegroundColor Yellow

$prePushContent = @'
#!/usr/bin/env pwsh
# Pre-push hook - uruchamia testy przed pushem

Write-Host "`n=== Pre-Push Hook ===" -ForegroundColor Cyan

# Uruchom build
Write-Host "1. Sprawdzanie buildu..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ? Build failed!" -ForegroundColor Red
    Write-Host "`nPush zablokowany. Napraw błędy i spróbuj ponownie." -ForegroundColor Yellow
    Write-Host "Aby pominąć ten hook, użyj: git push --no-verify`n" -ForegroundColor Gray
    exit 1
}
Write-Host "   ? Build sukces" -ForegroundColor Green

# Uruchom testy
Write-Host "2. Uruchamianie testów..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ? Testy failed!" -ForegroundColor Red
    Write-Host "`nPush zablokowany. Napraw testy i spróbuj ponownie." -ForegroundColor Yellow
    Write-Host "Aby pominąć ten hook, użyj: git push --no-verify`n" -ForegroundColor Gray
    exit 1
}
Write-Host "   ? Testy sukces" -ForegroundColor Green

Write-Host "`n? Wszystkie sprawdzenia przeszły. Push dozwolony.`n" -ForegroundColor Green
exit 0
'@

$prePushPath = Join-Path $hooksDir "pre-push"
Set-Content -Path $prePushPath -Value $prePushContent -Encoding UTF8
Write-Host "   ? Utworzono pre-push hook" -ForegroundColor Green

# === POST-MERGE HOOK ===
Write-Host ""
Write-Host "4. Konfiguracja post-merge hook..." -ForegroundColor Yellow

$postMergeContent = @'
#!/usr/bin/env pwsh
# Post-merge hook - przywraca pakiety i odbudowuje projekt po merge

Write-Host "`n=== Post-Merge Hook ===" -ForegroundColor Cyan

Write-Host "Przywracanie pakietów..." -ForegroundColor Yellow
dotnet restore --verbosity quiet

Write-Host "Odbudowywanie projektu..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Projekt odbudowany po merge`n" -ForegroundColor Green
} else {
    Write-Host "? Sprawdź projekt - mogą wystąpić konflikty`n" -ForegroundColor Yellow
}
'@

$postMergePath = Join-Path $hooksDir "post-merge"
Set-Content -Path $postMergePath -Value $postMergeContent -Encoding UTF8
Write-Host "   ? Utworzono post-merge hook" -ForegroundColor Green

# === Utwórz katalog na logi ===
Write-Host ""
Write-Host "5. Tworzenie katalogu logów..." -ForegroundColor Yellow
$buildLogsDir = "build-logs"
if (-not (Test-Path $buildLogsDir)) {
    New-Item -Path $buildLogsDir -ItemType Directory -Force | Out-Null
    Write-Host "   ? Utworzono katalog: $buildLogsDir" -ForegroundColor Green
} else {
    Write-Host "   ? Katalog już istnieje: $buildLogsDir" -ForegroundColor Gray
}

# Dodaj .gitignore dla build-logs
$gitignorePath = Join-Path $buildLogsDir ".gitignore"
Set-Content -Path $gitignorePath -Value "*`n!.gitignore" -Encoding UTF8

# === Podsumowanie ===
Write-Host ""
Write-Host "=== Instalacja zakończona ===" -ForegroundColor Green
Write-Host ""
Write-Host "Zainstalowane Git Hooks:" -ForegroundColor Cyan
Write-Host "  ? pre-commit   - Sprawdza build przed commitem" -ForegroundColor White
Write-Host "  ? post-commit  - Loguje commity" -ForegroundColor White
Write-Host "  ? pre-push     - Uruchamia testy przed pushem" -ForegroundColor White
Write-Host "  ? post-merge   - Odbudowuje projekt po merge" -ForegroundColor White
Write-Host ""
Write-Host "Użycie:" -ForegroundColor Cyan
Write-Host "  - Hooki działają automatycznie przy git commit/push/merge" -ForegroundColor White
Write-Host "  - Aby pominąć hook: git commit --no-verify" -ForegroundColor White
Write-Host "  - Logi commitów: build-logs\commits.log" -ForegroundColor White
Write-Host ""
Write-Host "Dodatkowe skrypty:" -ForegroundColor Cyan
Write-Host "  - ci-cd-build-commit.ps1  - Manualny build & commit" -ForegroundColor White
Write-Host "  - ci-cd-watch.ps1         - Automatyczny watch mode" -ForegroundColor White
Write-Host ""

# CI/CD Pipeline - Auto-commit po ka¿dym buildzie
# Wersja: 1.0
# Data: 2024-12-12

param(
    [string]$Configuration = "Release",
    [string]$CommitMessage = "Auto-commit: Build successful",
    [switch]$SkipTests = $false,
    [switch]$SkipPush = $false,
    [string[]]$Remotes = @("prospeo", "origin")
)

Write-Host "=== CI/CD Pipeline - Auto Build & Commit ===" -ForegroundColor Green
Write-Host "Konfiguracja: $Configuration" -ForegroundColor Cyan
Write-Host "Skip Tests: $SkipTests" -ForegroundColor Cyan
Write-Host "Skip Push: $SkipPush" -ForegroundColor Cyan
Write-Host ""

# Funkcja do wyœwietlania b³êdów
function Write-Error-Message {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
}

# Funkcja do wyœwietlania sukcesu
function Write-Success-Message {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

# Funkcja do wyœwietlania ostrze¿eñ
function Write-Warning-Message {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Yellow
}

# 1. SprawdŸ czy jesteœmy w repozytorium Git
Write-Host "1. Sprawdzanie repozytorium Git..." -ForegroundColor Yellow
$gitStatus = git status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Message "Nie jesteœ w repozytorium Git!"
    exit 1
}
Write-Success-Message "Repozytorium Git zidentyfikowane"

# Pobierz bie¿¹c¹ ga³¹Ÿ
$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Host "   Bie¿¹ca ga³¹Ÿ: $currentBranch" -ForegroundColor Gray

# 2. SprawdŸ status zmian
Write-Host ""
Write-Host "2. Sprawdzanie zmian..." -ForegroundColor Yellow
$hasChanges = $false
$gitStatusOutput = git status --porcelain
if ($gitStatusOutput) {
    $hasChanges = $true
    Write-Host "   Wykryte zmiany:" -ForegroundColor Gray
    $gitStatusOutput | ForEach-Object { Write-Host "     $_" -ForegroundColor DarkGray }
} else {
    Write-Host "   Brak zmian do zacommitowania" -ForegroundColor Gray
}

# 3. Czyszczenie poprzednich buildów
Write-Host ""
Write-Host "3. Czyszczenie poprzednich buildów..." -ForegroundColor Yellow
dotnet clean --configuration $Configuration --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Success-Message "Projekt wyczyszczony"
} else {
    Write-Warning-Message "Nie uda³o siê wyczyœciæ projektu"
}

# 4. Przywracanie pakietów NuGet
Write-Host ""
Write-Host "4. Przywracanie pakietów NuGet..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Success-Message "Pakiety przywrócone"
} else {
    Write-Error-Message "B³¹d przywracania pakietów"
    exit 1
}

# 5. Build projektu
Write-Host ""
Write-Host "5. Kompilacja projektu..." -ForegroundColor Yellow
$buildStartTime = Get-Date
dotnet build --configuration $Configuration --no-restore --verbosity minimal
$buildEndTime = Get-Date
$buildDuration = $buildEndTime - $buildStartTime

if ($LASTEXITCODE -eq 0) {
    Write-Success-Message "Build zakoñczony sukcesem (czas: $($buildDuration.TotalSeconds.ToString('F2'))s)"
} else {
    Write-Error-Message "Build zakoñczony b³êdem!"
    Write-Host ""
    Write-Host "Aby zobaczyæ szczegó³y b³êdów, uruchom:" -ForegroundColor Yellow
    Write-Host "  dotnet build --configuration $Configuration --verbosity detailed" -ForegroundColor White
    exit 1
}

# 6. Uruchom testy (jeœli nie pominiête)
if (-not $SkipTests) {
    Write-Host ""
    Write-Host "6. Uruchamianie testów..." -ForegroundColor Yellow
    $testStartTime = Get-Date
    dotnet test --configuration $Configuration --no-build --verbosity quiet
    $testEndTime = Get-Date
    $testDuration = $testEndTime - $testStartTime
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success-Message "Testy zakoñczone sukcesem (czas: $($testDuration.TotalSeconds.ToString('F2'))s)"
    } else {
        Write-Error-Message "Testy zakoñczone b³êdem!"
        Write-Host ""
        Write-Host "Czy chcesz kontynuowaæ pomimo b³êdów testów? (y/n)" -ForegroundColor Yellow
        $response = Read-Host
        if ($response -ne 'y') {
            exit 1
        }
        Write-Warning-Message "Kontynuowanie pomimo b³êdów testów..."
    }
} else {
    Write-Host ""
    Write-Host "6. Testy pominiête (--SkipTests)" -ForegroundColor Gray
}

# 7. Generuj statystyki projektu
Write-Host ""
Write-Host "7. Generowanie statystyk..." -ForegroundColor Yellow
$projectFiles = (Get-ChildItem -Recurse -Include *.csproj).Count
$csFiles = (Get-ChildItem -Recurse -Include *.cs -Exclude obj,bin).Count
$linesOfCode = (Get-ChildItem -Recurse -Include *.cs -Exclude obj,bin | Get-Content | Measure-Object -Line).Lines

Write-Host "   Projekty: $projectFiles" -ForegroundColor Gray
Write-Host "   Pliki .cs: $csFiles" -ForegroundColor Gray
Write-Host "   Linie kodu: $linesOfCode" -ForegroundColor Gray

# 8. Tworzenie commit message z dodatkowymi informacjami
Write-Host ""
Write-Host "8. Przygotowanie commita..." -ForegroundColor Yellow

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$hostname = $env:COMPUTERNAME
$username = $env:USERNAME

$detailedCommitMessage = @"
$CommitMessage

Build Info:
- Configuration: $Configuration
- Build Time: $($buildDuration.TotalSeconds.ToString('F2'))s
- Date: $timestamp
- Machine: $hostname
- User: $username
- Projects: $projectFiles
- Files: $csFiles
- Lines of Code: $linesOfCode

[Auto-generated by CI/CD pipeline]
"@

# 9. Dodaj wszystkie zmiany do staging
if ($hasChanges) {
    Write-Host ""
    Write-Host "9. Dodawanie zmian do staging..." -ForegroundColor Yellow
    git add .
    if ($LASTEXITCODE -eq 0) {
        Write-Success-Message "Zmiany dodane do staging"
    } else {
        Write-Error-Message "B³¹d podczas dodawania zmian"
        exit 1
    }
    
    # 10. Commit zmian
    Write-Host ""
    Write-Host "10. Commitowanie zmian..." -ForegroundColor Yellow
    git commit -m $detailedCommitMessage
    if ($LASTEXITCODE -eq 0) {
        $commitHash = git rev-parse --short HEAD
        Write-Success-Message "Commit utworzony: $commitHash"
    } else {
        Write-Error-Message "B³¹d podczas commitowania"
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "9. Brak zmian do zacommitowania" -ForegroundColor Gray
}

# 11. Push do zdalnych repozytoriów (jeœli nie pominiête)
if (-not $SkipPush -and $hasChanges) {
    Write-Host ""
    Write-Host "11. Wysy³anie zmian do zdalnych repozytoriów..." -ForegroundColor Yellow
    
    foreach ($remote in $Remotes) {
        Write-Host "    Pushing do $remote..." -ForegroundColor Cyan
        git push $remote $currentBranch
        if ($LASTEXITCODE -eq 0) {
            Write-Success-Message "Zmiany wys³ane do $remote"
        } else {
            Write-Warning-Message "Nie uda³o siê wys³aæ do $remote"
        }
    }
} elseif ($SkipPush) {
    Write-Host ""
    Write-Host "11. Push pominiêty (--SkipPush)" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "11. Brak zmian do wypchniêcia" -ForegroundColor Gray
}

# 12. Podsumowanie
Write-Host ""
Write-Host "=== Pipeline zakoñczony ===" -ForegroundColor Green
Write-Host ""
Write-Host "Podsumowanie:" -ForegroundColor Cyan
Write-Host "  ? Build: Success ($($buildDuration.TotalSeconds.ToString('F2'))s)" -ForegroundColor White
if (-not $SkipTests) {
    Write-Host "  ? Tests: Success ($($testDuration.TotalSeconds.ToString('F2'))s)" -ForegroundColor White
}
if ($hasChanges) {
    Write-Host "  ? Commit: Created" -ForegroundColor White
    if (-not $SkipPush) {
        Write-Host "  ? Push: Completed" -ForegroundColor White
    }
}
Write-Host ""
Write-Host "Build artifacts:" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host "  Output: bin\$Configuration\" -ForegroundColor White
Write-Host ""

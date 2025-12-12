# CI/CD Pipeline Documentation

## Przegl¹d

System CI/CD dla projektu Prosepo.Webhooks zawiera zestaw skryptów PowerShell do automatyzacji procesów buildowania, testowania i wdra¿ania.

## Komponenty

### 1. Manual Build & Commit (`ci-cd-build-commit.ps1`)

G³ówny skrypt do manualnego uruchamiania pipeline'u.

#### Funkcje:
- ? Sprawdzanie statusu repozytorium Git
- ? Czyszczenie poprzednich buildów
- ? Przywracanie pakietów NuGet
- ? Kompilacja projektu
- ? Uruchamianie testów jednostkowych
- ? Generowanie statystyk projektu
- ? Automatyczne commitowanie z detalami buildu
- ? Pushowanie do wielu zdalnych repozytoriów

#### Parametry:

| Parametr | Typ | Domyœlna | Opis |
|----------|-----|----------|------|
| `Configuration` | string | "Release" | Konfiguracja buildu (Release/Debug) |
| `CommitMessage` | string | "Auto-commit..." | Treœæ commit message |
| `SkipTests` | switch | false | Pomiñ uruchamianie testów |
| `SkipPush` | switch | false | Pomiñ pushowanie do remotes |
| `Remotes` | string[] | @("prospeo", "origin") | Lista zdalnych repozytoriów |

#### Przyk³ady u¿ycia:

```powershell
# Podstawowy build & commit
.\ci-cd-build-commit.ps1

# Z niestandardowym message
.\ci-cd-build-commit.ps1 -CommitMessage "feat: Added new API endpoint"

# Bez testów (szybsze)
.\ci-cd-build-commit.ps1 -SkipTests

# Tylko build i commit (bez push)
.\ci-cd-build-commit.ps1 -SkipPush

# Debug configuration
.\ci-cd-build-commit.ps1 -Configuration Debug

# Tylko do jednego remote
.\ci-cd-build-commit.ps1 -Remotes @("origin")
```

#### Output:
```
=== CI/CD Pipeline - Auto Build & Commit ===
Konfiguracja: Release
Skip Tests: False
Skip Push: False

1. Sprawdzanie repozytorium Git...
? Repozytorium Git zidentyfikowane
   Bie¿¹ca ga³¹Ÿ: master

2. Sprawdzanie zmian...
   Wykryte zmiany:
     M Program.cs
     M Controllers/WebhookController.cs

3. Czyszczenie poprzednich buildów...
? Projekt wyczyszczony

4. Przywracanie pakietów NuGet...
? Pakiety przywrócone

5. Kompilacja projektu...
? Build zakoñczony sukcesem (czas: 3.45s)

6. Uruchamianie testów...
? Testy zakoñczone sukcesem (czas: 1.23s)

7. Generowanie statystyk...
   Projekty: 4
   Pliki .cs: 127
   Linie kodu: 8542

8. Przygotowanie commita...

9. Dodawanie zmian do staging...
? Zmiany dodane do staging

10. Commitowanie zmian...
? Commit utworzony: abc1234

11. Wysy³anie zmian do zdalnych repozytoriów...
    Pushing do prospeo...
? Zmiany wys³ane do prospeo
    Pushing do origin...
? Zmiany wys³ane do origin

=== Pipeline zakoñczony ===

Podsumowanie:
  ? Build: Success (3.45s)
  ? Tests: Success (1.23s)
  ? Commit: Created
  ? Push: Completed
```

---

### 2. Git Hooks Setup (`setup-git-hooks.ps1`)

Instalator Git hooks do automatycznego sprawdzania kodu.

#### Instalowane Hooki:

##### pre-commit
- Sprawdza czy build siê udaje przed commitem
- Blokuje commit jeœli build fails
- Mo¿na pomin¹æ: `git commit --no-verify`

##### post-commit
- Loguje wszystkie commity do `build-logs/commits.log`
- Zapisuje timestamp, hash i message
- Nie blokuje ¿adnych operacji

##### pre-push
- Uruchamia build i testy przed pushem
- Blokuje push jeœli testy failuj¹
- Mo¿na pomin¹æ: `git push --no-verify`

##### post-merge
- Automatycznie przywraca pakiety po merge
- Odbudowuje projekt
- Pomaga unikn¹æ problemów po merge

#### U¿ycie:

```powershell
# Instalacja (jednorazowo)
.\setup-git-hooks.ps1

# Deinstalacja
Remove-Item .git\hooks\* -Include pre-commit,post-commit,pre-push,post-merge

# Tymczasowe wy³¹czenie
git commit --no-verify  # Pomiñ pre-commit
git push --no-verify    # Pomiñ pre-push
```

#### Output:
```
=== Instalator Git Hooks CI/CD ===

1. Konfiguracja pre-commit hook...
   ? Utworzono pre-commit hook

2. Konfiguracja post-commit hook...
   ? Utworzono post-commit hook

3. Konfiguracja pre-push hook...
   ? Utworzono pre-push hook

4. Konfiguracja post-merge hook...
   ? Utworzono post-merge hook

5. Tworzenie katalogu logów...
   ? Utworzono katalog: build-logs

=== Instalacja zakoñczona ===

Zainstalowane Git Hooks:
  ? pre-commit   - Sprawdza build przed commitem
  ? post-commit  - Loguje commity
  ? pre-push     - Uruchamia testy przed pushem
  ? post-merge   - Odbudowuje projekt po merge
```

---

### 3. Watch Mode (`ci-cd-watch.ps1`)

Automatyczny watch mode który monitoruje zmiany i buduje projekt.

#### Funkcje:
- ?? Ci¹g³e monitorowanie zmian w projekcie
- ?? Automatyczne buildowanie przy wykryciu zmian
- ?? Opcjonalne auto-commitowanie
- ?? Opcjonalne auto-pushowanie
- ?? Real-time logging z timestampami
- ?? Konfigurowalne interwa³y sprawdzania

#### Parametry:

| Parametr | Typ | Domyœlna | Opis |
|----------|-----|----------|------|
| `IntervalSeconds` | int | 30 | Czas miêdzy sprawdzeniami (sekundy) |
| `Configuration` | string | "Release" | Konfiguracja buildu |
| `AutoCommit` | switch | false | Automatyczne commitowanie po udanym buildzie |
| `AutoPush` | switch | false | Automatyczne pushowanie (wymaga AutoCommit) |
| `WatchPaths` | string[] | @("*.cs", "*.csproj", "*.json") | Wzorce plików do monitorowania |
| `ExcludePaths` | string[] | @("bin", "obj", "node_modules") | Katalogi do pominiêcia |

#### Przyk³ady u¿ycia:

```powershell
# Podstawowy watch (tylko build)
.\ci-cd-watch.ps1

# Z auto-commitem
.\ci-cd-watch.ps1 -AutoCommit

# Z auto-commitem i pushem
.\ci-cd-watch.ps1 -AutoCommit -AutoPush

# Czêstsze sprawdzanie (co 15s)
.\ci-cd-watch.ps1 -IntervalSeconds 15

# Rzadsze sprawdzanie (co 2 min)
.\ci-cd-watch.ps1 -IntervalSeconds 120

# Debug configuration z auto-commitem
.\ci-cd-watch.ps1 -Configuration Debug -AutoCommit

# Monitoruj tylko pliki .cs
.\ci-cd-watch.ps1 -WatchPaths @("*.cs")
```

#### Output:
```
=== CI/CD Watch Mode ===
Interval: 30 seconds
Configuration: Release
Auto Commit: True
Auto Push: False

Press Ctrl+C to stop...

[10:15:30] Watching... (last build: 2.3 min ago)

=== Iteration 8 ===
[10:16:00] Changes detected:
  M Controllers/WebhookController.cs
  M Services/QueueService.cs
[10:16:00] Building...
[10:16:03] ? Build successful
[10:16:03] Committing changes...
[10:16:04] ? Committed: def5678

[10:16:30] Watching... (last build: 0.5 min ago)
[10:17:00] Watching... (last build: 1.0 min ago)
```

#### Zatrzymywanie:
- Naciœnij `Ctrl+C` aby zatrzymaæ
- Lub zamknij okno PowerShell

---

## Workflows

### Workflow 1: Development

```powershell
# 1. Setup (jednorazowo)
.\setup-git-hooks.ps1

# 2. Rozpocznij watch mode w osobnym terminalu
.\ci-cd-watch.ps1

# 3. Pracuj normalnie - zmiany s¹ automatycznie buildowane
# Edytuj pliki...

# 4. Manual commit gdy gotowe
git add .
git commit -m "feat: Add new feature"  # pre-commit sprawdzi build
git push                                # pre-push uruchomi testy
```

### Workflow 2: Feature Development z Auto-Commit

```powershell
# 1. Rozpocznij pracê z auto-commitem
.\ci-cd-watch.ps1 -AutoCommit

# 2. Pracuj normalnie
# Ka¿da zmiana -> auto build -> auto commit

# 3. Kontroluj commity przez --amend lub rebase
git commit --amend
git rebase -i HEAD~5
```

### Workflow 3: Quick Fix & Deploy

```powershell
# 1. WprowadŸ zmiany
# Edytuj pliki...

# 2. Build, test, commit i push jednym poleceniem
.\ci-cd-build-commit.ps1 -CommitMessage "fix: Critical bug fix"

# 3. Deploy do IIS
.\publish-to-iis.ps1
```

### Workflow 4: Release Preparation

```powershell
# 1. Zatrzymaj watch mode jeœli dzia³a

# 2. Final build z testami
.\ci-cd-build-commit.ps1 -CommitMessage "chore: Release v1.2.0"

# 3. Utwórz tag
git tag -a v1.2.0 -m "Release version 1.2.0"
git push origin v1.2.0
git push prospeo v1.2.0

# 4. Deploy produkcyjny
.\publish-to-iis.ps1 -Configuration Release
```

---

## Logi i Monitoring

### Struktura Logów

```
build-logs/
??? commits.log          # Historia wszystkich commitów
??? .gitignore          # Wykluczone z repo
```

### Format Logu Commitów

```
[2024-12-12 10:15:30] Commit: abc1234
Message: feat: Add new webhook endpoint

[2024-12-12 10:45:12] Commit: def5678
Message: fix: Fix database connection issue
```

### Przegl¹danie Logów

```powershell
# Ostatnie 10 commitów
Get-Content build-logs\commits.log -Tail 20

# Wszystkie commity z dzisiaj
Get-Content build-logs\commits.log | Select-String (Get-Date -Format "yyyy-MM-dd")

# Statystyki
(Get-Content build-logs\commits.log | Select-String "Commit:").Count
```

---

## Best Practices

### 1. U¿ywaj Git Hooks
? **DO**: Zainstaluj Git hooks od razu
```powershell
.\setup-git-hooks.ps1
```

? **DON'T**: Pomijaj hooki regularnie
```powershell
# Unikaj tego:
git commit --no-verify
```

### 2. Commit Messages

? **DO**: U¿ywaj Conventional Commits
```
feat: Add new API endpoint for products
fix: Resolve database connection timeout
docs: Update API documentation
chore: Update dependencies
```

? **DON'T**: Nieokreœlone messages
```
Update files
Fixed stuff
WIP
```

### 3. Watch Mode Usage

? **DO**: U¿ywaj podczas active development
```powershell
# W osobnym terminalu
.\ci-cd-watch.ps1
```

? **DON'T**: Pozostawiaj dzia³aj¹cy z auto-push
```powershell
# Unikaj:
.\ci-cd-watch.ps1 -AutoCommit -AutoPush
# Bo tworzy za du¿o commitów
```

### 4. Testing

? **DO**: Zawsze uruchamiaj testy przed pushem
```powershell
.\ci-cd-build-commit.ps1  # Domyœlnie z testami
```

? **DON'T**: Pomijaj testów w produkcji
```powershell
# Unikaj w prod:
.\ci-cd-build-commit.ps1 -SkipTests
```

### 5. Deployment

? **DO**: U¿ywaj Release configuration
```powershell
.\ci-cd-build-commit.ps1 -Configuration Release
.\publish-to-iis.ps1
```

? **DON'T**: Deployuj Debug builds
```powershell
# NIGDY:
.\publish-to-iis.ps1 -Configuration Debug
```

---

## Troubleshooting

### Problem: "Build failed" w watch mode

**Przyczyna**: B³êdy kompilacji

**Rozwi¹zanie**:
1. Watch mode poka¿e b³êdy
2. Napraw b³êdy w kodzie
3. Watch automatycznie spróbuje ponownie

### Problem: Za du¿o auto-commitów

**Przyczyna**: Watch mode z AutoCommit

**Rozwi¹zanie**:
```powershell
# Opcja 1: U¿yj bez AutoCommit
.\ci-cd-watch.ps1

# Opcja 2: Zwiêksz interval
.\ci-cd-watch.ps1 -AutoCommit -IntervalSeconds 120

# Opcja 3: Squash commitów póŸniej
git rebase -i HEAD~10
```

### Problem: Hook blokuje commit

**Przyczyna**: Build lub testy failuj¹

**Rozwi¹zanie**:
```powershell
# Opcja 1: Napraw b³êdy (zalecane)
# ... napraw kod ...
git commit -m "fix: Fixed issues"

# Opcja 2: Tymczasowo pomiñ (tylko development!)
git commit --no-verify -m "WIP: Work in progress"
```

### Problem: Nie mo¿na uruchomiæ skryptów

**Przyczyna**: Execution Policy

**Rozwi¹zanie**:
```powershell
# SprawdŸ policy
Get-ExecutionPolicy

# Ustaw dla current user
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

# Lub uruchom bezpoœrednio
powershell -ExecutionPolicy Bypass -File .\ci-cd-build-commit.ps1
```

### Problem: Watch nie wykrywa zmian

**Przyczyna**: Pliki w excluded paths

**Rozwi¹zanie**:
```powershell
# SprawdŸ WatchPaths i ExcludePaths
.\ci-cd-watch.ps1 -WatchPaths @("*.cs", "*.csproj") -ExcludePaths @("bin", "obj")
```

---

## Configuration

### Dostosowywanie Watch Paths

Edytuj `ci-cd-watch.ps1` aby zmieniæ domyœlne œcie¿ki:

```powershell
param(
    [string[]]$WatchPaths = @(
        "*.cs",           # C# source files
        "*.csproj",       # Project files
        "*.json",         # Config files
        "*.razor",        # Razor files (jeœli u¿ywasz)
        "*.cshtml"        # Views (jeœli u¿ywasz)
    ),
    [string[]]$ExcludePaths = @(
        "bin",
        "obj",
        "node_modules",
        ".vs",
        "wwwroot/lib"     # Vendor libraries
    )
)
```

### Dostosowywanie Commit Message

Edytuj `ci-cd-build-commit.ps1` w sekcji commit message:

```powershell
$detailedCommitMessage = @"
$CommitMessage

Build Info:
- Configuration: $Configuration
- Build Time: $($buildDuration.TotalSeconds.ToString('F2'))s
- Date: $timestamp
- Machine: $hostname
- Branch: $currentBranch

[Auto-generated by CI/CD]
"@
```

---

## Integration z IDE

### Visual Studio

1. Dodaj External Tools:
   - Tools ? External Tools ? Add
   - Title: "CI/CD Build"
   - Command: `powershell.exe`
   - Arguments: `-File "$(SolutionDir)ci-cd-build-commit.ps1"`

2. Przypisz skrót klawiszowy:
   - Tools ? Options ? Environment ? Keyboard
   - ZnajdŸ: "Tools.ExternalCommand1"
   - Assign: `Ctrl+Shift+B`

### VS Code

Dodaj do `.vscode/tasks.json`:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "CI/CD Build & Commit",
            "type": "shell",
            "command": "powershell",
            "args": [
                "-File",
                "${workspaceFolder}/ci-cd-build-commit.ps1"
            ],
            "group": {
                "kind": "build",
                "isDefault": true
            }
        },
        {
            "label": "CI/CD Watch Mode",
            "type": "shell",
            "command": "powershell",
            "args": [
                "-File",
                "${workspaceFolder}/ci-cd-watch.ps1"
            ],
            "isBackground": true
        }
    ]
}
```

---

## Support

Jeœli masz problemy:
1. SprawdŸ logi w `build-logs/`
2. Uruchom z verbose: `dotnet build --verbosity detailed`
3. SprawdŸ Git status: `git status`
4. SprawdŸ Git hooks: `ls .git/hooks/`

---

**Wersja:** 1.0  
**Data:** 2024-12-12  
**Autor:** Prospeo Development Team

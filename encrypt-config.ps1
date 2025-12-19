# PowerShell Helper Script dla narzêdzia szyfrowania
# U¿ycie:
#   .\encrypt-config.ps1 -GenerateKey
#   .\encrypt-config.ps1 -Encrypt "tekst" -Key "klucz"
#   .\encrypt-config.ps1 -Decrypt "zaszyfrowany" -Key "klucz"

param(
    [switch]$GenerateKey,
    [string]$Encrypt,
    [string]$Decrypt,
    [string]$Key,
    [switch]$Help
)

$ProjectPath = "Prosepo.Webhooks"

function Show-Help {
    Write-Host ""
    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host "?     ?? Helper do szyfrowania danych konfiguracyjnych      ?" -ForegroundColor Cyan
    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "U¿ycie:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Generowanie klucza:" -ForegroundColor Yellow
    Write-Host "     .\encrypt-config.ps1 -GenerateKey"
    Write-Host ""
    Write-Host "  2. Szyfrowanie tekstu:" -ForegroundColor Yellow
    Write-Host "     .\encrypt-config.ps1 -Encrypt `"moj_sekret`" -Key `"klucz_base64`""
    Write-Host ""
    Write-Host "  3. Odszyfrowywanie tekstu:" -ForegroundColor Yellow
    Write-Host "     .\encrypt-config.ps1 -Decrypt `"zaszyfrowany_tekst`" -Key `"klucz_base64`""
    Write-Host ""
    Write-Host "Przyk³ady:" -ForegroundColor Green
    Write-Host ""
    Write-Host "  # Wygeneruj klucz i zapisz do zmiennej"
    Write-Host "  `$key = .\encrypt-config.ps1 -GenerateKey"
    Write-Host ""
    Write-Host "  # Zaszyfruj has³o"
    Write-Host "  `$encrypted = .\encrypt-config.ps1 -Encrypt `"moje_haslo`" -Key `$key"
    Write-Host ""
    Write-Host "  # Odszyfruj has³o"
    Write-Host "  `$decrypted = .\encrypt-config.ps1 -Decrypt `$encrypted -Key `$key"
    Write-Host ""
}

function Invoke-EncryptionTool {
    param(
        [string[]]$Arguments
    )
    
    $fullArgs = @("run", "--project", $ProjectPath, "--") + @("encrypt-tool") + $Arguments
    
    $output = & dotnet $fullArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? B³¹d podczas wykonywania polecenia" -ForegroundColor Red
        Write-Host $output
        exit 1
    }
    
    return $output
}

# G³ówna logika
if ($Help) {
    Show-Help
    exit 0
}

if ($GenerateKey) {
    Write-Host "?? Generowanie klucza szyfrowania..." -ForegroundColor Cyan
    Write-Host ""
    $output = Invoke-EncryptionTool @("--generate-key")
    
    # Wyodrêbnij sam klucz (trzecia linia output)
    $lines = $output -split "`n"
    $keyLine = $lines | Where-Object { $_ -match "^[A-Za-z0-9+/=]+$" } | Select-Object -First 1
    
    Write-Host $output
    Write-Host ""
    Write-Host "?? Aby zapisaæ klucz w User Secrets:" -ForegroundColor Yellow
    Write-Host "   dotnet user-secrets set `"Encryption:Key`" `"$keyLine`" --project $ProjectPath"
    Write-Host ""
    
    # Zwróæ sam klucz dla u¿ycia w skryptach
    return $keyLine
}
elseif ($Encrypt) {
    if ([string]::IsNullOrEmpty($Key)) {
        Write-Host "? B³¹d: Musisz podaæ klucz szyfrowania (-Key)" -ForegroundColor Red
        Write-Host "   Wygeneruj nowy klucz u¿ywaj¹c: .\encrypt-config.ps1 -GenerateKey" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "?? Szyfrowanie tekstu..." -ForegroundColor Cyan
    Write-Host ""
    $output = Invoke-EncryptionTool @("--encrypt", $Encrypt, "--key", $Key)
    
    # Wyodrêbnij zaszyfrowany tekst (ostatnia niepusta linia)
    $lines = $output -split "`n" | Where-Object { $_ -match "\S" }
    $encryptedLine = $lines | Where-Object { $_ -match "^[A-Za-z0-9+/=]+$" } | Select-Object -Last 1
    
    Write-Host $output
    Write-Host ""
    
    # Zwróæ zaszyfrowany tekst
    return $encryptedLine
}
elseif ($Decrypt) {
    if ([string]::IsNullOrEmpty($Key)) {
        Write-Host "? B³¹d: Musisz podaæ klucz szyfrowania (-Key)" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "?? Odszyfrowywanie tekstu..." -ForegroundColor Cyan
    Write-Host ""
    $output = Invoke-EncryptionTool @("--decrypt", $Decrypt, "--key", $Key)
    
    # Wyodrêbnij odszyfrowany tekst (ostatnia niepusta linia)
    $lines = $output -split "`n" | Where-Object { $_ -match "\S" }
    $decryptedLine = $lines[-1]
    
    Write-Host $output
    Write-Host ""
    
    # Zwróæ odszyfrowany tekst
    return $decryptedLine
}
else {
    Show-Help
}

# Test script to verify Microsoft.Data.SqlClient is working
Write-Host "Testing Microsoft.Data.SqlClient resolution..." -ForegroundColor Green

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Start the application in the background
Write-Host "Starting webhook service..." -ForegroundColor Yellow
cd "Prosepo.Webhooks"
Start-Process -FilePath "dotnet" -ArgumentList "run" -WindowStyle Hidden -PassThru

# Wait for the service to start
Start-Sleep -Seconds 10

try {
    # Test the database connection endpoint
    Write-Host "Testing database connection..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/webhook/test/database" -Method Get
    
    if ($response.Success) {
        Write-Host "Database connection test PASSED!" -ForegroundColor Green
        Write-Host "SQL Client Version: $($response.SqlClientVersion)" -ForegroundColor Cyan
        Write-Host "Database: $($response.Database)" -ForegroundColor Cyan
        Write-Host "Server Version: $($response.ServerVersion)" -ForegroundColor Cyan
    } else {
        Write-Host "Database connection test FAILED!" -ForegroundColor Red
        Write-Host "Error: $($response.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "Error testing database connection: $_" -ForegroundColor Red
} finally {
    # Stop any running dotnet processes
    Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -eq "" } | Stop-Process -Force
    cd ..
}

Write-Host "Test completed." -ForegroundColor Green
# run\api-desktop.ps1
# Launch both Backend API and Desktop in Live mode

Write-Host "Starting KeyCard Backend + Desktop (Live Mode)..." -ForegroundColor Cyan

# Set environment variables for the Desktop app
$env:DOTNET_ENVIRONMENT = "Production"
$env:KeyCard__UseMocks = "false"
$env:KeyCard__Mode = "Live"

Write-Host "Environment: $env:DOTNET_ENVIRONMENT" -ForegroundColor Yellow
Write-Host "KeyCard__UseMocks: $env:KeyCard__UseMocks" -ForegroundColor Yellow
Write-Host "KeyCard__Mode: $env:KeyCard__Mode" -ForegroundColor Yellow

# Start the Backend API
Write-Host "`nStarting Backend API..." -ForegroundColor Green
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src\Backend\KeyCard.Api\KeyCard.Api.csproj" `
    -PassThru `
    -NoNewWindow

# Wait a moment for API to start
Start-Sleep -Seconds 2

# Start the Desktop app with environment variables
Write-Host "Starting Desktop App (Live Mode)..." -ForegroundColor Green
$desktopProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src\Desktop\KeyCard.Desktop\KeyCard.Desktop.csproj" `
    -PassThru `
    -NoNewWindow

Write-Host "`nBoth processes started. Press Ctrl+C to stop." -ForegroundColor Cyan
Write-Host "API Process ID: $($apiProcess.Id)" -ForegroundColor Gray
Write-Host "Desktop Process ID: $($desktopProcess.Id)" -ForegroundColor Gray

# Wait for both processes
try {
    Wait-Process -Id $apiProcess.Id, $desktopProcess.Id
}
catch {
    Write-Host "`nProcesses terminated." -ForegroundColor Yellow
}
finally {
    # Cleanup
    if (-not $apiProcess.HasExited) {
        Write-Host "Stopping API..." -ForegroundColor Red
        Stop-Process -Id $apiProcess.Id -Force
    }
    if (-not $desktopProcess.HasExited) {
        Write-Host "Stopping Desktop..." -ForegroundColor Red
        Stop-Process -Id $desktopProcess.Id -Force
    }
}

Write-Host "`nShutdown complete." -ForegroundColor Cyan

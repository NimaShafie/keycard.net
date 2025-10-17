$ErrorActionPreference = "Stop"

# Start API in a new window
Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:ASPNETCORE_URLS http://localhost:8080; dotnet run --project "src/Backend/KeyCard.Api/KeyCard.Api.csproj"'

# Give API a moment to boot
Start-Sleep -Seconds 3

# Start Desktop in a new window
Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:KeyCard__Api__HttpBaseUrl http://localhost:8080; dotnet run --project "src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj"'

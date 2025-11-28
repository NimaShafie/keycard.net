$ErrorActionPreference = "Stop"

Start-Process powershell -ArgumentList '-NoExit','-Command', '$env:ASPNETCORE_URLS="http://localhost:8080"; dotnet run --project "src/Backend/KeyCard.Api/KeyCard.Api.csproj"'
Start-Sleep -Seconds 3
Start-Process powershell -ArgumentList '-NoExit','-Command', '$env:DOTNET_ENVIRONMENT="Production"; $env:KEYCARD_MODE="Live"; dotnet run --project "src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj"'
Start-Sleep -Seconds 1
Start-Process powershell -ArgumentList '-NoExit','-Command', '$env:ASPNETCORE_URLS="http://localhost:8081"; dotnet run --project "src/Web/KeyCard.Web/KeyCard.Web.csproj"'

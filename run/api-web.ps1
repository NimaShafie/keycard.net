$ErrorActionPreference = "Stop"

Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:ASPNETCORE_URLS http://localhost:8080; dotnet run --project "src/Backend/KeyCard.Api/KeyCard.Api.csproj"'
Start-Sleep -Seconds 3
Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:ASPNETCORE_URLS http://localhost:8081; dotnet run --project "src/Web/KeyCard.Web/KeyCard.Web.csproj"'

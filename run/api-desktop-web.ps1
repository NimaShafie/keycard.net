$ErrorActionPreference = "Stop"

Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:ASPNETCORE_URLS http://localhost:8080; dotnet run --project "src/Backend/KeyCard.Api/KeyCard.Api.csproj"'
Start-Sleep -Seconds 3
Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:KeyCard__Api__HttpBaseUrl http://localhost:8080; dotnet run --project "src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj"'
Start-Sleep -Seconds 1
Start-Process pwsh -ArgumentList '-NoExit','-Command', 'Set-Item Env:ASPNETCORE_URLS http://localhost:8081; dotnet run --project "src/Web/KeyCard.Web/KeyCard.Web.csproj"'

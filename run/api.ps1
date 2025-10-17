$ErrorActionPreference = "Stop"
$env:ASPNETCORE_URLS = "http://localhost:8080"
dotnet run --project "src/Backend/KeyCard.Api/KeyCard.Api.csproj"

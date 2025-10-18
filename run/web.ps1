$ErrorActionPreference = "Stop"
$env:ASPNETCORE_URLS = "http://localhost:8081"
dotnet run --project "src/Web/KeyCard.Web/KeyCard.Web.csproj"

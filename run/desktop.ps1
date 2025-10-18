$ErrorActionPreference = "Stop"
$env:KeyCard__Api__HttpBaseUrl = "http://localhost:8080"
dotnet run --project "src/Desktop/KeyCard.Desktop/KeyCard.Desktop.csproj"

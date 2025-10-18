Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.Path -match "KeyCard\.(Api|Desktop|Web)" } | Stop-Process -Force

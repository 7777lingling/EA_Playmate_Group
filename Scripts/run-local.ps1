$ErrorActionPreference = "Stop"

$env:ASPNETCORE_ENVIRONMENT = "Development"
Set-Location "E:\ling\EA_Playmate_Group"

dotnet run --configuration Release --no-build

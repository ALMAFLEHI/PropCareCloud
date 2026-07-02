$ErrorActionPreference = "Stop"

Write-Host "PropCare Cloud Optional Local PostgreSQL Database Update"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script is for local PostgreSQL only. It does not configure or update Amazon RDS."

if ([string]::IsNullOrWhiteSpace($env:PROPCLOUD_CONNECTION_STRING)) {
    Write-Host "PROPCLOUD_CONNECTION_STRING is not set."
    Write-Host "Set a safe local PostgreSQL connection string first. Example:"
    Write-Host '$env:PROPCLOUD_CONNECTION_STRING="Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>"'
    Write-Host "No database update was run."
    exit 1
}

Write-Host "Connection string detected. The value will not be printed."
Write-Host "Restoring local EF tooling..."
dotnet tool restore
Write-Host "Applying EF Core migrations to the configured local PostgreSQL database..."
dotnet tool run dotnet-ef database update --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj --startup-project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj

$ErrorActionPreference = "Continue"

function Write-Result {
    param(
        [string]$Status,
        [string]$Name,
        [string]$Detail = ""
    )

    if ([string]::IsNullOrWhiteSpace($Detail)) {
        Write-Host ("{0,-7} {1}" -f $Status, $Name)
    }
    else {
        Write-Host ("{0,-7} {1}: {2}" -f $Status, $Name, $Detail)
    }
}

Write-Host "PropCare Cloud Local PostgreSQL Setup Guide"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$psqlCommand = Get-Command psql -ErrorAction SilentlyContinue
if ($null -eq $psqlCommand) {
    Write-Result "WARN" "PostgreSQL psql" "psql was not found on PATH"
    Write-Host ""
    Write-Host "Install PostgreSQL manually before running local database updates."
    Write-Host "Recommended local database name: propcarecloud_db"
    Write-Host "After installation, create the database manually using a local PostgreSQL admin tool or psql."
}
else {
    $psqlVersion = & psql --version 2>&1
    Write-Result "PASS" "PostgreSQL psql" $psqlVersion
}

Write-Host ""
if ([string]::IsNullOrWhiteSpace($env:PROPCLOUD_CONNECTION_STRING)) {
    Write-Result "WARN" "PROPCLOUD_CONNECTION_STRING" "Not set"
    Write-Host "Use PROPCLOUD_CONNECTION_STRING only for local EF migration update scripts."
    Write-Host 'Example: $env:PROPCLOUD_CONNECTION_STRING="Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>"'
}
else {
    Write-Result "PASS" "PROPCLOUD_CONNECTION_STRING" "Set; value hidden"
}

Write-Host ""
Write-Host "For the running API, store the app connection string with user-secrets or an environment variable."
Write-Host "User-secrets example:"
Write-Host 'dotnet user-secrets init --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj'
Write-Host 'dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>" --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj'
Write-Host ""
Write-Host "This guide script does not create databases automatically and never prints secret values."

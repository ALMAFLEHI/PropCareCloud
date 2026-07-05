$ErrorActionPreference = "Continue"

function Write-Result {
    param(
        [string]$Status,
        [string]$Name,
        [string]$Detail = ""
    )

    if ([string]::IsNullOrWhiteSpace($Detail)) {
        Write-Host ("{0,-6} {1}" -f $Status, $Name)
    }
    else {
        Write-Host ("{0,-6} {1}: {2}" -f $Status, $Name, $Detail)
    }
}

function Invoke-ChecklistScript {
    param(
        [string]$Name,
        [string]$Path
    )

    Write-Host ""
    Write-Host "== $Name =="
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Result "FAIL" $Name "$Path was not found"
        return $false
    }

    & powershell -ExecutionPolicy Bypass -File $Path
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Write-Result "PASS" $Name
        return $true
    }

    Write-Result "FAIL" $Name "Exit code $exitCode"
    return $false
}

Write-Host "PropCare Cloud Sprint 12 RDS Checklist"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script validates support steps only. It does not create or delete AWS resources."

$awsPassed = Invoke-ChecklistScript "AWS CLI check" ".\scripts\aws\check-aws-cli.ps1"
$environmentPassed = Invoke-ChecklistScript "RDS environment validation" ".\scripts\aws\validate-rds-environment.ps1"

$migrationPassed = $false
if ($environmentPassed) {
    $migrationPassed = Invoke-ChecklistScript "RDS migration update" ".\scripts\aws\update-rds-database.ps1"
}
else {
    Write-Result "WARN" "RDS migration update" "Skipped because RDS environment validation did not pass."
}

Write-Host ""
Write-Host "== Backend Reminder =="
Write-Host "After migration, start the backend from the same PowerShell session:"
Write-Host "dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj"

$backendRunning = $false
try {
    $null = Invoke-RestMethod -Method Get -Uri "http://localhost:5015/api/database/status" -TimeoutSec 3
    $backendRunning = $true
}
catch {
    $backendRunning = $false
}

$readinessPassed = $false
if ($backendRunning) {
    $readinessPassed = Invoke-ChecklistScript "RDS API readiness" ".\scripts\aws\check-rds-api-readiness.ps1"
}
else {
    Write-Result "WARN" "RDS API readiness" "Backend is not running on http://localhost:5015. Start it and run check-rds-api-readiness.ps1."
}

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- AWS CLI: {0}" -f $(if ($awsPassed) { "PASS" } else { "CHECK" }))
Write-Host ("- RDS environment: {0}" -f $(if ($environmentPassed) { "PASS" } else { "CHECK" }))
Write-Host ("- RDS migration: {0}" -f $(if ($migrationPassed) { "PASS" } else { "CHECK" }))
Write-Host ("- RDS API readiness: {0}" -f $(if ($readinessPassed) { "PASS" } else { "CHECK" }))

if ($environmentPassed -and $migrationPassed) {
    Write-Result "PASS" "Sprint 12 RDS support checklist" "Migration support completed. Capture manual AWS/RDS evidence next."
    exit 0
}

Write-Result "WARN" "Sprint 12 RDS support checklist" "Manual AWS RDS setup or local backend startup is still needed."
exit 1

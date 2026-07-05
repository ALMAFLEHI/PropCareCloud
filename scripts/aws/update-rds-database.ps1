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

function Invoke-Step {
    param(
        [string]$Name,
        [string[]]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Command[0] $Command[1..($Command.Length - 1)]
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Result "PASS" $Name
        return $true
    }

    Write-Result "FAIL" $Name "Exit code $exitCode"
    return $false
}

Write-Host "PropCare Cloud RDS Database Migration"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path
Write-Host "The RDS connection string value will not be printed."

$validationPassed = Invoke-Step "Validate RDS environment" @(
    "powershell",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    ".\scripts\aws\validate-rds-environment.ps1"
)

if (-not $validationPassed) {
    exit 1
}

$projectPath = ".\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj"
if (-not (Test-Path -LiteralPath $projectPath)) {
    Write-Result "FAIL" "API project" "$projectPath was not found"
    exit 1
}

$toolRestorePassed = Invoke-Step "dotnet tool restore" @("dotnet", "tool", "restore")
if (-not $toolRestorePassed) {
    exit 1
}

$migrationPassed = Invoke-Step "EF Core database update" @(
    "dotnet",
    "tool",
    "run",
    "dotnet-ef",
    "database",
    "update",
    "--project",
    $projectPath,
    "--startup-project",
    $projectPath
)

if ($migrationPassed) {
    Write-Result "PASS" "RDS migration" "EF Core migrations were applied to the configured RDS database."
    exit 0
}

Write-Result "FAIL" "RDS migration" "EF Core migration update failed."
exit 1

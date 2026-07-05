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

Write-Host "PropCare Cloud Sprint 13 Backend Deployment Package"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path
Write-Host "This script builds a local deployment package only. It does not deploy to AWS."

$projectPath = ".\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj"
$packageRoot = ".\artifacts\deployment\backend"
$publishPath = Join-Path $packageRoot "publish"
$zipPath = Join-Path $packageRoot "PropCareCloud.Api.zip"

if (-not (Test-Path -LiteralPath $projectPath)) {
    Write-Result "FAIL" "API project" "$projectPath was not found"
    exit 1
}

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishPath | Out-Null

$restorePassed = Invoke-Step "dotnet restore" @("dotnet", "restore", $projectPath)
if (-not $restorePassed) {
    exit 1
}

$publishPassed = Invoke-Step "dotnet publish Release" @(
    "dotnet",
    "publish",
    $projectPath,
    "-c",
    "Release",
    "-o",
    $publishPath,
    "--no-restore"
)

if (-not $publishPassed) {
    exit 1
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipPath -Force
Write-Result "PASS" "Backend publish output" (Resolve-Path -LiteralPath $publishPath).Path
Write-Result "PASS" "Backend zip package" (Resolve-Path -LiteralPath $zipPath).Path
Write-Host "Configure deployment secrets in AWS environment variables only. Do not place secrets in the package."
exit 0

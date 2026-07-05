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
        [string[]]$Command,
        [string]$WorkingDirectory
    )

    Write-Host ""
    Write-Host "== $Name =="
    Push-Location $WorkingDirectory
    try {
        & $Command[0] $Command[1..($Command.Length - 1)]
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($exitCode -eq 0) {
        Write-Result "PASS" $Name
        return $true
    }

    Write-Result "FAIL" $Name "Exit code $exitCode"
    return $false
}

Write-Host "PropCare Cloud Sprint 13 Frontend Deployment Package"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path
Write-Host "This script builds a local frontend bundle only. It does not upload to S3."

$frontendPath = ".\frontend"
$packageJsonPath = Join-Path $frontendPath "package.json"
$distPath = Join-Path $frontendPath "dist"

if (-not (Test-Path -LiteralPath $packageJsonPath)) {
    Write-Result "FAIL" "Frontend package" "$packageJsonPath was not found"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($env:VITE_API_BASE_URL)) {
    Write-Result "WARN" "VITE_API_BASE_URL" "Not set. The build will use the app fallback; set it before final production build."
}
else {
    Write-Result "PASS" "VITE_API_BASE_URL" "Configured. Value not printed."
}

$installPassed = Invoke-Step "Frontend dependency install" @("npm", "install") $frontendPath
if (-not $installPassed) {
    exit 1
}

$buildPassed = Invoke-Step "Frontend production build" @("npm", "run", "build") $frontendPath
if (-not $buildPassed) {
    exit 1
}

if (Test-Path -LiteralPath $distPath) {
    Write-Result "PASS" "Frontend dist output" (Resolve-Path -LiteralPath $distPath).Path
    Write-Host "Upload the contents of frontend/dist to the chosen AWS frontend hosting target during manual deployment."
    exit 0
}

Write-Result "FAIL" "Frontend dist output" "dist folder was not created"
exit 1

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

function Get-NpmCommand {
    $npmCmd = "C:\Program Files\nodejs\npm.cmd"
    if (Test-Path -LiteralPath $npmCmd) {
        return $npmCmd
    }

    $npm = Get-Command npm -ErrorAction SilentlyContinue
    if ($null -ne $npm) {
        return $npm.Source
    }

    return $null
}

function Invoke-NpmCheck {
    param(
        [string]$Name,
        [string[]]$Arguments,
        [string]$NpmCommand
    )

    Write-Host ""
    Write-Host "== $Name =="
    $output = & $NpmCommand @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    foreach ($line in $output) {
        Write-Host $line
    }

    if ($exitCode -eq 0) {
        Write-Result "PASS" $Name
        return ,$true
    }

    Write-Result "FAIL" $Name "Exit code $exitCode"
    return ,$false
}

Write-Host "PropCare Cloud Frontend Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$frontendPath = ".\frontend"
$packageJsonPath = Join-Path $frontendPath "package.json"
if (-not (Test-Path -LiteralPath $packageJsonPath)) {
    Write-Result "FAIL" "Frontend package" "$packageJsonPath was not found"
    exit 1
}

$npmCommand = Get-NpmCommand
if ([string]::IsNullOrWhiteSpace($npmCommand)) {
    Write-Result "FAIL" "npm" "npm command was not found"
    exit 1
}

Write-Result "PASS" "Frontend package" $packageJsonPath
Write-Result "INFO" "npm command" $npmCommand

Push-Location $frontendPath
try {
    $installPassed = Invoke-NpmCheck "npm install" @("install") $npmCommand
    $buildPassed = Invoke-NpmCheck "npm run build" @("run", "build") $npmCommand
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- Install: {0}" -f $(if ($installPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Build: {0}" -f $(if ($buildPassed) { "PASS" } else { "FAIL" }))

if ($installPassed -and $buildPassed) {
    Write-Result "PASS" "Frontend validation" "npm install and npm run build completed successfully"
    exit 0
}

Write-Result "FAIL" "Frontend validation" "One or more checks failed"
exit 1

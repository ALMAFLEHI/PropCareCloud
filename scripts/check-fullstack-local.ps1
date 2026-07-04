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

function Invoke-Validation {
    param(
        [string]$Name,
        [string[]]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    $output = & $Command[0] $Command[1..($Command.Length - 1)] 2>&1
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

Write-Host "PropCare Cloud Full-Stack Local Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$frontendScript = ".\scripts\check-frontend.ps1"
$backendScript = ".\scripts\check-backend.ps1"

if (-not (Test-Path -LiteralPath $frontendScript)) {
    Write-Result "FAIL" "Frontend validation script" "$frontendScript was not found"
    exit 1
}

if (-not (Test-Path -LiteralPath $backendScript)) {
    Write-Result "FAIL" "Backend validation script" "$backendScript was not found"
    exit 1
}

Write-Result "PASS" "Frontend validation script" $frontendScript
Write-Result "PASS" "Backend validation script" $backendScript

$frontendPassed = Invoke-Validation "Frontend validation" @(
    "powershell",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $frontendScript
)

$backendPassed = Invoke-Validation "Backend validation" @(
    "powershell",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $backendScript
)

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- Frontend: {0}" -f $(if ($frontendPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Backend: {0}" -f $(if ($backendPassed) { "PASS" } else { "FAIL" }))

if ($frontendPassed -and $backendPassed) {
    Write-Result "PASS" "Full-stack local validation" "Frontend install/build and backend restore/build/test completed successfully"
    exit 0
}

Write-Result "FAIL" "Full-stack local validation" "One or more checks failed"
exit 1

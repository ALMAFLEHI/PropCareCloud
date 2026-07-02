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

function Invoke-Check {
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

Write-Host "PropCare Cloud Backend Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$solutionPath = ".\backend\PropCareCloud.sln"
if (-not (Test-Path -LiteralPath $solutionPath)) {
    Write-Result "FAIL" "Solution file" "$solutionPath was not found"
    exit 1
}

Write-Result "PASS" "Solution file" $solutionPath

$restorePassed = Invoke-Check "dotnet restore" @("dotnet", "restore", $solutionPath)
$buildPassed = Invoke-Check "dotnet build" @("dotnet", "build", $solutionPath)
$testPassed = Invoke-Check "dotnet test" @("dotnet", "test", $solutionPath)

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- Restore: {0}" -f $(if ($restorePassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Build: {0}" -f $(if ($buildPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Test: {0}" -f $(if ($testPassed) { "PASS" } else { "FAIL" }))

if ($restorePassed -and $buildPassed -and $testPassed) {
    Write-Result "PASS" "Backend validation" "Restore, build, and test completed successfully"
    exit 0
}

Write-Result "FAIL" "Backend validation" "One or more checks failed"
exit 1

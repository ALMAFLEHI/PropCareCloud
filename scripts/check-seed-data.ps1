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

Write-Host "PropCare Cloud Seed Data Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$solutionPath = ".\backend\PropCareCloud.sln"
$migrationPath = ".\backend\src\PropCareCloud.Api\Data\Migrations"
$requiredFiles = @(
    ".\backend\src\PropCareCloud.Api\Services\SeedDataService.cs",
    ".\backend\src\PropCareCloud.Api\Controllers\SeedDataController.cs",
    ".\backend\src\PropCareCloud.Api\Services\DatabaseReadinessService.cs",
    ".\backend\src\PropCareCloud.Api\Controllers\DatabaseReadinessController.cs",
    ".\backend\src\PropCareCloud.Api\Models\SeedDataResult.cs"
)

if (-not (Test-Path -LiteralPath $solutionPath)) {
    Write-Result "FAIL" "Solution file" "$solutionPath was not found"
    exit 1
}
Write-Result "PASS" "Solution file" $solutionPath

$migrationFilesPassed = (Test-Path -LiteralPath $migrationPath) -and
    (@(Get-ChildItem -LiteralPath $migrationPath -Filter "*.cs" -File).Count -gt 0)
if ($migrationFilesPassed) {
    Write-Result "PASS" "Migration files" $migrationPath
}
else {
    Write-Result "FAIL" "Migration files" "No migration files found under $migrationPath"
}

$requiredFilesPassed = $true
foreach ($file in $requiredFiles) {
    if (Test-Path -LiteralPath $file) {
        Write-Result "PASS" "Required file" $file
    }
    else {
        Write-Result "FAIL" "Required file" "$file was not found"
        $requiredFilesPassed = $false
    }
}

$restorePassed = Invoke-Check "dotnet restore" @("dotnet", "restore", $solutionPath)
$buildPassed = Invoke-Check "dotnet build" @("dotnet", "build", $solutionPath)
$testPassed = Invoke-Check "dotnet test" @("dotnet", "test", $solutionPath)

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- Migration files: {0}" -f $(if ($migrationFilesPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Seed/readiness files: {0}" -f $(if ($requiredFilesPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Restore: {0}" -f $(if ($restorePassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Build: {0}" -f $(if ($buildPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Test: {0}" -f $(if ($testPassed) { "PASS" } else { "FAIL" }))

if ($migrationFilesPassed -and $requiredFilesPassed -and $restorePassed -and $buildPassed -and $testPassed) {
    Write-Result "PASS" "Seed data validation" "Code, migrations, restore, build, and test completed successfully"
    exit 0
}

Write-Result "FAIL" "Seed data validation" "One or more checks failed"
exit 1

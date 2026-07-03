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

Write-Host "PropCare Cloud CRUD API Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$solutionPath = ".\backend\PropCareCloud.sln"
$requiredFiles = @(
    ".\backend\src\PropCareCloud.Api\Controllers\PropertiesController.cs",
    ".\backend\src\PropCareCloud.Api\Controllers\MaintenanceRequestsController.cs",
    ".\backend\src\PropCareCloud.Api\Services\PropertyService.cs",
    ".\backend\src\PropCareCloud.Api\Services\MaintenanceRequestService.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\PropertyCreateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\PropertyUpdateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\PropertyResponse.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\RentalUnitCreateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\RentalUnitUpdateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\Properties\RentalUnitResponse.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestCreateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestUpdateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestResponse.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestAssignRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestStatusUpdateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestCommentCreateRequest.cs",
    ".\backend\src\PropCareCloud.Api\DTOs\MaintenanceRequests\MaintenanceRequestCommentResponse.cs"
)

if (-not (Test-Path -LiteralPath $solutionPath)) {
    Write-Result "FAIL" "Solution file" "$solutionPath was not found"
    exit 1
}
Write-Result "PASS" "Solution file" $solutionPath

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
Write-Host ("- Required CRUD files: {0}" -f $(if ($requiredFilesPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Restore: {0}" -f $(if ($restorePassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Build: {0}" -f $(if ($buildPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Test: {0}" -f $(if ($testPassed) { "PASS" } else { "FAIL" }))

if ($requiredFilesPassed -and $restorePassed -and $buildPassed -and $testPassed) {
    Write-Result "PASS" "CRUD API validation" "Required files, restore, build, and test completed successfully"
    exit 0
}

Write-Result "FAIL" "CRUD API validation" "One or more checks failed"
exit 1

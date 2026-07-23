param(
    [string]$Profile = "",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint18",
    [switch]$ConfirmRemoval
)

$ErrorActionPreference = "Stop"

if ($StackName -ne "propcarecloud-task2-sprint18") {
    throw "Rollback is restricted to the Sprint 18 stack."
}

if (-not $ConfirmRemoval) {
    Write-Host "No resources were removed."
    Write-Host "Re-run with -ConfirmRemoval only for a deliberate Sprint 18 rollback."
    exit 0
}

$arguments = @(
    "cloudformation", "delete-stack",
    "--region", $Region,
    "--stack-name", $StackName
)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $arguments += @("--profile", $Profile)
}
& aws @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 18 stack deletion request failed."
}

$waitArguments = @(
    "cloudformation", "wait", "stack-delete-complete",
    "--region", $Region,
    "--stack-name", $StackName
)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $waitArguments += @("--profile", $Profile)
}
& aws @waitArguments
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 18 stack deletion did not complete successfully."
}

Write-Host "PASS   Sprint 18 stack removed. Task 1 and Sprint 17 were not targeted."

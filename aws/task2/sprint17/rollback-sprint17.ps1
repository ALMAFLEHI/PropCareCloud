param(
    [string]$Profile = "",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint17",
    [switch]$ConfirmRemoval
)

$ErrorActionPreference = "Stop"

if ($StackName -ne "propcarecloud-task2-sprint17") {
    throw "Rollback is restricted to the Sprint 17 stack."
}

if (-not $ConfirmRemoval) {
    Write-Host "No resources were removed."
    Write-Host "Re-run with -ConfirmRemoval only after preserving required attachment objects."
    exit 0
}

Write-Host "Deleting only the Sprint 17 CloudFormation stack."
Write-Host "The retained private attachment bucket may require separate, deliberate cleanup."
$deleteArguments = @(
    "cloudformation", "delete-stack",
    "--region", $Region,
    "--stack-name", $StackName
)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $deleteArguments += @("--profile", $Profile)
}
& aws @deleteArguments
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 17 stack deletion request failed."
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
    throw "Sprint 17 stack deletion did not complete successfully."
}
Write-Host "PASS   Sprint 17 stack removed. Task 1 resources were not targeted."

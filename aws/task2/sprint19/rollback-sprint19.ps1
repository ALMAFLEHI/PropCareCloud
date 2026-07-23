param(
    [string]$Profile = "fresh-propcare-task2-deployer",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint19",
    [switch]$ConfirmRemoval
)

$ErrorActionPreference = "Stop"

if ($Profile -ne "fresh-propcare-task2-deployer" -or $Region -ne "us-east-1") {
    throw "Sprint 19 rollback requires the approved AWS profile and region."
}
if ($StackName -ne "propcarecloud-task2-sprint19") {
    throw "Rollback is restricted to the Sprint 19 monitoring stack."
}
if (-not $ConfirmRemoval) {
    Write-Host "No resources were removed."
    Write-Host "Re-run with -ConfirmRemoval only for a deliberate Sprint 19 rollback."
    exit 0
}

& aws cloudformation delete-stack `
    --stack-name $StackName `
    --profile $Profile --region $Region
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 19 stack deletion request failed."
}
& aws cloudformation wait stack-delete-complete `
    --stack-name $StackName `
    --profile $Profile --region $Region
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 19 stack deletion did not complete."
}

Write-Host "PASS   Sprint 19 dashboard and alarms removed."
Write-Host "INFO   Shared Sprint 17/18 tracing remains enabled for operational safety."
Write-Host "PASS   Task 1, Sprint 17, Sprint 18, RDS, and application data were not deleted."

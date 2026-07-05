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

Write-Host "PropCare Cloud AWS CLI Check"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Result "INFO" "Working directory" (Get-Location).Path

$awsCommand = Get-Command aws -ErrorAction SilentlyContinue
if ($null -eq $awsCommand) {
    Write-Result "FAIL" "AWS CLI" "aws command was not found. Install and configure AWS CLI before RDS validation."
    exit 1
}

Write-Result "PASS" "AWS CLI path" $awsCommand.Source

$versionOutput = & aws --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Result "FAIL" "aws --version" "AWS CLI did not return a version."
    exit 1
}

Write-Result "PASS" "aws --version" ($versionOutput -join " ")

$configureOutput = & aws configure list 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Result "WARN" "AWS CLI configuration" "Unable to inspect configuration. Run aws configure if needed."
    exit 0
}

$configureText = $configureOutput -join "`n"
$hasAccessKey = $configureText -match "access_key\s+\*+"
$hasProfile = -not [string]::IsNullOrWhiteSpace($env:AWS_PROFILE)
$hasWebIdentity = -not [string]::IsNullOrWhiteSpace($env:AWS_WEB_IDENTITY_TOKEN_FILE)
$hasContainerCredentials = -not [string]::IsNullOrWhiteSpace($env:AWS_CONTAINER_CREDENTIALS_RELATIVE_URI)

if (-not ($hasAccessKey -or $hasProfile -or $hasWebIdentity -or $hasContainerCredentials)) {
    Write-Result "WARN" "AWS CLI configuration" "No configured credentials detected. Run aws configure or set an AWS profile before RDS work."
    exit 0
}

Write-Result "PASS" "AWS CLI configuration" "Credentials/profile appear configured. No secrets printed."

$identityOutput = & aws sts get-caller-identity --output json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Result "WARN" "aws sts get-caller-identity" "AWS CLI is present, but caller identity could not be confirmed."
    exit 0
}

$identity = $identityOutput | ConvertFrom-Json
Write-Result "PASS" "AWS caller account" $identity.Account
Write-Result "PASS" "AWS caller ARN" $identity.Arn
exit 0

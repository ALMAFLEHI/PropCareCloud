param(
    [string]$Profile = "",
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

function Write-Result {
    param([string]$Status, [string]$Name, [string]$Detail = "")
    if ([string]::IsNullOrWhiteSpace($Detail)) {
        Write-Host ("{0,-6} {1}" -f $Status, $Name)
    }
    else {
        Write-Host ("{0,-6} {1}: {2}" -f $Status, $Name, $Detail)
    }
}

$publisherTests = Join-Path $PSScriptRoot "lambda\publisher\tests"
$processorTests = Join-Path $PSScriptRoot "lambda\processor\tests"
$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"

Write-Host "PropCare Cloud Sprint 18 Local Serverless Validation"

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "Python is required to run the Sprint 18 Lambda unit tests."
}

& python -m unittest discover -s $publisherTests -p "test_*.py" -v
if ($LASTEXITCODE -ne 0) {
    throw "Publisher Lambda unit tests failed."
}
Write-Result "PASS" "Publisher Lambda unit tests" "9 passed"

& python -m unittest discover -s $processorTests -p "test_*.py" -v
if ($LASTEXITCODE -ne 0) {
    throw "Processor Lambda unit tests failed."
}
Write-Result "PASS" "Processor Lambda unit tests" "6 passed"

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Result "WARN" "CloudFormation validation" "AWS CLI unavailable"
    exit 0
}

$identityArguments = @("sts", "get-caller-identity", "--region", $Region)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $identityArguments += @("--profile", $Profile)
}
$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$null = & aws @identityArguments 2>$null
$authenticationExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
if ($authenticationExitCode -ne 0) {
    Write-Result "WARN" "CloudFormation validation" "AWS authentication unavailable"
    exit 0
}

$validationArguments = @(
    "cloudformation", "validate-template",
    "--region", $Region,
    "--template-body", "file://$templatePath"
)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $validationArguments += @("--profile", $Profile)
}
$null = & aws @validationArguments
if ($LASTEXITCODE -ne 0) {
    throw "CloudFormation template validation failed."
}
Write-Result "PASS" "CloudFormation template"

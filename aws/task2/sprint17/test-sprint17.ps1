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

$testPath = Join-Path $PSScriptRoot "lambda\presign_service\tests"
$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"

Write-Host "PropCare Cloud Sprint 17 Local Serverless Validation"

$pythonCommand = Get-Command python -ErrorAction SilentlyContinue
if (-not $pythonCommand) {
    throw "Python is required to run the Sprint 17 Lambda unit tests."
}

& python -m unittest discover -s $testPath -p "test_*.py" -v
if ($LASTEXITCODE -ne 0) {
    throw "Lambda unit tests failed."
}
Write-Result "PASS" "Lambda unit tests"

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Result "WARN" "CloudFormation validation" "AWS CLI unavailable"
    exit 0
}

$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$identityArguments = @("sts", "get-caller-identity", "--region", $Region)
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $identityArguments += @("--profile", $Profile)
}
$null = & aws @identityArguments 2>&1
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

param(
    [string]$Profile = "fresh-propcare-task2-deployer",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint19"
)

$ErrorActionPreference = "Stop"

function Write-Result {
    param([string]$Status, [string]$Name, [string]$Detail = "")
    Write-Host ("{0,-6} {1}{2}" -f $Status, $Name, $(
        if ($Detail) { ": $Detail" } else { "" }
    ))
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

if ($Profile -ne "fresh-propcare-task2-deployer" -or $Region -ne "us-east-1") {
    throw "Sprint 19 validation requires the approved AWS profile and region."
}
if ($StackName -ne "propcarecloud-task2-sprint19") {
    throw "Sprint 19 validation is restricted to the Sprint 19 stack."
}

$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"
$deployPath = Join-Path $PSScriptRoot "deploy-sprint19.ps1"
$rollbackPath = Join-Path $PSScriptRoot "rollback-sprint19.ps1"
$sprint17Template = Join-Path $PSScriptRoot "..\sprint17\infrastructure\template.yaml"
$sprint18Template = Join-Path $PSScriptRoot "..\sprint18\infrastructure\template.yaml"
foreach ($path in @(
    $templatePath,
    $deployPath,
    $rollbackPath,
    $sprint17Template,
    $sprint18Template
)) {
    Assert-True (Test-Path -LiteralPath $path) "A Sprint 19 validation input is missing."
}

Write-Host "PropCare Cloud Sprint 19 Monitoring Validation"

$template = Get-Content -LiteralPath $templatePath -Raw
$alarmCount = ([regex]::Matches(
    $template,
    "(?m)^\s+Type: AWS::CloudWatch::Alarm\s*$"
)).Count
Assert-True ($alarmCount -eq 9) "Expected nine focused CloudWatch alarms."
Assert-True ($template.Contains("propcarecloud-task2-monitoring")) (
    "The expected dashboard name is missing."
)
foreach ($metric in @(
    "AWS/Lambda",
    "AWS/ApiGateway",
    "AWS/SNS",
    "AWS/SQS",
    "AWS/ElasticBeanstalk",
    "AWS/RDS"
)) {
    Assert-True ($template.Contains($metric)) "A required dashboard metric is missing."
}
Write-Result "PASS" "Dashboard and alarms" "Readable dashboard plus 9 focused alarms"

$dashboardMatch = [regex]::Match(
    $template,
    "(?ms)DashboardBody: !Sub \|\r?\n(?<body>.*?)(?=\r?\n  Sprint17LambdaErrorAlarm:)"
)
Assert-True $dashboardMatch.Success "Dashboard body could not be extracted."
$dashboardJson = (($dashboardMatch.Groups["body"].Value -split "\r?\n") |
    ForEach-Object { $_ -replace "^\s{8}", "" }) -join [Environment]::NewLine
$dashboardJson = [regex]::Replace($dashboardJson, "\$\{[^}]+\}", "test-value")
$null = $dashboardJson | ConvertFrom-Json
Write-Result "PASS" "Dashboard JSON" "Valid JSON after CloudFormation substitution"

$sprint17Infrastructure = Get-Content -LiteralPath $sprint17Template -Raw
$sprint18Infrastructure = Get-Content -LiteralPath $sprint18Template -Raw
foreach ($infrastructure in @($sprint17Infrastructure, $sprint18Infrastructure)) {
    Assert-True ($infrastructure.Contains("TracingConfig:")) (
        "Lambda active tracing configuration is missing."
    )
    Assert-True ($infrastructure.Contains("TracingEnabled: true")) (
        "API Gateway active tracing configuration is missing."
    )
}
$combinedInfrastructure = $sprint17Infrastructure + $sprint18Infrastructure
$xrayTraceActionCount = ([regex]::Matches(
    $combinedInfrastructure,
    "xray:PutTraceSegments"
)).Count
$xrayTelemetryActionCount = ([regex]::Matches(
    $combinedInfrastructure,
    "xray:PutTelemetryRecords"
)).Count
Assert-True ($xrayTraceActionCount -eq 3) (
    "X-Ray trace permission must be present for exactly three functions."
)
Assert-True ($xrayTelemetryActionCount -eq 3) (
    "X-Ray telemetry permission must be present for exactly three functions."
)
Write-Result "PASS" "X-Ray infrastructure" (
    "Native tracing and only required telemetry actions configured"
)

$deployScript = Get-Content -LiteralPath $deployPath -Raw
$rollbackScript = Get-Content -LiteralPath $rollbackPath -Raw
Assert-True ($deployScript.Contains('fresh-propcare-task2-deployer')) (
    "The named AWS profile is not enforced."
)
Assert-True (-not $rollbackScript.Contains(
    'delete-stack", "--stack-name", "propcarecloud-task2-sprint17'
)) "Rollback must not delete Sprint 17."
Assert-True (-not $rollbackScript.Contains(
    'delete-stack", "--stack-name", "propcarecloud-task2-sprint18'
)) "Rollback must not delete Sprint 18."
Assert-True (-not $rollbackScript.Contains("propcarecloud-postgres")) (
    "Rollback must not target RDS."
)
Write-Result "PASS" "Deployment and rollback safety" (
    "Named profile enforced; protected resources are not deletion targets"
)

$null = & aws sts get-caller-identity `
    --profile $Profile --region $Region `
    --query Account --output text 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Result "WARN" "Live AWS validation" "Authentication unavailable"
    exit 0
}

foreach ($validationTemplate in @(
    $templatePath,
    $sprint17Template,
    $sprint18Template
)) {
    $null = & aws cloudformation validate-template `
        --template-body "file://$validationTemplate" `
        --profile $Profile --region $Region
    if ($LASTEXITCODE -ne 0) {
        throw "CloudFormation template validation failed."
    }
}
Write-Result "PASS" "CloudFormation templates" "All three templates validate"

$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = "Continue"
$stackStatus = & aws cloudformation describe-stacks `
    --stack-name $StackName `
    --profile $Profile --region $Region `
    --query "Stacks[0].StackStatus" --output text 2>$null
$stackQueryExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
if ($stackQueryExitCode -ne 0) {
    Write-Result "INFO" "Live Sprint 19 stack" "Not deployed yet"
    exit 0
}
Assert-True ($stackStatus -in @("CREATE_COMPLETE", "UPDATE_COMPLETE")) (
    "The live Sprint 19 stack is not healthy."
)
$dashboardName = & aws cloudformation describe-stacks `
    --stack-name $StackName `
    --profile $Profile --region $Region `
    --query "Stacks[0].Outputs[?OutputKey=='DashboardName'].OutputValue | [0]" `
    --output text
if ($LASTEXITCODE -ne 0 -or $dashboardName -ne "propcarecloud-task2-monitoring") {
    throw "The live CloudWatch dashboard was not verified."
}
$alarms = & aws cloudwatch describe-alarms `
    --alarm-name-prefix "propcarecloud-" `
    --profile $Profile --region $Region `
    --query "MetricAlarms[?contains(AlarmName, 'sprint17') || contains(AlarmName, 'sprint18') || contains(AlarmName, 'notification') || contains(AlarmName, 'elastic-beanstalk') || contains(AlarmName, 'rds-high-cpu')].AlarmName" `
    --output text
if ($LASTEXITCODE -ne 0 -or ($alarms -split "\s+").Count -lt 9) {
    throw "The live Sprint 19 alarms were not verified."
}
Write-Result "PASS" "Live monitoring stack" "Dashboard and alarms verified"

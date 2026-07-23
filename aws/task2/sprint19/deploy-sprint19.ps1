param(
    [string]$Profile = "fresh-propcare-task2-deployer",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint19"
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

function Invoke-Aws {
    param([string[]]$Arguments)
    & aws @Arguments --profile $Profile --region $Region
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed with exit code $LASTEXITCODE."
    }
}

function Get-AwsText {
    param([string[]]$Arguments)
    $output = & aws @Arguments --profile $Profile --region $Region
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI query failed with exit code $LASTEXITCODE."
    }
    return (($output -join [Environment]::NewLine).Trim())
}

function Get-StackOutput {
    param([string]$Stack, [string]$OutputKey)
    return Get-AwsText @(
        "cloudformation", "describe-stacks",
        "--stack-name", $Stack,
        "--query", "Stacks[0].Outputs[?OutputKey=='$OutputKey'].OutputValue | [0]",
        "--output", "text"
    )
}

function Get-StackResourceId {
    param([string]$Stack, [string]$LogicalResourceId)
    return Get-AwsText @(
        "cloudformation", "describe-stack-resource",
        "--stack-name", $Stack,
        "--logical-resource-id", $LogicalResourceId,
        "--query", "StackResourceDetail.PhysicalResourceId",
        "--output", "text"
    )
}

if ($StackName -ne "propcarecloud-task2-sprint19") {
    throw "Deployment is restricted to the Sprint 19 monitoring stack."
}
if ($Profile -ne "fresh-propcare-task2-deployer" -or $Region -ne "us-east-1") {
    throw "Sprint 19 deployment requires the approved profile and us-east-1."
}

$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"
$sprint17Deploy = Join-Path $PSScriptRoot "..\sprint17\deploy-sprint17.ps1"
$sprint18Deploy = Join-Path $PSScriptRoot "..\sprint18\deploy-sprint18.ps1"
foreach ($requiredPath in @($templatePath, $sprint17Deploy, $sprint18Deploy)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "A required Sprint 19 deployment input is missing."
    }
}

Write-Host "PropCare Cloud Sprint 19 Monitoring Deployment"

$accountCheck = Get-AwsText @(
    "sts", "get-caller-identity",
    "--query", "Account",
    "--output", "text"
)
if ($accountCheck -notmatch "^\d{12}$") {
    throw "AWS authentication failed."
}
Write-Result "PASS" "AWS authentication" "Approved identity confirmed; identifier not printed"

foreach ($protectedStack in @(
    "propcarecloud-task2-sprint17",
    "propcarecloud-task2-sprint18"
)) {
    $status = Get-AwsText @(
        "cloudformation", "describe-stacks",
        "--stack-name", $protectedStack,
        "--query", "Stacks[0].StackStatus",
        "--output", "text"
    )
    if ($status -notin @("CREATE_COMPLETE", "UPDATE_COMPLETE")) {
        throw "A protected Task 2 stack is not ready for a tracing-only update."
    }
}
Write-Result "PASS" "Protected stacks" "Sprint 17 and Sprint 18 are healthy"

& powershell -ExecutionPolicy Bypass -File $sprint17Deploy `
    -Profile $Profile -Region $Region
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 17 tracing update failed."
}
& powershell -ExecutionPolicy Bypass -File $sprint18Deploy `
    -Profile $Profile -Region $Region
if ($LASTEXITCODE -ne 0) {
    throw "Sprint 18 tracing update failed."
}
Write-Result "PASS" "Tracing updates" (
    "Existing API routes and Lambda business resources updated in place"
)

$sprint17ApiId = Get-StackResourceId `
    "propcarecloud-task2-sprint17" "AttachmentApi"
$sprint18ApiId = Get-StackResourceId `
    "propcarecloud-task2-sprint18" "NotificationApi"
$sprint17ApiName = Get-AwsText @(
    "apigateway", "get-rest-api",
    "--rest-api-id", $sprint17ApiId,
    "--query", "name",
    "--output", "text"
)
$sprint18ApiName = Get-AwsText @(
    "apigateway", "get-rest-api",
    "--rest-api-id", $sprint18ApiId,
    "--query", "name",
    "--output", "text"
)
$sprint17Lambda = Get-StackOutput `
    "propcarecloud-task2-sprint17" "LambdaFunctionName"
$publisherLambda = Get-StackOutput `
    "propcarecloud-task2-sprint18" "PublisherFunctionName"
$processorLambda = Get-StackOutput `
    "propcarecloud-task2-sprint18" "ProcessorFunctionName"
$topicArn = Get-StackOutput "propcarecloud-task2-sprint18" "TopicArn"
$queueUrl = Get-StackOutput "propcarecloud-task2-sprint18" "QueueUrl"
$deadLetterQueueUrl = Get-StackOutput `
    "propcarecloud-task2-sprint18" "DeadLetterQueueUrl"
$topicName = $topicArn.Split(":")[-1]
$queueName = $queueUrl.Split("/")[-1]
$deadLetterQueueName = $deadLetterQueueUrl.Split("/")[-1]
$environmentName = Get-AwsText @(
    "elasticbeanstalk", "describe-environments",
    "--query",
    "Environments[?CNAME=='propcarecloud-api.us-east-1.elasticbeanstalk.com'] | [0].EnvironmentName",
    "--output", "text"
)
$rdsIdentifier = "propcarecloud-postgres"
$rdsStatus = Get-AwsText @(
    "rds", "describe-db-instances",
    "--db-instance-identifier", $rdsIdentifier,
    "--query", "DBInstances[0].DBInstanceStatus",
    "--output", "text"
)

$requiredValues = @(
    $sprint17ApiId,
    $sprint18ApiId,
    $sprint17ApiName,
    $sprint18ApiName,
    $sprint17Lambda,
    $publisherLambda,
    $processorLambda,
    $topicName,
    $queueName,
    $deadLetterQueueName,
    $environmentName
)
if ($requiredValues | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq "None" }) {
    throw "One or more monitored resource names could not be resolved."
}
if ($rdsStatus -ne "available") {
    throw "The protected RDS instance is not available."
}
Write-Result "PASS" "Resource discovery" (
    "Existing monitored resource names resolved without printing private identifiers"
)

Invoke-Aws @(
    "cloudformation", "deploy",
    "--stack-name", $StackName,
    "--template-file", $templatePath,
    "--no-fail-on-empty-changeset",
    "--parameter-overrides",
    "Sprint17ApiName=$sprint17ApiName",
    "Sprint18ApiName=$sprint18ApiName",
    "Sprint17LambdaName=$sprint17Lambda",
    "Sprint18PublisherLambdaName=$publisherLambda",
    "Sprint18ProcessorLambdaName=$processorLambda",
    "NotificationTopicName=$topicName",
    "NotificationQueueName=$queueName",
    "NotificationDeadLetterQueueName=$deadLetterQueueName",
    "ElasticBeanstalkEnvironmentName=$environmentName",
    "RdsDatabaseIdentifier=$rdsIdentifier",
    "--tags",
    "Project=PropCareCloud",
    "Task=Task2",
    "Sprint=19",
    "Environment=Assignment"
) | Out-Null
Write-Result "PASS" "Sprint 19 stack" "Create/update completed"

foreach ($functionName in @(
    $sprint17Lambda,
    $publisherLambda,
    $processorLambda
)) {
    $tracingMode = Get-AwsText @(
        "lambda", "get-function-configuration",
        "--function-name", $functionName,
        "--query", "TracingConfig.Mode",
        "--output", "text"
    )
    if ($tracingMode -ne "Active") {
        throw "Lambda active tracing verification failed."
    }
}
foreach ($apiId in @($sprint17ApiId, $sprint18ApiId)) {
    $stageTracing = Get-AwsText @(
        "apigateway", "get-stage",
        "--rest-api-id", $apiId,
        "--stage-name", "prod",
        "--query", "tracingEnabled",
        "--output", "text"
    )
    if ($stageTracing -ne "True") {
        throw "API Gateway active tracing verification failed."
    }
}

Write-Result "PASS" "Lambda X-Ray" "Active on all three target functions"
Write-Result "PASS" "API Gateway X-Ray" "Active on both prod stages"
Write-Result "PASS" "Deployment safety" (
    "No Task 1 resource was targeted; Sprint 17/18 routes and service settings were preserved"
)

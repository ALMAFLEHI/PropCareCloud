param(
    [string]$Profile = "",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint18",
    [string]$ElasticBeanstalkEnvironmentName = "",
    [switch]$ConfigureElasticBeanstalk
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
    $commandArguments = @($Arguments)
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        $commandArguments += @("--profile", $Profile)
    }
    & aws @commandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed with exit code $LASTEXITCODE."
    }
}

function Get-AwsText {
    param([string[]]$Arguments)
    $commandArguments = @($Arguments)
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        $commandArguments += @("--profile", $Profile)
    }
    $output = & aws @commandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI query failed with exit code $LASTEXITCODE."
    }
    return (($output -join [Environment]::NewLine).Trim())
}

function Get-StackOutput {
    param([string]$OutputKey)
    return Get-AwsText @(
        "cloudformation", "describe-stacks",
        "--region", $Region,
        "--stack-name", $StackName,
        "--query", "Stacks[0].Outputs[?OutputKey=='$OutputKey'].OutputValue | [0]",
        "--output", "text"
    )
}

if ($StackName -ne "propcarecloud-task2-sprint18") {
    throw "Deployment is restricted to the Sprint 18 stack."
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"
$publisherAppPath = Join-Path $PSScriptRoot "lambda\publisher\app.py"
$processorAppPath = Join-Path $PSScriptRoot "lambda\processor\app.py"
$contractPath = Join-Path $PSScriptRoot "lambda\common\notification_contract.py"
$artifactPath = Join-Path $projectRoot "artifacts\deployment\task2-sprint18"
$publisherStagePath = Join-Path $artifactPath "publisher-package"
$processorStagePath = Join-Path $artifactPath "processor-package"
$publisherZipPath = Join-Path $artifactPath "notification-publisher.zip"
$processorZipPath = Join-Path $artifactPath "notification-processor.zip"
$temporarySettingsPath = $null

try {
    Write-Host "PropCare Cloud Sprint 18 Notification Pipeline Deployment"
    Write-Result "INFO" "Region" $Region
    Write-Result "INFO" "Stack" $StackName

    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI is required."
    }
    foreach ($requiredPath in @(
        $templatePath,
        $publisherAppPath,
        $processorAppPath,
        $contractPath
    )) {
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            throw "A required Sprint 18 deployment file is missing."
        }
    }

    $accountId = Get-AwsText @(
        "sts", "get-caller-identity",
        "--region", $Region,
        "--query", "Account",
        "--output", "text"
    )
    if ([string]::IsNullOrWhiteSpace($accountId) -or $accountId -eq "None") {
        throw "AWS authentication is required for the approved profile."
    }
    Write-Result "PASS" "AWS authentication" (
        "Authenticated account confirmed; identifier not printed"
    )

    New-Item -ItemType Directory -Force -Path $artifactPath | Out-Null
    foreach ($stagePath in @($publisherStagePath, $processorStagePath)) {
        if (Test-Path -LiteralPath $stagePath) {
            Remove-Item -LiteralPath $stagePath -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $stagePath | Out-Null
    }
    Copy-Item -LiteralPath $publisherAppPath -Destination (
        Join-Path $publisherStagePath "app.py")
    Copy-Item -LiteralPath $contractPath -Destination (
        Join-Path $publisherStagePath "notification_contract.py")
    Copy-Item -LiteralPath $processorAppPath -Destination (
        Join-Path $processorStagePath "app.py")
    Copy-Item -LiteralPath $contractPath -Destination (
        Join-Path $processorStagePath "notification_contract.py")
    Compress-Archive -Path (Join-Path $publisherStagePath "*") `
        -DestinationPath $publisherZipPath -Force
    Compress-Archive -Path (Join-Path $processorStagePath "*") `
        -DestinationPath $processorZipPath -Force
    Write-Result "PASS" "Lambda packages" (
        "Publisher and processor ZIPs created under ignored artifacts output"
    )

    $artifactBucket = "propcarecloud-task2-deployment-$accountId-$Region"
    $headArguments = @(
        "s3api", "head-bucket",
        "--bucket", $artifactBucket,
        "--region", $Region
    )
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        $headArguments += @("--profile", $Profile)
    }
    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $null = & aws @headArguments 2>$null
    $headExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    if ($headExitCode -ne 0) {
        if ($Region -eq "us-east-1") {
            Invoke-Aws @(
                "s3api", "create-bucket",
                "--bucket", $artifactBucket,
                "--region", $Region
            ) | Out-Null
        }
        else {
            Invoke-Aws @(
                "s3api", "create-bucket",
                "--bucket", $artifactBucket,
                "--region", $Region,
                "--create-bucket-configuration", "LocationConstraint=$Region"
            ) | Out-Null
        }
        Invoke-Aws @(
            "s3api", "put-public-access-block",
            "--bucket", $artifactBucket,
            "--region", $Region,
            "--public-access-block-configuration",
            "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
        ) | Out-Null
        Invoke-Aws @(
            "s3api", "put-bucket-encryption",
            "--bucket", $artifactBucket,
            "--region", $Region,
            "--server-side-encryption-configuration",
            "Rules=[{ApplyServerSideEncryptionByDefault={SSEAlgorithm=AES256}}]"
        ) | Out-Null
    }
    Write-Result "PASS" "Deployment artifact bucket" (
        "Existing private artifact storage reused"
    )

    $publisherHash = (
        Get-FileHash -LiteralPath $publisherZipPath -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    $processorHash = (
        Get-FileHash -LiteralPath $processorZipPath -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    $publisherCodeKey = "sprint18/publisher-$publisherHash.zip"
    $processorCodeKey = "sprint18/processor-$processorHash.zip"
    Invoke-Aws @(
        "s3", "cp", $publisherZipPath,
        "s3://$artifactBucket/$publisherCodeKey",
        "--region", $Region,
        "--only-show-errors"
    ) | Out-Null
    Invoke-Aws @(
        "s3", "cp", $processorZipPath,
        "s3://$artifactBucket/$processorCodeKey",
        "--region", $Region,
        "--only-show-errors"
    ) | Out-Null
    Write-Result "PASS" "Lambda artifact upload" (
        "Two versioned artifacts uploaded without printing account-specific locations"
    )

    Invoke-Aws @(
        "cloudformation", "deploy",
        "--region", $Region,
        "--stack-name", $StackName,
        "--template-file", $templatePath,
        "--capabilities", "CAPABILITY_NAMED_IAM",
        "--no-fail-on-empty-changeset",
        "--parameter-overrides",
        "LambdaCodeBucket=$artifactBucket",
        "PublisherCodeKey=$publisherCodeKey",
        "ProcessorCodeKey=$processorCodeKey",
        "--tags",
        "Project=PropCareCloud",
        "Task=Task2",
        "Sprint=18",
        "Environment=Assignment"
    )
    Write-Result "PASS" "CloudFormation stack" "Create/update completed"

    $apiBaseUrl = Get-StackOutput "ApiBaseUrl"
    $apiKeyId = Get-StackOutput "ApiKeyId"
    if ([string]::IsNullOrWhiteSpace($apiBaseUrl) -or
        [string]::IsNullOrWhiteSpace($apiKeyId) -or
        $apiBaseUrl -eq "None" -or
        $apiKeyId -eq "None") {
        throw "CloudFormation did not return the expected notification outputs."
    }
    $serviceApiKey = Get-AwsText @(
        "apigateway", "get-api-key",
        "--region", $Region,
        "--api-key", $apiKeyId,
        "--include-value",
        "--query", "value",
        "--output", "text"
    )
    if ([string]::IsNullOrWhiteSpace($serviceApiKey) -or $serviceApiKey -eq "None") {
        throw "The backend notification API key could not be retrieved securely."
    }
    Write-Result "PASS" "Notification API configuration" (
        "Base URL and backend-only key resolved; secret not printed"
    )

    if ($ConfigureElasticBeanstalk) {
        if ([string]::IsNullOrWhiteSpace($ElasticBeanstalkEnvironmentName)) {
            throw "ElasticBeanstalkEnvironmentName is required with ConfigureElasticBeanstalk."
        }

        $applicationName = Get-AwsText @(
            "elasticbeanstalk", "describe-environments",
            "--region", $Region,
            "--environment-names", $ElasticBeanstalkEnvironmentName,
            "--query", "Environments[0].ApplicationName",
            "--output", "text"
        )
        if ([string]::IsNullOrWhiteSpace($applicationName) -or
            $applicationName -eq "None") {
            throw "The requested Elastic Beanstalk environment was not found."
        }

        $settingNamesText = Get-AwsText @(
            "elasticbeanstalk", "describe-configuration-settings",
            "--region", $Region,
            "--application-name", $applicationName,
            "--environment-name", $ElasticBeanstalkEnvironmentName,
            "--query",
            "ConfigurationSettings[0].OptionSettings[?Namespace=='aws:elasticbeanstalk:application:environment'].OptionName",
            "--output", "text"
        )
        $settingNames = @($settingNamesText -split "\s+" | Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        })
        foreach ($requiredName in @(
            "PROPCLOUD_CONNECTION_STRING",
            "Jwt__SigningKey",
            "Task2Attachments__ApiKey"
        )) {
            if ($settingNames -notcontains $requiredName) {
                throw "Required existing Elastic Beanstalk setting $requiredName was not found."
            }
        }

        $settings = @(
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Notifications__ApiBaseUrl"
                Value = $apiBaseUrl
            },
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Notifications__ApiKey"
                Value = $serviceApiKey
            },
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Notifications__TimeoutSeconds"
                Value = "3"
            }
        )
        $temporarySettingsPath = Join-Path ([System.IO.Path]::GetTempPath()) (
            "propcare-sprint18-eb-{0}.json" -f [Guid]::NewGuid())
        [System.IO.File]::WriteAllText(
            $temporarySettingsPath,
            ($settings | ConvertTo-Json -Depth 5),
            [System.Text.UTF8Encoding]::new($false))
        Invoke-Aws @(
            "elasticbeanstalk", "update-environment",
            "--region", $Region,
            "--environment-name", $ElasticBeanstalkEnvironmentName,
            "--option-settings", "file://$temporarySettingsPath"
        ) | Out-Null
        Invoke-Aws @(
            "elasticbeanstalk", "wait", "environment-updated",
            "--region", $Region,
            "--environment-names", $ElasticBeanstalkEnvironmentName
        )
        Write-Result "PASS" "Elastic Beanstalk secure configuration" (
            "Sprint 18 settings added without printing or replacing existing secrets"
        )
    }
    else {
        Write-Result "INFO" "Elastic Beanstalk secure configuration" (
            "Not changed; use -ConfigureElasticBeanstalk with the existing environment name"
        )
    }

    Write-Result "PASS" "Sprint 18 serverless deployment" "Completed"
}
finally {
    if ($temporarySettingsPath -and
        (Test-Path -LiteralPath $temporarySettingsPath)) {
        Remove-Item -LiteralPath $temporarySettingsPath -Force
    }
}

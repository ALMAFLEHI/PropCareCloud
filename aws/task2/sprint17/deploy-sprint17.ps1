param(
    [string]$Profile = "",
    [string]$Region = "us-east-1",
    [string]$StackName = "propcarecloud-task2-sprint17",
    [string[]]$FrontendOrigins = @(
        "http://propcarecloud-frontend-20260706.s3-website-us-east-1.amazonaws.com",
        "http://localhost:5173"
    ),
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
    if ([string]::IsNullOrWhiteSpace($Profile)) {
        & aws @Arguments
    }
    else {
        & aws @Arguments --profile $Profile
    }
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed with exit code $LASTEXITCODE."
    }
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$templatePath = Join-Path $PSScriptRoot "infrastructure\template.yaml"
$lambdaAppPath = Join-Path $PSScriptRoot "lambda\presign_service\app.py"
$artifactPath = Join-Path $projectRoot "artifacts\deployment\task2-sprint17"
$lambdaZipPath = Join-Path $artifactPath "propcarecloud-task2-presign-service.zip"
$temporarySettingsPath = $null

try {
    Write-Host "PropCare Cloud Sprint 17 Serverless Deployment"
    Write-Result "INFO" "Region" $Region
    Write-Result "INFO" "Stack" $StackName

    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI is required."
    }

    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $identityArguments = @("sts", "get-caller-identity", "--region", $Region, "--query", "Account", "--output", "text")
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        $identityArguments += @("--profile", $Profile)
    }
    $accountIdOutput = & aws @identityArguments 2>$null
    $authenticationExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    $accountId = ([string]$accountIdOutput).Trim()
    if ($authenticationExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($accountId)) {
        throw "AWS authentication is required. Run aws login or configure an approved AWS profile."
    }
    Write-Result "PASS" "AWS authentication" "Authenticated account confirmed; identifier not printed"

    if (-not (Test-Path -LiteralPath $templatePath) -or
        -not (Test-Path -LiteralPath $lambdaAppPath)) {
        throw "Sprint 17 infrastructure or Lambda source is missing."
    }

    New-Item -ItemType Directory -Force -Path $artifactPath | Out-Null
    if (Test-Path -LiteralPath $lambdaZipPath) {
        Remove-Item -LiteralPath $lambdaZipPath -Force
    }
    Compress-Archive -LiteralPath $lambdaAppPath -DestinationPath $lambdaZipPath -Force
    Write-Result "PASS" "Lambda package" "Created locally under ignored artifacts output"

    $artifactBucket = "propcarecloud-task2-deployment-$accountId-$Region"
    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $headBucketArguments = @("s3api", "head-bucket", "--bucket", $artifactBucket, "--region", $Region)
    if (-not [string]::IsNullOrWhiteSpace($Profile)) {
        $headBucketArguments += @("--profile", $Profile)
    }
    $null = & aws @headBucketArguments 2>&1
    $headBucketExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorPreference
    if ($headBucketExitCode -ne 0) {
        if ($Region -eq "us-east-1") {
            Invoke-Aws @("s3api", "create-bucket", "--bucket", $artifactBucket, "--region", $Region) | Out-Null
        }
        else {
            Invoke-Aws @(
                "s3api", "create-bucket",
                "--bucket", $artifactBucket,
                "--region", $Region,
                "--create-bucket-configuration", "LocationConstraint=$Region"
            ) | Out-Null
        }
    }
    Invoke-Aws @(
        "s3api", "put-public-access-block",
        "--bucket", $artifactBucket,
        "--public-access-block-configuration",
        "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
    ) | Out-Null
    Invoke-Aws @(
        "s3api", "put-bucket-encryption",
        "--bucket", $artifactBucket,
        "--server-side-encryption-configuration",
        "Rules=[{ApplyServerSideEncryptionByDefault={SSEAlgorithm=AES256}}]"
    ) | Out-Null
    Invoke-Aws @(
        "s3api", "put-bucket-tagging",
        "--bucket", $artifactBucket,
        "--tagging",
        "TagSet=[{Key=Project,Value=PropCareCloud},{Key=Task,Value=Task2},{Key=Sprint,Value=17},{Key=Environment,Value=Assignment}]"
    ) | Out-Null
    Write-Result "PASS" "Deployment artifact bucket" "Private and encrypted"

    $packageHash = (Get-FileHash -LiteralPath $lambdaZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $lambdaCodeKey = "sprint17/presign-service-$packageHash.zip"
    Invoke-Aws @(
        "s3", "cp", $lambdaZipPath, "s3://$artifactBucket/$lambdaCodeKey",
        "--region", $Region,
        "--only-show-errors"
    ) | Out-Null
    Write-Result "PASS" "Lambda artifact upload" "Uploaded without printing account-specific location"

    $originParameter = $FrontendOrigins -join ","
    Invoke-Aws @(
        "cloudformation", "deploy",
        "--region", $Region,
        "--stack-name", $StackName,
        "--template-file", $templatePath,
        "--capabilities", "CAPABILITY_NAMED_IAM",
        "--no-fail-on-empty-changeset",
        "--parameter-overrides",
        "LambdaCodeBucket=$artifactBucket",
        "LambdaCodeKey=$lambdaCodeKey",
        "FrontendOrigins=$originParameter",
        "--tags",
        "Project=PropCareCloud", "Task=Task2", "Sprint=17", "Environment=Assignment"
    )
    Write-Result "PASS" "CloudFormation stack" "Create/update completed"

    $apiBaseUrl = (& aws cloudformation describe-stacks `
        --profile $Profile `
        --region $Region `
        --stack-name $StackName `
        --query "Stacks[0].Outputs[?OutputKey=='ApiBaseUrl'].OutputValue | [0]" `
        --output text).Trim()
    $apiKeyId = (& aws cloudformation describe-stacks `
        --profile $Profile `
        --region $Region `
        --stack-name $StackName `
        --query "Stacks[0].Outputs[?OutputKey=='ApiKeyId'].OutputValue | [0]" `
        --output text).Trim()
    if ([string]::IsNullOrWhiteSpace($apiBaseUrl) -or [string]::IsNullOrWhiteSpace($apiKeyId)) {
        throw "CloudFormation did not return the expected attachment-service outputs."
    }

    $serviceApiKey = (& aws apigateway get-api-key `
        --profile $Profile `
        --region $Region `
        --api-key $apiKeyId `
        --include-value `
        --query value `
        --output text).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($serviceApiKey)) {
        throw "The backend service API key could not be retrieved securely."
    }
    Write-Result "PASS" "Attachment API configuration" "Base URL and service key resolved; secret not printed"

    if ($ConfigureElasticBeanstalk) {
        if ([string]::IsNullOrWhiteSpace($ElasticBeanstalkEnvironmentName)) {
            throw "ElasticBeanstalkEnvironmentName is required with ConfigureElasticBeanstalk."
        }

        $environment = & aws elasticbeanstalk describe-environments `
            --profile $Profile `
            --region $Region `
            --environment-names $ElasticBeanstalkEnvironmentName `
            --query "Environments[0].ApplicationName" `
            --output text
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($environment) -or $environment -eq "None") {
            throw "The requested Elastic Beanstalk environment was not found."
        }

        $settingNamesJson = & aws elasticbeanstalk describe-configuration-settings `
            --profile $Profile `
            --region $Region `
            --application-name $environment.Trim() `
            --environment-name $ElasticBeanstalkEnvironmentName `
            --query "ConfigurationSettings[0].OptionSettings[?Namespace=='aws:elasticbeanstalk:application:environment'].OptionName" `
            --output json
        if ($LASTEXITCODE -ne 0) {
            throw "Existing Elastic Beanstalk environment variable names could not be read."
        }
        $settingNames = @(($settingNamesJson -join [Environment]::NewLine) | ConvertFrom-Json)
        foreach ($requiredName in @("PROPCLOUD_CONNECTION_STRING", "Jwt__SigningKey")) {
            if ($settingNames -notcontains $requiredName) {
                throw "Required existing Elastic Beanstalk setting $requiredName was not found."
            }
        }

        $settings = @(
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Attachments__ApiBaseUrl"
                Value = $apiBaseUrl
            },
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Attachments__ApiKey"
                Value = $serviceApiKey
            },
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Attachments__MaxFileSizeBytes"
                Value = "10485760"
            },
            [pscustomobject]@{
                Namespace = "aws:elasticbeanstalk:application:environment"
                OptionName = "Task2Attachments__UrlExpirySeconds"
                Value = "300"
            }
        )

        $temporarySettingsPath = Join-Path ([System.IO.Path]::GetTempPath()) (
            "propcare-sprint17-eb-{0}.json" -f [Guid]::NewGuid())
        $settingsJson = $settings | ConvertTo-Json -Depth 5
        [System.IO.File]::WriteAllText(
            $temporarySettingsPath,
            $settingsJson,
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
        Write-Result "PASS" "Elastic Beanstalk secure configuration" "Updated without printing secrets"
    }
    else {
        Write-Result "INFO" "Elastic Beanstalk secure configuration" (
            "Not changed. Re-run with -ConfigureElasticBeanstalk and the existing environment name."
        )
    }

    Write-Result "PASS" "Sprint 17 serverless deployment" "Completed"
}
finally {
    if ($temporarySettingsPath -and (Test-Path -LiteralPath $temporarySettingsPath)) {
        Remove-Item -LiteralPath $temporarySettingsPath -Force
    }
    $serviceApiKey = $null
}

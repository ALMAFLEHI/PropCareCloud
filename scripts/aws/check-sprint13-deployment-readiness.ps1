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

function Invoke-Check {
    param(
        [string]$Name,
        [string[]]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    & $Command[0] $Command[1..($Command.Length - 1)]
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Result "PASS" $Name
        return $true
    }

    Write-Result "FAIL" $Name "Exit code $exitCode"
    return $false
}

function Test-NoSecretFiles {
    $stagedFiles = @()
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if ($null -ne $gitCommand) {
        $stagedFiles = @(git diff --cached --name-only)
    }

    $forbiddenNamePattern = "(^|/|\\)(\.env|.*\.pem|.*\.key|.*\.pfx|secrets\.json|aws-credentials.*|rds-connection.*|connection-string.*|local-secrets.*)$"
    foreach ($file in $stagedFiles) {
        if ($file -match $forbiddenNamePattern) {
            Write-Result "FAIL" "Staged secret file" $file
            return $false
        }
    }

    Write-Result "PASS" "Staged secret files" "No forbidden secret files staged."
    return $true
}

function Test-AppSettingsSafe {
    $appSettingsPath = ".\backend\src\PropCareCloud.Api\appsettings.json"
    if (-not (Test-Path -LiteralPath $appSettingsPath)) {
        Write-Result "FAIL" "appsettings.json" "$appSettingsPath was not found"
        return $false
    }

    $content = Get-Content -LiteralPath $appSettingsPath -Raw
    $json = $content | ConvertFrom-Json
    $defaultConnection = $json.ConnectionStrings.DefaultConnection
    if (-not [string]::IsNullOrWhiteSpace($defaultConnection)) {
        Write-Result "FAIL" "DefaultConnection" "Committed appsettings.json contains a configured connection string."
        return $false
    }

    Write-Result "PASS" "DefaultConnection" "Committed appsettings.json keeps DefaultConnection empty."
    return $true
}

function Test-NoSecretText {
    $patterns = @(
        "AKIA[0-9A-Z]{16}",
        "ASIA[0-9A-Z]{16}",
        "aws_access_key_id\s*=",
        "aws_secret_access_key\s*=",
        ("BEGIN " + "RSA PRIVATE KEY"),
        ("BEGIN " + "OPENSSH PRIVATE KEY"),
        ("BEGIN " + "PRIVATE KEY"),
        ("Host" + "=" + "[^<;\s][^;]*;[^\r\n]*Pass" + "word=[^<;\s][^;\r\n]*")
    )

    $textExtensions = @(".cs", ".json", ".md", ".ps1", ".txt", ".http", ".config", ".xml", ".yml", ".yaml")
    $textFiles = @()
    if (Test-Path -LiteralPath ".\README.md") {
        $textFiles += Get-Item -LiteralPath ".\README.md"
    }
    foreach ($root in @(".\docs", ".\scripts", ".\backend\src\PropCareCloud.Api")) {
        if (Test-Path -LiteralPath $root) {
            $textFiles += Get-ChildItem -LiteralPath $root -Recurse -File |
                Where-Object {
                    ($textExtensions -contains $_.Extension.ToLowerInvariant()) -and
                    ($_.Name -ne "check-sprint13-deployment-readiness.ps1")
                }
        }
    }

    foreach ($pattern in $patterns) {
        $matches = @(Select-String -LiteralPath $textFiles.FullName -Pattern $pattern -ErrorAction SilentlyContinue)
        if ($matches.Count -gt 0) {
            Write-Result "FAIL" "Secret text scan" ("Pattern matched: {0}" -f $pattern)
            return $false
        }
    }

    Write-Result "PASS" "Secret text scan" "No obvious AWS keys, private keys, or real DB connection strings found."
    return $true
}

Write-Host "PropCare Cloud Sprint 13 Deployment Readiness Check"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script validates local deployment readiness only. It does not create or update AWS resources."

$backendPassed = Invoke-Check "Backend validation" @("powershell", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\check-backend.ps1")
$frontendPassed = Invoke-Check "Frontend validation" @("powershell", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\check-frontend.ps1")
$backendPackagePassed = Invoke-Check "Backend deployment package" @("powershell", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\aws\build-sprint13-backend-package.ps1")
$frontendPackagePassed = Invoke-Check "Frontend deployment package" @("powershell", "-ExecutionPolicy", "Bypass", "-File", ".\scripts\aws\build-sprint13-frontend-package.ps1")
$secretFilesPassed = Test-NoSecretFiles
$appSettingsPassed = Test-AppSettingsSafe
$secretTextPassed = Test-NoSecretText

Write-Host ""
Write-Host "== Summary =="
Write-Host ("- Backend validation: {0}" -f $(if ($backendPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Frontend validation: {0}" -f $(if ($frontendPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Backend package: {0}" -f $(if ($backendPackagePassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Frontend package: {0}" -f $(if ($frontendPackagePassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Secret file check: {0}" -f $(if ($secretFilesPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- appsettings check: {0}" -f $(if ($appSettingsPassed) { "PASS" } else { "FAIL" }))
Write-Host ("- Secret text scan: {0}" -f $(if ($secretTextPassed) { "PASS" } else { "FAIL" }))

if ($backendPassed -and
    $frontendPassed -and
    $backendPackagePassed -and
    $frontendPackagePassed -and
    $secretFilesPassed -and
    $appSettingsPassed -and
    $secretTextPassed) {
    Write-Result "PASS" "Sprint 13 deployment readiness" "Local validation and deployment packaging completed successfully."
    exit 0
}

Write-Result "FAIL" "Sprint 13 deployment readiness" "One or more checks failed."
exit 1

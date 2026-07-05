param(
    [string]$ApiBaseUrl = "http://propcarecloud-api.us-east-1.elasticbeanstalk.com"
)

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

function Invoke-SafePost {
    param(
        [string]$Name,
        [string]$Uri
    )

    try {
        $response = Invoke-RestMethod -Method Post -Uri $Uri -TimeoutSec 30
        Write-Result "PASS" $Name $Uri
        return $response
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($null -ne $statusCode) {
            Write-Result "FAIL" $Name ("HTTP {0}" -f $statusCode)
        }
        else {
            Write-Result "FAIL" $Name "Endpoint did not respond"
        }

        return $null
    }
}

function Write-Count {
    param(
        [object]$Result,
        [string]$Name,
        [string]$Property
    )

    if ($null -ne $Result -and $Result.PSObject.Properties.Name -contains $Property) {
        Write-Result "INFO" $Name $Result.$Property
    }
}

Write-Host "PropCare Cloud Sprint 13 Demo Data Check"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script does not read or print database passwords, connection strings, AWS keys, or tokens."

$normalizedBaseUrl = $ApiBaseUrl.TrimEnd("/")
Write-Result "INFO" "API base URL" $normalizedBaseUrl

$seedResult = Invoke-SafePost "Seed demo data" "$normalizedBaseUrl/api/seed/demo-data"
$accountsResult = Invoke-SafePost "Ensure demo accounts" "$normalizedBaseUrl/api/auth/ensure-demo-accounts"

if ($null -ne $seedResult) {
    Write-Host ""
    Write-Host "== Seed Result =="
    Write-Count $seedResult "Success" "success"
    Write-Count $seedResult "Created or repaired" "createdOrRepaired"
    Write-Count $seedResult "Skipped because already seeded" "skippedBecauseAlreadySeeded"
    Write-Count $seedResult "Records created" "recordsCreated"
    Write-Count $seedResult "Records repaired" "recordsRepaired"
    Write-Count $seedResult "Users total" "usersTotal"
    Write-Count $seedResult "Properties total" "propertiesTotal"
    Write-Count $seedResult "Units total" "unitsTotal"
    Write-Count $seedResult "Tenant assignments total" "tenantAssignmentsTotal"
    Write-Count $seedResult "Requests total" "requestsTotal"
    Write-Count $seedResult "Comments total" "commentsTotal"
    Write-Count $seedResult "Attachments total" "attachmentsTotal"
    Write-Count $seedResult "Message" "message"
}

if ($null -ne $accountsResult) {
    Write-Host ""
    Write-Host "== Demo Accounts Result =="
    Write-Count $accountsResult "Success" "success"
    Write-Count $accountsResult "Message" "message"
}

$hasExpectedPortfolioData = $null -ne $seedResult -and
    $seedResult.success -eq $true -and
    $seedResult.usersTotal -ge 5 -and
    $seedResult.propertiesTotal -ge 2 -and
    $seedResult.unitsTotal -ge 4 -and
    $seedResult.tenantAssignmentsTotal -ge 4 -and
    $seedResult.requestsTotal -ge 4 -and
    $seedResult.commentsTotal -ge 4 -and
    $seedResult.attachmentsTotal -ge 1

Write-Host ""
Write-Host "== Summary =="
if ($hasExpectedPortfolioData) {
    Write-Result "PASS" "Sprint 13 demo data" "Expected demo portfolio counts are present."
    exit 0
}

Write-Result "FAIL" "Sprint 13 demo data" "Expected demo portfolio counts were not confirmed."
exit 1

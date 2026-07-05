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
        $response = Invoke-RestMethod -Method Post -Uri $Uri -TimeoutSec 20
        Write-Result "PASS" $Name $Uri
        return $response
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        if ($null -ne $statusCode) {
            Write-Result "WARN" $Name ("HTTP {0}. Endpoint may require setup, authentication, or may not be available." -f $statusCode)
        }
        else {
            Write-Result "WARN" $Name "Could not call endpoint. Start the backend with the RDS connection string first."
        }

        return $null
    }
}

Write-Host "PropCare Cloud RDS Demo Data Seed"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script does not print database passwords or connection strings."

$seedResult = Invoke-SafePost "Seed demo data" "http://localhost:5015/api/seed/demo-data"
if ($null -ne $seedResult) {
    Write-Result "INFO" "Seed success" $seedResult.success
    Write-Result "INFO" "Seed skipped because already seeded" $seedResult.skippedBecauseAlreadySeeded
    Write-Result "INFO" "Seed message" $seedResult.message
}

$accountsResult = Invoke-SafePost "Ensure demo accounts" "http://localhost:5015/api/auth/ensure-demo-accounts"
if ($null -ne $accountsResult) {
    Write-Result "INFO" "Demo accounts success" $accountsResult.success
    Write-Result "INFO" "Demo accounts message" $accountsResult.message
}

if ($null -ne $seedResult) {
    Write-Result "PASS" "RDS demo data seed" "Seed endpoint responded safely."
    exit 0
}

Write-Result "FAIL" "RDS demo data seed" "Seed endpoint did not respond successfully."
exit 1

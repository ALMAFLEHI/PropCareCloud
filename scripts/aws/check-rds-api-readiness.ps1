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

function Invoke-SafeJsonGet {
    param(
        [string]$Name,
        [string]$Uri
    )

    try {
        $response = Invoke-RestMethod -Method Get -Uri $Uri -TimeoutSec 10
        Write-Result "PASS" $Name $Uri
        return $response
    }
    catch {
        Write-Result "FAIL" $Name "Could not call $Uri. Start the backend with the RDS connection string first."
        return $null
    }
}

Write-Host "PropCare Cloud RDS API Readiness Check"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))
Write-Host "This script does not print database passwords or connection strings."

$readiness = Invoke-SafeJsonGet "Database readiness endpoint" "http://localhost:5015/api/database/readiness"
if ($null -ne $readiness) {
    Write-Result "INFO" "Connection string configured" $readiness.connectionStringConfigured
    Write-Result "INFO" "AppDbContext registered" $readiness.appDbContextRegistered
    Write-Result "INFO" "Provider" $readiness.provider
    Write-Result "INFO" "Planned cloud provider" $readiness.plannedCloudProvider
    Write-Result "INFO" "Can connect" $readiness.canConnect
    Write-Result "INFO" "Pending migrations" $readiness.pendingMigrations
    Write-Result "INFO" "Applied migrations" $readiness.appliedMigrations
    Write-Result "INFO" "Message" $readiness.message
}

$status = Invoke-SafeJsonGet "Database status endpoint" "http://localhost:5015/api/database/status"
if ($null -ne $status) {
    Write-Result "INFO" "Status provider" $status.provider
    Write-Result "INFO" "Status planned cloud provider" $status.plannedCloudProvider
    Write-Result "INFO" "Status connection configured" $status.connectionStringConfigured
    Write-Result "INFO" "Migrations created" $status.migrationsCreated
}

if ($null -ne $readiness -and $null -ne $status) {
    Write-Result "PASS" "RDS API readiness check" "Endpoints responded safely."
    exit 0
}

Write-Result "FAIL" "RDS API readiness check" "One or more endpoints did not respond."
exit 1

$ErrorActionPreference = "Stop"

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

function Get-ConnectionParts {
    param([string]$ConnectionString)

    $parts = @{}
    foreach ($segment in $ConnectionString -split ";") {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $keyValue = $segment -split "=", 2
        if ($keyValue.Count -eq 2) {
            $key = $keyValue[0].Trim().ToLowerInvariant()
            $parts[$key] = $keyValue[1].Trim()
        }
    }

    return $parts
}

function Get-MaskedHost {
    param([string]$HostName)

    if ([string]::IsNullOrWhiteSpace($HostName)) {
        return "<missing>"
    }

    if ($HostName.Length -le 16) {
        return "<configured-rds-host>"
    }

    return ("{0}...{1}" -f $HostName.Substring(0, 6), $HostName.Substring($HostName.Length - 10))
}

Write-Host "PropCare Cloud RDS Environment Validation"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))

$connectionString = $env:PROPCLOUD_CONNECTION_STRING
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Result "FAIL" "PROPCLOUD_CONNECTION_STRING" "Environment variable is not set."
    Write-Host 'Set it locally only, for example:'
    Write-Host '$env:PROPCLOUD_CONNECTION_STRING="Host=<rds-endpoint>;Port=5432;Database=propcarecloud_db;Username=propcareadmin;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true"'
    exit 1
}

$parts = Get-ConnectionParts -ConnectionString $connectionString
$hostName = $parts["host"]
$databaseName = $parts["database"]
$username = $parts["username"]
$password = $parts["password"]
$sslMode = $parts["ssl mode"]

if ([string]::IsNullOrWhiteSpace($hostName)) {
    Write-Result "FAIL" "RDS host" "Connection string is missing Host."
    exit 1
}

$lowerHost = $hostName.ToLowerInvariant()
$localHosts = @("localhost", "127.0.0.1", "::1", "host.docker.internal")
if ($localHosts -contains $lowerHost) {
    Write-Result "FAIL" "RDS host" "Local database host detected. Use the Amazon RDS endpoint for Sprint 12."
    exit 1
}

if ($lowerHost -like "*.local" -or $lowerHost -like "local*") {
    Write-Result "FAIL" "RDS host" "Local-looking database host detected. Use the Amazon RDS endpoint for Sprint 12."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($databaseName)) {
    Write-Result "FAIL" "Database" "Connection string is missing Database."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($username)) {
    Write-Result "FAIL" "Username" "Connection string is missing Username."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($password)) {
    Write-Result "FAIL" "Password" "Connection string is missing Password. The value will not be printed."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($sslMode)) {
    Write-Result "WARN" "SSL Mode" "SSL Mode was not found. RDS validation recommends SSL Mode=Require."
}
else {
    Write-Result "PASS" "SSL Mode" "Configured"
}

Write-Result "PASS" "PROPCLOUD_CONNECTION_STRING" "Configured. Full value and password were not printed."
Write-Result "PASS" "Host" (Get-MaskedHost -HostName $hostName)
Write-Result "PASS" "Database" $databaseName
Write-Result "PASS" "Username" $username
exit 0

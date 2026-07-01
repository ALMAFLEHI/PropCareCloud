$ErrorActionPreference = "Continue"

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "== $Title =="
}

function Write-Result {
    param(
        [string]$Status,
        [string]$Name,
        [string]$Detail = ""
    )

    if ([string]::IsNullOrWhiteSpace($Detail)) {
        Write-Host ("{0,-8} {1}" -f $Status, $Name)
    }
    else {
        Write-Host ("{0,-8} {1}: {2}" -f $Status, $Name, $Detail)
    }
}

function Test-Tool {
    param(
        [string]$Name,
        [string]$Command,
        [string[]]$Arguments = @(),
        [string[]]$CandidatePaths = @(),
        [switch]$PreferCandidatePaths
    )

    $candidatePath = $CandidatePaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    $toolPath = $null

    if ($PreferCandidatePaths -and $candidatePath) {
        $toolPath = $candidatePath
    }
    else {
        $tool = Get-Command $Command -ErrorAction SilentlyContinue
        if ($null -ne $tool) {
            $toolPath = $tool.Source
        }
        elseif ($candidatePath) {
            $toolPath = $candidatePath
        }
    }

    if ([string]::IsNullOrWhiteSpace($toolPath)) {
        Write-Result "MISSING" $Name "Command '$Command' was not found"
        return @{
            Name = $Name
            Found = $false
            Detail = "Command '$Command' was not found"
        }
    }

    try {
        $rawOutput = & $toolPath @Arguments 2>&1
        $output = @($rawOutput | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) {
                $_.Exception.Message
            }
            else {
                $_.ToString()
            }
        })
        $detailLines = @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 5)
        $detail = ($detailLines -join " ").Trim()
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = $toolPath
        }

        $exitCode = $LASTEXITCODE
        if ($null -ne $exitCode -and $exitCode -ne 0) {
            Write-Result "MISSING" $Name ("Command failed with exit code {0}. {1}" -f $exitCode, $detail)
            return @{
                Name = $Name
                Found = $false
                Detail = $detail
            }
        }

        Write-Result "PASS" $Name $detail
        return @{
            Name = $Name
            Found = $true
            Detail = $detail
        }
    }
    catch {
        $detail = $_.Exception.Message
        Write-Result "MISSING" $Name $detail
        return @{
            Name = $Name
            Found = $false
            Detail = $detail
        }
    }
}

function Test-VisualStudio2022 {
    $detected = @()
    $programFilesX86 = ${env:ProgramFiles(x86)}

    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path -LiteralPath $vswhere) {
            try {
                $paths = & $vswhere -products * -version "[17.0,18.0)" -property installationPath 2>$null
                if ($paths) {
                    $detected += $paths
                }
            }
            catch {
                Write-Result "INFO" "Visual Studio 2022" "vswhere was found but detection failed: $($_.Exception.Message)"
            }
        }
    }

    $commonPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools"
    )

    foreach ($path in $commonPaths) {
        if (Test-Path -LiteralPath $path) {
            $detected += $path
        }
    }

    $detected = $detected | Sort-Object -Unique
    if ($detected.Count -gt 0) {
        Write-Result "PASS" "Visual Studio 2022" ($detected -join "; ")
        return @{
            Name = "Visual Studio 2022"
            Found = $true
            Detail = ($detected -join "; ")
        }
    }

    Write-Result "MISSING" "Visual Studio 2022" "No installation detected by vswhere or common install paths"
    return @{
        Name = "Visual Studio 2022"
        Found = $false
        Detail = "No installation detected"
    }
}

Write-Host "PropCare Cloud Environment Check"
Write-Host ("Run time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"))

Write-Section "System Information"
$os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
if ($os) {
    Write-Result "INFO" "Operating system" ("{0} {1}" -f $os.Caption, $os.Version)
}
else {
    Write-Result "INFO" "Operating system" ([System.Environment]::OSVersion.VersionString)
}
Write-Result "INFO" "Current working directory" (Get-Location).Path
Write-Result "INFO" "PowerShell version" $PSVersionTable.PSVersion.ToString()

Write-Section "Critical Tools"
$git = Test-Tool "Git" "git" @("--version")
$dotnet = Test-Tool ".NET SDK" "dotnet" @("--version")
if ($dotnet.Found) {
    $sdks = & dotnet --list-sdks 2>&1
    if ($sdks) {
        Write-Result "INFO" "Installed .NET SDK list" (($sdks | Out-String).Trim())
    }
    else {
        Write-Result "INFO" "Installed .NET SDK list" "No SDK entries returned"
    }
}
else {
    Write-Result "MISSING" "Installed .NET SDK list" "dotnet command is not available or not usable"
}
$node = Test-Tool "Node.js" "node" @("--version")
$npm = Test-Tool -Name "npm" -Command "npm" -Arguments @("--version") -CandidatePaths @("C:\Program Files\nodejs\npm.cmd") -PreferCandidatePaths
$aws = Test-Tool -Name "AWS CLI" -Command "aws" -Arguments @("--version") -CandidatePaths @("C:\Program Files\Amazon\AWSCLIV2\aws.exe")

Write-Section "Recommended Tools"
$code = Test-Tool "Visual Studio Code" "code" @("--version")
$vs2022 = Test-VisualStudio2022
$psql = Test-Tool "PostgreSQL psql" "psql" @("--version")

Write-Section "Summary"
$criticalTools = @($git, $dotnet, $node, $npm, $aws)
$recommendedTools = @($code, $vs2022, $psql)

Write-Host "Critical tools:"
foreach ($tool in $criticalTools) {
    $status = if ($tool.Found) { "PASS" } else { "MISSING" }
    Write-Host ("- {0}: {1}" -f $tool.Name, $status)
}

Write-Host "Recommended tools:"
foreach ($tool in $recommendedTools) {
    $status = if ($tool.Found) { "PASS" } else { "MISSING" }
    Write-Host ("- {0}: {1}" -f $tool.Name, $status)
}

$missingCritical = @($criticalTools | Where-Object { -not $_.Found })
if ($missingCritical.Count -eq 0) {
    Write-Result "PASS" "Overall critical environment" "All critical tools are available"
}
else {
    Write-Result "MISSING" "Overall critical environment" ("Missing critical tools: {0}" -f (($missingCritical | ForEach-Object { $_.Name }) -join ", "))
}

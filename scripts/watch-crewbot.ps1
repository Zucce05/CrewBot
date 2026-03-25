[CmdletBinding()]
param(
    [int]$MaxSilenceMinutes = 15,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeDirectory = Join-Path $repoRoot 'CrewBot\json\runtime'
$logsDirectory = Join-Path $repoRoot 'CrewBot\json\logs'
$statePath = Join-Path $runtimeDirectory 'crewbot-process.json'
$monitorLogPath = Join-Path $logsDirectory 'MonitorLog.txt'
$startScriptPath = Join-Path $PSScriptRoot 'start-crewbot.ps1'

function Write-MonitorLog {
    param([string]$Message)

    New-Item -ItemType Directory -Force -Path $runtimeDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null

    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -LiteralPath $monitorLogPath -Value "$timestamp $Message"
}

function Get-ProcessRecord {
    if (-not (Test-Path -LiteralPath $statePath)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    }
    catch {
        Write-MonitorLog "State file could not be read and will be ignored: $($_.Exception.Message)"
        return $null
    }
}

function Get-ActiveRecordedProcess {
    $record = Get-ProcessRecord
    if ($null -eq $record) {
        return $null
    }

    try {
        $process = Get-Process -Id ([int]$record.Pid) -ErrorAction Stop
        if ($null -eq $process.StartTime) {
            return $null
        }

        $actualStart = $process.StartTime.ToUniversalTime().ToString('o')
        if ($actualStart -ne $record.StartedAtUtc) {
            return $null
        }

        return $process
    }
    catch {
        return $null
    }
}

function Get-NewestBotLogTimeUtc {
    $logFiles = Get-ChildItem -LiteralPath $logsDirectory -Filter '*.txt' -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne 'MonitorLog.txt' }
    if ($null -eq $logFiles -or $logFiles.Count -eq 0) {
        return $null
    }

    return ($logFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
}

$process = Get-ActiveRecordedProcess
if ($null -eq $process) {
    Write-MonitorLog "CrewBot process record is missing or stale. Starting bot."
    & $startScriptPath -Quiet
    exit $LASTEXITCODE
}

$newestLogTimeUtc = Get-NewestBotLogTimeUtc
if ($null -ne $newestLogTimeUtc) {
    $silenceWindow = New-TimeSpan -Start $newestLogTimeUtc -End (Get-Date).ToUniversalTime()
    if ($silenceWindow.TotalMinutes -gt $MaxSilenceMinutes) {
        Write-MonitorLog "CrewBot PID $($process.Id) appears stale after $([math]::Round($silenceWindow.TotalMinutes, 1)) minutes without log activity. Restarting bot."
        & $startScriptPath -ForceRestart -Quiet
        exit $LASTEXITCODE
    }
}

Write-MonitorLog "CrewBot PID $($process.Id) passed watchdog check."
if (-not $Quiet) {
    Write-Output "CrewBot PID $($process.Id) is healthy."
}

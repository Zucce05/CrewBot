[CmdletBinding()]
param(
    [switch]$ForceRestart,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeDirectory = Join-Path $repoRoot 'CrewBot\json\runtime'
$logsDirectory = Join-Path $repoRoot 'CrewBot\json\logs'
$statePath = Join-Path $runtimeDirectory 'crewbot-process.json'
$monitorLogPath = Join-Path $logsDirectory 'MonitorLog.txt'
$preferredDllPath = Join-Path $repoRoot 'artifacts\bin\CrewBot.live\CrewBot.dll'

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

function Resolve-BotDllPath {
    if (Test-Path -LiteralPath $preferredDllPath) {
        return $preferredDllPath
    }

    $candidate = Get-ChildItem -Path (Join-Path $repoRoot 'artifacts\bin') -Filter 'CrewBot.dll' -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch 'CrewBot\.Tests' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "Could not find a CrewBot.dll build output."
    }

    return $candidate.FullName
}

function Save-ProcessRecord {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$DllPath
    )

    New-Item -ItemType Directory -Force -Path $runtimeDirectory | Out-Null

    $record = [pscustomobject]@{
        Pid = $Process.Id
        StartedAtUtc = $Process.StartTime.ToUniversalTime().ToString('o')
        DllPath = $DllPath
        WorkingDirectory = $repoRoot
        UpdatedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    }

    $record | ConvertTo-Json | Set-Content -LiteralPath $statePath
}

$existingProcess = Get-ActiveRecordedProcess
if ($existingProcess -and -not $ForceRestart) {
    Write-MonitorLog "CrewBot already running with PID $($existingProcess.Id)."
    if (-not $Quiet) {
        Write-Output "CrewBot already running with PID $($existingProcess.Id)."
    }

    exit 0
}

if ($existingProcess -and $ForceRestart) {
    Write-MonitorLog "Stopping CrewBot PID $($existingProcess.Id) before restart."
    Stop-Process -Id $existingProcess.Id -Force
    Start-Sleep -Seconds 2
}

$dllPath = Resolve-BotDllPath
$dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source
$process = Start-Process -FilePath $dotnetPath -ArgumentList @($dllPath) -WorkingDirectory $repoRoot -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 3

$startedProcess = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
if ($null -eq $startedProcess) {
    throw "CrewBot failed to stay running after startup."
}

Save-ProcessRecord -Process $startedProcess -DllPath $dllPath
Write-MonitorLog "Started CrewBot PID $($startedProcess.Id) using $dllPath."

if (-not $Quiet) {
    Write-Output "Started CrewBot PID $($startedProcess.Id)."
}

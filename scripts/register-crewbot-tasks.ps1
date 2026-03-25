[CmdletBinding()]
param(
    [int]$WatchdogFrequencyMinutes = 5,
    [int]$MaxSilenceMinutes = 15
)

$ErrorActionPreference = 'Stop'

$powerShellPath = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
$startScriptPath = Join-Path $PSScriptRoot 'start-crewbot.ps1'
$watchScriptPath = Join-Path $PSScriptRoot 'watch-crewbot.ps1'
$startTaskName = 'CrewBot Start'
$watchTaskName = 'CrewBot Watchdog'

if ($WatchdogFrequencyMinutes -lt 1) {
    throw 'WatchdogFrequencyMinutes must be at least 1.'
}

if ($MaxSilenceMinutes -lt 1) {
    throw 'MaxSilenceMinutes must be at least 1.'
}

$startCommand = "`"$powerShellPath`" -NoProfile -ExecutionPolicy Bypass -File `"$startScriptPath`" -Quiet"
$watchCommand = "`"$powerShellPath`" -NoProfile -ExecutionPolicy Bypass -File `"$watchScriptPath`" -Quiet -MaxSilenceMinutes $MaxSilenceMinutes"

schtasks /Create /TN $startTaskName /TR $startCommand /SC ONLOGON /F | Out-Null
schtasks /Create /TN $watchTaskName /TR $watchCommand /SC MINUTE /MO $WatchdogFrequencyMinutes /F | Out-Null

Write-Output "Registered task '$startTaskName'."
Write-Output "Registered task '$watchTaskName' to run every $WatchdogFrequencyMinutes minute(s)."

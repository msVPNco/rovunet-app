# Public 0.2.x package that carries the Rovunet Free v2 official source.
#
# This wrapper exists so the owner runs the one step that reads the local Free
# secrets file, without the assistant handling that file. It passes the same
# arguments to package-windows.ps1 that a normal public build uses, plus
# -PublicFree. The trust values are read from the owner's own
# %LOCALAPPDATA%\Rovunet\DebugSecrets\free-v2.local.json at build time and are
# never written into the repository, the source archive or this script.
#
# Finish and commit every edit first: package-windows.ps1 aborts if the source
# changes while it runs.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$SourceRevision,
  [Parameter(Mandatory=$true)][string]$OutputDirectory,
  [string]$Dotnet = 'H:\02-Rovunet-Lead\Windows\tools\dotnet\dotnet.exe',
  [string]$MakeNsis = 'H:\02-Rovunet-Lead\Windows\tools\nsis-tauri\nsis-3.09\Bin\makensis.exe',
  [string]$EngineDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'engines\windows-x64'),
  [string]$FreeSecretsPath = (Join-Path $env:LOCALAPPDATA 'Rovunet\DebugSecrets\free-v2.local.json')
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'package-windows.ps1') `
  -Dotnet $Dotnet `
  -MakeNsis $MakeNsis `
  -EngineDirectory $EngineDirectory `
  -OutputDirectory $OutputDirectory `
  -SourceRevision $SourceRevision `
  -PublicFree `
  -FreeSecretsPath $FreeSecretsPath
if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }

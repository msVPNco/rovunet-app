# Local-only installer for a Debug test build of Windows Rovunet.
#
# This is NOT the public packaging path. It wraps an already-published Debug
# payload (which reads its Free v2 debug values from the owner's local
# %LOCALAPPDATA%\Rovunet\DebugSecrets\free-v2.local.json at runtime) into the
# same NSIS backend + Rovunet.Setup installer the public build uses, with a
# distinct version string so the artifact is never mistaken for a release.
#
# Never upload the produced setup: a Debug build is not a public package.
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Dotnet,
  [Parameter(Mandatory=$true)][string]$MakeNsis,
  [Parameter(Mandatory=$true)][string]$Staging,
  [Parameter(Mandatory=$true)][string]$OutputDirectory,
  [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$SourceRevision
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$sourceRoot=Split-Path -Parent $PSScriptRoot
$staging=[IO.Path]::GetFullPath($Staging)
$outputRoot=[IO.Path]::GetFullPath($OutputDirectory)
if(-not (Test-Path (Join-Path $staging 'Rovunet.exe'))){throw 'Staging folder has no Rovunet.exe.'}
if(Test-Path $outputRoot){throw 'Output directory must not exist yet.'}
$version='0.1.7-beta-free-debug'
New-Item -ItemType Directory -Path $outputRoot | Out-Null
Push-Location $sourceRoot
try {
  # This artifact is a debug test build; the provenance says so plainly.
  $provenance=@{
    version=$version; platform='windows-x64'; source_commit=$SourceRevision;
    source_origin='local-maintenance-git-commit'; build_configuration='Debug';
    debug_key='free-v2 runtime file under %LOCALAPPDATA%\Rovunet\DebugSecrets';
    public_package=$false; signed=$false; published=$false;
    free_scope='Debug only: the AES key is read from the local secrets file and is not compiled in';
    sdk=(& $Dotnet --version)
  }
  $provenance | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $staging 'BUILD-PROVENANCE.json') -Encoding utf8

  $manifest=Join-Path $outputRoot 'delete-manifest.nsh'
  $lines=@(Get-ChildItem -LiteralPath $staging -File -Recurse | Where-Object { $_.FullName -notin @((Join-Path $staging 'Rovunet.exe'),(Join-Path $staging 'review-install.marker')) } | ForEach-Object {
    $relative=$_.FullName.Substring($staging.Length+1)
    'IfFileExists "$INSTDIR\'+$relative+'" 0 +3'
    'Delete "$INSTDIR\'+$relative+'"'
    'IfErrors refused'
  })
  $lines+=@(Get-ChildItem -LiteralPath $staging -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object { 'RMDir "$INSTDIR\'+$_.FullName.Substring($staging.Length+1)+'"' })
  $lines | Set-Content -LiteralPath $manifest -Encoding utf8

  $preflight=Join-Path $outputRoot 'preflight-manifest.nsh'
  $index=0
  $guards=@(Get-ChildItem -LiteralPath $staging -File -Recurse | ForEach-Object {
    $index++
    $relative=$_.FullName.Substring($staging.Length+1)
    'IfFileExists "$INSTDIR\'+$relative+'" 0 preflight_'+$index
    'System::Call ''kernel32::CreateFileW(w "$INSTDIR\'+$relative+'", i 0x40000000, i 0, p 0, i 3, i 128, p 0)p.r0'''
    'StrCmp $0 -1 refused'
    'System::Call ''kernel32::CloseHandle(p r0)'''
    'preflight_'+$index+':'
  })
  $guards | Set-Content -LiteralPath $preflight -Encoding utf8

  $backend=Join-Path $outputRoot 'review-backend.exe'
  & $MakeNsis /V2 "/DREVIEW_VERSION=$version" "/DSTAGING=$staging" "/DOUTPUT=$backend" "/DDELETE_MANIFEST=$manifest" "/DPREFLIGHT_MANIFEST=$preflight" 'installer/ReviewBackend.nsi'
  if($LASTEXITCODE -ne 0){throw 'NSIS failed.'}

  & $Dotnet publish installer/Rovunet.Setup -c Release -r win-x64 --self-contained true -m:1 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true "-p:SourceRevisionId=$SourceRevision" "-p:BackendPath=$backend" -o (Join-Path $outputRoot 'setup')
  if($LASTEXITCODE -ne 0){throw 'Setup publish failed.'}
  Get-FileHash -LiteralPath (Join-Path $outputRoot 'setup/Rovunet.Setup.exe') | Select-Object Algorithm,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'setup-sha256.json')
  Write-Output ('debug setup: '+(Join-Path $outputRoot 'setup\Rovunet.Setup.exe'))
} finally { Pop-Location }

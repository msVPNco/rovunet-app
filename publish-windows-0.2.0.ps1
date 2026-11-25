# Publish Rovunet Windows 0.2.0 as a public release.
#
# Uses the GitHub credential already stored in Windows Credential Manager for git.
# Never prints the token and never writes it to disk. Build the installer with
# windows/scripts/package-windows-free-public.ps1, which carries the Rovunet Free
# v2 trust values that this release is meant to ship.

$ErrorActionPreference = 'Stop'

$git    = 'C:\Users\Mohsen\AppData\Local\Temp\rovunet_git\git\cmd\git.exe'
$build  = 'H:\02-Rovunet-Lead\Windows\versions\v0.2.0\candidate1'
$body   = 'H:\02-Rovunet-Lead\Windows\work\next-0.1.5\windows\docs\releases\windows-0.2.0.md'
$tag    = 'windows-v0.2.0'
$name   = 'Rovunet-Windows-0.2.0-x64-Setup.exe'
$nice   = 'Rovunet Windows 0.2.0 · x64'

$setup = Join-Path $build 'setup\Rovunet.Setup.exe'
if (-not (Test-Path $setup)) { throw "Installer not found: $setup" }
if (-not (Test-Path (Join-Path $build 'PUBLIC-FREE-BUILD.txt'))) { throw 'This folder was not built with -PublicFree.' }
foreach ($leak in @('free-v2.local.props', 'local.properties')) {
    if (Test-Path (Join-Path (Split-Path (Split-Path $build -Parent) -Parent) $leak)) { throw "Refusing to publish with $leak in the tree." }
}
$stage = Join-Path $build 'public-assets'
New-Item -ItemType Directory -Force $stage | Out-Null
$installer = Join-Path $stage $name
Copy-Item $setup $installer -Force
foreach ($extra in @('Rovunet-Windows-0.2.0-source.zip', 'setup-sha256.json', 'SOURCE-SHA256.json', 'payload\BUILD-PROVENANCE.json', 'PUBLIC-FREE-BUILD.txt')) {
    $path = Join-Path $build $extra
    if (Test-Path $path) { Copy-Item $path $stage -Force } else { Write-Warning "missing $extra" }
}
$sums = @(Get-ChildItem $stage -File | Where-Object { $_.Name -notlike '*.sha256' -and $_.Name -ne 'SHA256SUMS.txt' } | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name
})
$sums | Set-Content (Join-Path $stage 'SHA256SUMS.txt') -Encoding utf8
Write-Output ("installer bytes = " + (Get-Item $installer).Length)
Write-Output ("installer sha256 = " + (Get-FileHash $installer -Algorithm SHA256).Hash.ToLower())

$env:GIT_TERMINAL_PROMPT = '0'
$raw = ("protocol=https`nhost=github.com`n`n" | & $git credential fill) 2>$null
$token = ($raw | Where-Object { $_ -like 'password=*' }) -replace '^password=',''
if ([string]::IsNullOrWhiteSpace($token)) { throw "No stored GitHub credential found." }
$headers = @{ Authorization = "Bearer $token"; 'User-Agent' = 'rovunet-release'; Accept = 'application/vnd.github+json' }

$existing = $null
try { $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/msVPNco/rovunet-app/releases/tags/$tag" -Headers $headers } catch { }

if ($existing) { $release = $existing; Write-Output "release exists: id=$($release.id)" }
else {
    $payload = @{
        tag_name    = $tag
        name        = $nice
        body        = [System.IO.File]::ReadAllText($body)
        draft       = $false
        prerelease  = $false
        make_latest = 'false'
    } | ConvertTo-Json -Depth 4
    $release = Invoke-RestMethod -Method Post -Uri 'https://api.github.com/repos/msVPNco/rovunet-app/releases' `
        -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($payload))
    Write-Output "release created: id=$($release.id) prerelease=$($release.prerelease)"
}

Get-ChildItem $stage | Sort-Object Length -Descending | Select-Object -First 8 | ForEach-Object {
    $assetName = $_.Name
    $present = $release.assets | Where-Object { $_.name -eq $assetName }
    if ($present) { Write-Output "skip: $assetName"; return }
    $url = "https://uploads.github.com/repos/msVPNco/rovunet-app/releases/$($release.id)/assets?name=$assetName"
    $ct = if ($assetName -like '*.exe') { 'application/vnd.microsoft.portable-executable' } else { 'application/octet-stream' }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $r = Invoke-WebRequest -Method Post -Uri $url -Headers $headers -ContentType $ct -InFile $_.FullName -TimeoutSec 3600
    $sw.Stop()
    $asset = $r.Content | ConvertFrom-Json
    Write-Output ("uploaded {0} {1} MB state={2} {3}s digest={4}" -f $asset.name, [math]::Round($asset.size/1MB,1), $asset.state, [int]$sw.Elapsed.TotalSeconds, $asset.digest)
}

$final = Invoke-RestMethod -Uri "https://api.github.com/repos/msVPNco/rovunet-app/releases/tags/$tag" -Headers $headers
Write-Output "tag=$($final.tag_name) prerelease=$($final.prerelease) draft=$($final.draft)"
$latest = Invoke-RestMethod -Uri 'https://api.github.com/repos/msVPNco/rovunet-app/releases/latest' -Headers $headers
Write-Output "repo_latest=$($latest.tag_name)"
$final.assets | ForEach-Object { Write-Output ("asset {0} {1} bytes {2}" -f $_.name, $_.size, $_.state) }

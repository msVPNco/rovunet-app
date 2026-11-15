param([Parameter(Mandatory=$true)][string]$Go,[Parameter(Mandatory=$true)][string]$Output,[string]$CorrespondingSourceDirectory)
$ErrorActionPreference='Stop'
$Go=[IO.Path]::GetFullPath($Go);$Output=[IO.Path]::GetFullPath($Output)
if((Get-FileHash -LiteralPath $Go).Hash.ToLowerInvariant() -ne '83b7ba66215e696414fa49462d0db2da4f4cf15c7bebd42f0b189fa04866999d'){throw 'Pinned Go compiler required'}
if((& $Go version).Trim() -ne 'go version go1.27.3 windows/amd64'){throw 'Pinned Go version required'}
if(Test-Path -LiteralPath $Output){throw 'Choose a fresh executable output path'}
$env:CGO_ENABLED='0';$env:GOPROXY='off'
$native=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\native\handshake30'))
$buildRoot=if($CorrespondingSourceDirectory){[IO.Path]::GetFullPath($CorrespondingSourceDirectory)}else{$native}
if(!(Test-Path -LiteralPath (Join-Path $buildRoot 'vendor\modules.txt'))){throw 'Extract the accompanying complete source ZIP and pass its handshake30 directory'}
foreach($file in (Get-ChildItem -LiteralPath $native -File|Where-Object{$_.Extension -eq '.go'-or $_.Name -in @('go.mod','go.sum')})){if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $buildRoot $file.Name)).Hash){throw 'Corresponding Go source differs from checkout'}}
Push-Location $buildRoot
try{
 & $Go build -mod=vendor -trimpath -buildvcs=false '-ldflags=-s -w' -o $Output .
 if($LASTEXITCODE -ne 0){throw 'Handshake helper build failed'}
 if((Get-FileHash -LiteralPath $Output).Hash.ToLowerInvariant() -ne '6bbc096a48a349de766d8ac5d84877d3981fe919649ca0b832acff33889b1b1a'){throw 'Handshake helper hash mismatch'}
}finally{Pop-Location}

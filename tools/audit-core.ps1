param([string]$Binary='bin/Release/ScreenCraft.exe',[string]$Output='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$binaryPath=if([IO.Path]::IsPathRooted($Binary)){[IO.Path]::GetFullPath($Binary)}else{Join-Path $root $Binary}
$manifest=[IO.Path]::ChangeExtension($binaryPath,'.sources.json')
if(!(Test-Path -LiteralPath $binaryPath) -or !(Test-Path -LiteralPath $manifest)){throw 'Build the binary first; its compiler input manifest must accompany it.'}
$record=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
if((Get-FileHash -LiteralPath $binaryPath -Algorithm SHA256).Hash -ne $record.BinarySHA256){throw 'The binary does not match its compiler manifest.'}
foreach($entry in $record.CompilerInputs){
    if($entry.Path -notmatch '^(src/(App|Core)|tests)/[^.].*\.cs$'){throw "Unexpected compiler input: $($entry.Path)"}
    if((Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256){throw "Source changed after compilation: $($entry.Path)"}
}
if(@(Get-ChildItem (Join-Path $root 'src/Upstream') -Recurse -Filter '*.cs').Count){throw 'Legacy runtime source remains.'}
if(@(Get-ChildItem (Join-Path $root 'src') -Recurse -Filter '*.cs' | Select-String -Pattern '\b(namespace|using)\s+Stackshot\b|\bStackshot\.').Count){throw 'Legacy runtime namespace remains.'}
if($Output){$destination=if([IO.Path]::IsPathRooted($Output)){$Output}else{Join-Path $root $Output};$record | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $destination -Encoding UTF8}
Write-Output ('PASS core boundary: '+$record.CompilerInputs.Count+' compiler inputs; no upstream C# sources or legacy runtime namespace; input hashes and binary match.')

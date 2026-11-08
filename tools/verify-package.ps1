param([switch]$Rebuild,[string]$DistributionDirectory='dist')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.IO.Compression.FileSystem
$dist=if([IO.Path]::IsPathRooted($DistributionDirectory)){[IO.Path]::GetFullPath($DistributionDirectory)}else{[IO.Path]::GetFullPath((Join-Path $root $DistributionDirectory))}
$portable=Join-Path $dist 'ScreenCraft-Portable-win-x64.zip'
$zip=[IO.Compression.ZipFile]::OpenRead($portable)
try{
 $entry=$zip.GetEntry('ScreenCraft-Portable/ScreenCraft.exe')
 if(!$entry){throw 'Portable executable missing'}
 $sha=[Security.Cryptography.SHA256]::Create()
 $stream=$entry.Open()
 try{$archiveHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
 if($archiveHash -ne (Get-FileHash (Join-Path $dist 'ScreenCraft.exe')).Hash){throw 'Portable binary does not match published binary'}
 foreach($file in 'ScreenCraft.exe.config','portable.flag','LICENSE','THIRD_PARTY_NOTICES.md','USER_GUIDE.md','KNOWN_LIMITATIONS.md'){if(!$zip.GetEntry('ScreenCraft-Portable/'+$file)){throw "Missing $file"}}
 if([Version](Get-Item (Join-Path $dist 'ScreenCraft.exe')).VersionInfo.FileVersion -ge [Version]'0.8.0.0' -and !$zip.GetEntry('ScreenCraft-Portable/COLORBREWER_LICENSE.txt')){throw 'ColorBrewer license missing'}
}finally{$zip.Dispose()}
foreach($line in Get-Content (Join-Path $dist 'SHA256SUMS.txt')){if($line -match '^([a-f0-9]{64})  (.+)$'){if((Get-FileHash -LiteralPath (Join-Path $dist $Matches[2]) -Algorithm SHA256).Hash -ne $Matches[1]){throw "Hash mismatch: $($Matches[2])"}}else{throw 'Invalid checksum line'}}
$sourceZip=[IO.Compression.ZipFile]::OpenRead((Join-Path $dist 'ScreenCraft-Source.zip'))
try{foreach($file in Get-ChildItem @((Join-Path $root 'src'),(Join-Path $root 'tests'),(Join-Path $root 'tools')) -Recurse -File){$relative=$file.FullName.Substring($root.Length+1).Replace('\','/');$entry=$sourceZip.GetEntry('ScreenCraft-Source/'+$relative);if(!$entry){throw "Source file missing from ZIP: $relative"};$stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create();try{$hash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()};if($hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){throw "Source ZIP is stale: $relative"}}}finally{$sourceZip.Dispose()}
Write-Output 'PASS portable contents and all published SHA-256 checksums'
$version=[Version](Get-Item (Join-Path $dist 'ScreenCraft.exe')).VersionInfo.FileVersion
if($version -ge [Version]'0.9.0.0'){
 & (Join-Path $PSScriptRoot 'inspect-installer.ps1') -InstallerPath (Join-Path $dist 'ScreenCraft-Setup-PerUser-win-x64.msi')
}
if($Rebuild){
 $base=[IO.Path]::GetFullPath((Join-Path $root '.build'))
 $repro=[IO.Path]::GetFullPath((Join-Path $base ('repro-'+[guid]::NewGuid().ToString('N'))))
 if(!$repro.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Rebuild directory outside workspace'}
 Expand-Archive -LiteralPath (Join-Path $dist 'ScreenCraft-Source.zip') -DestinationPath $repro
 $checkout=Join-Path $repro 'ScreenCraft-Source'
 $prior=$env:SCREENCRAFT_TEST_OUTPUT
 try{$env:SCREENCRAFT_TEST_OUTPUT=Join-Path $checkout 'docs\evidence\automated'; & (Join-Path $checkout 'build.ps1') -Test; & (Join-Path $checkout 'build.ps1');if($version -ge [Version]'0.9.0.0'){& (Join-Path $checkout 'tools\build-installer.ps1') -DistributionDirectory (Join-Path $checkout 'dist');& (Join-Path $checkout 'tools\inspect-installer.ps1') -InstallerPath (Join-Path $checkout 'dist\ScreenCraft-Setup-PerUser-win-x64.msi')}}
 finally{$env:SCREENCRAFT_TEST_OUTPUT=$prior}
 Write-Output "PASS source ZIP independently rebuilt and tested: $checkout"
}

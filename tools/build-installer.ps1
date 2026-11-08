param([string]$DistributionDirectory='dist',[switch]$IsolatedTest,[string]$VersionOverride)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$wix=Join-Path $root '.tools\wix314'
$zip=Join-Path $root '.tools\wix314-binaries.zip'
$expected='6ac824e1642d6f7277d0ed7ea09411a508f6116ba6fae0aa5f2c7daa2ff43d31'
if(!(Test-Path (Join-Path $wix 'candle.exe'))){
 New-Item -ItemType Directory -Force (Split-Path $zip -Parent) | Out-Null
 if(!(Test-Path $zip)){Invoke-WebRequest 'https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip' -OutFile $zip}
 if((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected){throw 'WiX archive checksum mismatch'}
 Expand-Archive -LiteralPath $zip -DestinationPath $wix -Force
}
# The checked archive is build tooling only, never a runtime dependency.
if(!(Test-Path $zip) -or (Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected){throw 'Verified WiX archive required; restore .tools/wix314-binaries.zip'}
$dist=if([IO.Path]::IsPathRooted($DistributionDirectory)){[IO.Path]::GetFullPath($DistributionDirectory)}else{[IO.Path]::GetFullPath((Join-Path $root $DistributionDirectory))}
New-Item -ItemType Directory -Force $dist | Out-Null
$exe=Join-Path $root 'bin\Release\ScreenCraft.exe'
$version=if($VersionOverride){$VersionOverride}else{([Version](Get-Item $exe).VersionInfo.FileVersion).ToString(3)}
$build=Join-Path $root ('.build\msi-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $build | Out-Null
$name='ScreenCraft';$registry='Software\ScreenCraft\Install';$upgrade='28A312CE-7AC5-43A6-94AE-E6A2309E99A1';$main='9187C9C1-A5A2-4854-A0C7-4DDE763C25A2';$shortcut='1A647859-2C43-4AB8-B40E-519E89C66C32'
if($IsolatedTest){$name='ScreenCraft Installer Test';$registry='Software\ScreenCraftInstallerTests\Package';$upgrade='BC30869A-B705-432F-859B-951382A20125';$main='3B0714FD-D904-4220-A897-4BC717ABAB37';$shortcut='D50911CE-18D7-4C9D-8758-BC250DF7341D'}
$obj=Join-Path $build 'ScreenCraft.wixobj'
$directoryName=if($IsolatedTest){'ScreenCraftInstallerTest'}else{'ScreenCraft'}
$arguments=@('-nologo',('-dVersion='+$version),('-dProductName='+$name),('-dDirectoryName='+$directoryName),('-dUpgradeCode='+$upgrade),('-dMainGuid='+$main),('-dShortcutGuid='+$shortcut),('-dRegistryKey='+$registry),('-dRoot='+$root),('-dPayload='+(Split-Path $exe -Parent)),('-dIcon='+(Join-Path $root 'src\Assets\ScreenCraft.ico')),'-out',$obj)
if($IsolatedTest){$arguments+='-dIsolatedTest=1'}
& (Join-Path $wix 'candle.exe') @arguments (Join-Path $PSScriptRoot 'ScreenCraft.wxs')
if($LASTEXITCODE -ne 0){throw 'WiX compilation failed'}
$msi=Join-Path $dist $(if($IsolatedTest){'ScreenCraft-InstallerTest.msi'}else{'ScreenCraft-Setup-PerUser-win-x64.msi'})
# ICE90: USERMENUDIR is public for isolated testing; ALLUSERS is forbidden by launch condition.
& (Join-Path $wix 'light.exe') -nologo -ext WixUIExtension -spdb -sice:ICE91 -sice:ICE61 -sice:ICE69 -sice:ICE90 -cultures:zh-CN -out $msi $obj
if($LASTEXITCODE -ne 0){throw 'WiX linking failed'}
Write-Output ('Built per-user MSI: '+$msi)

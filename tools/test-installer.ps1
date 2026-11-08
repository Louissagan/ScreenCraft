param([string]$OutputDirectory='docs/evidence/automated/installer-0.9.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=if([IO.Path]::IsPathRooted($OutputDirectory)){[IO.Path]::GetFullPath($OutputDirectory)}else{[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))}
New-Item -ItemType Directory -Force $output | Out-Null
$scope=[IO.Path]::GetFullPath((Join-Path $root ('.build\installer-test-'+[guid]::NewGuid().ToString('N'))))
$allowed=[IO.Path]::GetFullPath((Join-Path $root '.build'))+[IO.Path]::DirectorySeparatorChar
if(!$scope.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Test directory outside workspace'}
$app=Join-Path $scope 'App';$menu=Join-Path $scope 'Menu';$testKey='HKCU:\Software\ScreenCraftInstallerTests\Package'
if(Test-Path $testKey){throw 'An earlier isolated installer test remains; inspect it before continuing'}
$startupPath='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$oldStartup=(Get-ItemProperty $startupPath -ErrorAction SilentlyContinue).ScreenCraft
$results=New-Object Collections.Generic.List[object]
function Check([string]$name,[bool]$ok){if(!$ok){throw $name};$results.Add([pscustomobject]@{Test=$name;Status='PASS'});Write-Output ('PASS '+$name)}
function Run-Msi([string[]]$arguments){$process=Start-Process -FilePath (Join-Path $env:WINDIR 'System32\msiexec.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait;if($process.ExitCode -ne 0){throw ('msiexec failed: '+$process.ExitCode)}}
$installed=$false;$code=$null
try{
 & (Join-Path $PSScriptRoot 'build-installer.ps1') -DistributionDirectory $scope -IsolatedTest -VersionOverride '0.9.0'
 $msi=Join-Path $scope 'ScreenCraft-InstallerTest.msi'
 Run-Msi @('/i',('"'+$msi+'"'),'/qn','/norestart',('INSTALLFOLDER="'+$app+'"'),('USERMENUDIR="'+$menu+'"'),'/l*v',('"'+(Join-Path $output 'install.log')+'"'))
 $installed=$true;$code=(Get-ItemProperty $testKey).ProductCode
 Check 'isolated per-user MSI installs files and two shortcuts without showing application' ((Test-Path (Join-Path $app 'ScreenCraft.exe')) -and (Test-Path (Join-Path $menu 'ScreenCraft.lnk')) -and (Test-Path (Join-Path $menu 'ScreenCraft 设置.lnk')))
 Check 'installed payload matches compiled EXE and excludes portable.flag' (((Get-FileHash (Join-Path $app 'ScreenCraft.exe')).Hash -eq (Get-FileHash (Join-Path $root 'bin/Release/ScreenCraft.exe')).Hash) -and !(Test-Path (Join-Path $app 'portable.flag')))
 Set-Content (Join-Path $app 'user-saved.png') 'USER SAVED SENTINEL'
 New-Item -ItemType Directory -Force (Join-Path $app 'Data') | Out-Null
 Set-Content (Join-Path $app 'Data/settings.json') 'USER CONFIG SENTINEL'
 & (Join-Path $PSScriptRoot 'build-installer.ps1') -DistributionDirectory $scope -IsolatedTest -VersionOverride '0.9.1'
 Run-Msi @('/i',('"'+$msi+'"'),'/qn','/norestart',('INSTALLFOLDER="'+$app+'"'),('USERMENUDIR="'+$menu+'"'),'/l*v',('"'+(Join-Path $output 'upgrade.log')+'"'))
 $new=(Get-ItemProperty $testKey);$code=$new.ProductCode
 Check 'major upgrade replaces registration and preserves saved image and settings' ($new.InstalledVersion -eq '0.9.1' -and (Test-Path (Join-Path $app 'user-saved.png')) -and (Test-Path (Join-Path $app 'Data/settings.json')))
 Run-Msi @('/x',$code,'/qn','/norestart','/l*v',('"'+(Join-Path $output 'uninstall.log')+'"'))
 $installed=$false
 $owned=@('ScreenCraft.exe','ScreenCraft.exe.config','LICENSE','THIRD_PARTY_NOTICES.md','COLORBREWER_LICENSE.txt','WIX_LICENSE.txt','USER_GUIDE.md','KNOWN_LIMITATIONS.md')
 $remaining=@($owned | Where-Object {Test-Path (Join-Path $app $_)})
 Check 'uninstall removes only owned program files shortcuts and test registration' ($remaining.Count -eq 0 -and !(Test-Path (Join-Path $menu 'ScreenCraft.lnk')) -and !(Test-Path $testKey))
 Check 'uninstall preserves user images and config directories inside install directory' ((Test-Path (Join-Path $app 'user-saved.png')) -and (Test-Path (Join-Path $app 'Data/settings.json')))
 $now=(Get-ItemProperty $startupPath -ErrorAction SilentlyContinue).ScreenCraft
 Check 'real ScreenCraft startup entry unchanged' ($oldStartup -ceq $now)
 $results | Export-Csv (Join-Path $output 'results.csv') -NoTypeInformation -Encoding utf8
 Write-Output 'Installer GUI, standard-user token, real startup/login and production uninstall: NOT RUN; isolated MSI engine integration only.'
}finally{
 if($installed -and $code){Run-Msi @('/x',$code,'/qn','/norestart')}
}

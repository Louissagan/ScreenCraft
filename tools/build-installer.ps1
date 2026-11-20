param(
    [string]$DistributionDirectory='dist',
    [switch]$IsolatedTest,
    [string]$VersionOverride
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$payload=Join-Path $root 'bin\Release'
$exe=Get-Item -LiteralPath (Join-Path $payload 'ScreenCraft.exe')
$version=if($VersionOverride){([version]$VersionOverride).ToString(3)}else{([version]$exe.VersionInfo.FileVersion).ToString(3)}

$install=@{
    ProductName='ScreenCraft'
    DirectoryName='ScreenCraft'
    RegistryKey='Software\ScreenCraft\Install'
    UpgradeCode='28A312CE-7AC5-43A6-94AE-E6A2309E99A1'
    MainGuid='9187C9C1-A5A2-4854-A0C7-4DDE763C25A2'
    ShortcutGuid='1A647859-2C43-4AB8-B40E-519E89C66C32'
}
$filename='ScreenCraft-Setup-PerUser-win-x64.msi'
if($IsolatedTest){
    $install.ProductName='ScreenCraft Installer Test'
    $install.DirectoryName='ScreenCraftInstallerTest'
    $install.RegistryKey='Software\ScreenCraftInstallerTests\Package'
    $install.UpgradeCode='BC30869A-B705-432F-859B-951382A20125'
    $install.MainGuid='3B0714FD-D904-4220-A897-4BC717ABAB37'
    $install.ShortcutGuid='D50911CE-18D7-4C9D-8758-BC250DF7341D'
    $install.IsolatedTest='1'
    $filename='ScreenCraft-InstallerTest.msi'
}
$install.Version=$version
$install.Root=$root
$install.Payload=$payload
$install.Icon=Join-Path $root 'src\Assets\ScreenCraft.ico'

$build=Join-Path $root ('.build\msi-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $build | Out-Null
$cache=Join-Path $root '.tools'
$archive=Join-Path $cache 'wix314-binaries.zip'
$tools=Join-Path $cache 'wix314'
$expected='6ac824e1642d6f7277d0ed7ea09411a508f6116ba6fae0aa5f2c7daa2ff43d31'
if(!(Test-Path -LiteralPath $archive)){
    $download=Join-Path $build 'wix.zip'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip' -OutFile $download
    if((Get-FileHash -LiteralPath $download).Hash -ne $expected){throw 'WiX download did not match the pinned checksum.'}
    New-Item -ItemType Directory -Path $cache -Force | Out-Null
    Move-Item -LiteralPath $download -Destination $archive
}
if((Get-FileHash -LiteralPath $archive).Hash -ne $expected){throw 'The cached WiX ZIP failed checksum verification.'}
if(!(Test-Path -LiteralPath (Join-Path $tools 'candle.exe')) -or !(Test-Path -LiteralPath (Join-Path $tools 'light.exe'))){
    Expand-Archive -LiteralPath $archive -DestinationPath $tools -Force
}

$dist=if([IO.Path]::IsPathRooted($DistributionDirectory)){$DistributionDirectory}else{Join-Path $root $DistributionDirectory}
$dist=[IO.Path]::GetFullPath($dist)
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$object=Join-Path $build 'Setup.wixobj'
$linkedPackage=Join-Path $build $filename
$definitions=@($install.GetEnumerator() | Sort-Object Key | ForEach-Object { '-d{0}={1}' -f $_.Key,$_.Value })
& (Join-Path $tools 'candle.exe') -nologo @definitions -out $object (Join-Path $PSScriptRoot 'ScreenCraft.wxs')
if($LASTEXITCODE -ne 0){throw 'Could not compile the ScreenCraft installer definition.'}

# The application lives in the user profile and the two links refer to its registered file.
& (Join-Path $tools 'light.exe') -nologo -spdb -sice:ICE91 -sice:ICE61 -sice:ICE69 -sice:ICE90 -out $linkedPackage $object
if($LASTEXITCODE -ne 0){throw 'Could not link the ScreenCraft installer.'}
$destination=Join-Path $dist $filename
Copy-Item -LiteralPath $linkedPackage -Destination $destination -Force
Write-Output "Built ScreenCraft MSI: $destination"

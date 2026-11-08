param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'bin\Release'
New-Item -ItemType Directory -Force $out | Out-Null
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
if (!(Test-Path $csc)) { throw '.NET Framework C# compiler is missing.' }
$sources = @(Get-ChildItem (Join-Path $root 'src') -Recurse -Filter '*.cs' | ForEach-Object FullName)
if ($Test) { $sources += @(Get-ChildItem (Join-Path $root 'tests') -Filter '*.cs' | ForEach-Object FullName) }
$name = if($Test){'ScreenCraft.Tests'}else{'ScreenCraft'}
$entry = if($Test){'Stackshot.ScreenCraftTests'}else{'Stackshot.ScreenCraftProgram'}
$staging=Join-Path $root ('.build\'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $staging | Out-Null
$icon=Join-Path $root 'src\Assets\ScreenCraft.ico'
$iconBuilder=Join-Path $staging 'IconBuilder.exe'
& $csc /nologo /target:exe /r:System.Drawing.dll ("/out:"+$iconBuilder) (Join-Path $root 'src\App\BrandArtwork.cs') (Join-Path $root 'tools\IconBuilder.cs')
if($LASTEXITCODE -ne 0){throw 'Icon builder compilation failed'}
& $iconBuilder $icon
if($LASTEXITCODE -ne 0){throw 'Icon generation failed'}
$argsList = @('/nologo','/codepage:65001','/utf8output','/optimize+','/platform:x64',"/main:$entry",("/out:"+(Join-Path $staging "$name.exe")),('/win32manifest:'+(Join-Path $root 'src\app.manifest')))
$argsList += @(('/win32icon:'+$icon),('/resource:'+$icon+',ScreenCraft.AppIcon.ico'))
$argsList += if($Test){'/target:exe'}else{'/target:winexe'}
foreach($a in 'System.Windows.Forms','System.Drawing','System.IO.Compression','System.IO.Compression.FileSystem','System.Web.Extensions','System.Security','System.Xaml') { $argsList += "/r:$a.dll" }
foreach($a in 'PresentationFramework','PresentationCore','WindowsBase','WindowsFormsIntegration','UIAutomationProvider','UIAutomationTypes','UIAutomationClient') { $argsList += '/r:'+(Join-Path $fw "WPF\$a.dll") }
foreach($a in 'System.Runtime','System.Runtime.WindowsRuntime'){ $argsList += '/r:'+(Join-Path $fw "$a.dll") }
foreach($a in 'Foundation','Graphics','Media','Storage','Globalization') { $argsList += '/r:'+(Join-Path $env:WINDIR "System32\WinMetadata\Windows.$a.winmd") }
& $csc @argsList @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed; see compiler output.' }
$destination=Join-Path $out "$name.exe"
if(Test-Path $destination){Move-Item -LiteralPath $destination -Destination (Join-Path $out ($name+'.previous-'+[guid]::NewGuid().ToString('N')+'.exe'))}
Copy-Item -LiteralPath (Join-Path $staging "$name.exe") -Destination $destination
Copy-Item -LiteralPath (Join-Path $root 'src\ScreenCraft.exe.config') -Destination (Join-Path $out "$name.exe.config")
Write-Output "Release built: $out\$name.exe"
if($Test){ & (Join-Path $out "$name.exe"); if($LASTEXITCODE -ne 0){throw 'Tests failed'} }

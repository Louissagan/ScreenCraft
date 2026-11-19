param([switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'bin\Release'
New-Item -ItemType Directory -Force $out | Out-Null
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
if (!(Test-Path $csc)) { throw '.NET Framework C# compiler is missing.' }
$sources = @(foreach($folder in 'App','Core'){ Get-ChildItem (Join-Path $root ('src\'+$folder)) -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName })
$allSources = @(Get-ChildItem (Join-Path $root 'src') -Recurse -Filter '*.cs' | ForEach-Object FullName)
if(@($allSources | Where-Object {$_ -notin $sources}).Count -ne 0){throw 'Unexpected C# source outside src/App and src/Core. Review source ownership before building.'}
if ($Test) { $sources += @(Get-ChildItem (Join-Path $root 'tests') -Filter '*.cs' | ForEach-Object FullName) }
$name = if($Test){'ScreenCraft.Tests'}else{'ScreenCraft'}
$entry = if($Test){'ScreenCraft.ScreenCraftTests'}else{'ScreenCraft.ScreenCraftProgram'}
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
foreach($a in 'System.Windows.Forms','System.Drawing','System.IO.Compression','System.IO.Compression.FileSystem','System.Web.Extensions','System.Security') { $argsList += "/r:$a.dll" }
foreach($a in 'WindowsBase','UIAutomationProvider','UIAutomationTypes','UIAutomationClient') { $argsList += '/r:'+(Join-Path $fw "WPF\$a.dll") }
foreach($a in 'System.Runtime','System.Runtime.WindowsRuntime'){ $argsList += '/r:'+(Join-Path $fw "$a.dll") }
foreach($a in 'Foundation','Graphics','Media','Storage','Globalization') { $argsList += '/r:'+(Join-Path $env:WINDIR "System32\WinMetadata\Windows.$a.winmd") }
$manifest=Join-Path $staging 'compiled-sources.txt'
$sources | ForEach-Object { $_.Substring($root.Length+1) } | Set-Content -LiteralPath $manifest -Encoding UTF8
$inputs=@($sources | ForEach-Object {[ordered]@{Path=$_.Substring($root.Length+1).Replace('\','/');SHA256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()}})
& $csc @argsList @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed; see compiler output.' }
$destination=Join-Path $out "$name.exe"
if(Test-Path $destination){Move-Item -LiteralPath $destination -Destination (Join-Path $out ($name+'.previous-'+[guid]::NewGuid().ToString('N')+'.exe'))}
Copy-Item -LiteralPath (Join-Path $staging "$name.exe") -Destination $destination
Copy-Item -LiteralPath (Join-Path $root 'src\ScreenCraft.exe.config') -Destination (Join-Path $out "$name.exe.config")
Copy-Item -LiteralPath $manifest -Destination (Join-Path $out ($name+'.sources.txt'))
[ordered]@{BinarySHA256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant();CompilerInputs=$inputs} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out ($name+'.sources.json')) -Encoding UTF8
Write-Output "Release built: $out\$name.exe"
if($Test){ & (Join-Path $out "$name.exe"); if($LASTEXITCODE -ne 0){throw 'Tests failed'} }

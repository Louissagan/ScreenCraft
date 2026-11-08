param([switch]$NoBuild)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$prior=$env:SCREENCRAFT_TEST_OUTPUT
try{
 $env:SCREENCRAFT_TEST_OUTPUT=Join-Path $root 'docs\evidence\automated'
 if(!$NoBuild){& (Join-Path $root 'build.ps1') -Test}
 & (Join-Path $root 'bin\Release\ScreenCraft.Tests.exe') --ocr-probe
 if($LASTEXITCODE -ne 0){throw 'Native OCR probe failed; inspect native-ocr-probe.json'}
}finally{$env:SCREENCRAFT_TEST_OUTPUT=$prior}

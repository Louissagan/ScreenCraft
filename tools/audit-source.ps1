$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$findings=@()
$patterns=@{NetworkClient='HttpClient|WebClient|WebRequest|HttpWebRequest|TcpClient|UdpClient';KeyboardHook='SetWindowsHookEx|WH_KEYBOARD_LL';MachineRegistry='Registry\.LocalMachine|HKEY_LOCAL_MACHINE';Elevation='requireAdministrator|highestAvailable'}
foreach($file in Get-ChildItem (Join-Path $root 'src') -Recurse -File | Where-Object Extension -in '.cs','.manifest'){
 $body=Get-Content -LiteralPath $file.FullName -Raw
 $body=[regex]::Replace($body,'/\*[\s\S]*?\*/|(?m)^\s*//.*$','')
 foreach($category in $patterns.Keys){if($body -match $patterns[$category]){$findings+=[pscustomobject]@{Category=$category;Path=$file.FullName.Substring($root.Length+1)}}}
}
$report=[pscustomobject]@{Date='2026-11-05';Scope='compiled src only; static check, not packet capture';Findings=@($findings);Status=if($findings.Count -eq 0){'PASS'}else{'FAIL'}}
New-Item -ItemType Directory -Force (Join-Path $root 'docs\evidence\automated') | Out-Null
$report | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $root 'docs\evidence\automated\source-audit.json') -Encoding utf8
if($findings.Count -gt 0){throw 'Source audit has findings'}
Write-Output 'PASS static compiled-source audit (no network clients, low-level hooks, HKLM or elevation manifest)'

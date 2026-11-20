param([string]$InstallerPath='dist/ScreenCraft-Setup-PerUser-win-x64.msi',[string]$ReportPath='docs/evidence/automated/installer-artifact.json')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$path=if([IO.Path]::IsPathRooted($InstallerPath)){[IO.Path]::GetFullPath($InstallerPath)}else{[IO.Path]::GetFullPath((Join-Path $root $InstallerPath))}
if(!('ScreenCraftMsiRead' -as [type])){Add-Type @'
using System;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Text;
public static class ScreenCraftMsiRead {
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]static extern uint MsiOpenDatabase(string path,IntPtr mode,out uint database);
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]static extern uint MsiDatabaseOpenView(uint database,string query,out uint view);
 [DllImport("msi.dll")]static extern uint MsiViewExecute(uint view,uint record);
 [DllImport("msi.dll")]static extern uint MsiViewFetch(uint view,out uint record);
 [DllImport("msi.dll")]static extern uint MsiRecordGetFieldCount(uint record);
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]static extern uint MsiRecordGetString(uint record,uint field,StringBuilder buffer,ref uint length);
 [DllImport("msi.dll")]static extern uint MsiCloseHandle(uint handle);
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]static extern uint MsiGetSummaryInformation(uint database,string path,uint update,out uint summary);
 [DllImport("msi.dll",CharSet=CharSet.Unicode)]static extern uint MsiSummaryInfoGetProperty(uint summary,uint property,out uint type,out int integer,IntPtr time,StringBuilder buffer,ref uint length);
 static void Check(uint result){if(result!=0)throw new InvalidOperationException("MSI database error "+result);}
 public static string[][] Query(string path,string sql){uint database=0,view=0;var rows=new List<string[]>();try{Check(MsiOpenDatabase(path,IntPtr.Zero,out database));Check(MsiDatabaseOpenView(database,sql,out view));Check(MsiViewExecute(view,0));while(true){uint record;uint status=MsiViewFetch(view,out record);if(status==259)break;Check(status);try{var row=new string[MsiRecordGetFieldCount(record)];for(uint i=1;i<=row.Length;i++){uint length=8192;var value=new StringBuilder((int)length);Check(MsiRecordGetString(record,i,value,ref length));row[i-1]=value.ToString();}rows.Add(row);}finally{MsiCloseHandle(record);}}return rows.ToArray();}finally{if(view!=0)MsiCloseHandle(view);if(database!=0)MsiCloseHandle(database);}}
 public static int WordCount(string path){uint summary;Check(MsiGetSummaryInformation(0,path,0,out summary));try{uint type,length=1;int value;Check(MsiSummaryInfoGetProperty(summary,15,out type,out value,IntPtr.Zero,new StringBuilder(1),ref length));return value;}finally{MsiCloseHandle(summary);}}
}
'@}
$properties=@{};foreach($row in [ScreenCraftMsiRead]::Query($path,'SELECT `Property`,`Value` FROM `Property`')){$properties[$row[0]]=$row[1]}
$registry=[ScreenCraftMsiRead]::Query($path,'SELECT `Root`,`Key`,`Name` FROM `Registry`')
$remove=[ScreenCraftMsiRead]::Query($path,'SELECT `FileKey`,`FileName` FROM `RemoveFile`')
$files=[ScreenCraftMsiRead]::Query($path,'SELECT `File`,`FileName`,`FileSize` FROM `File`')
$shortcuts=[ScreenCraftMsiRead]::Query($path,'SELECT `Shortcut`,`Directory_`,`Target` FROM `Shortcut`')
$conditions=[ScreenCraftMsiRead]::Query($path,'SELECT `Condition`,`Description` FROM `LaunchCondition`')
$actions=[ScreenCraftMsiRead]::Query($path,'SELECT `Action`,`Type`,`Target` FROM `CustomAction`')
$wordCount=[ScreenCraftMsiRead]::WordCount($path)
if(($wordCount -band 8) -eq 0 -or $properties.ContainsKey('ALLUSERS')){throw 'Installer is not limited per-user'}
if(@($registry | Where-Object {$_[0] -ne '1'}).Count -gt 0){throw 'Installer writes outside HKCU'}
if(@($remove | Where-Object {![string]::IsNullOrEmpty($_[1])}).Count -gt 0){throw 'Installer contains broad deletion entries'}
if($properties.LAUNCHAPP -ne '0' -or $properties.REMOVEUSERDATA -ne '0' -or $properties.MSIRESTARTMANAGERCONTROL -ne 'Disable'){throw 'Unexpected launch cleanup or process-control defaults'}
$version=[version]$properties.ProductVersion
$expectedFiles=@('ScreenCraft.exe','ScreenCraft.exe.config','LICENSE','THIRD_PARTY_NOTICES.md','USER_GUIDE.md','KNOWN_LIMITATIONS.md')
if($version -lt [version]'0.10.10'){$expectedFiles+=@('COLORBREWER_LICENSE.txt','WIX_LICENSE.txt')}
$actualFiles=@($files | ForEach-Object {($_[1] -split '\|')[-1]})
if($shortcuts.Count -ne 2 -or @(Compare-Object $expectedFiles $actualFiles).Count -ne 0){throw 'Unexpected payload or shortcuts'}
if($version -ge [version]'0.10.10'){
 $tables=@([ScreenCraftMsiRead]::Query($path,'SELECT `Name` FROM `_Tables`') | ForEach-Object {$_[0]})
 if(@($tables | Where-Object {$_ -in 'Dialog','Control','Binary','ControlEvent','EventMapping'}).Count -ne 0){throw 'Installer includes an embedded third-party dialog or binary table'}
 if(@($actions | Where-Object {$_[0] -notin 'CloseInstalledInstance','CleanupOwnedStartup','CleanupOwnedCache','LaunchApp'}).Count -ne 0){throw 'Unexpected custom action'}
 Write-Output 'PASS MSI uses the Windows Installer interface and contains only the ScreenCraft payload'
}
if(!($conditions | Where-Object {$_[0] -eq 'NOT ALLUSERS'})){throw 'ALLUSERS override is not blocked'}
$ownActions=@($actions | Where-Object {$_[0] -in @('CloseInstalledInstance','CleanupOwnedStartup','CleanupOwnedCache')})
if($ownActions.Count -ne 3 -or @($ownActions | Where-Object {([int]$_[1] -band 2048) -ne 0}).Count -gt 0){throw 'Uninstall actions must impersonate the current user'}
$report=[pscustomobject]@{Version=$properties.ProductVersion;Artifact=[IO.Path]::GetFileName($path);Status='PASS';SummaryWordCount=$wordCount;RegistryRoots=@($registry | ForEach-Object {$_[0]} | Select-Object -Unique);PayloadCount=$files.Count;ShortcutCount=$shortcuts.Count;LaunchDefault=$properties.LAUNCHAPP;CacheRemovalDefault=$properties.REMOVEUSERDATA;Result='limited per-user; HKCU writes only; no wildcard removal; no automatic launch or process forcing';GuiStatus='NOT RUN'}
$reportFull=if([IO.Path]::IsPathRooted($ReportPath)){$ReportPath}else{Join-Path $root $ReportPath}
New-Item -ItemType Directory -Force (Split-Path $reportFull -Parent) | Out-Null
$report | ConvertTo-Json -Depth 4 | Set-Content $reportFull -Encoding utf8
Write-Output ('PASS compiled MSI privilege registry payload and deletion boundaries: '+$path)

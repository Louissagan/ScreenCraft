$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$base=Join-Path $root 'src\Upstream\Stackshot'
$manifest=Join-Path $root 'docs\UPSTREAM_FILES.csv'
$known=@{}
if(Test-Path $manifest){foreach($row in Import-Csv $manifest){$known[$row.Path]=$row.OriginalSHA256}}
$rows=Get-ChildItem $base -Recurse -Filter '*.cs' | ForEach-Object {
 $relative=$_.FullName.Substring($base.Length+1)
 $path='src/Upstream/Stackshot/'+$relative.Replace('\','/')
 $original=Join-Path (Join-Path $root 'evaluation\stackshot\src') $relative
 $oldHash=if(Test-Path $original){(Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash}else{$known[$path]}
 if(!$oldHash){throw "No upstream origin hash for $path"}
 [pscustomobject]@{Path=$path;UpstreamCommit='0fa71af2346f09dc34dd8f48eb4f8df211f5ae7c';OriginalSHA256=$oldHash;CurrentSHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;License='MIT'}
}
$rows | Export-Csv $manifest -NoTypeInformation -Encoding utf8

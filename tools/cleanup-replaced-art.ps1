param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskPrefix=[System.IO.Path]::GetFullPath($taskRoot).TrimEnd('\')+'\'
$taskOld=@()
for($taskIndex=0;$taskIndex -lt 12;$taskIndex++){
    $taskOld+=('Assets/Resources/Art/npc-{0:00}.png' -f $taskIndex)
    $taskOld+=('Assets/Resources/Art/Portraits/survivor-{0:00}-v2.png' -f $taskIndex)
}
for($taskIndex=0;$taskIndex -lt 4;$taskIndex++){$taskOld+="Assets/Resources/Art/facility-$taskIndex.png"}
$taskOld+=@('Assets/Scripts/UI/PixelArt.cs','Assets/Scripts/UI/PixelGridImage.cs')
$taskTargets=@($taskOld)+@($taskOld|ForEach-Object{"$_.meta"})
$taskRequired=@()
for($taskIndex=0;$taskIndex -lt 12;$taskIndex++){$taskRequired+=('Assets/Resources/Art/Portraits/survivor-{0:00}-v3.png' -f $taskIndex)}
foreach($taskName in @('clinic-v2','chapel-v2','warehouse-v2','personnel-v2','city-v2','rift-v2')){$taskRequired+="Assets/Resources/Art/Environments/$taskName.png"}
$taskRequired+=@('Assets/Resources/Art/Constellations/napoleon-v1.png','Assets/Resources/Art/Constellations/yog-v1.png')
foreach($taskRelative in $taskRequired){if(!(Test-Path -LiteralPath (Join-Path $taskRoot $taskRelative) -PathType Leaf)){throw "Replacement missing: $taskRelative"}}
Push-Location -LiteralPath $taskRoot
try {
    $taskCode=@(& rg --files Assets/Scripts Assets/Editor Assets/Tests -g '*.cs' | Where-Object{$_ -notmatch '[/\\](PixelArt|PixelGridImage)\.cs$'})
    $taskReferences=@(& rg -n 'PixelArt\b|PixelGridImage\b|Art/facility-|Art/npc-|survivor-[0-9]{2}-v2' -- $taskCode)
    if($LASTEXITCODE -eq 0){throw ('Old resource still referenced: '+($taskReferences -join '; '))}
    if($LASTEXITCODE -gt 1){throw 'Reference scan failed'}
    $taskGuidList=@()
    foreach($taskRelative in $taskOld){
        $taskMeta=Join-Path $taskRoot "$taskRelative.meta"
        if(Test-Path -LiteralPath $taskMeta){$taskGuid=@(& rg '^guid: ' -- $taskMeta);if($taskGuid.Count -gt 0){$taskGuidList+=($taskGuid[0] -replace '^guid: ','')}}
    }
    if($taskGuidList.Count -gt 0){
        $taskSerialized=@(& rg --files Assets -g '*.unity' -g '*.prefab' -g '*.asset')
        $taskDependencies=@(& rg -n ($taskGuidList -join '|') -- $taskSerialized)
        if($LASTEXITCODE -eq 0){throw ('Serialized reference remains: '+($taskDependencies -join '; '))}
        if($LASTEXITCODE -gt 1){throw 'Serialized dependency scan failed'}
    }
    $taskFiles=@()
    foreach($taskRelative in $taskTargets){
        $taskAbsolute=[System.IO.Path]::GetFullPath((Join-Path $taskRoot $taskRelative))
        if(!$taskAbsolute.StartsWith($taskPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Cleanup path escaped workspace'}
        if(Test-Path -LiteralPath $taskAbsolute){
            $taskFile=Get-Item -LiteralPath $taskAbsolute
            if($taskFile.PSIsContainer){throw 'Only whitelisted files may be removed'}
            $taskFiles+=[pscustomobject]@{path=$taskRelative;bytes=$taskFile.Length;sha256=(Get-FileHash -LiteralPath $taskAbsolute -Algorithm SHA256).Hash}
        }
    }
    $taskByteTotal=0;foreach($taskFile in $taskFiles){$taskByteTotal+=$taskFile.bytes}
    $taskReport=@{applied=[bool]$Apply;count=$taskFiles.Count;bytes=$taskByteTotal;files=$taskFiles;replacementCount=$taskRequired.Count;referenceScan='PASS';serializedDependencyScan='PASS'}
    if($Apply -and $taskFiles.Count -gt 0){
        foreach($taskFile in $taskFiles){Remove-Item -LiteralPath (Join-Path $taskRoot $taskFile.path) -Force}
        $taskReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskRoot 'TestResults/art-cleanup.json') -Encoding UTF8
    }
    [pscustomobject]$taskReport | Select-Object applied,count,bytes,replacementCount,referenceScan,serializedDependencyScan | ConvertTo-Json
} finally {Pop-Location}

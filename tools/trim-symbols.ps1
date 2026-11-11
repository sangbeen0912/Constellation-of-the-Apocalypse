param([ValidateSet('Windows','QA')][string]$Build = 'Windows')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskBuildRoot = Join-Path $taskRoot ('Builds\' + $Build)
$taskManaged = Join-Path $taskBuildRoot 'ApocalypseConstellation_Data\Managed'
if (!(Test-Path -LiteralPath (Join-Path $taskBuildRoot 'build-summary.txt'))) { throw 'No completed generated build.' }
if (!(Get-Content -LiteralPath (Join-Path $taskBuildRoot 'build-summary.txt') -Raw).Contains('result=Succeeded')) { throw 'Build has not succeeded.' }
$taskActual = (Resolve-Path -LiteralPath $taskManaged).Path
if ($taskActual -ne $taskManaged -or (Get-Item -LiteralPath $taskManaged -Force).LinkType) { throw 'Unexpected managed directory.' }
# Burst emits source-level diagnostic text in a directory explicitly excluded from shipping.
$taskBuildActual=(Resolve-Path -LiteralPath $taskBuildRoot).Path
foreach($taskDebug in @(Get-ChildItem -LiteralPath $taskBuildActual -Directory | Where-Object { $_.Name.EndsWith('_BurstDebugInformation_DoNotShip') })) {
    $taskDebugPath=(Resolve-Path -LiteralPath $taskDebug.FullName).Path
    if($taskDebug.Parent.FullName -ne $taskBuildActual -or !$taskDebugPath.StartsWith($taskBuildActual+'\',[StringComparison]::OrdinalIgnoreCase) -or ($taskDebug.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Unexpected Burst diagnostic directory.'}
    if(@(Get-ChildItem -LiteralPath $taskDebugPath -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count){throw 'Linked Burst diagnostic directory.'}
    Remove-Item -LiteralPath $taskDebugPath -Recurse -Force
}
$taskFiles = @(Get-ChildItem -LiteralPath $taskManaged -Filter '*.pdb' -File)
if ($taskFiles.Count -eq 0) { Write-Output 'No deployment symbols to move.'; return }
foreach ($taskFile in $taskFiles) {
    if ($taskFile.DirectoryName -ne $taskManaged -or $taskFile.LinkType) { throw 'Unexpected symbol path.' }
}
$taskArchiveDirectory = Join-Path $taskRoot 'TestResults\SymbolArchives'
New-Item -ItemType Directory -Path $taskArchiveDirectory -Force | Out-Null
$taskArchive = Join-Path $taskArchiveDirectory ($Build.ToLowerInvariant() + '-symbols.zip')
Compress-Archive -LiteralPath $taskFiles.FullName -DestinationPath $taskArchive -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskZip = [System.IO.Compression.ZipFile]::OpenRead($taskArchive)
try {
    foreach ($taskFile in $taskFiles) {
        $taskEntry = $taskZip.GetEntry($taskFile.Name)
        if (!$taskEntry -or $taskEntry.Length -ne $taskFile.Length) { throw 'Symbol backup verification failed.' }
        $taskStream = $taskEntry.Open()
        $taskHash = [System.Security.Cryptography.SHA256]::Create()
        try {
            $taskDigest = ([BitConverter]::ToString($taskHash.ComputeHash($taskStream))).Replace('-','')
            if ($taskDigest -ne (Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash) { throw 'Symbol backup hash mismatch.' }
        } finally { $taskStream.Dispose(); $taskHash.Dispose() }
    }
} finally { $taskZip.Dispose() }
$taskBytes = ($taskFiles | Measure-Object Length -Sum).Sum
foreach ($taskFile in $taskFiles) { Remove-Item -LiteralPath $taskFile.FullName -Force }
[ordered]@{build=$Build;count=$taskFiles.Count;removedFromDeploymentBytes=$taskBytes;archiveBytes=(Get-Item -LiteralPath $taskArchive).Length} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskArchiveDirectory ($Build.ToLowerInvariant() + '-symbols.json')) -Encoding UTF8
Write-Output ("Archived deployment symbols: " + $taskFiles.Count + ", bytes=" + $taskBytes)


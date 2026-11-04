param([string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path (Join-Path $taskRoot 'TestResults') -Force | Out-Null
$taskAlias = Join-Path ([System.IO.Path]::GetTempPath()) 'ApocalypseConstellation-VerifySource'
function Remove-TaskAlias {
    if (!(Test-Path -LiteralPath $taskAlias)) { return }
    $taskLink = Get-Item -LiteralPath $taskAlias -Force
    if ($taskLink.LinkType -ne 'Junction' -or $taskLink.Target -ne $taskRoot) {
        throw '검증 임시 경로가 예상한 프로젝트 연결이 아니므로 정리하지 않습니다.'
    }
    # Windows PowerShell Remove-Item can throw NullReferenceException on junctions.
    # Non-recursive Directory.Delete removes only this validated junction, never its target.
    [System.IO.Directory]::Delete($taskAlias)
}
# Unity's TypeDB subprocess corrupts non-ASCII project paths on this installation.
# A temporary junction references the original source without moving or copying it.
Remove-TaskAlias
New-Item -ItemType Junction -Path $taskAlias -Target $taskRoot | Out-Null
function Invoke-UnityTask([string]$Name, [string]$Arguments) {
    $taskLog = Join-Path $taskRoot "TestResults\$Name.log"
    $taskArgs = "-batchmode -nographics -projectPath `"$taskAlias`" -logFile `"$taskLog`" $Arguments"
    $taskProcess = Start-Process -FilePath $Unity -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
    $taskProcess.WaitForExit()
    if ($taskProcess.ExitCode -ne 0) { Get-Content -LiteralPath $taskLog -Tail 50; throw "Unity $Name exit $($taskProcess.ExitCode)" }
}
try {
    Invoke-UnityTask 'generate' '-quit -executeMethod ProjectBuilder.Generate'
    Invoke-UnityTask 'editmode' "-runTests -testPlatform EditMode -testResults `"$taskRoot\TestResults\editmode.xml`""
    Invoke-UnityTask 'playmode' "-runTests -testPlatform PlayMode -testResults `"$taskRoot\TestResults\playmode.xml`""
    foreach ($taskMode in @('editmode','playmode')) {
        [xml]$taskXml = Get-Content -LiteralPath (Join-Path $taskRoot "TestResults\$taskMode.xml") -Raw
        if ([int]$taskXml.'test-run'.total -lt 1 -or [int]$taskXml.'test-run'.failed -gt 0) { throw "$taskMode tests did not pass" }
    }
    if (!$SkipBuild) { Invoke-UnityTask 'build' '-quit -executeMethod ProjectBuilder.BuildWindows' }
} finally {
    Remove-TaskAlias
}
Write-Output '검증 완료. TestResults/*.xml 및 로그를 확인하십시오.'

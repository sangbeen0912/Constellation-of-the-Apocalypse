param([ValidateSet('before','after')][string]$Stage='after',[string]$Unity='C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskAlias=Join-Path ([IO.Path]::GetTempPath()) 'ApocalypseConstellation-UIBenchmark'
if(Test-Path -LiteralPath $taskAlias){throw '측정용 임시 경로가 이미 존재합니다.'}
New-Item -ItemType Junction -Path $taskAlias -Target $taskRoot | Out-Null
try {
    $taskResult=Join-Path $taskRoot "TestResults\ui-mesh-$Stage.json"
    $taskLog=Join-Path $taskRoot "TestResults\ui-mesh-$Stage.log"
    $taskArgs="-batchmode -nographics -quit -projectPath `"$taskAlias`" -executeMethod UIOptimizationBenchmark.Run -ui-benchmark-output `"$taskResult`" -logFile `"$taskLog`""
    $taskProcess=Start-Process -FilePath $Unity -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
    $taskProcess.WaitForExit()
    if($taskProcess.ExitCode -ne 0){throw "UI benchmark failed: $taskLog"}
    Get-Content -LiteralPath $taskResult
} finally {
    $taskLink=Get-Item -LiteralPath $taskAlias -Force
    if($taskLink.LinkType -ne 'Junction' -or $taskLink.Target -ne $taskRoot){throw '측정 경로의 연결 대상이 달라 정리하지 않습니다.'}
    [IO.Directory]::Delete($taskAlias)
}

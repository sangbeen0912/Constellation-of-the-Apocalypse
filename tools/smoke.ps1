param([switch]$ResetOnly,[switch]$FontDiagnostics,[switch]$HoldUI)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskPublished = Join-Path $taskRoot 'TestResults\PlayerQA'
$taskOutput = Join-Path $taskPublished 'Latest'
if(Test-Path -LiteralPath $taskOutput) {
    $taskActual=[IO.Path]::GetFullPath((Resolve-Path -LiteralPath $taskOutput).Path)
    $taskExpected=[IO.Path]::GetFullPath((Join-Path $taskRoot 'TestResults\PlayerQA\Latest'))
    if($taskActual -ne $taskExpected -or ((Get-Item -LiteralPath $taskOutput -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '예상한 QA 전용 폴더가 아닙니다.'}
    if(@(Get-ChildItem -LiteralPath $taskOutput -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count){throw 'QA 폴더에 연결 경로가 있습니다.'}
    if(Get-Process ApocalypseConstellation -ErrorAction SilentlyContinue){throw '게임 실행 중에는 QA 자료를 교체하지 않습니다.'}
    Remove-Item -LiteralPath $taskActual -Recurse -Force
}
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskExe = Join-Path $taskRoot 'Builds\QA\ApocalypseConstellation.exe'
if (!(Test-Path -LiteralPath $taskExe)) { throw 'Windows 빌드가 없습니다. 먼저 tools/verify.ps1 -BuildQA를 실행하십시오.' }
$taskCampaign=Get-Content -LiteralPath (Join-Path $taskRoot 'TestResults\simulation-1000.json') -Raw | ConvertFrom-Json
if(!$taskCampaign.winningSeeds -or $taskCampaign.winningSeeds.Count -lt 1){throw '새 규칙에서 승리를 재현한 캠페인 시드가 필요합니다.'}
$taskSeed=$taskCampaign.winningSeeds[0]
$taskLog = Join-Path $taskOutput 'player.log'
$taskArguments = "-screen-width 1280 -screen-height 720 -force-d3d11 -smoke-test -qa-seed $taskSeed -qa-output `"$taskOutput`" -logFile `"$taskLog`""
if($ResetOnly){$taskArguments += ' -qa-reset-only'}
if($FontDiagnostics){$taskArguments += ' -qa-font-diagnostics'}
if($HoldUI){$taskArguments += ' -qa-hold-ui'}
# A hidden Unity window does not present frames on this machine; visual QA must render a visible game.
$taskPlayer = Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Normal -PassThru
if (!$taskPlayer.WaitForExit(60000)) {
    Stop-Process -Id $taskPlayer.Id
    throw 'Windows 플레이어 QA가 60초 이내 완료되지 않았습니다. player.log를 확인하십시오.'
}
if ($taskPlayer.ExitCode -ne 0) { throw "Windows 플레이어 종료 코드: $($taskPlayer.ExitCode)" }
$taskReportPath = Join-Path $taskOutput 'player-smoke.txt'
if (!(Test-Path -LiteralPath $taskReportPath)) { throw "이번 실행이 완료 보고서를 만들지 못했습니다. 이전 PASS는 사용하지 않습니다: $taskOutput" }
$taskReport = Get-Content -LiteralPath $taskReportPath -Raw
if (!$taskReport.Contains('result=PASS')) { throw $taskReport }
if (!(Get-Content -LiteralPath $taskLog -Raw).Contains('APOCALYPSE_PLAYER_SMOKE_PASS')) { throw "이번 실행의 완료 표식이 없습니다: $taskLog" }
$taskHashes = Get-ChildItem -LiteralPath $taskOutput -Filter '*.png' | Get-FileHash | Select-Object -ExpandProperty Hash -Unique
if ($taskHashes.Count -lt 4) { throw '서로 다른 화면 캡처가 부족합니다. 화면 렌더링을 확인하십시오.' }
# Keep one current result set; no duplicate screenshots or per-run history directories.
Write-Output $taskReport
Write-Output "qa-output=$taskOutput"

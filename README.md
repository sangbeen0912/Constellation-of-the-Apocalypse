# 종말의 성좌

Unity 6 기반 오프라인 성좌 후원/생존 게임. 최신 기획은 [GAME_DESIGN.md v4.1](GAME_DESIGN.md), [통합 제작 명령서](docs/IMPLEMENTATION_ORDER_2026-10-30.md), 임시 정책은 [DECISIONS.md](DECISIONS.md)를 확인한다.

UI·픽셀아트 레퍼런스 분석은 [데이브 더 다이버 연구 1차](docs/research/DAVE_UI_ART_STUDY.md)를 참조한다. 공식 자료의 실제 관찰, 미확인 사항, 우리 게임의 적용 제안을 구분해 기록했다.

## 실행

Windows 빌드가 생성된 경우 `Builds/Windows/ApocalypseConstellation.exe`를 실행한다.

Unity Hub에서 이 프로젝트 폴더를 Unity **6000.3.23f1**로 열고 `Assets/Scenes/Apocalypse.unity`를 열어 Play할 수도 있다. Scene 재생성은 `Tools > Apocalypse > Generate Scene`, Windows 빌드는 `Tools > Apocalypse > Build Windows`.

## 플레이

1. 새로운 계약 → 세 후보 중 한 명 선택.
2. 쉘터의 의무실·예배방·보급창고·인재관리실에서 회복/보급/성유물/후원을 준비.
3. 전투준비에서 탐사/공략을 고르고 '진행'. 탐사는 물자 수색과 구조를 포함한다. 로그 속도는 계산 결과에 영향을 주지 않는다.
4. 중요 이벤트에서 선택. 성좌 이야기는 확인하면 영구 해금된다. 해금과 현재 화신체 계승은 별개다.
5. 현재 군단의 일반 좀비는1점, 특수·네임드는3점. 탐사 처치도 반영한다. 진행도30점 이후 균열 활동에서 악마 이벤트가 열린다. 악마·네임드는 균열 활동에서만 등장한다. 사망 또는 봉인 시 해당 진행도를 초기화한다. 일곱 악마 동시 봉인 시 승리.
6. 사망하면 다음 날 다른 후보와 계약. 신위·성좌·네임드 기록·창고는 유지된다.

성좌 도감은 획득한 성좌만 표시한다. 휠 또는 오른쪽 스크롤바로 이동하고, 급수 제목을 눌러 접거나 펼친다. `진명 성좌명 / 급수`를 클릭하면 소속·능력·효과·획득 당시 이야기를 읽는다. 나폴레옹/요그 소토스는 획득 이벤트와 도감에 같은 큰 일러스트를 표시한다. 나머지 성좌 그림은 공백이다. NPC12개 외형은 승인된 서도윤 계열의 새 초상화를 사용한다. [아트 변경 기록](docs/CHANGES_2026-10-30.md).

현재 봉인 N명에 따라 모든 적 체력은1+0.5N배, 공격력은1+0.2N배다. 봉인이 만료되면 배율도 낮아진다. 진행도와 신위는 별개다.

인재관리실은 이름에 고정 연결된 정면 픽셀 초상화와 체력·공격력·정신력·허기를 표시한다. NPC12종/시설4종/도시·균열 배경을 새로 제작했다. [현재 아트·프롬프트](docs/art/ART_ASSETS.md). 사용하지 않는 것으로 확인된 옛 아트/코드만 정리했으며 삭제 이력은 `TestResults/art-cleanup.json`에 있다.

저장은 `%USERPROFILE%\AppData\LocalLow\Starfall Studio\종말의 성좌\constellation-v1.json`에 기록된다. 중요 단계마다 자동 저장하며 `.bak` 백업을 유지한다. 테스트는 별도 임시 저장 폴더를 사용한다. 옛 방문/승리 기준 저장은 자원·인물·성좌·봉인을 유지하고 정확히 환산할 수 없는 진행도만0부터 새 규칙으로 전환한다.

## 검증

```powershell
powershell -ExecutionPolicy Bypass -File tools/verify.ps1
```

Unity 버전 경로가 다르면 `-Unity`로 실행 파일 경로를 지정한다. `TestResults/editmode.xml`, `playmode.xml`, `simulation-100.json`, `simulation-1000.json`과 각 로그를 생성한다. 실패 시 난수 시드와 직전 상태를 `failure-*.json`에 남긴다.

이 설치 환경의 Unity 빌드 보조 도구는 한글 경로를 잘못 처리하므로, 검증 스크립트가 임시 영문 junction으로 **같은 폴더**를 참조한 뒤 별칭만 제거한다. 소스와 실제 프로젝트 위치는 바뀌지 않는다. 원래 한글 경로에서 Unity 메뉴로 직접 빌드하면 이 도구의 오류가 다시 나타날 수 있으므로 Windows 빌드는 위 스크립트를 사용한다.

빌드 후 `powershell -ExecutionPolicy Bypass -File tools/smoke.ps1`로 실제 게임 창의 렌더링·UI 레이캐스트 클릭·저장·승리 경로를 검사한다. 별도 QA 저장을 사용하고 완료 후 창이 자동 종료된다. 화면 PNG는 `TestResults/PlayerQA`에 기록한다. 이 장치에서는 숨김 게임 창의 프레임 캡처가 검게 나오므로 화면 검증 중 게임 창이 잠시 표시된다.

현재 실제 검증 결과와 알려진 한계는 `TEST_REPORT.md`, 작업 상태는 `DEVELOPMENT_STATUS.md`에 기록한다.

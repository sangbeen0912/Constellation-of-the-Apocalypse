# 종말의 성좌 개발 규칙

- 최신 통합 기획은 `GAME_DESIGN.md` (Unity 6 v5.6) 및 `docs/PLANNER_ORDER_2026-10-30.md`다. 기획자 통합 오더와 이후 사용자 확정 지시가 같은 기능의 이전 명령서/임시 구현보다 우선한다. 이전 변경 기록과 웹 v2.0은 이력으로 보존한다.
- 확정 규칙을 임의 변경하지 않는다. 미확정 값은 `Assets/Resources/balance.json`과 `DECISIONS.md`에 표시한다.
- 핵심 로직은 Unity UI에 의존하지 않는 `Assets/Scripts/Core`에 둔다. 정적 콘텐츠와 회차 상태를 분리한다.
- 기능 변경마다 관련 EditMode/PlayMode 테스트를 실행하고 실제 결과만 `TEST_REPORT.md`에 기록한다.
- 오류를 재현하고 수정한 뒤 회귀 검증한다. 실패 테스트 삭제/무력화 및 기존 사용자 저장 삭제를 금한다.
- 사용자 요청한 타이틀 전체 초기화는 플레이어가 앱의 경고창에서 확정할 때만 실행한다. 개발/검증에서 실제 사용자 슬롯을 직접 초기화하지 말고 격리된 임시 슬롯을 사용한다.
- 중단 시 검증된 상태와 남은 작업을 `DEVELOPMENT_STATUS.md`에 남긴다.
- Unity 6000.3.23f1로 이 폴더를 연다. `Tools > Apocalypse > Generate Scene`으로 Scene/Prefab을 생성하고 Play한다.
- 자동 검증/Windows 빌드: `powershell -ExecutionPolicy Bypass -File tools/verify.ps1`.
- 게임 저장은 Application.persistentDataPath의 버전 있는 JSON이며 임시 파일/백업을 사용한다.

# 성좌 일러스트 / NPC 초상화 / 환경 아트

## 최신 추가: 기획자 통합 오더의 희귀 NPC4 / 전승 성유물8

내장 image_gen으로12개를 제작하고 인재 카드/희귀 영입/성유물 사건·보관·복원 역사에 연결했다. NPC256px/성유물128px Point import, 원본 보존, CPU 읽기 복사 없음. [그림12종](planner-order-gallery.html), [프롬프트](planner-order-art-manifest.json), [메타데이터·해시](planner-order-art-metadata.json), [최신 구현 보고](../PLANNER_IMPLEMENTATION_REPORT_2026-10-30.md)를 따른다. 아래20종 성좌/기존 NPC12/환경6개도 계속 사용한다.

## 최신: 성좌20종 전체 연결

후속 사용자 지시로 다른18명의 장면을 개별 구도로 새로 제작해 승인된 나폴레옹/요그와 함께20종을 연결한다. 아래의18종 공백 정책은 과거 이력이다. [현재20종 제작 기록·출처·최적화](CONSTELLATIONS_2026-10-30.md), [그림 전체 보기](constellation-gallery.html), [최종 프롬프트](constellation-manifest-v2.json), [원본 검증/해시](constellation-metadata-v2.json)를 따른다.

## 최신: 정면 초상화 v3와 환경6종

2026-10-30 후속 승인으로 NPC12종의 얼굴·눈·어깨를 정면으로 재제작했다. `Assets/Resources/Art/Portraits/survivor-00-v3.png`~`survivor-11-v3.png`가 현재 사용 파일이며 이름 연결 번호는 유지한다. 원본은1254×1254 RGBA, 게임 import는256×256 Point/밉맵 없음/무압축으로 관리한다. 12종 RGBA32 픽셀 저장량의 이론 합계는 약72.0MiB에서3.0MiB로 줄어든다. 이는 초상화 텍셀 기준이며 게임 전체 메모리 측정값이 아니다. 실제 화면에서 축소 품질을 확인한다.

`Assets/Resources/Art/Environments/`의 `clinic-v2.png`, `chapel-v2.png`, `warehouse-v2.png`, `personnel-v2.png`, `city-v2.png`, `rift-v2.png`는 각각 의무실/예배방/보급창고/인재관리실/도시/균열의 새 배경이다. 모두1672×941 원본이며 현재 시설/타이틀/전투준비/이벤트에 연결한다. 나폴레옹·요그 두 성좌 이미지는 유지한다.

제작 방식은 **내장 image_gen**. [현재 프롬프트·입력 원본·결과 경로](generation-manifest-v3.json), [현재20개 파일의 크기·알파·해시](asset-metadata-v3.json)를 기록했다. 재제작 입력의 원본 생성 파일은 Codex 생성 폴더에 보존되어 있다.

코드/씬/프리팹 참조가 없음을 확인한 옛28개 PNG와 meta, 미사용 PixelArt/PixelGridImage 코드와 meta 총60개/14,834,963바이트를 whitelist로 제거했다. `tools/cleanup-replaced-art.ps1`과 `TestResults/art-cleanup.json`에 검증/목록/해시가 있다. 새 배경 원본이 추가되었으므로 이 삭제 수치를 프로젝트 전체의 순 감소량이라고 표현하지 않는다. 기존 저장/연구 자료/생성 원본은 삭제하지 않았다.

아래는 초기 v2 제작 이력이다. 초기32px fallback과 v2 초상화가 현재도 사용된다는 설명은 아래 당시 기록에 한정한다.

## 초기 v2 제작 이력

2026-10-30 사용자 승인 서도윤 초상화를 기준으로 제작했다. 제작 방식은 **내장 image_gen**이며 CLI/API fallback은 사용하지 않았다. 각 요청의 최종 프롬프트, 스타일 참고 원본과 생성 원본 위치, 실제 게임 리소스 경로는 [generation-manifest.json](generation-manifest.json)에 보존했다. [asset-metadata.json](asset-metadata.json)은 파일별 실제 해상도·알파·SHA256 기록이다. 원본 생성 파일을 삭제하거나 변경하지 않았다.

## 성좌2종

| 성좌 | 게임 파일 | 구도와 자료 |
|---|---|---|
| 나폴레옹 | `Assets/Resources/Art/Constellations/napoleon-v1.png` | 황제 전신, 지도, 제국의 상징과 군대. 월계관/벌/독수리/홀 등은 [Musée de l’Armée](https://www.musee-armee.fr/en/collections/museum-treasures/napoleon-on-the-throne.html)의 역사 설명을 참고. 기존 회화를 복제하지 않은 게임용 재해석이다. |
| 요그 소토스 | `Assets/Resources/Art/Constellations/yog-v1.png` | 거대한 차원의 문, 발광 구체, 폐허로 초월적 규모를 표현. 전통 민속 신화의 신이 아니라 러브크래프트 문학의 존재다. [The Dunwich Horror](https://www.hplovecraft.com/writings/texts/fiction/dh.aspx)의 문/열쇠/시제 결합과 [Through the Gates of the Silver Key](https://www.hplovecraft.com/writings/texts/fiction/tgsk.aspx)의 시공간을 초월하는 존재 개념을 참고했다. 구체·문 건축·색상은 게임용 시각 해석이며 원전의 확정 외형으로 단정하지 않는다. |

두 파일은1536×1024이며 실제 이벤트와 획득한 도감 상세에서 같은 Texture2D를 사용한다. FitInParent와 전체 UV로 화면 크기에 맞추고 주요 인물/구도를 자르지 않는다. 다른18성좌는 기존 공백 정책을 유지한다.

## NPC12종

`Assets/Resources/Art/Portraits/survivor-00-v2.png`부터 `survivor-11-v2.png`까지1254×1254 RGBA 파일이다. 모두 실제 투명 영역이 있으며 사용자 승인 서도윤 PNG는 `survivor-01-v2.png`로 바이트 그대로 복사했다. 생성기의 최종 이미지 해상도이며1254px를 논리 픽셀 제작 격자라고 주장하지 않는다.

| 외형 번호 | 구분 |
|---|---|
|00|검은 머리 청년, 청회색 작업복|
|01|승인된 서도윤, 갈색 머리/상처/올리브 재킷|
|02|안경 남성, 청회색 기술 재킷|
|03|두건을 쓴 남성, 황갈색 재킷|
|04|회색 머리 노년 남성, 짙은 청록 외투/목도리|
|05|짧은 회색 수염 중년 남성, 갈색 외투|
|06|검은 단발 여성, 청록 재킷|
|07|묶은 갈색 머리 여성, 버건디 외투|
|08|안경/낮은 머리 묶음 여성, 청회색 의료 작업복|
|09|주근깨/짧은 적갈색 머리 여성, 황색 조끼|
|10|짧은 회색 머리 중년 여성, 녹색 외투/갈색 목도리|
|11|검은 땋은 머리 여성, 파란 셔츠|

기존 `PixelArt.PortraitIndex(name)`를 보존하여64개 생성 이름이12종 외형을 공유한다. `ArtCatalog.PortraitKey`로 리소스를 찾고 후보 선택/상태창/인재관리실에 공통 사용한다. 이는64명의 전용 얼굴 제작이 아니다. 이후 고정 등장인물 설정이 확정되면 전용 명시 매핑으로 확장할 수 있다.

초기32×32 아이콘과 생성 코드는 fallback으로 보존했다. `ProjectBuilder.Generate`는 `Portraits/`, `Constellations/` 새 폴더를 덮어쓰지 않는다. 새 `PixelAssetImporter`는 Point/무압축/밉맵 없음/NPOT 비율 유지/Clamp를 설정한다. 큰 초상화는 작은 아이콘의 최소1배 배치 규칙 대신 전체 비율 맞춤으로 표시한다.

## 검증 범위

실행 결과는 루트 `TEST_REPORT.md`와 `TestResults/`를 기준으로 한다. 새 검사는12종 파일 구분/알파/모든 이름의 연결, 승인 서도윤 번호 유지, 원본 해상도와 import 설정, 두 성좌의 실제 이벤트→해금→도감 이미지/능력/소속/이야기/전체 비율을 확인한다. 실제 Windows QA는 정상 캠페인 완료 후 별도 QA 저장에서 두 성좌 이벤트 fixture를 표시하고 클릭해 도감까지 확인한다. Fixture를 자연 추첨 획득 검증으로 표현하지 않는다.

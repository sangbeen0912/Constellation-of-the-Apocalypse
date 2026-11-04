# 성좌20종 · 고유 장면 제작 기록

2026-10-30. 제작 방식: **내장 image_gen**. 공통 나폴레옹 이미지 참조와 정면 전신 포즈 강제를 제거하고, 18명 각각의 행동·시점·인물 크기·배경 공간을 설계해 새로 생성했다. 이미 승인된 나폴레옹/요그 v1은 유지했다. [전체 그림 보기](constellation-gallery.html), [최종 프롬프트18개 및 생성 원본 경로](constellation-manifest-v2.json), [20개 원본 크기·해시](constellation-metadata-v2.json).

현재 게임 파일은 `Assets/Resources/Art/Constellations/`의20개 PNG다. 모두1536×1024 RGB, 총53,006,428바이트(약50.55MiB). 다른 인물에 같은 PNG를 돌려 쓰지 않는다. 고유 구도 여부는 생성물20종을 직접 관찰했고, 해시 차이만으로 구도 품질을 판정하지 않았다. 거절된 획일적 구도 결과는 생성 폴더에만 남기고 Resources/빌드에 복사하지 않았다.

|성좌|구도와 행동|현재 파일|자료|
|---|---|---|---|
|세종대왕|집필 중인 세종과 큰 작업대 · 사선 위 시점|sejong-v2.png|[자료](https://www.hangeul.go.kr/en/exhi/dailyExhibition.do)|
|잔다르크|오를레앙을 향한 기마 지휘 · 깃발과 상승 대각선|joan-v2.png|[자료](https://www.orleans.fr/fileadmin/orleans/MEDIA/presse/Dossier_de_presse_A4_JdA_2024_vDEF.pdf)|
|나폴레옹|지도와 제국을 배경으로 한 황제 · 기존 승인본|napoleon-v1.png|[기존 제작 기록](ART_ASSETS.md)|
|제갈량|군세를 내려다보는 책사 · 인물을 작게 둔 광역 원경|zhuge-v2.png|[자료](https://www.museum.go.kr/JPN/contents/E0403000000.do?relicId=5760&schM=view&searchId=search)|
|레오나르도 다빈치|비행 장치의 설계 · 전경 기어로 깊이를 만든 작업실|leonardo-v2.png|[자료](https://www.rct.uk/collection/stories/leonardo-in-the-royal-collection)|
|알렉산드로스|창을 뻗은 기마 돌격 · 낮은 시점|alexander-v2.png|[자료](https://www.clevelandart.org/art/1927.209)|
|니콜라 테슬라|거대한 고전압 실험실 · 작은 독서 인물과 크기 대비|tesla-v2.png|[자료](https://tesla-museum.org/en/qr-en/exhibit-025/)|
|화타|환자를 진찰하는 화타 · 손과 맥진에 집중|hua-v2.png|[자료](https://wellcomecollection.org/works/pct7xu6a)|
|레오니다스|방패 뒤에 낮게 버틴 방어 자세 · 협곡|leonidas-v2.png|[자료](https://collections.louvre.fr/en/ark%3A/53355/cl010065425)|
|미야모토 무사시|회전하는 이도류 동작 · 좌측 실루엣과 여백|musashi-v2.png|[자료](https://www.britishmuseum.org/collection/term/BIOG3948)|
|마리 퀴리|광물과 계측기를 살피는 퀴리 · 근접 사선|curie-v2.png|[자료](https://www.nobelprize.org/womenwhochangedscience/stories/marie-curie.)|
|클레오파트라|나일강 선단을 바라보는 여왕 · 배 위 측면 좌상|cleopatra-v2.png|[자료](https://www.britishmuseum.org/collection/object/C_EH-p691-1-CleAnt)|
|이순신|수군을 지휘하는 이순신 · 어깨 너머 해협|yi-v2.png|[자료](https://www.kh.or.kr/visit/en/road/8/view.do?key=2408010008)|
|칭기즈칸|초원과 행군 대열 · 후면 원경|genghis-v2.png|[자료](https://chinggismuseum.com/en/visit)|
|아르테미스|가지 위에 웅크린 활 사냥 · 비대칭 삼각 구도|artemis-v2.png|[자료](https://www.metmuseum.org/art/collection/search/255695)|
|헤라클레스|하늘의 무게를 버티는 시련 · 압도적 천구|heracles-v2.png|[자료](https://gulbenkian.pt/museu/en/olympic-gods-in-the-gulbenkian-collection/herakles/)|
|아서왕|돌에서 검을 뽑는 순간 · 가파른 내려다보기|arthur-v2.png|[자료](https://cdn.essentiels.bnf.fr/uploads/media/attachment/20221123153439000000_fiche_2.pdf)|
|제우스|번개를 던지는 천둥신 · 신전 아래에서 올려다보기|zeus-v2.png|[자료](https://www.getty.edu/art/collection/object/1GCAQ6)|
|하데스|저승을 관장하는 왕 · 우측 왕좌와 좌측 케르베로스|hades-v2.png|[자료](https://www.getty.edu/publications/terracottas/catalogue/60/)|
|요그 소토스|구체와 차원의 문 · 기존 승인본|yog-v1.png|[기존 제작 기록](ART_ASSETS.md)|

## 자료와 재해석 구분

역사/전통 자료는 인물의 특징과 상징을 찾는 근거이며 완벽한 고증 복원 선언이 아니다. 잔다르크의 말·복식·깃발은 후대 기념 및 재현 자료와 구분한다. 제갈량/화타는 전통 도상이며 실제 생전 초상으로 단정하지 않는다. 레오니다스 자료는 후대 다비드의 그림이다. 무사시 자료는 기관의 인물 목록이며 전체 페이지 접근이 제한됐으므로 상세 복식의 확인 근거로 삼지 않는다. 과도한 발광·구름·배경 건축은 게임의 판타지 해석이다.

헤라클레스는 [아틀라스 대신 하늘을 지탱하는 시련](https://static.perseus.tufts.edu/Herakles/apples.html)을 선택해 몽둥이를 든 정자세와 구별했다. 아서의 돌 속 검과 호수에서 받는 검의 전승을 단일한 역사 사실로 합치지 않는다. 테슬라의 번개 속 독서는 상징적 장면이며 실제 안전한 실험 상황의 증거가 아니다. 퀴리의 광채도 방사선의 실제 가시 색상이라고 단정하지 않는다. 이순신의 해협 지휘 장면은 게임용 구성으로, 함선 한 척 한 척과 장식의 정확한 사료 복원을 주장하지 않는다. 요그는 실제 민속 신화가 아닌 러브크래프트 문학의 존재다.

## 연결과 최적화

20개 고정 ID를 버전 파일로 명시 연결한다. 이벤트에서 이야기 확인→영구 해금→획득 도감 상세로 이어지고, 이름/성좌명/급수·소속·능력 이름·효과·획득 당시 본문을 기존 구조에 표시한다. 전체 그림 비율과 UV를 유지한다. 미획득 도감 숨김, 급수 접기, 휠 스크롤은 유지한다.

원본을 재압축·축소하지 않고 Point/밉맵 없음/CPU 읽기 복사 없음으로 가져온다. `ConstellationArtLease`로 화면이 필요한 그림만 소유한다. 같은 그림을 보는 화면은 공유하고, 마지막 사용자가 닫히면 Resources.UnloadAsset으로 실제 텍스처를 반환한다. 따라서20개를 열람해도 사용한20장을 계속 쌓아두지 않는다. 한 게임 화면은 큰 성좌 그림1개를 사용한다. NPC256px import 및 빌드 LZ4HC 압축은 유지한다. 소스 디스크 용량, 텍스처 메모리, 전체 프로세스 메모리를 혼동하지 않는다.

자동 검사와 실제 Windows 플레이어 결과는 루트 TEST_REPORT.md에 기록한다. 게임의20종 fixture 캡처는 TestResults/PlayerQA/15-*-event.png 및16-*-codex.png다. 자연 캠페인 추첨 통계와 아트 fixture 검증은 별개다.

최종 2026-10-30 02:13 KST: Edit Mode87/87, Play Mode5/5, Windows 빌드 및 실제 플레이어20종 fixture PASS. 실제 성좌 텍스처 최대1장/6,291,904바이트, 닫은 뒤0장.20개 원본의1536×1024 해상도는 그대로 유지했다. 이는 성좌 그림 메모리의 측정값이며 전체 프로세스 메모리나 전체 소스 용량 감소량이 아니다.


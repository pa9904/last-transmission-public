# v0.4 UI 참고 작품과 적용 원칙

확인: 2026-11-16. 제작자가 공개한 공식 Steam 상점 스크린샷을 비교했습니다. 아래 내용은 본 프로젝트의 화면 분석·디자인 판단이며 원작 제작자의 권장 사항이나 협업을 뜻하지 않습니다.

| 참고 작품 / 원본 | 관찰한 구성 | 마지막 교신에 적용한 원칙 |
|---|---|---|
| [Papers, Please](https://store.steampowered.com/app/239030/Papers_Please/) / [공식 홈페이지](https://papersplea.se/) | 문서가 실제 책상 위 서류처럼 보이며 문서·규칙집·업무 공간의 재질과 위치가 구분됨 | 근무 기록을 서류철과 종이 원문으로 분리. 상황판 선택은 종이에 기록하는 메모로 표현. 장치 화면과 문서의 대비를 확보 |
| [Stories Untold](https://store.steampowered.com/app/558420/Stories_Untold/) | 전자 장치의 외함·표시창·물리적 스위치가 하나의 공간 안에 놓이고, 화면 정보가 장치에 속해 보임 | 무전 주파수를 큰 수신기 표시창과 선택 스위치에 배치. 출입 제어에 금속 외함·나사·3회 확인등·관측 보고를 구분 |
| [Observation](https://store.steampowered.com/app/906100/Observation/) | 현재 위치·장치 상태·조작 힌트가 구분되고, 시스템 조작과 관찰 대상의 초점이 명확함 | 상단 상태 표시와 하단 장치 메뉴 분리. 아이콘·선택 강조·단축키를 일관되게 배치. 진행·일반 조작·위험한 결정을 서로 다른 버튼으로 표시 |

직접 비교한 공식 스크린샷 예시:

- [Papers, Please / 서류·규칙집](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/239030/ss_4b50458fbc4afe2917803b59a35afb3e5d61c960.1920x1080.jpg)
- [Stories Untold / 장치와 표시창](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/558420/ss_0fbc97d86f6d2bd3a0c981a7dc557573ffa81a77.1920x1080.jpg)
- [Observation / 시스템 조작](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/906100/ss_81f4ff971e71e33c85afa57aaa1c4c1a83962b03.1920x1080.jpg)

참고 이미지·로고·아이콘·폰트·UI 파일·사운드는 게임이나 저장소에 복사하지 않았습니다. 비교용 다운로드는 Git에서 제외한 `Artifacts/Reference-v04/`에만 두었습니다. 새 외함, 종이 질감, 아이콘과 버튼은 프로젝트 코드로 직접 그립니다. 원작의 이미지에 본 프로젝트 라이선스를 적용하지 않습니다.

## 구현

- `Assets/Scripts/InterfaceUI.cs`: 공통 외함·종이·버튼·아이콘·장치 메뉴·무전·서류철·출입·일시정지·확인창.
- `CaseworkUI.cs`: 원문과 연결되는 종이 판단 카드. 선택을 정답으로 자동 판정하지 않으며 기존 송신 후 피드백을 유지.
- `QualityUI.cs`: 읽기·음량 설정과 여러 회차 엔딩 보관. 본문 크기 100~130% 유지.
- `MapUI.cs`: v0.3.1 지형도와 지점·확대 원문을 보존하고 공통 버튼·장치 메뉴를 적용.

주파수·교신·응답·문서·상황판·출입의 데이터와 엔딩 조건은 그대로 사용합니다. 일반 버튼음은 없으며 기존 교신·출입·노크만 재생합니다. 큰 소리나 점멸을 새로운 UI 효과로 추가하지 않았습니다.

# 외부 에셋과 재배포 조건

공식 페이지 및 실제 포함 라이선스 확인: 2026-11-11. 구매·기부·로그인·유료 에셋 없음.

| 이름 / 제작자 | 실제 사용 파일 | 원본 배포처 | 라이선스와 확인 위치 | 상업 게임 / 표시 / GitHub 원본 포함 |
|---|---|---|---|---|
| Sci-fi Sounds 1.0 / Kenney | computerNoise_000.ogg, doorClose_000.ogg | https://kenney.nl/assets/sci-fi-sounds | 공식 페이지 CC0 표기, 실제 ZIP License.txt → `Assets/Resources/Audio/Kenney-License.txt` | 상업용 가능. 표시 의무 없음(자발적으로 크레딧 기재). CC0이므로 파일 재배포 가능. 실제 사용 2개와 라이선스만 포함. 로고는 사용하지 않음. |
| SUIT v2.0.5 / SUNN | SUIT-Regular.ttf, SUIT-Bold.ttf (수정 없음) | https://github.com/sun-typeface/SUIT/releases/tag/v2.0.5 | SIL OFL 1.1. 공식 태그의 LICENSE와 Fonts/SUIT-OFL.txt | 상업 게임에 포함 가능. 저작권과 OFL 사본을 함께 보관해 GitHub와 빌드에 재배포할 수 있습니다. 글꼴 단독 판매 금지. |

Kenney 공식 상업 이용·표시 안내: https://kenney.nl/support . 다운로드 페이지의 개별 무료 다운로드와 CC0 조건을 확인했습니다. 컴퓨터에 이미 있던 동일한 공식 배포 ZIP에서 사용 파일을 취득했고 실제 License.txt도 확인했습니다. v0.2.1에서 일반 버튼 클릭음을 제거하면서 사용하지 않는 computerNoise_003.ogg도 제외했습니다. ZIP 전체를 불필요하게 커밋하지 않았습니다.

SUIT는 제작자 공식 v2.0.5 태그의 Static TTF와 OFL을 다운로드했습니다. 각 라이선스는 Unity Resources에 보관되고 Windows 배포 폴더의 ThirdPartyNotices에도 복사됩니다. 별도의 에셋 설치 단계는 없습니다.

파일 SHA256은 아래와 같습니다. 에셋을 교체할 때 이 값은 달라집니다.

```
SUIT-Regular.ttf : 88C94C990B9996C558666EF07C11664AB177C3BE191E2846950B48F245D02274
SUIT-Bold.ttf    : E9B000DCC7A21BC82289FA7D159119ED6DBAE621FABDB2553A104394A208E23D
computerNoise_000.ogg   : 1527944E16EB14B48EE03FE3E7CE6AAE94262833A4E1F83928D451A7414FE4E1
doorClose_000.ogg       : C9134651EBBD7C016F90393AEE495FF8120BD343BB7569B689A7506987EBD6C2
```

## 프로젝트 생성 배경 (외부 무료 다운로드와 구분)

`Assets/Resources/Art/ControlRoom-v2.png`: 이 프로젝트를 위해 built-in image_gen으로 생성한 관제실 원화. 제작자 공식 배포처에서 받은 CC0 에셋이 아닙니다. 외부 스톡 원본이나 작가 이름을 사용하지 않았으며 프로젝트에 생성 원본을 포함합니다. [생성 도구·최종 프롬프트·저장 경로](ART_PROMPTS.md)를 별도로 기록했습니다. 이 파일은 본 저장소 LICENSE의 프로젝트 제작물 범위로 제공합니다.

## 직접 제작

- 지도·서류·상황판·확인등·장치 UI: Unity에서 그리는 2D 그래픽. 관제실 바탕은 위 AI 생성 원화이며, v0.1의 기하학적 배경은 누락 시 대체 렌더링으로 보존했습니다.
- v0.3.1 지형도 바탕 `Assets/Resources/Art/RescueTerrain-v1.png`: 직접 작성한 `Tools/MakeMapTerrain.cjs`로 생성한 가상 지형. 수학적 높이장·등고선·종이 질감·수목을 결정적으로 그립니다. 외부 지도, 위성사진, AI 생성 이미지나 스톡 에셋을 사용하지 않습니다. 경로·문자·건물·출입 확대도는 `MapUI.cs`에서 별도로 그립니다. 프로젝트 MIT 범위이며 상업 사용·GitHub 원본 재배포 가능, MIT 고지 유지. 원본 배포처는 본 저장소, 자세한 재현 정보는 [MAP_ART.md](MAP_ART.md).
- 눈보라, 미세한 화면 잡음, 예비 전원 색 변화: 런타임 그래픽.
- 바람, 무전 반송파, 문밖 노크: 코드로 생성한 PCM. 별도 음원 라이선스 없음.
- 게임 대사, 단서, 분기, UI, 진행·저장 코드: 프로젝트에서 직접 작성.
- v0.4의 금속 외함·종이 질감·장치 아이콘·버튼·확인등 UI: `InterfaceUI.cs` 등에서 직접 그린 그래픽. 외부 UI 패키지 없음. 참고 작품의 원본 그림·파일은 게임과 Git에 포함하지 않으며 [UI_REFERENCES.md](UI_REFERENCES.md)에 원본 링크와 적용 원칙만 기록. 직접 작성한 부분은 프로젝트 MIT 범위.
- 성우 음성, 음악, Asset Store 원본 패키지: 사용하지 않음.

## 취득 재현

Kenney 페이지에서 무료 개별 ZIP을 내려받아 Audio 하위의 위 2개 OGG와 License.txt를 복사합니다. 글꼴은 SUIT v2.0.5 태그의 fonts/static/ttf에서 Regular와 Bold를 받고, 같은 태그의 LICENSE를 SUIT-OFL.txt로 복사합니다. 저장소에 둘 다 포함되어 있으므로 일반 사용자는 이를 다시 할 필요가 없습니다.

다른 에셋으로 교체할 때에는 그 파일의 상업 이용과 소스 재배포 조건을 새로 확인해야 합니다. 무료 다운로드라는 이유만으로 이 목록의 허용 조건을 다른 파일에 적용하지 않습니다.

0.5.0의 창문 서리, 표지등, 문 그림자, 수신 용지는 기존 관제실 원화 위에 Unity로 그립니다. 바람 새는 소리, 계전기, 마찰음, 반송파는 실행 시 프로젝트 코드에서 합성합니다. 이 연출에는 새 외부 파일을 사용하지 않습니다.
v0.6.0 배경 음악은 프로젝트가 직접 만든 7개 편곡으로, Tools/MakeScore.cjs에서 Assets/Resources/Score의 WAV 원본을 재생성합니다. 프로젝트 MIT 조건으로 배포합니다. 위 Kenney 효과음 두 파일은 이전 버전 호환 자료로 포함하며 현재 플레이 중에는 재생하지 않습니다.

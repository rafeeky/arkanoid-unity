# Unity 에디터 작업 가이드 (Phase 3~5 마무리)

작성: 2026-05-25. 코드 포팅은 100% 완료 (commit `b7f1085`). 이 문서는 Unity 에디터에서 *클릭*으로 해야 할 작업만 정리.

목표: 이 문서 끝까지 따라가면 **Title → InGame → GameOver** 가 실제로 동작하는 빌드가 나옴.

> 순서는 의존 관계대로. **상위 단계 안 하면 하위가 작동 안 함**.
>
> 각 단계 끝에 ✅ **체크포인트** — 다음 단계로 넘어가도 좋은지 자기 점검.

---

## 0. 사전 점검

| 항목 | 확인법 |
|---|---|
| Unity 버전 | `Help → About Unity` → `Unity 6000.3.12f1` |
| 빌드 타겟 | `File → Build Profiles` → Android Switched (이미 됨) |
| 컴파일 에러 | `Window → General → Console` → 빨간 ❌ 없어야 함 |

❗ Console 에 에러가 있으면 *이 가이드 진행 금지*. 메시지 캡처해서 Claude에게 보고.

---

## 1. Package Manager — Input System 설치

**왜**: 코드 `UnityInputSnapshotBuilder.cs` 가 `UnityEngine.InputSystem.Keyboard` / `Pointer` 를 사용. 이 패키지 없으면 컴파일 안 됨.

### 단계

1. `Window → Package Manager` 열기
2. 좌상단 드롭다운 → **Unity Registry**
3. 검색창에 `Input System` → **Install**
4. 팝업 "Enable the new Input System backends?" → **Yes** (에디터 재시작됨)
5. 재시작 후 `Edit → Project Settings → Player → Other Settings → Active Input Handling` 가 `Input System Package (New)` 또는 `Both` 인지 확인

### ✅ 체크포인트

- Console 에서 `Keyboard.current` 관련 에러 사라짐
- `Edit → Project Settings → Input System Package` 메뉴가 생김

---

## 2. ScriptableObject `.asset` 13개 생성

**왜**: 게임플레이 / 레벨 / 텍스트 / 오디오 등 *데이터*는 코드에 박혀 있지 않고 `.asset` 파일로 존재. 이걸 만들고 Inspector 에서 값을 채워야 동작.

### 폴더 준비

`Assets/` 우클릭 → `Create → Folder` 로 다음 폴더 생성:
```
Assets/Data/
  Gameplay/
  Stages/
  Blocks/
  Items/
  Spinners/
  Presentation/
```

### 각 SO 생성 — `Assets/Data/<폴더>` 안에서 우클릭 → `Create → Arkanoid → ...`

| # | 메뉴 | 위치 | 갯수 | 채울 값 (참고 — TS `definitions/tables/*.ts`) |
|---|---|---|---|---|
| 1 | `Gameplay/Config` | `Data/Gameplay/` | 1 | InitialLives, BallSpeed, RoundIntroDurationMs 등. TS `gameplayConfigTable.ts` 그대로. |
| 2 | `Gameplay/Difficulty Config` | `Data/Gameplay/` | 2 | `Difficulty_Normal`, `Difficulty_Hard` 두 장. TS `difficultyConfigTable.ts`. |
| 3 | `Gameplay/Layout Config` | `Data/Gameplay/` | 1 | PlayfieldHeight=900, CanvasRegion 등 TS `layoutConfigTable.ts` (옵션 iii). |
| 4 | `Gameplay/Block Definition` | `Data/Blocks/` | TS Table 개수 (~6) | `Block_blue`, `Block_red`, `Block_silver` ... DefinitionId / BaseColor (Hex) / Hits / Score / DropItem. |
| 5 | `Gameplay/Item Definition` | `Data/Items/` | 3 | Item_Expand / Item_Magnet / Item_Laser. ItemType, FallSpeed, DurationMs. |
| 6 | `Gameplay/Spinner Definition` | `Data/Spinners/` | TS 개수 | DefinitionId / Radius / AngularSpeed. |
| 7 | `Gameplay/Stage Definition` | `Data/Stages/` | 3 (Round 1~3) | DefinitionId, BlockPlacements (x/y/blockDefId 배열), BorderPlacements, DoorPlacements. **TS `stageDefinitionTable.ts` 행 그대로 옮기기.** |
| 8 | `Presentation/UI Text Table` | `Data/Presentation/` | 1 | TextId → 문자열. TS `uiTextTable.ts` 전체. |
| 9 | `Presentation/Audio Cue Table` | `Data/Presentation/` | 1 | CueId → EventType + ResourceId + Pitch + Volume. TS `audioCueTable.ts`. |
| 10 | `Presentation/Intro Sequence` | `Data/Presentation/` | 1 | 인트로 스토리 페이지별 텍스트 + 머무는 시간. TS `introSequenceTable.ts`. |
| 11 | `Presentation/Mascot` | `Data/Presentation/` | 1 | MascotId → 표시명 / 잠금 해제 조건. |
| 12 | `Presentation/Trail Style Table` | `Data/Presentation/` | 1 | GoldenSun / BlueMeteor / Sunset 3 종. 각 색 hex + segmentCount. |
| 13 | `Presentation/Powerup Table` | `Data/Presentation/` | 1 | 파워업 토큰 정의 (있으면). |

### 데이터 입력 팁

- TS 표가 가장 정확한 소스. WSL 에서 `\\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\src\definitions\tables\` 열어보면 됨
- Hex 색은 **정수 형식** (예: `#FF6633` → `16737843`). Inspector 에 정수로 입력
- `StageDefinition` 의 BlockPlacements 는 배열이 길어서 시간 많이 듦 → **3 라운드 중 1 라운드만 먼저** 채우고 나머지는 나중에

### ✅ 체크포인트

- `Assets/Data/` 안에 13개 `.asset` 파일 존재
- 각각 더블클릭 → Inspector 에 값 들어 있음
- 정수 hex 색 입력했을 때 *Inspector 미리보기 색깔* 이 의도한 색

---

## 3. Prefab 생성

**왜**: Renderer (`BallsRenderer` 등) 가 `Instantiate(ballPrefab)` 으로 풀에 인스턴스 만듦. Prefab 이 없으면 게임 오브젝트가 화면에 안 나타남.

### 폴더 준비

`Assets/Prefabs/` 폴더 생성.

### 각 Prefab 만드는 일반 절차

```
Hierarchy 빈 곳 우클릭 → 2D Object → Sprites → Square (또는 다른 도형)
└→ 이름 변경
└→ Inspector 에서 SpriteRenderer 의 sprite/color/size 조절
└→ Hierarchy 의 GameObject 를 Assets/Prefabs/ 폴더로 드래그 (Prefab 화)
└→ Hierarchy 에서 원본 삭제
```

### 만들 Prefab 목록

| Prefab | 자식 컴포넌트 | 크기 (px) | 비고 |
|---|---|---|---|
| `Ball.prefab` | SpriteRenderer (원) | 14×14 | Sprite: Knob 또는 직접 그린 원 |
| `Bar.prefab` | SpriteRenderer (직사각형) | 120×16 | width 는 코드가 scale.x 로 조절 |
| `Block.prefab` | SpriteRenderer | 60×30 | 모든 블록 공통 — 색은 코드가 BlockDefinition 에서 |
| `Border.prefab` | SpriteRenderer | 30×30 | 정적 회색 |
| `Door.prefab` | SpriteRenderer | 60×30 | 짙은 회색 |
| `Spinner.prefab` | SpriteRenderer | 40×40 | 회전체 — sprite 는 단순 다각형/별 |
| `Item.prefab` | SpriteRenderer | 24×24 | sprite 는 코드가 ItemType 별로 swap |
| `LaserShot.prefab` | SpriteRenderer (얇은 세로 막대) | 4×30 | 빨강 |
| `Trail.prefab` | TrailRenderer | — | `Add Component → Effects → Trail Renderer`. Time/Width 는 코드가 덮어씀 |
| `Mascot.prefab` | SpriteRenderer | 자유 | sprite 는 MascotRenderer 가 ID 로 swap |

> **`expandSprite/magnetSprite/laserSprite`** : `Item.prefab` 자체가 아니라 ItemsRenderer 의 Inspector field. Sprite 3장 미리 import 해 두기. 임시로는 동일 sprite 3개 써도 됨.

### ✅ 체크포인트

- `Assets/Prefabs/` 에 10개 prefab 존재
- 각 prefab 더블클릭 → 미리보기에 도형 보임

---

## 4. Scene 구성

**왜**: GameManager 가 어디 있고 Camera 가 어디 있고 Canvas 가 어디 있는지 — Scene 이 다 묶는다.

### Scene 만들기

1. `File → New Scene` → 2D 템플릿 선택 → `Ctrl+S` 로 `Assets/Scenes/Main.unity` 저장
2. `File → Build Profiles → Add Open Scenes` 로 빌드 포함

### Main Camera 설정

Hierarchy 의 `Main Camera` 선택 → Inspector:
- **Projection**: Orthographic
- **Size**: `960` (D3.4: 1 unit = 1 px, 화면 높이 1920 px 가정)
- **Position**: `(480, 450, -10)` — 좌상단 (0,0) ~ 우하단 (960, 900) 영역 중심. **Y 좌표 부호는 TS 코드와 다르게 양수 사용** (Unity 화면 좌상단에 가깝게 보이도록)

> 📌 **알려진 보정 작업** (지금 무시 OK):
> 코드는 TS Y+ 아래 규약. Unity world 는 Y+ 위. 그래서 *위아래가 뒤집혀 보임* — 일단 빠르게 동작 확인 후, 보정은 별도 batch. 옵션:
> (a) GameManager.BindViews 에서 모든 Vector3 의 y 부호 반전
> (b) PlayfieldRoot scale.y = -1 + sprite 각각 scale.y = -1 로 상쇄
> 현 시점에서는 *동작* 만 보면 됨.

### Canvas (UI)

1. Hierarchy 우클릭 → `UI → Canvas`
2. Canvas Inspector:
   - **Render Mode**: Screen Space - Overlay
   - **UI Scale Mode**: Scale With Screen Size → Reference Resolution 1080×1920
3. Canvas 자식으로 빈 GameObject 7개 만들고 이름: `TitlePanel`, `IntroStoryPanel`, `RoundIntroPanel`, `InGamePanel`, `GameOverPanel`, `GameClearPanel`, `PauseOverlay`
4. 각 Panel 자식에 TMPro Text — `UI → Text - TextMeshPro` (`Window → TextMeshPro → Import TMP Essential Resources` 미실행이면 먼저)
   - 어떤 Panel 에 어떤 Text 들이 필요한지는 `Assets/Scripts/Presentation/View/*Panel.cs` 의 `[SerializeField] private TMP_Text xxx` 보면 됨
   - 예: GameOverPanel.cs → `gameOverLabel / finalScoreLabel / highScoreLabel / retryText` 4개

### GameManager 부착

1. Hierarchy 우클릭 → `Create Empty` → 이름 `GameManager`
2. Inspector → `Add Component` → `Game Manager` (Arkanoid.Presentation 네임스페이스)
3. 추가로 `Add Component` → `Screen Router` 도 같은 GameObject 에 (또는 별 GameObject 에)
4. **각 Panel 에 해당 Panel Script 부착**: `Title Panel.cs` → Canvas/TitlePanel GameObject 에, `Game Over Panel.cs` → Canvas/GameOverPanel 등

### Renderer 부착

Hierarchy 우클릭 → `Create Empty` → 이름 `PlayfieldRoot`. 그 자식으로 빈 GameObject 11개:
- `BarRendererGO`, `BallsRendererGO`, `BlocksRendererGO`, `BordersRendererGO`,
  `DoorsRendererGO`, `SpinnersRendererGO`, `ItemsRendererGO`, `LaserShotsRendererGO`,
  `BallTrailRendererGO`, `MascotRendererGO`

각각에 해당 Renderer script 부착 (Add Component).

### 와이어링 (가장 중요)

**`GameManager` Inspector 의 `[SerializeField]` field 들에 위에서 만든 자산/오브젝트 드래그**:

| Field 그룹 | Field | 드래그할 것 |
|---|---|---|
| Gameplay SO | gameplayConfigSO | `Data/Gameplay/Config.asset` |
| Gameplay SO | normalDifficultySO / hardDifficultySO | `Data/Gameplay/Difficulty_Normal.asset` / `Difficulty_Hard.asset` |
| Gameplay SO | blockDefinitionSOs | `Data/Blocks/*.asset` 전부 드래그 (List size 자동 증가) |
| Gameplay SO | expandItemSO / magnetItemSO / laserItemSO | `Data/Items/Item_Expand.asset` 등 |
| Gameplay SO | spinnerDefinitionSOs | `Data/Spinners/*.asset` |
| Gameplay SO | stageDefinitionSOs | `Data/Stages/*.asset` (순서가 라운드 순서) |
| Presentation SO | uiTextSO / introSequenceSO / audioCueSO / layoutConfigSO | 각 `.asset` |
| Camera + UI | mainCamera | Hierarchy 의 `Main Camera` |
| View | barRenderer / ballsRenderer / ... | Hierarchy 의 `BarRendererGO` 등 |
| View | titlePanel / ... / pauseOverlay | Canvas 자식 Panel GameObject 들 |
| View | toastView | (별도 ToastView GameObject 만들면 드래그) |
| Audio | unityAudio | (선택) UnityAudio MonoBehaviour 가 붙은 GameObject |

**각 Renderer GameObject 에도 Inspector 작업**:
- `BarRendererGO` → `Body` 필드에 Bar Prefab Instantiate 한 SpriteRenderer 또는 Inspector 에서 직접 자식 만들기
- `BallsRendererGO` → `Ball Prefab` 에 `Assets/Prefabs/Ball.prefab` 드래그
- `BlocksRendererGO` → `Block Prefab` + `Block Definition SOs` (각 BlockDefinitionSO 들)
- `BordersRendererGO` → `Border Prefab`
- 동일 패턴으로 나머지 7개

**Panel Inspector 작업**:
- `TitlePanel` GameObject 의 `Start Text / High Score Text / Difficulty Text` 필드에 자식 TMP_Text 드래그
- `GameOverPanel` → `Game Over Label / Final Score Label / High Score Label / Retry Text`
- 동일 패턴 6개 Panel

### ✅ 체크포인트

- `Main.unity` 저장
- `Play` 버튼 (▶) 클릭
- Title 화면 텍스트가 보임 + Console 에 `NullReferenceException` 없음
- ❌ 텍스트 안 보이면: TitlePanel 의 Active 상태 + Canvas Render Mode + TMP_Text 의 Color α 점검
- ❌ NRE 나면: 어떤 field 안 채워졌는지 메시지 보고 채우기

---

## 5. Input Actions 자산

**왜**: 현재 `UnityInputSnapshotBuilder` 는 `Keyboard.current` / `Pointer.current` 직접 폴링이라 `.inputactions` 없이도 동작. 하지만 *향후 게임패드 / 모바일 터치 가상 컨트롤* 위해 자산화 권장.

### 단계 (필수는 아님 — 키보드만 쓸 거면 SKIP)

1. `Assets/` 우클릭 → `Create → Input Actions` → `ArkanoidInput.inputactions`
2. 더블클릭 → Action Maps 추가:
   - **Gameplay** (PlayerInput map)
     - `Left` (Button) → Binding: `<Keyboard>/leftArrow`, `<Keyboard>/a`
     - `Right` (Button) → `<Keyboard>/rightArrow`, `<Keyboard>/d`
     - `Action` (Button) → `<Keyboard>/space`
     - `Pause` (Button) → `<Keyboard>/p`
     - `QuitToTitle` (Button) → `<Keyboard>/q`
   - **UI**
     - `Submit`, `Cancel`, `Navigate` (PlayerInput 표준)
3. **Save Asset**

### ✅ 체크포인트

- `.inputactions` 더블클릭 시 좌측에 Gameplay/UI 두 map 표시
- 각 Action 의 Binding 에 Keyboard 항목 보임

---

## 6. Audio (선택)

UnityAudio 어댑터를 안 쓰면 NoopAudio (사운드 없음) 로 동작. 사운드 넣으려면:

1. `Assets/Audio/` 폴더 + `.wav` / `.mp3` 파일 import
2. Hierarchy 의 GameManager 옆에 빈 GameObject → `Add Component → Unity Audio`
3. `Add Component → Audio Source` 2개 (Bgm/Sfx 분리)
4. UnityAudio 컴포넌트 Inspector:
   - `Clip Table` 에 ResourceId ↔ AudioClip 매핑 입력 (AudioCueSO 에 쓴 ResourceId 와 동일해야 함)
   - `Bgm Source` / `Sfx Source` 필드에 위 AudioSource 드래그
5. GameManager 의 `Unity Audio` 필드에 이 GameObject 드래그

---

## 7. Player Settings + Build

1. `File → Build Profiles → Player Settings`
   - Company Name: 아무거나
   - Product Name: `Arkanoid`
   - **Default Orientation**: Portrait (세로) — 알바트로스는 세로 모바일
   - Other Settings → **Scripting Backend**: Mono (개발용, 학습 목적). 출시 시 IL2CPP+ARM64.
   - Other Settings → **Target Architecture**: ARMv7
   - Icon: Default 또는 직접 만든 png
2. `File → Build Profiles → Build` → 출력 폴더 선택
3. (USB 디버깅 켠 안드로이드 연결되어 있으면) `Build And Run`

### ✅ 체크포인트

- `.apk` 파일이 생성됨
- 폰에서 설치 → 실행 → Title 화면

---

## 8. 트러블슈팅

| 증상 | 원인 후보 | 해결 |
|---|---|---|
| 화면이 검정만 | Camera Size 잘못 / Canvas Render Mode | Camera Size=960 / Canvas Overlay |
| 텍스트 안 보임 | TMP Essential Resources import 안 됨 | `Window → TextMeshPro → Import TMP Essential Resources` |
| `Play` 누르자마자 NullReferenceException | GameManager Inspector field 빈 칸 | Console 메시지의 라인 → 해당 field 채우기 |
| Block 다 같은 색 | blockDefinitionSOs 가 비어 있음 | BlocksRenderer Inspector 에 `.asset` 다 드래그 |
| 공이 이동 안 함 | Stage Definition 의 Ball 초기 위치 또는 BallSpeed=0 | GameplayConfig.BallSpeed 확인 |
| 화면 상하 반전 | 알려진 Y flip 보정 미적용 (위 §4 메모) | 별도 batch — 우선 무시 가능 |
| 빌드 실패 — "Input System" | Active Input Handling 미설정 | Player → Active Input Handling = `Both` |

---

## 9. 진행 순서 추천

1. §1 Input System 설치 (5 분)
2. §2 SO 중 우선 `Config`, `Difficulty_Normal`, `Layout`, `UI Text`, `Audio Cue` 5장만 생성 + 값 입력 (30 분)
3. §3 Prefab 중 우선 `Ball / Bar / Block / Border` 4개만 (15 분)
4. §4 Scene 까지 만들고 §4 와이어링은 *부분만* (Title + InGame Panel 만 우선) → **Play 눌러서 Title 텍스트 보이는 지 확인** (체크포인트)
5. 안정되면 나머지 SO + Prefab + Panel 추가 (1~2시간)
6. §5 Input Actions (선택, 30 분)
7. §6 Audio (선택)
8. §7 Build

> **하루에 다 끝내려 하지 말 것**. SO 데이터 입력만 해도 2~3 시간. *§4 의 Play 첫 동작 체크* 가 가장 큰 마일스톤.

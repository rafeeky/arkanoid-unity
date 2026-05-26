# 05 Full Porting Plan v2 — TS Phaser → Unity 6

작성: 2026-05-25. `04_ts_parity_audit.md` 의 누락 분석 + [[feedback_inventory_first]] 학습 반영.

**목표**: 이 계획표를 위에서 아래로 따라가면 *사용자는 마지막에 Play test 만 하면 되는 수준* 으로 TS Phaser 와 시각/UX 가 1:1 일치하는 Unity 빌드 완성.

**전제**: 새 Unity 프로젝트 처음부터 시작 가정. 기존 프로젝트 재사용 시 Phase 0 끝나면 jump 가능.

---

## 핵심 원칙 (이전 사고 반영)

1. **Phase 0 = Inventory** (코드 + 자산 모두 ls -R). 가정 X, 검증 O.
2. **자산 import 가 별도 Phase** (코드 phase 와 분리).
3. **각 task atomic** — "Renderer 10개" 같은 추상 금지. 각 Renderer 의 SerializeField 까지 명세.
4. **Wire 매트릭스 표** — 모든 SerializeField → drag 대상 1:1.
5. **각 Phase 끝 self-audit checklist** — 능동 점검.
6. **TS 가정 검증** — sprite 사용 여부, 색만 사용 여부, 폰트 family 직접 확인.
7. **Data 입력 시 갯수 확인** — entry count 가 TS table 과 매칭하는지 체크.

---

# Phase 0. Inventory (반드시 첫 단계)

## T0.1 TS 코드 전체 listing

```bash
# WSL 또는 Glob
ls -R \\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\src\
```

결과를 카테고리별 cnt 로 정리 (예시):

| 디렉토리 | .ts 파일 수 | 라벨 |
|---|---|---|
| shared/ | 3 | 포팅 (Result, Brand, easing) |
| gameplay/state/ | 9 | 포팅 (struct) |
| gameplay/entities/ | 6 | 포팅 (static) |
| gameplay/systems/ | 8 + tests | 포팅 |
| gameplay/controller/ | 3 + tests | 포팅 |
| gameplay/events/ | 1 | 포팅 |
| flow/ | 6 + 4 tests | 포팅 |
| definitions/types/ | 14 | 포팅 (record struct) |
| definitions/tables/ | 13 | SO 변환 |
| definitions/validators/ | 8 | **P2 — 옵션** |
| presentation/renderer/ | ~10 | Renderer 매핑 |
| presentation/views/ | ~8 | Panel 매핑 |
| presentation/view-models/ | 6 | 포팅 |
| presentation/controller/ | 4 | 포팅 |
| presentation/state/ | 1 | 포팅 |
| presentation/ui/ | Button/Toast/GlossyStyle 등 | Unity UI 로 대체 또는 P2 |
| audio/ | 4 | 포팅 (IArkanoidAudio, AudioCueResolver, NoopAudio, UnityAudio adapter) |
| input/ | 3 | 포팅 (InputSnapshot, KeyboardInputSource → UnityInputSnapshotBuilder, PointerInputSource → PointerToPlayfield) |
| persistence/ | 4 | 포팅 (SaveData, ISaveRepository, InMemory, **PlayerPrefs**(=LocalSaveRepository)) |
| assets/ | 2 | ❌ 불필요 (Phaser loader. Unity SerializeField wire) |
| app/ | AppContext + dev/ | AppContext → GameManager. dev/ P3 |
| app/dev/ | 4 | P3 (ReplayRecorder, CollisionLog, BallTrail dev, InvariantChecker) |

**합계 약 50+ .ts → 약 90+ .cs** (각 entity 가 state + 정적 함수로 분리됨)

## T0.2 TS 자산 전체 listing

```bash
ls -R \\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\public\assets\
```

결과 (실측 2026-05-25):

| TS 경로 | 갯수 | 카테고리 |
|---|---|---|
| sfx/*.wav + .mp3 | 14 | Audio |
| fonts/DNFBitBitv2.{ttf,otf} | 2 | Font |
| backgrounds/bg_title, bg_stage_01~03, bg_gameover, bg_gameclear, bg_pixel_01~03 | 9 | Background |
| mascots/<5 id>/frame0~3 | 20 | Mascot 4 frame |
| mascots/gameover_frame_1~4 | 4 | GameOver 마스코트 애니 |
| mascots/dance_sheet | 1 | Sprite sheet (용도 확인) |
| portraits/<5 id> | 5 | Mascot 잠금 해제용 |
| portraits2/<5 id> | 5 | Mascot 큰 일러스트 |
| borders/border_vertical, border_horizontal | 2 | Border |
| borders/door_closed, door_opening_frame0~4 | 6 | Door + 5 frame opening 애니 |
| blocks/block_basic, block_basic_drop, block_magnet_drop, block_laser_drop, block_tough | 5 | Block |
| intro/intro_story_01~04 | 4 | IntroStory illustration |
| gameplay/item_expand, item_magnet, item_laser | 3 | Item |

**합계 80 파일** (sfx 의 .wav+.mp3 dup 포함).

## T0.3 TS 가정 검증

다음 항목 TS 코드 grep 으로 직접 확인:

| 항목 | 확인 위치 | 결과 (입력) |
|---|---|---|
| Block sprite 사용 여부 | `src/presentation/renderer/inGame/renderBlocks.ts` (또는 유사) | sprite 사용 / 색만 사용 |
| Bar sprite 또는 graphics | `renderBar.ts` | |
| Ball sprite 또는 graphics | `renderBall.ts` | |
| Border sprite | `renderBorders.ts` | |
| Door sprite + 애니 | `renderDoors.ts` | |
| Spinner sprite | `renderSpinners.ts` | |
| Mascot 표시 위치/타이밍 | `GameScene.ts`, `TitleScene.ts` | Title 어디, InGame 어디 |
| 폰트 family | `GlossyStyle.ts` 또는 CSS | "Press Start 2P", "DNFBitBitv2" 등 |
| 카메라 zoom / centerOn 정확한 값 | `GameScene.ts` | zoom=1.5, centerOn(360, 450) |
| 입력 매핑 (left/right/space/p) | `KeyboardInputSource.ts` | KeyCode |
| SaveData 필드 | `persistence/SaveData.ts` | highScore, gold, mascotsUnlocked, masterVolume, etc |

## ✅ Self-audit Phase 0

- [ ] TS 자산 80개 모두 list 됐는가
- [ ] TS 코드 90+ 파일 list + 라벨링 됐는가
- [ ] 각 자산이 어디서 쓰이는지 매핑 됐는가 (sprite → 어느 Renderer)
- [ ] T0.3 의 11 가정 모두 검증 됐는가

---

# Phase 1. Unity Project Setup

## T1.1 Unity 프로젝트 생성

- Unity Hub → New Project → 2D (URP) → Unity 6000.3.12f1
- Build Target: Android (Switch Platform 후 진행)

## T1.2 Package Manager — Input System

1. Window → Package Manager → Unity Registry → Input System → Install
2. 팝업 "Enable new Input System backends?" → **Yes** (재시작)
3. Project Settings → Player → Other Settings → Active Input Handling = **Both**

## T1.3 ASMDef 6개

| asmdef 이름 | 위치 | autoReferenced | references |
|---|---|---|---|
| Arkanoid.Shared | Assets/Scripts/Shared/ | true | (없음) |
| Arkanoid.Gameplay | Assets/Scripts/Gameplay/ | false | Arkanoid.Shared, Arkanoid.Definitions |
| Arkanoid.Gameplay.Tests | Assets/Scripts/Gameplay.Tests/ | true | Arkanoid.Gameplay, UnityEngine.TestRunner, UnityEditor.TestRunner, NUnit |
| Arkanoid.Definitions | Assets/Scripts/Definitions/ | true | Arkanoid.Shared |
| Arkanoid.Flow | Assets/Scripts/Flow/ | false | Arkanoid.Gameplay, Arkanoid.Definitions, Arkanoid.Shared |
| Arkanoid.Presentation | Assets/Scripts/Presentation/ | false | All above + Unity.InputSystem, Unity.TextMeshPro |

## T1.4 csc.rsp

`Assets/csc.rsp`:
```
-langversion:10
-nullable:enable
```

## T1.5 Folder Structure (사전 생성)

```
Assets/
├── Scripts/
│   ├── Shared/
│   ├── Gameplay/
│   │   ├── State/, Entities/, Systems/, Controller/, Events/, Input/
│   ├── Gameplay.Tests/
│   ├── Definitions/
│   │   ├── Types/, Enums/, Tables/
│   ├── Definitions.SO/
│   ├── Flow/
│   └── Presentation/
│       ├── View/, ViewModels/, Controller/, Audio/, Input/, Persistence/, State/
├── Data/
│   ├── Gameplay/, Stages/, Blocks/, Items/, Spinners/, Presentation/
├── Prefabs/
├── Sprites/
│   ├── Backgrounds/, Mascots/, Portraits/, Portraits2/, Blocks/, Borders/, Intro/, Gameplay/
│   └── Mascots/GameOver/
├── Audio/
├── Fonts/
├── Scenes/
```

## ✅ Self-audit Phase 1

- [ ] Console 컴파일 에러 0
- [ ] 6 asmdef 모두 정상 (Inspector 에서 reference 확인)
- [ ] Input System 패키지 설치 + Both 설정
- [ ] 폴더 구조 위와 동일

---

# Phase 2. Asset Pipeline (자산 import — 별도 Phase)

## T2.1 자산 80개 일괄 복사

PowerShell 또는 Windows Explorer:

```powershell
$src = "\\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\public\assets"
$dst = "C:\Users\rimse\UnityProjects\Arkanoid-Unity\Assets"

# Audio
Copy-Item "$src\sfx\*.*" "$dst\Audio\"

# Fonts
Copy-Item "$src\fonts\*.*" "$dst\Fonts\"

# Backgrounds
Copy-Item "$src\backgrounds\*.png" "$dst\Sprites\Backgrounds\"

# Mascots (5 폴더 × 4 frame)
Copy-Item "$src\mascots\albatross\*.png" "$dst\Sprites\Mascots\albatross\" -Force
Copy-Item "$src\mascots\kongming\*.png" "$dst\Sprites\Mascots\kongming\" -Force
Copy-Item "$src\mascots\snowrabbit\*.png" "$dst\Sprites\Mascots\snowrabbit\" -Force
Copy-Item "$src\mascots\reaper\*.png" "$dst\Sprites\Mascots\reaper\" -Force
Copy-Item "$src\mascots\seraphin\*.png" "$dst\Sprites\Mascots\seraphin\" -Force
Copy-Item "$src\mascots\gameover_frame_*.png" "$dst\Sprites\Mascots\GameOver\"
Copy-Item "$src\mascots\dance_sheet.png" "$dst\Sprites\Mascots\"

# Portraits
Copy-Item "$src\portraits\*.png" "$dst\Sprites\Portraits\"
Copy-Item "$src\portraits2\*.png" "$dst\Sprites\Portraits2\"

# Borders (border + door)
Copy-Item "$src\borders\*.png" "$dst\Sprites\Borders\"

# Blocks
Copy-Item "$src\blocks\*.png" "$dst\Sprites\Blocks\"

# Intro
Copy-Item "$src\intro\*.png" "$dst\Sprites\Intro\"

# Gameplay items
Copy-Item "$src\gameplay\*.png" "$dst\Sprites\Gameplay\"
```

## T2.2 Sprite Import Settings (Unity Editor)

**모든 PNG**:
- Texture Type: Sprite (2D and UI)
- Pixels Per Unit: **1** (D3.4: 1 unit = 1 px)
- Filter Mode: Point (no filter) — 픽셀 아트
- Compression: None
- Wrap Mode: Clamp

**일괄 적용 방법**: Project 창에서 Sprites/ 전체 select → Inspector 한 번에 적용.

## T2.3 폰트 TMP SDF 변환

1. Window → TextMeshPro → Font Asset Creator
2. Source Font File: `DNFBitBitv2.ttf`
3. Sampling: Custom Characters
4. Character Set 입력 (영문 26 × 2 + 숫자 10 + 기호 + 자주 쓰는 한글 100자):
   ```
   ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789
   !@#$%^&*()_+-=[]{};:'",.<>/?\|`~
   가나다라마바사아자차카타파하... (TS UITextSO 의 모든 한글 글자 + 자주 쓰는 100자)
   ♥ (U+2665 하트)
   ```
5. Atlas Resolution: 1024×1024, Padding: 5
6. Render Mode: SDFAA
7. Generate → Save: `Assets/Fonts/DNFBitBitv2 SDF.asset`

8. Project Settings → TextMeshPro Settings → Default Font Asset = DNFBitBitv2 SDF
9. DNFBitBitv2 SDF 의 Inspector → Fallback Font Assets → MalgunGothic SDF (없으면 별도 생성 — 한글 fallback)

## T2.4 Audio Import Settings

**BGM (.wav, .mp3)**: Load Type = Streaming, Compression = Vorbis, Quality 70%
**SFX (.wav 짧은 거)**: Load Type = Decompress On Load, Compression = ADPCM

## ✅ Self-audit Phase 2

- [ ] Assets/Sprites/ 하위에 75+ sprite 모두 import (각 폴더 갯수 확인)
- [ ] Assets/Audio/ 에 14 audio
- [ ] Assets/Fonts/ 에 DNFBitBitv2.ttf + DNFBitBitv2 SDF.asset
- [ ] 모든 sprite PPU=1, Filter=Point
- [ ] TMP Default Font = DNFBitBitv2 SDF, fallback MalgunGothic SDF
- [ ] Console warning 0

---

# Phase 3. Code Porting (TS → C#)

## T3.1 Shared (Arkanoid.Shared)

| C# 파일 | TS 원본 | 비고 |
|---|---|---|
| Result.cs | `shared/Result.ts` | `Result<T,E>` struct, Ok/Err static factory |
| Brand.cs | `shared/Brand.ts` | C# typedef (struct wrapping primitive) |
| Easing.cs | `shared/easing.ts` | static class, linear/easeIn/easeOut/etc |
| Vec2.cs | (TS 는 tuple) | struct `(float X, float Y)` |

## T3.2 Definitions/Types/ (Arkanoid.Definitions)

14 record struct/class 포팅. 각 파일 1:1 매핑.

- BallState 같은 state 는 `Arkanoid.Gameplay.State` 로 (T3.4)
- 순수 데이터 정의는 `Arkanoid.Definitions.Types`

| C# | TS | 종류 |
|---|---|---|
| BlockDefinition.cs | BlockDefinition.ts | `record struct` (DefinitionId, MaxHits, Score, DropItemType, BaseColor int) |
| ItemDefinition.cs | ItemDefinition.ts | record struct |
| DifficultyConfig.cs | DifficultyConfig.ts | record struct |
| SpinnerDefinition.cs | | |
| StageBlockPlacement.cs / StageBorderPlacement.cs / StageDoorPlacement.cs / StageSpinnerPlacement.cs | | |
| StageDefinition.cs | | |
| IntroSequenceEntry.cs | | (text, durationMs, **illustrationId** string) |
| AudioCueEntry.cs | | (CueId, EventType, ResourceId, Pitch, Volume) |
| MascotDefinition.cs | | (Id, DisplayName, UnlockCondition, **portraitId**, **frameIds[4]**) |
| UITextEntry.cs | | (TextId, Value) |
| PowerupToken.cs | | |
| TrailStyle.cs | | |
| GameplayConfig.cs | | |
| LayoutConfig.cs | | |

**Enums** (Arkanoid.Definitions.Enums):
- ItemType, DifficultyKind, DropItemKind, BorderOrientation, TrailStyleId, DoorPhase, SpinnerPhase, BarEffect, FlowStateKind

## T3.3 Definitions.SO (Editor-only ScriptableObject 13개)

각 SO 는 Inspector 직접 참조용. CreateAssetMenu 경로 명시.

### T3.3.1 GameplayConfigSO.cs
```csharp
[CreateAssetMenu(menuName = "Arkanoid/Gameplay/Config")]
public class GameplayConfigSO : ScriptableObject {
    [SerializeField] private GameplayConfig _data;
    public GameplayConfig Data => _data;
}
```

### T3.3.2 DifficultyConfigSO.cs — 동일 패턴
### T3.3.3 LayoutConfigSO.cs — 동일

### T3.3.4 BlockDefinitionSO.cs ← **sprite 필드 포함**
```csharp
public class BlockDefinitionSO : ScriptableObject {
    [SerializeField] private string definitionId;
    [SerializeField] private int maxHits;
    [SerializeField] private int score;
    [SerializeField] private ItemType? dropItemType;
    [SerializeField] private int baseColor; // hex
    [SerializeField] private Sprite blockSprite; // ★ 추가
    public BlockDefinition Data => new(...);
    public Sprite Sprite => blockSprite;
}
```

### T3.3.5 ItemDefinitionSO.cs ← sprite 필드 포함
### T3.3.6 SpinnerDefinitionSO.cs
### T3.3.7 StageDefinitionSO.cs
### T3.3.8 AudioCueSO.cs

### T3.3.9 UITextSO.cs
- Data: List<UITextEntry>

### T3.3.10 IntroSequenceSO.cs ← **illustration sprite 필드 포함**
```csharp
[System.Serializable]
public struct IntroPageEntry {
    public string text;
    public float durationMs;
    public Sprite illustration; // ★
}
public class IntroSequenceSO : ScriptableObject {
    [SerializeField] private List<IntroPageEntry> pages;
    ...
}
```

### T3.3.11 MascotSO.cs ← **4 frame array + portrait**
```csharp
[System.Serializable]
public struct MascotEntry {
    public string id;
    public string displayName;
    public string unlockCondition;
    public Sprite[] frames; // 4
    public Sprite portrait;
}
```

### T3.3.12 PowerupSO.cs
### T3.3.13 TrailStyleSO.cs

## T3.4 Gameplay (Arkanoid.Gameplay)

### State (9)
- BallState, BarState, BlockState, BorderBlockState, DoorState, ItemDropState, LaserShotState, SpinnerRuntimeState, GameSessionState — `record struct`
- GameplayRuntimeState — `class` (mutable)

### Entities (6)
- Ball, Bar, Block, BorderBlock, Door, Spinner, Wall — `static class` (순수 함수)

### Systems (8 + helpers)
- PlayfieldLayout (좌표 SSOT)
- CollisionService (Circle-AABB)
- CollisionResolutionService
- MovementSystem (sub-step sweep)
- StageRuleService
- BarEffectService
- LaserSystem
- SpinnerSystem
- StageRuntimeFactory

### Controller (3)
- InputCommandResolver
- GameplayController (Tick)
- GameplayLifecycleHandler

### Events
- GameplayEvents.cs — sealed class hierarchy (14 종)

### Input (POCO, asmdef 안에 둠)
- InputSnapshot.cs

## T3.5 Flow (Arkanoid.Flow)

- GameFlowState.cs (FlowStateKind enum + state)
- FlowEvents.cs / PresentationEvents.cs
- FlowTransitionPolicy.cs
- FlowInputResolver.cs
- FlowLifecycleHandler.cs
- GameFlowController.cs

## T3.6 Presentation (Arkanoid.Presentation)

### ViewModels (6)
- HudViewModel, TitleScreenViewModel, IntroScreenViewModel, RoundIntroViewModel, GameOverViewModel, GameClearViewModel

### State (1)
- ScreenState.cs

### Controller (4)
- HUDPresenter, ScreenPresenter, ScreenDirector, VisualEffectController

### Audio (4)
- IArkanoidAudio (interface)
- AudioCueResolver (POCO)
- NoopAudio (no-op impl)
- UnityAudio (MonoBehaviour adapter — clipTable, bgmSource, sfxSource)

### Input (3)
- InputSnapshot (이미 T3.4 에 있음)
- UnityInputSnapshotBuilder (MonoBehaviour? or just class — Keyboard.current + Pointer.current 폴링)
- PointerToPlayfield (mainCamera + layoutConfigSO 의존 — offsetX 보정)

### Persistence (4)
- SaveData (POCO record class)
- ISaveRepository (interface)
- InMemorySaveRepository
- PlayerPrefsSaveRepository (Unity PlayerPrefs)

### View — Renderer 10개 + Panel 8개 (T3.7, T3.8)
### GameManager / ScreenRouter (T3.9, T3.10)

## T3.7 Renderer 10개 (atomic)

### T3.7.1 BarRenderer.cs
```csharp
public sealed class BarRenderer : MonoBehaviour {
    [SerializeField] SpriteRenderer body;
    [SerializeField] SpriteRenderer effectIndicator;
    [SerializeField] float referenceWidthPx = 1f; // body sprite 가 1×1 라면 1, 120 라면 120
    public void Bind(BarState bar) {
        transform.localPosition = new Vector3(bar.X, bar.Y, 0);
        body.transform.localScale = new Vector3(bar.Width / referenceWidthPx, ...);
        effectIndicator.color = ...; // BarEffect 따라
    }
}
```

### T3.7.2 BallsRenderer.cs (Pool)
- `ballPrefab` + `poolRoot`
- Bind: 각 ball 의 `localPosition` (active 여부 무관 항상 표시)

### T3.7.3 BlocksRenderer.cs (Pool + sprite swap)
```csharp
[System.Serializable]
public struct SpriteEntry { public string DefinitionId; public Sprite Sprite; }
[SerializeField] SpriteEntry[] spriteEntries;
// Bind 에서 sprite map lookup
```

### T3.7.4 BordersRenderer.cs (Pool + sprite swap by orientation)
```csharp
[SerializeField] Sprite verticalSprite;
[SerializeField] Sprite horizontalSprite;
// Bind: BorderBlockState.Orientation 따라 sprite 선택
```

### T3.7.5 DoorsRenderer.cs (Pool + 5 frame opening 애니)
```csharp
[SerializeField] Sprite closedSprite;
[SerializeField] Sprite[] openingFrames; // 5 (frame0~4)
// Bind: phase=Opening 이면 (elapsed / openingDurationMs) * 5 로 frame index
//       phase=Closed 이면 closedSprite, Opened 면 SetActive(false)
```

### T3.7.6 SpinnersRenderer.cs (Pool + rotation)
- spinnerPrefab, localRotation set
- Spinner 종류별 sprite map (cube/triangle)

### T3.7.7 ItemsRenderer.cs (Pool + sprite swap by ItemType)
```csharp
[SerializeField] GameObject itemPrefab;
[SerializeField] Sprite expandSprite, magnetSprite, laserSprite;
// Bind: ItemType 따라 sprite swap
```

### T3.7.8 LaserShotsRenderer.cs (Pool)
- laserPrefab (4×30 빨강)

### T3.7.9 BallTrailRenderer.cs (Pool + TrailRenderer + style swap)
- trailPrefab (TrailRenderer 컴포넌트 포함)
- trailStyleSO

### T3.7.10 MascotRenderer.cs (Pool + dynamic sprite swap)
```csharp
[SerializeField] GameObject mascotPrefab; // child SpriteRenderer
[SerializeField] MascotSO mascotSO;
SpriteRenderer _sprite; int _currentFrame; float _frameTimerMs;
public void SetMascot(string id) { /* lookup mascotSO and set _frames */ }
void Update() {
    // animate frames 0~3 looping every 200ms
}
```

## T3.8 Panel 9개 (atomic)

### T3.8.1 TitlePanel.cs
```csharp
public class TitlePanel : MonoBehaviour {
    [SerializeField] TMP_Text startText;
    [SerializeField] TMP_Text highScoreText;
    [SerializeField] TMP_Text difficultyText;
    [SerializeField] TMP_Text logoText;
    [SerializeField] Image mascotImage; // Title 마스코트
    [SerializeField] Image backdropImage;

    public void Bind(TitleScreenViewModel vm) {
        startText.text = vm.StartLabel;
        highScoreText.text = $"HIGH SCORE  {vm.HighScore}";
        difficultyText.text = $"[{vm.DifficultyLabel}]";
        logoText.text = "ALBATROSS";
        if (vm.CurrentMascotSprite != null) mascotImage.sprite = vm.CurrentMascotSprite;
    }
}
```

### T3.8.2 IntroStoryPanel.cs
```csharp
[SerializeField] TMP_Text bodyText;
[SerializeField] Image illustrationImage;
[SerializeField] Image backdropImage;
public void Bind(IntroScreenViewModel vm) {
    bodyText.text = vm.PageText.Substring(0, vm.TypingProgress);
    illustrationImage.sprite = vm.IllustrationSprite;
}
```

### T3.8.3 RoundIntroPanel.cs
```csharp
[SerializeField] TMP_Text roundLabel;
[SerializeField] TMP_Text readyLabel;
[SerializeField] CanvasGroup canvasGroup;
public void Bind(RoundIntroViewModel vm) {
    roundLabel.text = $"라운드 {vm.RoundNumber}";
    readyLabel.text = "READY";
    canvasGroup.alpha = vm.FadeAlpha;
}
```

### T3.8.4 InGamePanel.cs (HUD wrapper + 마스코트 BG)
```csharp
[SerializeField] HudView hudView;
[SerializeField] Image mascotBgImage; // 게임 영역 위/아래 마스코트
public void Bind(HudViewModel vm) {
    hudView.Bind(vm);
    if (vm.CurrentMascotSprite != null) mascotBgImage.sprite = vm.CurrentMascotSprite;
}
```

### T3.8.5 HudView.cs
- 5 TMP_Text (Score, HighScore, Lives, Round, Effect)
- Lives 는 `LIVES x{N}` (또는 ♥ 폰트 SDF 에 포함 시 ♥{N})

### T3.8.6 GameOverPanel.cs
- 4 라벨 + GameOver mascot Image (gameover_frame_1~4 애니)

### T3.8.7 GameClearPanel.cs — 동일 구조

### T3.8.8 PauseOverlay.cs
- label, helpText, BackdropImage

### T3.8.9 ToastView.cs
- label, CanvasGroup (fade)

## T3.9 GameManager.cs (28 SerializeField + lifecycle)

```csharp
public sealed class GameManager : MonoBehaviour {
    // ─── 14 SO ───
    [SerializeField] GameplayConfigSO gameplayConfigSO;
    [SerializeField] DifficultyConfigSO normalDifficultySO;
    [SerializeField] DifficultyConfigSO hardDifficultySO;
    [SerializeField] List<BlockDefinitionSO> blockDefinitionSOs;
    [SerializeField] ItemDefinitionSO expandItemSO;
    [SerializeField] ItemDefinitionSO magnetItemSO;
    [SerializeField] ItemDefinitionSO laserItemSO;
    [SerializeField] List<SpinnerDefinitionSO> spinnerDefinitionSOs;
    [SerializeField] List<StageDefinitionSO> stageDefinitionSOs;
    [SerializeField] UITextSO uiTextSO;
    [SerializeField] IntroSequenceSO introSequenceSO;
    [SerializeField] AudioCueSO audioCueSO;
    [SerializeField] LayoutConfigSO layoutConfigSO;
    [SerializeField] MascotSO mascotSO;
    [SerializeField] TrailStyleSO trailStyleSO;
    [SerializeField] PowerupSO powerupSO;
    // ─── Camera ───
    [SerializeField] Camera mainCamera;
    // ─── 10 Renderer ───
    [SerializeField] BarRenderer barRenderer;
    [SerializeField] BallsRenderer ballsRenderer;
    [SerializeField] BlocksRenderer blocksRenderer;
    [SerializeField] BordersRenderer bordersRenderer;
    [SerializeField] DoorsRenderer doorsRenderer;
    [SerializeField] SpinnersRenderer spinnersRenderer;
    [SerializeField] ItemsRenderer itemsRenderer;
    [SerializeField] LaserShotsRenderer laserShotsRenderer;
    [SerializeField] BallTrailRenderer ballTrailRenderer;
    [SerializeField] MascotRenderer mascotRenderer;
    // ─── Router + 8 Panel ───
    [SerializeField] ScreenRouter screenRouter;
    [SerializeField] TitlePanel titlePanel;
    [SerializeField] IntroStoryPanel introStoryPanel;
    [SerializeField] RoundIntroPanel roundIntroPanel;
    [SerializeField] InGamePanel inGamePanel;
    [SerializeField] GameOverPanel gameOverPanel;
    [SerializeField] GameClearPanel gameClearPanel;
    [SerializeField] PauseOverlay pauseOverlay;
    [SerializeField] ToastView toastView;
    // ─── Audio ───
    [SerializeField] UnityAudio unityAudio;

    // ─── Runtime services ───
    private GameplayController _gameplayController;
    private GameFlowController _gameFlowController;
    private HUDPresenter _hudPresenter;
    private ScreenPresenter _screenPresenter;
    private ScreenDirector _screenDirector;
    private VisualEffectController _visualEffectController;
    private ISaveRepository _saveRepo;
    private IArkanoidAudio _audio;
    private UnityInputSnapshotBuilder _inputBuilder;
    private PointerToPlayfield _pointerToPlayfield;

    void Awake() { Initialize(); }
    void Initialize() {
        _saveRepo = new PlayerPrefsSaveRepository();
        var save = _saveRepo.Load();
        // ... services 생성, mascot id load
        mascotRenderer.SetMascot(save.SelectedMascotId ?? "albatross");
    }
    void Update() {
        var input = _inputBuilder.Build(_pointerToPlayfield?.GetPlayfieldX());
        _gameFlowController.HandleInput(input);
        if (_gameFlowController.GetState().Kind == FlowStateKind.InGame) {
            var events = _gameplayController.Tick(input, Time.deltaTime);
            foreach (var e in events) {
                _audio.Play(_audioCueResolver.Resolve(e));
                _visualEffectController.HandleGameplayEvent(e);
                _gameFlowController.HandleGameplayEvent(e);
            }
        }
        _screenDirector.Update(_gameFlowController.GetState(), Time.deltaTime * 1000f, OnPresentationEvent);
        BindViews();
    }
    void BindViews() {
        var flowState = _gameFlowController.GetState();
        screenRouter.Apply(flowState.Kind);
        // Renderer Bind (InGame/RoundIntro)
        // Panel Bind (switch on flowState.Kind)
        // HighScore 갱신 시 _saveRepo.Save(...)
    }
}
```

## T3.10 ScreenRouter.cs (panel toggle)

```csharp
public sealed class ScreenRouter : MonoBehaviour {
    [SerializeField] TitlePanel titlePanel;
    [SerializeField] IntroStoryPanel introStoryPanel;
    [SerializeField] RoundIntroPanel roundIntroPanel;
    [SerializeField] InGamePanel inGamePanel;
    [SerializeField] GameOverPanel gameOverPanel;
    [SerializeField] GameClearPanel gameClearPanel;
    [SerializeField] PauseOverlay pauseOverlay;
    public void Apply(FlowStateKind kind) {
        titlePanel.gameObject.SetActive(kind == FlowStateKind.Title);
        introStoryPanel.gameObject.SetActive(kind == FlowStateKind.IntroStory);
        // ...
    }
}
```

## T3.11 UnityInputSnapshotBuilder.cs

```csharp
using UnityEngine.InputSystem;
public sealed class UnityInputSnapshotBuilder {
    public InputSnapshot Build(float? pointerX) {
        var kb = Keyboard.current;
        return new InputSnapshot(
            Left: kb?.leftArrowKey.isPressed ?? false,
            Right: kb?.rightArrowKey.isPressed ?? false,
            Action: kb?.spaceKey.wasPressedThisFrame ?? false,
            Pause: kb?.pKey.wasPressedThisFrame ?? false,
            QuitToTitle: kb?.qKey.wasPressedThisFrame ?? false,
            PointerX: pointerX
        );
    }
}
```

## T3.12 NUnit 테스트 (선택, T1.8 의 Edit Mode 968 case)
- TS .test.ts → C# 1:1 포팅
- dotnet test 또는 Unity Test Runner

## ✅ Self-audit Phase 3

- [ ] 모든 .ts 파일이 .cs 로 매핑됨 (T0.1 의 list 와 비교)
- [ ] Console 컴파일 에러 0
- [ ] 6 asmdef reference 정확
- [ ] Renderer 10개 모두 sprite swap 또는 색 fill 구현됨
- [ ] Panel 9개 모두 Bind 메서드 + SerializeField 명세
- [ ] GameManager 28 SerializeField
- [ ] PlayerPrefsSaveRepository wire 됨

---

# Phase 4. SO Data Migration

## T4.1 13 SO Asset 생성

| SO | Asset 이름 | 위치 | 갯수 |
|---|---|---|---|
| GameplayConfig | GameplayConfig | Data/Gameplay/ | 1 |
| DifficultyConfig | Difficulty_Normal, Difficulty_Hard | Data/Gameplay/ | **2** |
| LayoutConfig | LayoutConfig | Data/Gameplay/ | 1 |
| BlockDefinition | Block_basic, Block_basic_drop, Block_magnet_drop, Block_laser_drop, Block_tough | Data/Blocks/ | 5 |
| ItemDefinition | Item_Expand, Item_Magnet, Item_Laser | Data/Items/ | 3 |
| SpinnerDefinition | Spinner_Cube, Spinner_Triangle | Data/Spinners/ | 2 |
| StageDefinition | Stage_01, Stage_02, Stage_03 | Data/Stages/ | 3 |
| AudioCue | AudioCueTable | Data/Presentation/ | 1 |
| UIText | UITextTable | Data/Presentation/ | 1 |
| IntroSequence | IntroSequence_Story | Data/Presentation/ | 1 |
| Mascot | MascotTable | Data/Presentation/ | 1 |
| TrailStyle | TrailStyleTable | Data/Presentation/ | 1 |
| Powerup | PowerupTable | Data/Presentation/ | 1 |

## T4.2 데이터 입력 (TS 파일 → Inspector 값)

각 SO 의 값을 TS table 에서 직접 읽어 입력. **갯수가 TS 와 매칭해야** Self-audit 통과.

| SO | TS 원본 | 입력 값 갯수 검증 |
|---|---|---|
| GameplayConfigSO | `gameplayConfigTable.ts` | (single object) — 모든 필드 채움 |
| Difficulty_Normal/Hard | `difficultyConfigTable.ts` | 2 entry × 모든 필드 |
| LayoutConfigSO | `layoutConfigTable.ts` | Canvas/Playfield/HUD/Slider 좌표 SSOT |
| Block_*.asset 5개 | `blockDefinitionTable.ts` | 각각 BaseColor(int hex), MaxHits, Score, DropItemType, **blockSprite** ← Assets/Sprites/Blocks/<id>.png |
| Item_*.asset 3개 | `itemDefinitionTable.ts` | ItemType, FallSpeed, DurationMs, **sprite** ← Assets/Sprites/Gameplay/item_*.png |
| Spinner_*.asset 2개 | `spinnerDefinitionTable.ts` | DefinitionId, Radius, AngularSpeed |
| Stage_01/02/03.asset | `stage1.json` / `stage2.json` / `stage3.json` | BlockPlacements 배열 (x/y/blockDefId 각각), BorderPlacements, DoorPlacements, SpinnerPlacements 모두 |
| AudioCueTable | `audioCueTable.ts` | **19 entry** |
| UITextTable | `uiTextTable.ts` | **20 entry** |
| IntroSequence_Story | `introSequenceTable.ts` | **4 page** entry — 각각 text + durationMs + **illustration sprite** ← Assets/Sprites/Intro/intro_story_0N.png |
| MascotTable | `mascotTable.ts` | **5 mascot** — 각각 Id, DisplayName, UnlockCondition, **frames[4]** (Assets/Sprites/Mascots/<id>/frame0~3.png), **portrait** (Assets/Sprites/Portraits/<id>.png) |
| TrailStyleTable | `trailStyleTable.ts` | **3 style** (GoldenSun, BlueMeteor, Sunset) — 각 색 hex + segment 수 + push interval + radius |
| PowerupTable | `powerupTable.ts` | (있다면) |

## ✅ Self-audit Phase 4

- [ ] 13 SO asset 모두 존재 (Difficulty_Hard 포함)
- [ ] **Inspector 에서 각 entry 갯수가 TS table 갯수와 일치** (특히 AudioCue 19, UIText 20, IntroSequence 4, Mascot 5, TrailStyle 3)
- [ ] 각 BlockDefinitionSO 의 BaseColor 가 TS hex 와 일치 (default 0xCCCCCC 가 아님)
- [ ] 각 IntroPageEntry 의 illustration sprite ✅
- [ ] 각 MascotEntry 의 4 frame sprite + portrait sprite ✅
- [ ] Inspector 미리보기에서 sprite/색 정상

---

# Phase 5. Scene + Wire

## T5.1 Main.unity 생성

1. File → New Scene → 2D → Save as `Assets/Scenes/Main.unity`
2. Build Profiles → Add Open Scenes

## T5.2 Main Camera

- Projection: Orthographic
- Size: **540** (PlayfieldHeight=900 의 60% ~ Canvas height 의 28%)
  - 또는 TS Phaser zoom 1.5 매칭 → 계산: TS canvas 1080×1920, zoom 1.5 → 보이는 영역 720×1280 → ortho size = 640
  - **T0.3 검증값 사용**
- Position: (PlayfieldWidth/2 = 360, PlayfieldHeight/2 = 450, -10) 일 때 게임 중심 = 카메라 중심 — 단 PlayfieldRoot 가 (0,0) 이라면. PlayfieldRoot transform 으로 보정 시 카메라는 (0, 0, -10).
- Clear Flags: Solid Color, Background: #0e0a23

## T5.3 Canvas

- Render Mode: Screen Space - Overlay
- Sort Order: 10
- Canvas Scaler: Scale With Screen Size, Reference Resolution 1080×1920, Match Width Or Height = 1 (Height match)

## T5.4 Hierarchy 전체 구성

```
Main Camera
Canvas (Screen Space Overlay, sortOrder 10)
├── TitlePanel (RectTransform stretch, Image backdrop, color #0e0a23)
│   ├── BackdropImage (Image, stretch, sprite=bg_title)
│   ├── LogoText (TMP, anchor center, anchoredPos (0, 660), sizeDelta (800, 200), font DNFBitBitv2 SDF, size 96)
│   ├── HighScoreText (TMP, anchor center, anchoredPos (0, 200), color yellow)
│   ├── DifficultyText (TMP, anchor center, anchoredPos (0, -200), color yellow)
│   ├── StartText (TMP, anchor center, anchoredPos (0, -700))
│   └── MascotImage (Image, anchor center, anchoredPos (0, -50), sizeDelta (400, 400))
├── IntroStoryPanel (stretch, backdrop)
│   ├── IllustrationImage (Image, anchor center, anchoredPos (0, 300), sizeDelta (800, 600))
│   ├── BodyText (TMP, anchor center, anchoredPos (0, -400), sizeDelta (900, 400))
│   └── BackdropImage
├── RoundIntroPanel (stretch, backdrop, CanvasGroup component)
│   ├── RoundLabel (TMP, anchor center, anchoredPos (0, 100), size 80)
│   └── ReadyLabel (TMP, anchor center, anchoredPos (0, -100), size 60, color yellow)
├── InGamePanel (stretch — HUD 만, BG 없음)
│   └── HudView GameObject
│       ├── ScoreText (TMP, anchor TopLeft, anchoredPos (80, -140))
│       ├── HighScoreText (TMP, anchor TopCenter, anchoredPos (0, -140))
│       ├── RoundText (TMP, anchor TopRight, anchoredPos (-80, -140))
│       ├── LivesText (TMP, anchor BottomLeft, anchoredPos (80, 140))
│       └── EffectText (TMP, anchor TopCenter, anchoredPos (0, -240))
├── GameOverPanel (stretch, backdrop=bg_gameover)
│   ├── GameOverLabel, FinalScoreLabel, HighScoreLabel, RetryText (각 TMP)
│   └── MascotImage (gameover_frame_1~4 애니, Animator 컴포넌트)
├── GameClearPanel (stretch, backdrop=bg_gameclear)
│   └── 동일 4 라벨 + MascotImage
├── PauseOverlay (stretch, backdrop 어둡게)
│   ├── Label (TMP "PAUSED")
│   └── HelpText (TMP "Press P to resume")
└── ToastView (CanvasGroup)
    └── Label (TMP)
EventSystem (UI → Event System 자동 생성)
GameManager (Empty)
├── GameManager.cs
├── ScreenRouter.cs
├── UnityAudio.cs
├── AudioSource (BGM, Loop on)
└── AudioSource (SFX, Loop off)
PlayfieldRoot (Empty)
- Position: (-360, 450, 0) 또는 (180, 1320, 0) — T0.3 결과 따라
- Scale: (1, -1, 1) — Y flip
├── BarRendererGO + BarRenderer.cs + Bar instance child
├── BallsRendererGO + BallsRenderer.cs
├── BlocksRendererGO + BlocksRenderer.cs
├── BordersRendererGO + BordersRenderer.cs
├── DoorsRendererGO + DoorsRenderer.cs
├── SpinnersRendererGO + SpinnersRenderer.cs
├── ItemsRendererGO + ItemsRenderer.cs
├── LaserShotsRendererGO + LaserShotsRenderer.cs
├── BallTrailRendererGO + BallTrailRenderer.cs
└── MascotRendererGO + MascotRenderer.cs + Mascot instance child
```

## T5.5 Prefab 생성 (10개)

각 Prefab 정확한 크기 + sprite + scale 명세.

| Prefab | 자식 컴포넌트 | 크기 px | sprite (assign) |
|---|---|---|---|
| Ball.prefab | SpriteRenderer (원) | 14×14 | (단순 흰 원) 또는 spinnerPrefab 의 단순 sprite |
| Bar.prefab | Empty 부모 + Body(SpriteRenderer) child | 120×16 | Body 의 sprite = 1×1 흰 사각 (referenceWidthPx=1) |
| Block.prefab | SpriteRenderer | 60×24 | empty (BlocksRenderer 가 sprite swap) |
| Border.prefab | SpriteRenderer | 30×30 (또는 60×12) | empty (BordersRenderer 가 vertical/horizontal swap) |
| Door.prefab | SpriteRenderer | 60×12 | empty (DoorsRenderer 가 frame swap) |
| Spinner.prefab | SpriteRenderer | 40×40 | empty |
| Item.prefab | SpriteRenderer | 24×24 | empty (ItemsRenderer swap) |
| LaserShot.prefab | SpriteRenderer | 4×30 | 빨강 |
| Trail.prefab | TrailRenderer | — | (Time/Width 코드 덮어씀) |
| Mascot.prefab | Empty 부모 + Sprite(SpriteRenderer) child | 자유 | empty (MascotRenderer SetMascot 호출) |

## T5.6 Wire 매트릭스

### T5.6.1 GameManager (28 wire)

| Field | Drag |
|---|---|
| gameplayConfigSO | Data/Gameplay/GameplayConfig.asset |
| normalDifficultySO | Data/Gameplay/Difficulty_Normal.asset |
| hardDifficultySO | Data/Gameplay/Difficulty_Hard.asset |
| blockDefinitionSOs (List) | Data/Blocks/Block_*.asset (5) |
| expandItemSO | Data/Items/Item_Expand.asset |
| magnetItemSO | Data/Items/Item_Magnet.asset |
| laserItemSO | Data/Items/Item_Laser.asset |
| spinnerDefinitionSOs (List) | Data/Spinners/Spinner_*.asset (2) |
| stageDefinitionSOs (List) | Data/Stages/Stage_0N.asset (3) |
| uiTextSO | Data/Presentation/UITextTable.asset |
| introSequenceSO | Data/Presentation/IntroSequence_Story.asset |
| audioCueSO | Data/Presentation/AudioCueTable.asset |
| layoutConfigSO | Data/Gameplay/LayoutConfig.asset |
| mascotSO | Data/Presentation/MascotTable.asset |
| trailStyleSO | Data/Presentation/TrailStyleTable.asset |
| powerupSO | Data/Presentation/PowerupTable.asset |
| mainCamera | Hierarchy/Main Camera |
| barRenderer ~ mascotRenderer (10) | Hierarchy/PlayfieldRoot/*RendererGO |
| screenRouter | Hierarchy/GameManager (self) |
| titlePanel ~ pauseOverlay (7) | Hierarchy/Canvas/*Panel |
| toastView | Hierarchy/Canvas/ToastView |
| unityAudio | Hierarchy/GameManager (self) |

### T5.6.2 ScreenRouter (7 panel wire) — 동일

### T5.6.3 Renderer 10개 wire

| Renderer | Field | Drag |
|---|---|---|
| BarRenderer | body | Bar.prefab Instantiate 후 Body child SpriteRenderer |
| BarRenderer | effectIndicator | (있다면 Bar 의 child 두 번째 SpriteRenderer) |
| BarRenderer | referenceWidthPx | 1 (Bar.prefab Body 의 sprite 가 1×1 일 때) |
| BallsRenderer | ballPrefab | Assets/Prefabs/Ball.prefab |
| BallsRenderer | poolRoot | Hierarchy/PlayfieldRoot/BallsRendererGO (self) |
| BlocksRenderer | blockPrefab | Block.prefab |
| BlocksRenderer | blockDefinitionSOs | Block_*.asset (5) |
| BlocksRenderer | spriteEntries (5) | (DefinitionId, Sprite) 5개:<br>basic → block_basic.png<br>basic_drop → block_basic_drop.png<br>... |
| BordersRenderer | borderPrefab | Border.prefab |
| BordersRenderer | verticalSprite | Assets/Sprites/Borders/border_vertical.png |
| BordersRenderer | horizontalSprite | Assets/Sprites/Borders/border_horizontal.png |
| DoorsRenderer | doorPrefab | Door.prefab |
| DoorsRenderer | closedSprite | Assets/Sprites/Borders/door_closed.png |
| DoorsRenderer | openingFrames (Sprite[5]) | door_opening_frame0~4.png (5) |
| SpinnersRenderer | spinnerPrefab | Spinner.prefab |
| SpinnersRenderer | cube/triangle sprite | (TS 의 spinner sprite 있으면 wire, 없으면 단순 도형) |
| ItemsRenderer | itemPrefab | Item.prefab |
| ItemsRenderer | expandSprite | Assets/Sprites/Gameplay/item_expand.png |
| ItemsRenderer | magnetSprite | item_magnet.png |
| ItemsRenderer | laserSprite | item_laser.png |
| LaserShotsRenderer | laserPrefab | LaserShot.prefab |
| BallTrailRenderer | trailPrefab | Trail.prefab |
| BallTrailRenderer | trailStyleSO | TrailStyleTable.asset |
| MascotRenderer | mascotPrefab | Mascot.prefab |
| MascotRenderer | mascotSO | MascotTable.asset |

### T5.6.4 Panel 9개 wire

| Panel | Field | Drag |
|---|---|---|
| TitlePanel | startText | Canvas/TitlePanel/StartText (TMP child) |
| TitlePanel | highScoreText | HighScoreText |
| TitlePanel | difficultyText | DifficultyText |
| TitlePanel | logoText | LogoText |
| TitlePanel | mascotImage | MascotImage |
| TitlePanel | backdropImage | BackdropImage |
| IntroStoryPanel | bodyText, illustrationImage, backdropImage | (각 child) |
| RoundIntroPanel | roundLabel, readyLabel, canvasGroup | (canvasGroup 은 RoundIntroPanel 자체에 컴포넌트로 추가) |
| InGamePanel | hudView | (자식 HudView GameObject) |
| InGamePanel | mascotBgImage | (게임 영역 BG image) |
| HudView | scoreText, highScoreText, livesText, roundText, effectText | (각 child TMP) |
| GameOverPanel | gameOverLabel, finalScoreLabel, highScoreLabel, retryText, mascotImage, backdropImage | (각 child) |
| GameClearPanel | (동일 4 라벨 + mascot + backdrop) | |
| PauseOverlay | label, helpText, backdropImage | |
| ToastView | label, canvasGroup | |

### T5.6.5 UnityAudio wire

| Field | Drag |
|---|---|
| bgmSource | GameManager 의 AudioSource (BGM) |
| sfxSource | GameManager 의 AudioSource (SFX) |
| clipTable (List of (ResourceId, AudioClip)) | 19 cue 매핑:<br>bgm_title → Assets/Audio/bgm_title.wav<br>jingle_gameover → jingle_gameover.wav<br>sfx_block_hit → sfx_block_hit.wav<br>... (19개) |

## T5.7 RectTransform 정확한 값 표

(T5.4 의 Hierarchy 에 anchor/anchoredPos/sizeDelta 명시. TS LayoutConfig 참조)

## ✅ Self-audit Phase 5

- [ ] Scene 저장
- [ ] Console NRE 0
- [ ] Wire 매트릭스 모든 행 ✅
- [ ] Play → Title 화면 정상 표시 (BG, 로고, 마스코트, HIGH SCORE, 시작하기 텍스트)

---

# Phase 6. Polish + Build

## T6.1 SaveData 동작 검증

- GameManager.Awake 에 `_saveRepo = new PlayerPrefsSaveRepository();`
- TitlePanel 의 HighScore 표시 = `_saveRepo.Load().HighScore`
- GameOver/Clear 시 `_saveRepo.Save(...)`
- Editor 에서 Play → 게임 클리어 → 재시작 → HighScore 유지 확인

## T6.2 Difficulty 토글 UI

- Title 에서 좌/우 화살표 → Normal ↔ Hard 토글
- 선택된 Difficulty 가 _gameplayController 에 반영

## T6.3 마스코트 선택 UI

- Title 의 마스코트 image 좌/우 또는 별도 키 → 5 마스코트 cycle
- 선택 후 GameManager → SaveData.SelectedMascotId 저장
- MascotRenderer.SetMascot(id) 호출

## T6.4 마스코트 잠금 해제 (옵션 P2)

- portraits/ + portraits2/ 활용
- UnlockCondition (예: "HighScore >= 1000") 만족 시 unlock

## T6.5 음소거 UI

- PauseOverlay 또는 Settings UI 에 toggle 버튼
- UnityAudio.SetMute(bool)

## T6.6 .inputactions (옵션)

- ArkanoidInput.inputactions 자산화 (D4.1)

## T6.7 Player Settings

- Default Orientation: Portrait
- Scripting Backend: Mono (개발) / IL2CPP (출시)
- Target Architecture: ARMv7 (개발) / ARM64 (출시)
- Company Name, Product Name "Arkanoid"
- Icon

## T6.8 Validator 포팅 (P2)

- TS validator 8개 → C# Editor script
- Inspector 에 "Validate" 버튼

## T6.9 Dev 도구 (P3)

- ReplayRecorder, CollisionLog, BallTrail debug, InvariantChecker

## T6.10 GlossyStyle Shader (P3)

- URP Shader Graph

## ✅ Self-audit Phase 6

- [ ] SaveData 정상 (HighScore 저장/로드)
- [ ] Difficulty 토글 동작
- [ ] 마스코트 선택 + SetMascot 호출
- [ ] Player Orientation Portrait

---

# Phase 7. Play Test Checklist (사용자가 확인)

각 항목 ✅ 체크. 모두 ✅ 면 Phase 8 진행 가능.

## T7.1 Title 화면
- [ ] BG (bg_title) 표시
- [ ] "ALBATROSS" 로고 (DNFBitBitv2 폰트)
- [ ] HIGH SCORE 숫자 표시 (SaveData 로드)
- [ ] [NORMAL] / [HARD] 토글 (좌/우 화살표)
- [ ] 마스코트 표시 + 선택 가능 (5 마스코트)
- [ ] "시작하기" 텍스트
- [ ] Space → IntroStory 진행

## T7.2 IntroStory 화면
- [ ] 4 page 모두 표시 (각 페이지 illustration + text)
- [ ] 페이지 자동 전환 또는 Space skip
- [ ] 마지막 페이지 후 RoundIntro

## T7.3 RoundIntro 화면
- [ ] "라운드 N" 라벨 (위치 정확)
- [ ] "READY" 라벨
- [ ] Fade in/out
- [ ] 잠시 후 자동 InGame

## T7.4 InGame 화면
- [ ] 게임 위쪽 블록들 (TS sprite 색)
- [ ] 게임 아래쪽 Bar (파란색, 길이 정확)
- [ ] Bar 위 Ball
- [ ] HUD: SCORE / HIGH / LIVES x N / ROUND 표시
- [ ] 좌/우 화살표 → Bar 움직임
- [ ] Space → Ball 발사
- [ ] 충돌 시 block 깨짐 + sfx
- [ ] Item drop + 캐치 → Effect (Expand/Magnet/Laser)
- [ ] LaserShot 발사 + sfx
- [ ] Door opening 5 frame 애니
- [ ] Spinner 회전
- [ ] 라운드 클리어 → 다음 라운드
- [ ] 모든 라운드 클리어 → GameClear

## T7.5 GameOver / GameClear
- [ ] 적절한 라벨
- [ ] FinalScore + HighScore (갱신 표시)
- [ ] 마스코트 gameover_frame_1~4 애니
- [ ] BGM jingle 재생
- [ ] Retry → Title 또는 새 round

## T7.6 Pause
- [ ] P 키 → Pause overlay
- [ ] 게임 일시정지
- [ ] P 재개

## T7.7 Audio
- [ ] BGM 재생
- [ ] 모든 SFX (block_hit, block_destroyed, item_collected, ball_attached, balls_released, laser_fired, life_lost, ui_confirm) 정상
- [ ] 음소거 toggle 동작

## T7.8 Persistence
- [ ] HighScore 저장 → 재시작 후 유지
- [ ] Gold / 마스코트 unlock 저장

## ✅ Self-audit Phase 7
- 모든 ✅ → Play test 성공

---

# Phase 8. Build

## T8.1 Android APK
- File → Build Profiles → Build And Run (USB 연결된 폰)
- APK 설치 → 폰에서 실행 → 동작 확인

---

# 9. 진행 시간 추정 (보수적)

| Phase | 추정 |
|---|---|
| 0 Inventory | 1시간 |
| 1 Project Setup | 1시간 |
| 2 Asset Pipeline | 2시간 |
| 3 Code Porting | 10~14일 (TS 50+ ts → C# 90+ cs) |
| 4 SO Data | 1일 |
| 5 Scene + Wire | 1일 |
| 6 Polish | 2~3일 |
| 7 Play Test | 0.5일 |
| 8 Build | 0.5일 |
| **합계** | **약 17~22일** |

이전 41~56일의 절반. 이유:
- 누락 사고 제거 → 재작업 없음
- atomic task → 의사결정 시간 최소
- Wire 매트릭스 → 점검 시간 최소
- Inventory 우선 → 발견 비용 최소

---

# 10. 핵심 누락 방지 체크 (각 Phase 끝 self-audit 필수)

| Phase | "이거 누락 안 했나?" 능동 질문 |
|---|---|
| 0 | 자산 카테고리 빠진 거 없나 (sfx/fonts/backgrounds/mascots/portraits/portraits2/borders/blocks/intro/gameplay) |
| 1 | asmdef reference 누락 없나, Input System 활성화됐나 |
| 2 | 자산 80개 모두 import 됐나 (Project 에서 폴더별 count) |
| 3 | TS 의 모든 .ts 파일이 매핑됐나 (T0.1 list 와 1:1) |
| 4 | SO entry 갯수가 TS table 과 일치하나 (특히 AudioCue 19, UIText 20, IntroSequence 4, Mascot 5) |
| 5 | Wire 매트릭스 모든 행 (50+ wire) 완료됐나 |
| 6 | SaveData wire 됐나, Player Orientation Portrait 인가 |
| 7 | T7.1~T7.8 모든 ✅ 인가 |

---

# 11. Memory 업데이트 권고

이 plan 으로 새 포팅 시작 시:
- 새 메모리: `project_arkanoid_unity_v2.md` (Phase 진행 상황)
- 기존 메모리 참조: [[feedback_inventory_first]], [[feedback_code_over_asset]]

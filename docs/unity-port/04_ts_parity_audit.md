# 04 TS↔Unity Parity Audit + Fix Plan

작성: 2026-05-25. TS Phaser 프로토타입 (`\\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\`) vs Unity 포팅 (`C:\Users\rimse\UnityProjects\Arkanoid-Unity\`) 전수조사 결과 + 정확한 수정 계획.

---

## 0. 현재 상태 요약 (2026-05-25 22:30)

- **TS Phaser 원본**: SHIPPED, 변경 금지. 75개 asset + 50+ ts 파일.
- **Unity 포팅 코드**: 100% 작성 (commit `b7f1085`).
- **Sub-agent 작업 중 (background)**: 2차 fix 진행 — 사용자 지적 8가지 (Title 배경/폰트, IntroStory 페이지, Block/Bar/Ball sprite, ROUND 라벨 위치, HUD 가시화, 마스코트 표시, 마스코트/난이도 선택 UI, 화살표 입력).
- **이전 sub-agent 1차 fix 19개 적용** (카메라/Canvas/PlayfieldRoot/Panel RectTransform/SerializeField wire/HudView ♥ 폴백/Backdrop).

---

## 1. 누락 자산 (총 25개)

### 1.1 미import (Unity Assets/ 에 파일 없음)

| 카테고리 | TS 경로 | 개수 | Unity 경로 (import 위치) | 우선순위 |
|---|---|---|---|---|
| Portrait | `public/assets/portraits/*.png` (5: snowrabbit/albatross/reaper/kongming/seraphin) | 5 | `Assets/Sprites/Portraits/` | **P1** |
| Portrait2 | `public/assets/portraits2/*.png` (5: 동일 5명) | 5 | `Assets/Sprites/Portraits2/` | **P1** |
| Border sprite | `public/assets/borders/border_vertical.png`<br>`public/assets/borders/border_horizontal.png` | 2 | `Assets/Sprites/Borders/` | **P0** |
| Door sprite | `public/assets/borders/door_closed.png`<br>`public/assets/borders/door_opening_frame0~4.png` | 6 | `Assets/Sprites/Borders/` | **P0** |
| BG pixel | `public/assets/backgrounds/bg_pixel_01~03.png` | 3 | `Assets/Sprites/Backgrounds/` | **P2** (용도 확인 필요) |
| GameOver 마스코트 애니 | `public/assets/mascots/gameover_frame_1~4.png` | 4 | `Assets/Sprites/Mascots/GameOver/` | **P1** |
| Dance sprite sheet | `public/assets/mascots/dance_sheet.png` | 1 | `Assets/Sprites/Mascots/` | **P2** (용도 확인) |
| Font OTF | `public/assets/fonts/DNFBitBitv2.otf` | 1 | `Assets/Fonts/` (TTF 있으니 옵션) | **P3** |
| **누락 합계** | | **27** | | |

### 1.2 import 작업 명령 (Sub-agent 완료 후)

```
# Import 명령 (총 27 file 복사)
Source: \\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\public\assets\
Dest:   C:\Users\rimse\UnityProjects\Arkanoid-Unity\Assets\Sprites\

borders\border_vertical.png      → Sprites\Borders\
borders\border_horizontal.png    → Sprites\Borders\
borders\door_closed.png          → Sprites\Borders\
borders\door_opening_frame0.png  → Sprites\Borders\
borders\door_opening_frame1.png  → Sprites\Borders\
borders\door_opening_frame2.png  → Sprites\Borders\
borders\door_opening_frame3.png  → Sprites\Borders\
borders\door_opening_frame4.png  → Sprites\Borders\

portraits\*.png  (5)             → Sprites\Portraits\
portraits2\*.png  (5)            → Sprites\Portraits2\

backgrounds\bg_pixel_01~03.png   → Sprites\Backgrounds\

mascots\gameover_frame_1~4.png   → Sprites\Mascots\GameOver\
mascots\dance_sheet.png          → Sprites\Mascots\

fonts\DNFBitBitv2.otf            → Fonts\  (옵션)
```

---

## 2. 자산 import 됐지만 사용 안 됨 (코드/wire 미적용)

| 자산 | Unity 위치 | 사용해야 할 곳 | 작업 |
|---|---|---|---|
| `Sprites/Blocks/block_*.png` 5개 | ✅ import | BlocksRenderer 가 BaseColor 만 사용 → sprite 미적용 | **코드 변경**: `BlockDefinitionSO` 에 `Sprite blockSprite` 필드 추가, BlocksRenderer 가 `_sprites[i].sprite = def.blockSprite` 호출. + 각 BlockDefinitionSO 에 sprite wire |
| `Sprites/Backgrounds/bg_title.png` | ✅ | TitlePanel 의 backdrop Image | TitlePanel 의 backdrop Image.sprite ← bg_title |
| `Sprites/Backgrounds/bg_stage_01~03.png` | ✅ | InGamePanel 또는 PlayfieldRoot 배경 | InGamePanel 또는 별도 BG GameObject 의 Image.sprite |
| `Sprites/Backgrounds/bg_gameover.png` | ✅ | GameOverPanel backdrop | GameOverPanel.Image.sprite |
| `Sprites/Backgrounds/bg_gameclear.png` | ✅ | GameClearPanel backdrop | GameClearPanel.Image.sprite |
| `Sprites/Intro/intro_story_01~04.png` | ✅ | IntroSequenceSO 의 페이지별 illustration sprite | IntroSequenceEntry struct 에 `Sprite illustration` 필드 확인 + 각 페이지에 wire |
| `Sprites/Gameplay/item_expand.png` | ✅ | ItemsRenderer.expandSprite | wire |
| `Sprites/Gameplay/item_magnet.png` | ✅ | ItemsRenderer.magnetSprite | wire |
| `Sprites/Gameplay/item_laser.png` | ✅ | ItemsRenderer.laserSprite | wire |
| `Sprites/Mascots/<id>/frame0~3.png` 20개 | ✅ | MascotSO + MascotRenderer | MascotSO 의 각 mascot entry 에 frame sprite array wire. MascotRenderer 에서 동적 swap |
| `Fonts/DNFBitBitv2.ttf` | ✅ | TMP Font Asset 으로 변환 (Font Asset Creator) | DNFBitBitv2 SDF 생성 → 모든 TMP_Text 의 font 로 set (현재 MalgunGothic SDF 만 사용) |

---

## 3. 누락 데이터 (SO 값 비어있음 / 일부만 채움)

| SO | 현재 상태 | TS 원본 | 작업 |
|---|---|---|---|
| **DifficultyConfigSO** | Normal 만 1장 | `difficultyConfigTable.ts` 에 2종 (Normal + Hard) | `Difficulty_Hard.asset` 생성 + 값 입력 (BallSpeed/SubstepMs 등 Hard 수치) |
| **BlockDefinitionSO 색** | 5개 모두 default 0xCCCCCC 가능성 | `blockDefinitionTable.ts` 의 각 BaseColor hex | 각 BlockDefinitionSO 의 BaseColor 를 TS 값으로 set:<br>- block_basic: ?<br>- block_basic_drop: ?<br>- block_magnet_drop: ?<br>- block_laser_drop: ?<br>- block_tough: ?<br>(TS 파일 read 필요) |
| **AudioCueSO** | 17 entry (메모리) | `audioCueTable.ts` 19 cue | 누락 2 cue 추가 |
| **UITextSO** | 12 entry (메모리) | `uiTextTable.ts` 20 text | 누락 8 text 추가 |
| **IntroSequenceSO** | Mini 1 페이지 | `introSequenceTable.ts` 4 페이지 (intro_story_01~04 매핑) | 4 페이지 entry 채우기 + illustration sprite wire |
| **StageDefinitionSO Round 2/3** | 4 asset 만 생성, 데이터 미확인 | `stageDefinitionTable.ts` + `stage2.json`, `stage3.json` | BlockPlacements/BorderPlacements/DoorPlacements 데이터 입력 |
| **MascotSO 5종** | 만들었으나 sprite wire 미확인 | `mascotTable.ts` 5 마스코트 | 각 마스코트 entry 에 4 frame sprite wire |
| **PowerupSO** | 만들었나? 미확인 | `powerupTable.ts` (있다면) | 데이터 입력 |

---

## 4. 누락 코드 (TS 에는 있지만 Unity 에 없음)

| 항목 | TS 위치 | 파일 수 | 영향 | 우선순위 |
|---|---|---|---|---|
| **Validator** | `src/definitions/validators/` | 8 (UITextTable, IntroSequenceTable, AudioCueTable, ItemDefinition, SpinnerDefinitionTable, StageDefinition, BlockDefinition + ValidationResult) | 데이터 오류 silent fail (Unity Inspector 가 일부 visual 검증) | **P2** |
| **개발 도구** | `src/app/dev/` | 4 (ReplayRecorder, CollisionLog, BallTrail (dev), InvariantChecker) | 디버깅 안 됨 | **P3** |
| **Button widget** | `src/presentation/ui/Button.ts` (추정) | 1 | 마스코트/난이도 선택 버튼. Unity 는 UnityEngine.UI.Button 으로 대체 가능 | **P1** (sub-agent 처리 가능성) |
| **GlossyStyle 효과** | `src/presentation/ui/GlossyStyle.ts` (추정) | 1 (Shader/Material) | UI 윤기 표현 | **P3** |
| **AssetCatalog/Resolver** | `src/assets/` | 2 | Phaser sprite loader. Unity 는 SerializeField wire 라 불필요 | ❌ 불필요 |

---

## 5. Wire 필요 (코드 있지만 GameManager 에 연결 안 됨)

| 항목 | Unity 코드 | wire 안 됨 영향 |
|---|---|---|
| **PlayerPrefsSaveRepository** | `Presentation/Persistence/PlayerPrefsSaveRepository.cs` | HighScore 표시 0, gold/마스코트 unlock 정보 저장 안 됨 |
| **InMemorySaveRepository** | `Presentation/Persistence/InMemorySaveRepository.cs` | 테스트용 미사용 OK |
| **SaveData 필드** | `Presentation/Persistence/SaveData.cs` | TS `SaveData.ts` 필드 (highScore, gold, mascotsUnlocked, etc) 와 일치 확인 필요 |
| **MascotRenderer.SetMascot(id)** | 코드 있음 | GameManager.BindViews 에서 호출 안 함 → 현재 prefab 기본 sprite (albatross frame0) 만 표시 |

### 5.1 SaveData wire 작업

1. `GameManager.cs` 에 `[SerializeField] private MonoBehaviour saveRepositoryMb;` 추가 (또는 `Awake` 에서 `new PlayerPrefsSaveRepository()` 직접 생성)
2. `_screenPresenter.BuildTitleViewModel` 에 highScore 전달
3. `HUDPresenter.BuildHudViewModel` 에 highScore 전달
4. Game over/clear 시 highScore 저장 호출

---

## 6. Build/Settings 누락

| 항목 | 현재 | TS 원본 | 작업 |
|---|---|---|---|
| **Player Orientation** | 미확인 | Portrait (모바일 세로) | `Player Settings → Default Orientation = Portrait` |
| **Active Input Handling** | Input System 설치됐다고 가정 | New Input System | `Both` (둘 다 지원) 권장 |
| **`.inputactions` asset** | 없음 | TS 는 자체 폴링 | 옵션 (Keyboard 직접 폴링이라 동작은 함) |
| **TMP Essential Resources** | 설치 가정 | - | `Window → TMP → Import TMP Essential` |

---

## 7. 사용자 지적 8가지 (sub-agent 진행 중)

| # | 사용자 지적 | 추정 root cause | sub-agent 처리 |
|---|---|---|---|
| 1 | Title 배경/폰트 누락 | bg_title.png 가 import 됐지만 wire 안 됨. DNFBitBitv2.ttf 가 SDF 변환 안 됨 | ⏳ 진행 중 |
| 2 | 스토리 자동 스킵 | IntroSequenceSO entry 가 1개 (Mini) 만 | ⏳ 4 페이지 채우기 |
| 3 | 블럭/바/공 sprite 미적용 | block_*.png import 됐지만 BlocksRenderer 가 sprite 안 씀. Bar 너무 짧음 (referenceWidthPx + body.sprite 문제) | ⏳ |
| 4 | ROUND 1 위치 잘못 | RoundLabel/ReadyLabel anchoredPosition 잘못 | ⏳ |
| 5 | HUD 안 보임 | HudView 5 텍스트 anchor 화면 밖 | ⏳ |
| 6 | 마스코트 표시 누락 | MascotRenderer.SetMascot 호출 안 됨. Title 의 마스코트 UI 미구현 | ⏳ |
| 7 | 마스코트/난이도 선택 화면 없음 | UI 미구현 | ⏳ |
| 8 | 화살표 무반응 | UnityInputSnapshotBuilder 또는 InputCommandResolver 에서 화살표 키 매핑 누락 또는 InputSystem 패키지 미설치 | ⏳ critical |

---

## 8. 우선순위 종합표 (sub-agent 완료 후 작업할 항목)

### P0 (즉시 — 게임 동작/visual 핵심)

| # | 항목 | 작업 |
|---|---|---|
| P0.1 | Block sprite 적용 | `BlockDefinitionSO` 에 sprite 필드 + BlocksRenderer 코드 변경 + 5 SO 에 sprite wire |
| P0.2 | Background 적용 | TitlePanel/InGamePanel/GameOverPanel/GameClearPanel backdrop Image.sprite wire |
| P0.3 | DNFBitBitv2 폰트 SDF 변환 + 모든 TMP_Text 의 font 로 set | TMP Font Asset Creator |
| P0.4 | Intro story sprite wire | IntroSequenceSO 4 페이지에 intro_story_01~04 sprite wire |
| P0.5 | Border sprite import + wire | 2 sprite import + Border.prefab 의 SpriteRenderer.sprite |
| P0.6 | Door sprite import + wire + 5 frame 애니 | 6 sprite import + Door.prefab + 애니 컨트롤러 |
| P0.7 | BlockDefinitionSO BaseColor TS 값으로 set | TS `blockDefinitionTable.ts` 읽고 hex 입력 |

### P1 (게임 완성도)

| # | 항목 | 작업 |
|---|---|---|
| P1.1 | Item sprite wire | ItemsRenderer.expandSprite/magnetSprite/laserSprite wire |
| P1.2 | Portrait import + 마스코트 선택 UI | 10 portrait import + UI 만들기 |
| P1.3 | MascotSO sprite wire + MascotRenderer.SetMascot 호출 | GameManager.BindViews 에 한 줄 + 5 MascotSO 에 4 frame sprite wire |
| P1.4 | DifficultyConfigSO Hard 생성 + 토글 UI | asset 생성 + Title 에 좌우 화살표 토글 |
| P1.5 | SaveData wire (PlayerPrefs) | HighScore 저장/로드 |
| P1.6 | AudioCueSO + UnityAudio ClipTable | 19 cue 매핑 + AudioClip 등록 |
| P1.7 | UITextSO 누락 8 text 채우기 | TS uiTextTable.ts 읽고 추가 |

### P2 (폴리싱)

| # | 항목 | 작업 |
|---|---|---|
| P2.1 | GameOver 마스코트 애니 (gameover_frame_1~4 import + 애니 컨트롤러) | |
| P2.2 | Validator 8개 포팅 | |
| P2.3 | PowerupSO 데이터 확인 + 입력 | |
| P2.4 | bg_pixel_01~03 / dance_sheet 용도 확인 후 적용 | |
| P2.5 | StageDefinitionSO Round 2/3 데이터 입력 | |
| P2.6 | Player Settings Portrait + Build 설정 | |

### P3 (출시 전)

| # | 항목 | 작업 |
|---|---|---|
| P3.1 | GlossyStyle Shader/Material | |
| P3.2 | dev 도구 (ReplayRecorder, CollisionLog, InvariantChecker) | |
| P3.3 | 음소거 UI / 마스코트 구매 UI | |
| P3.4 | `.inputactions` asset 자산화 | |
| P3.5 | APK 빌드 + 기기 테스트 | |

---

## 9. 진행 순서 추천

1. **Sub-agent 완료 대기** (현재 진행 중, 8가지 지적 fix)
2. **Play test 1차** — sub-agent 결과 검증, 추가 break 항목 catch
3. **P0 일괄 batch** (sub-agent 가 미처리 시):
   - Border/Door sprite import + 코드 변경
   - Block sprite 코드 변경 + wire
   - 폰트 SDF 변환
   - Intro sprite wire
4. **Play test 2차** — P0 후 visual parity 검증
5. **P1 batch** — SaveData wire, Difficulty Hard, Portrait + 선택 UI
6. **Play test 3차** — 게임 끝까지 한 판
7. **P2/P3** — 마무리 + 빌드

---

## 10. 정확한 값 입력 시 참조 위치

| 값 | TS 파일 경로 |
|---|---|
| BlockDefinition BaseColor hex 5개 | `\\wsl.localhost\Ubuntu\home\rimse\projects\arkanoid\src\definitions\tables\BlockDefinitionTable.ts` |
| DifficultyConfig Hard 수치 | `src/definitions/tables/DifficultyConfigTable.ts` |
| AudioCue 19 cue (CueId/EventType/ResourceId/Pitch/Volume) | `src/definitions/tables/AudioCueTable.ts` |
| UIText 20 entry | `src/definitions/tables/UITextTable.ts` |
| IntroSequence 4 page (text/duration/illustration) | `src/definitions/tables/IntroSequenceTable.ts` |
| Mascot 5 entry (id/displayName/unlockCondition) | `src/definitions/tables/MascotTable.ts` |
| Powerup token (있다면) | `src/definitions/tables/PowerupTable.ts` |
| LayoutConfig (Canvas/Playfield/HUD/Slider 좌표) | `src/definitions/tables/LayoutConfigTable.ts` |
| Stage Round 1/2/3 (Block/Border/Door placement) | `src/definitions/data/stage1.json`, `stage2.json`, `stage3.json` |
| SaveData 필드 (highScore/gold/mascotsUnlocked) | `src/persistence/SaveData.ts` |
| Camera position (Phaser centerOn) | `src/presentation/renderer/GameScene.ts` 또는 SceneRenderer |

---

## 11. 메모리 업데이트 권고

`memory/project_arkanoid_unity.md` 업데이트:
- "Unity Editor 작업" → "TS parity audit 완료, 4 batch fix 진행"
- 진행 표 P0/P1/P2/P3 추가
- next session entry point: "04_ts_parity_audit.md 참조 → P0.5 부터 시작" (또는 현재 진행 batch 위치)

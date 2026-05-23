# 01. 포팅 작업 계획 (Phase 1~5)

알바트로스 (Arkanoid TypeScript+Phaser) → Unity 6.3 *전체 포팅* 의 자세한 작업 계획. 작성 2026-05-23.

- Phase 0 (Unity 멘탈 모델) 완주 후 작성 — `00_mental_model.md` 참조
- 페이스: *혼합 모드* (핵심 구조 함께 설계, 세부 구현 Claude)
- 시간 제약: 없음, **완성도 우선**

---

## 0. 확정 결정 (11개)

| ID | 결정 | 채택안 |
|---|---|---|
| **D1.1** | Assembly Definition 구조 | **C** — `Arkanoid.Gameplay` / `Gameplay.Tests` / `Definitions` / `Presentation` / `Flow` / `Shared` (Shared 분리) |
| **D1.2** | State 타입: struct vs class | **A** — 소형 state (BallState, BarState 등) `struct`, `GameplayRuntimeState` `class` |
| **D1.3** | 수학 헬퍼 | **A** — TS 직역 (`(float x, float y)` ValueTuple), Unity 0의존 유지 (`Arkanoid.Gameplay` 에 `UnityEngine` 참조 X) |
| **D2.1** | SO 폴더 구조 | **B** — 계층별 (`Assets/Data/{Gameplay, Presentation, Definitions}/`) |
| **D2.2** | SO 로드 방식 | **A** — Inspector 직접 레퍼런스 (GameManager `[SerializeField]`) |
| **D3.1** | UI 아키텍처 | **A** — 단일 Unity Scene + Canvas Panel show/hide (TS SceneRenderer 직역) |
| **D3.2** | Object Pool | **A** — 도입, Unity 6 내장 `ObjectPool<T>` 사용 |
| **D3.3** | DOTween | **B** — 미도입, Coroutine + `easing.ts` 직접 포팅 |
| **D3.4** | 카메라/좌표계 단위 | **1 unit = 1 px, `orthographicSize = 960`** (물리 미사용이라 *멘탈 모델 #6 의 "1 unit = 1m" 룰 예외*) |
| **D4.1** | InputActions 구조 | **A** — 단일 `ArkanoidInputActions.inputactions` + Action Map 분리 (UI / Gameplay) |
| **D5.1** | 출시 구성 | **개발 Mono + ARMv7** (학습용, 스토어 출시 없음). 출시 결심 시 IL2CPP + ARM64 전환 (Phase 6 가상) |

---

## 1. TS 알바트로스 구조 (분석 결과)

### gameplay/ — src 29 + test 21 (총 50파일)

- **state/** (9): BallState, BarState, BlockState, BorderBlockState, DoorState, GameplayRuntimeState, GameSessionState, ItemDropState, LaserShotState, SpinnerRuntimeState
- **entities/** (6): Ball, Bar, Block, BorderBlock, Door, Spinner, Wall — 상태 파생 순수 헬퍼
- **systems/** (8 src + 12 test): MovementSystem (sub-step sweep), CollisionService (Circle-AABB), CollisionResolutionService, StageRuleService, StageRuntimeFactory, BarEffectService, LaserSystem, SpinnerSystem, playfieldLayout (좌표 SSOT)
- **controller/** (3 src + 2 test): GameplayController (tick), GameplayLifecycleHandler, InputCommandResolver
- **events/** (1): gameplayEvents.ts (14종 union)
- 좌표: PLAYFIELD 720×900 (논리 px), 캔버스 1080×1920, 카메라 zoom 1.5

### flow/ — 10파일 (src 6 + test 4)

- `FlowStateKind` = `title | introStory | roundIntro | inGame | gameOver | gameClear`
- GameFlowController (이벤트→FlowCommand→nextState), FlowTransitionPolicy, FlowLifecycleHandler

### definitions/tables/ — 13개

| 테이블 | 역할 |
|---|---|
| StageDefinitionTable | 스테이지 3개 (JSON), 블록/스피너/도어 배치 |
| BlockDefinitionTable | 블록 종류별 내구도·드랍 |
| ItemDefinitionTable | expand/magnet/laser 3종 효과 수치 |
| GameplayConfigTable | 초기 lives, 공 속도, 바 속도, 물리 파라미터 |
| DifficultyConfigTable | normal/hard |
| LayoutConfigTable | 캔버스·플레이필드·HUD·슬라이더 좌표 SSOT |
| SpinnerDefinitionTable | spinner_cube/triangle 물리 |
| AudioCueTable | 이벤트→사운드 19 cue |
| UITextTable | 화면 텍스트 20개 |
| IntroSequenceTable | 도입 스토리 4장면 |
| MascotTable | 마스코트 5종 |
| PowerupTable | 파워업 시각 토큰 |
| TrailStyleTable | 공 트레일 3종 |

### presentation/ — 42파일 (test 6)

- renderer/: SceneRenderer, GameScene, renderXxxScreen × 7, inGame/renderXxx × 10
- controller/: ScreenPresenter, ScreenDirector, HUDPresenter, VisualEffectController
- view-models/: HudViewModel 외 5종
- ui/: BallTrail, Button, Toast, GlossyStyle

### Phaser API 사용처 (presentation/)

- sprite/graphics/text: ~12파일
- audio: 1파일 (PhaserAudioPlayer)
- tween: 2파일 (Toast, renderToast)
- camera: 2파일 (GameScene, SceneRenderer)
- input: GameScene, Button, PointerInputSource, KeyboardInputSource

### 의존성 그래프

```
gameplay/ → definitions/ + shared/
flow/ → gameplay/events + presentation/events + definitions/ + input/
presentation/ → gameplay/state(read) + flow/state(read) + definitions/ + shared/
audio/ → definitions/tables/AudioCueTable
input/ (InputSnapshot) → Phaser 0의존 (POCO)
persistence/ → definitions/tables/MascotTable
```

---

## 2. Phase 1 — gameplay 코어 C# 포팅 (Unity 0의존, POCO)

### Task

- **T1.0** Assembly Definition 셋업 — D1.1 채택안 (asmdef 6개: Gameplay/Gameplay.Tests/Definitions/Presentation/Flow/Shared)
- **T1.1** 공유 타입/헬퍼 (Shared asmdef) — `Result<T,E>` struct, `Brand`, `easing` static
- **T1.2** state/ POCO (9개) — 소형 struct, GameplayRuntimeState class (D1.2)
- **T1.3** playfieldLayout.ts → `PlayfieldLayout.cs` 상수 — *모든 system 의 좌표 SSOT, 1번 변환 대상*
- **T1.4** entities/ 순수 함수 (6개) — static class
- **T1.5** systems/ (8개) — playfieldLayout 후 순서대로: Collision → CollisionResolution → Movement → StageRule → BarEffect → Laser → Spinner → StageRuntimeFactory
- **T1.6** controller/ (3개) — InputCommandResolver → GameplayController → GameplayLifecycleHandler
- **T1.7** gameplayEvents → C# sealed class hierarchy
- **T1.8** NUnit 테스트 포팅 (21 .test.ts → C#) — Edit Mode

### 의존성

T1.0 → T1.1 → T1.2 → T1.3 → T1.4 → T1.5 → T1.6 → T1.7 → T1.8

### 마일스톤

- C# NUnit Edit Mode 테스트 통과 (Unity 에디터 미실행 — `dotnet test` 가능)
- 968 테스트 케이스 중 gameplay 분 100%

---

## 3. Phase 2 — definitions/tables → ScriptableObject

### Task

- **T2.1** SO 기반 구조 (D2.1 채택안: `Assets/Data/{Gameplay, Presentation, Definitions}/`)
- **T2.2** definitions/types/ → C# data class (14개)
- **T2.3** 13개 SO 변환 (우선순위 순):
  1. GameplayConfigSO  2. LayoutConfigSO  3. BlockDefinitionSO  4. ItemDefinitionSO
  5. DifficultyConfigSO  6. StageDefinitionSO  7. SpinnerDefinitionSO  8. AudioCueSO
  9. UITextSO  10. IntroSequenceSO  11. MascotSO  12. PowerupSO  13. TrailStyleSO
- **T2.4** Inspector 직접 참조 와이어링 (D2.2)
- **T2.5** stage1~3.json → SO 마이그레이션 (EditorScript or 수동)
- **T2.6** validator C# 포팅 (필요성 재검토)

### 마일스톤

- 13 `.asset` 파일 Inspector 정상 표시
- SO 로드 → gameplay 코어가 값 정확히 읽어옴 (Play Mode 1회 검증)

---

## 4. Phase 3 — 프리젠테이션 매핑 (Phaser API → Unity)

### Task

- **T3.1** 단일 Scene + Panel 구조 셋업 (D3.1)
- **T3.2** AudioCueResolver + `IArkanoidAudio` — Unity/Noop 구현체 2개
- **T3.3** Input Adapter — InputSnapshot POCO + KeyboardInputSource / PointerInputSource (Unity Input System 구현체)
- **T3.4** view-models/ C# 포팅 (6개)
- **T3.5** ScreenState/ScreenDirector — C# enum + service
- **T3.6** HUDPresenter / ScreenPresenter — TextMeshPro
- **T3.7** inGame 렌더러 10개 → Unity 컴포넌트 + Object Pool (D3.2)
  - BallRenderer / BarRenderer / BlockRenderer (Pool) / BorderRenderer / HudView / ItemRenderer / LaserRenderer / SliderView / SpinnerRenderer / MascotRenderer
- **T3.8** 화면 렌더러 7개 → UI Panel (Title/RoundIntro/InGame/GameOver/GameClear/IntroStory/Pause)
- **T3.9** 시각 효과 — BallTrail (TrailRenderer), Toast (Coroutine), VisualEffectController, GlossyStyle (Shader/Material)
- **T3.10** 카메라 (D3.4: `orthographicSize = 960`, 1 unit = 1 px)

### 마일스톤

- InGame 씬 공·바·블록 렌더링 (Play Mode)
- 오디오 cue 정상 재생
- 전체 시각 효과 확인

---

## 5. Phase 4 — 씬 흐름 + 입력

### Task

- **T4.1** D3.1 의 단일 Scene + Panel 구조 따라감
- **T4.2** GameFlowController C# 포팅 (FlowStateKind, GameFlowState, FlowTransitionPolicy, FlowInputResolver, FlowLifecycleHandler)
- **T4.3** flowEvents / presentationEvents → C# sealed class hierarchy
- **T4.4** AppContext → Unity GameManager MonoBehaviour, FlowEventRouter
- **T4.5** `ArkanoidInputActions.inputactions` 작성 (D4.1: Action Map UI / Gameplay 분리)
  - MoveLeft/Right, LaunchBall, Pause, Touch/Drag
- **T4.6** 씬 전환 — FlowStateKind 전이 → Panel 활성화
- **T4.7** flow NUnit 테스트 포팅 (4파일)

### 마일스톤

- title → introStory → roundIntro → inGame → gameOver/Clear 전체 전환
- 키보드 + 터치 둘 다 동작
- flow 테스트 통과

---

## 6. Phase 5 — 통합 + 빌드

### Task

- **T5.1** SaveData — `ISaveRepository` + PlayerPrefs 구현 + InMemory 구현
- **T5.2** persistence 연동 (highScore, gold, mascots)
- **T5.3** Play Mode 통합 테스트 (한 스테이지 완주)
- **T5.4** Android 빌드 — D5.1 채택안 (Mono + ARMv7 유지, 학습용)
  - URP Android 최적화, 세로 고정
- **T5.5** 프로파일링 (GC Alloc, Object Pool 효과, batching)
- **T5.6** 폴리싱 (음소거 UI, 마스코트 구매 UI, 앱 재시작 복원)
- **T5.7** APK 빌드 + 기기 설치 테스트

### 마일스톤

- APK 설치 → Android 기기에서 전체 플로우 동작
- 60fps 목표 (또는 프로파일 기록)

---

## 7. 추정 (보수적, 완성도 우선)

| Phase | 내용 | 추정 |
|---|---|---|
| 1 | gameplay C# + NUnit | 10~14일 |
| 2 | 13 SO + 타입 변환 | 5~7일 |
| 3 | 프리젠테이션 42파일 | 14~18일 |
| 4 | 씬 흐름 + InputActions | 5~7일 |
| 5 | 통합 + Android 빌드 | 7~10일 |
| **합계** | | **41~56일** |

병렬 가능: Phase 1 진행 중 Phase 2 의 definitions/types C# 변환 (T2.2) 착수 가능.

---

## 8. 다음 세션 진입점

새 세션에서:

1. 이 파일 + `00_mental_model.md` + `HANDOFF.md` + `[[project_arkanoid_unity]]` 메모리 읽기
2. **Phase 1 T1.0** 부터 시작 — Assembly Definition 6개 셋업
3. 진행 순서: T1.0 → T1.1 (Shared) → T1.2 (state) → T1.3 (playfieldLayout) → ... → T1.8 (테스트)
4. *작은 단위마다 사용자 리뷰* — 사용자 페이스: 혼합 모드 (핵심 구조 함께, 세부 구현 Claude)
5. Phase 1 끝 마일스톤: `dotnet test` 로 NUnit Edit Mode 테스트 통과

### Critical files (변환 1순위)

- `playfieldLayout.ts` — 모든 system 의 좌표 SSOT, T1.3
- `GameplayRuntimeState.ts` — state 루트, T1.2
- `GameplayController.ts` — tick 루프, T1.6
- `GameFlowController.ts` — 씬 전환 상태기계, Phase 4 T4.2
- `LayoutConfigTable.ts` — 카메라/UI 좌표 SSOT, D3.4 기준값

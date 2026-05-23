# 00. Unity 멘탈 모델

알바트로스 TS → Unity 6.3 포팅에 필요한 *왜* 들. 한 개념씩 누적. 파인만 톤.

---

## 1. GameObject / Component / Transform

### 한 줄

**GameObject = 빈 봉투, Component = 기능 스티커, Transform = 모든 컴포넌트가 공유하는 좌표 원본.**

### 왜 클래스 하나로 안 하나

TS 의 `Ball` 클래스처럼 한 곳에 다 묶으면 두 가지가 부서져요:

1. **변종 추가 시 폭발** — "공 + 자석" 같은 거 만들려고 상속/복사 시작하면 금방 꼬임.
2. **부분 on/off 가 어려움** — 충돌만 끄고 렌더링 살리고 싶을 때 클래스 안 if 분기 늘어남.

Unity 는 *상속 대신 조립*. GameObject 에 Component 를 *붙였다 뗐다* 함. `magnet.enabled = false` 한 줄로 변종 효과 끔.

### Transform 의 특별함

- 모든 GameObject 가 *반드시* 가짐 — **떼낼 수 없음** (Inspector 에 Remove 메뉴 없음)
- 다른 Component 는 다 옵션 (SpriteRenderer 없으면 안 보임, Collider2D 없으면 안 부딪힘)
- 위치/회전/스케일을 가지며, **다른 모든 Component 가 여기서 좌표를 가져감**
- → 알바트로스 TS 만들 때 한 번 깨진 부분 (collision 좌표 vs render 좌표 sync 안 맞은 일) 이 *구조적으로 불가능* 해짐

### 물리는 두 층

TS Ball.ts 의 발사/이동 로직을 Unity 로 옮길 때 가장 큰 사고방식 차이:

- **`Rigidbody2D` (엔진 자동)** — velocity 한 번 세팅하면 그 다음은 엔진이 매 프레임 굴림. 중력/관성/충돌 응답까지 자동.
- **커스텀 MonoBehaviour (내 결정)** — *언제* 발사할지, *어떤 각도* 로 쏠지만 결정. Rigidbody2D 에 명령만 던짐.

→ **TS 의 `this.x += this.vx` 매 프레임 루프는 Unity 로 옮길 때 사라짐.** 그 일은 Rigidbody2D 가 함.

### 알바트로스 Ball → Unity 매핑

GameObject 1개 ("Ball") + Component 5개:

| Component | 역할 | TS Ball.ts 의 어느 부분 |
|---|---|---|
| `Transform` | 위치/회전/스케일 | `this.x, this.y` |
| `SpriteRenderer` | 그림 + 색 | render 함수 |
| `CircleCollider2D` | 충돌 모양 | `radius` + collision check |
| `Rigidbody2D` | 물리 자동 처리 | `x += vx` 매 프레임 루프 |
| `BallLauncher` (custom) | 발사 시점/각도 결정 | launch 메서드 |

### 셀프 체크

> 알바트로스 TS Ball.ts 에서 *매 프레임* `this.x += this.vx` 하던 코드는 Unity 로 옮기면 어디로 가야 하나? `Rigidbody2D`? 별도 커스텀 컴포넌트? 둘 다? 답하고 *왜* 인지.

(답: 이 라인 자체는 *사라짐*. 발사 시점에 `rb.velocity = direction * speed` 한 번만 세팅하면 그 다음은 Rigidbody2D 가 자동으로 굴림. 커스텀 컴포넌트는 "언제 발사" 만 결정.)

---

## 2. Scene / Prefab / ScriptableObject

### 한 줄

**Scene = 화면 배치도, Prefab = 객체 조립도, SO = 데이터 표.** 셋이 *따로* 살아있으면서 *참조* 로 협력해요.

### 생애주기가 다른 이유

- **Scene** → *판마다* 바뀜 (Stage1, Stage2, ...). TS 알바트로스의 `sceneStack` 동등 — `SceneManager.LoadScene("Game")` 이 sceneStack push/pop 과 같은 일.
- **Prefab** → *객체 종류마다* 1개 (Ball, Brick, Paddle, ...). 종류는 잘 안 늘어남.
- **SO** → *튜닝 단위마다* 1개 (BallStatsEasy/Normal/Hard, ...). 가장 자주 추가/수정.

→ 가장 자주 바뀌는 걸 *가장 작은 단위* 로 분리해두면, 코드/Prefab 안 건드리고 SO 만 새로 만들어서 튜닝 가능.

### Prefab 과 SO 의 *관계*

Prefab 은 SO 를 *담고 있지 않아요. 참조해요.* 두 파일은 *따로 살아있음*.

```
ballPrefab.prefab            ballStatsNormal.asset
 ├── Transform                 (SO 파일, prefab 밖)
 ├── SpriteRenderer
 ├── CircleCollider2D
 ├── Rigidbody2D
 └── BallLauncher
      └── config 슬롯 ──참조──> ballStatsNormal.asset
```

→ **같은 prefab 으로 다른 SO 를 끼우면** 조립은 동일, 숫자만 달라짐. "데이터-구조-배치 분리" 의 핵심.

### TS 알바트로스 → Unity 매핑

| TS 알바트로스 | Unity |
|---|---|
| `sceneStack` (Title/Game/GameOver 스위칭) | `Scene` (.unity) + `SceneManager.LoadScene` |
| `new Ball(...)` 호출 패턴 | `Prefab` + `Instantiate` |
| `definitions/tables/*.ts` (13개 데이터 테이블) | 13개 `ScriptableObject` (.asset) |

### 난이도 응용 (사용자 답)

"쉬움/보통/어려움" 다르게 두려면:
- **SO 3개** 만 만들어둠 (`ballStatsEasy/Normal/Hard.asset`)
- 게임 시작 시 *Prefab 의 config 슬롯에 어떤 SO 를 끼울지* 만 결정
- Prefab/Scene 은 *그대로*

알바트로스 TS 에서 difficulty 가 *데이터 테이블 분기* 였던 거랑 같은 패턴 — Unity 는 이걸 Inspector 차원에서 지원.

### 셀프 체크

> 알바트로스 TS 의 `definitions/tables/spinnerConfig.ts` 같은 회전체 설정표는 Unity 로 옮길 때 Scene/Prefab/SO 중 어디로? 그리고 회전체 *Prefab 자체* 와는 무슨 관계?

(답: SO 로 감 — 가변 숫자 데이터. 회전체 Prefab 의 커스텀 컴포넌트가 *SO 를 참조* 하는 관계 — Prefab 안에 *직접 들어있지 않음*.)

---

## 3. MonoBehaviour 라이프사이클

### 한 줄

**엔진이 내 메서드를 호출해요** — 알바트로스 TS 가 *내가 엔진 흐름 제어* 였다면, Unity 는 *엔진이 내 코드 호출* (Inversion of Control). 내가 할 일은 *정해진 이름의 메서드를 정의해두는 것* 뿐.

### 4+1 메서드

| 메서드 | 호출 시점 | 어떤 코드 |
|---|---|---|
| `Awake()` | GameObject 생성 직후, 1번만 | 자기 자신 init (`GetComponent`, `[SerializeField]` 검증) |
| `Start()` | 모든 Awake 후, 첫 Update 직전, 1번만 | 외부 의존 init (`GameObject.Find`, 매니저 구독) |
| `Update()` | *매 프레임* (모니터 Hz) | 입력, UI, 애니메이션 트리거 |
| `FixedUpdate()` | *고정 간격* (기본 50Hz) | 물리 (`Rigidbody2D`, AddForce, transform 이동) |
| `LateUpdate()` | 모든 Update 끝난 후 | 카메라 추적 |

### 두 가지 룰 (외울 것)

- **자기 안 = `Awake`, 자기 밖 = `Start`.** Awake 시점엔 *다른 GameObject 의 init 이 아직 안 됐을 수 있음*. Start 시점엔 *전부 끝난 게 보장*.
- **물리 = `FixedUpdate`, 그 외 = `Update`.** Update 는 프레임레이트에 따라 호출 빈도가 달라져서 *물리 명령* 을 Update 에 두면 모니터 Hz 에 따라 결과가 흔들림. FixedUpdate 는 고정 간격이라 일정.

### 알바트로스 TS → Unity 매핑

| TS 알바트로스 | Unity |
|---|---|
| Phaser scene 의 `create()` | `Start()` (외부 의존) + `Awake()` (자기 init) |
| `update(dt)` 의 *입력 부분* | `Update()` |
| `update(dt)` 의 *물리 부분* (`x += vx * dt`) | `FixedUpdate()` — 라인 자체는 사라지고 `rb.velocity` 한 번 세팅으로 대체 |

### 사용자가 도달한 메타

- **"좌표 변경 = 물리"** — TS 에선 단순 산술이었지만, Unity 에선 *물리 카테고리* 로 분류.
- **"매 프레임 = 최신 정보 보장"** — 입력은 *지금 누른 키* 가 매 프레임 필요하니 Update.

### 셀프 체크

> 알바트로스 TS 에서 카메라가 `update(dt)` 안에서 *공 위치를 따라가는* 코드가 있다고 해보자. Unity 로 옮길 때 `Update` / `FixedUpdate` / `LateUpdate` 중 어디로? 왜?

(답: `LateUpdate`. 공이 Update/FixedUpdate 에서 *먼저 이동* 한 후 카메라가 그 위치를 따라가야 *한 프레임 뒤떨어짐* 없음. 순서 보장.)

---

## 4. Coroutine vs async/await

### 한 줄

**Coroutine = Unity 안 시간 (게임 시간), async/await = Unity 밖 시간 (외부 IO).**

사용자 비유: *라면 끓이기 (타이머 3분 → Coroutine) vs 라면 배달 (그동안 다른 일 → async/await).*

### Coroutine — 시간 슬라이스 함수

```csharp
IEnumerator BlinkBall()
{
    while (true)
    {
        sprite.enabled = false;
        yield return new WaitForSeconds(0.5f);
        sprite.enabled = true;
        yield return new WaitForSeconds(0.5f);
    }
}
StartCoroutine(BlinkBall());
```

- `IEnumerator` 반환 + `yield return` → C# 이 *generator* 만들어줌
- Unity 가 매 프레임 generator 의 `MoveNext()` 호출
- `yield return new WaitForSeconds(0.5f)` 만나면 0.5초 동안 안 깨움 → 후 다음 라인 재개
- 본질: **Unity 매 프레임 루프 위에서 조각조각 진행**

### async/await — Task 기반 비동기

```csharp
async Task BlinkBall()
{
    while (true) { sprite.enabled = false; await Task.Delay(500); ... }
}
```

- `Task` 기반 — TS Promise 와 거의 동일
- 스레드 풀로 옮길 수 있음
- **Unity 라이프사이클과 무관**

### 가장 큰 차이 — 생명선

| 항목 | Coroutine | async/await |
|---|---|---|
| 기반 | Unity 매 프레임 루프 | C# Task |
| GameObject 비활성화 시 | **자동 중단** | *계속 돎* |
| `Time.timeScale = 0` | `WaitForSeconds` 멈춤 | `Task.Delay` 진행 (실제 시간) |
| 주 용도 | *게임 내 시간 시퀀스* | *외부 비동기 (네트워크/파일)* |

### 왜 게임 내 동작에 Coroutine 이 기본값인가

GameObject 죽었는데 코드가 계속 돌면 → *이미 없는 객체* 참조 → `NullReferenceException`. Unity 에선 *GameObject 죽음 ≠ C# 객체 죽음* 이라 더 위험.

- **Coroutine**: GameObject 죽으면 자동 중단 → NullReference 자동 회피
- **async/await**: 수동으로 `CancellationToken` 으로 끊어야 함. 안 끊으면 크래시 위험

룰: **게임 내 = Coroutine, 외부 IO = async/await (반드시 cancel 처리)**.

### 알바트로스 케이스 매핑

| 알바트로스 동작 | Unity |
|---|---|
| "3, 2, 1 카운트다운 후 발사" | Coroutine |
| "벽돌 깨질 때 0.3초 폭발 → 제거" | Coroutine (GameObject 죽으면 자동 멈춤이 안전) |
| "AdMob 광고 로드 대기" | async/await (외부 네트워크) |
| "타이틀 페이드인 1초" | Coroutine |

### 셀프 체크

> 알바트로스에서 *세이브 데이터를 디스크에 쓰는* 코드는 Unity 어디 패턴으로? Coroutine? async/await? 왜?

(답: async/await — 파일 IO 는 *외부* 작업이고 스레드 풀로 옮기면 메인 스레드 안 막음. Coroutine 으로 하면 메인 스레드가 디스크 쓰는 동안 멈춤. 단 cancel 처리 필수.)

---

## 5. Input System (new)

### 한 줄

**입력은 코드에서 분기하지 말고 자산에서 통합하라.** New Input System = *이벤트 기반 + 디바이스 추상화 액션*.

### Old vs New

| 항목 | Old (`Input.GetKey`) | New (`UnityEngine.InputSystem`) |
|---|---|---|
| 호출 방식 | 매 프레임 폴링 | 이벤트 콜백 (`action.performed += fn`) |
| 디바이스 추상화 | 디바이스별 분기 | **"Action" 으로 통합** |
| 멀티터치 | 번거로움 | `Touchscreen.current.touches[0..N]` |
| 리바인딩 | 어려움 | 자산에서 시각적 |

### Action 모델

```csharp
[SerializeField] InputActionReference tapAction;

void OnEnable()  { tapAction.action.Enable();  tapAction.action.performed += OnTap; }
void OnDisable() { tapAction.action.Disable(); tapAction.action.performed -= OnTap; }

void OnTap(InputAction.CallbackContext ctx) { LaunchBall(ctx.ReadValue<Vector2>()); }
```

- **InputAction**: "Tap", "Drag", "Pause" 같은 *추상 액션*
- **`.inputactions` 자산**: 액션 정의 + 디바이스 바인딩. 에디터에서 시각적으로 매핑

### 핵심 메타 — 추상화는 *자산* 에서

```
PaddleDrag 액션
 ├── 바인딩 1: <Pointer>/position    ← 마우스/터치 통합 (가상 디바이스 <Pointer>)
 ├── 바인딩 2: <Keyboard>/leftArrow  ← 옵션
 └── 바인딩 3: <Gamepad>/leftStick   ← 옵션
```

알바트로스 TS 라면 `if (mouse) ... else if (touch) ...` 코드 분기였을 텐데, Unity 는 **자산에서 매핑** — 분기 자체가 코드 밖으로 빠짐.

### 알바트로스 액션 설계

| Action | 바인딩 | 용도 |
|---|---|---|
| `PaddleDrag` | `<Pointer>/position` | 패들 이동 |
| `LaunchBall` | `<Pointer>/press` | 공 발사 |
| `Pause` | `<Keyboard>/escape`, UI Button | 일시정지 |

### 함정 — OnEnable/OnDisable

- `OnEnable` 에서 `Enable()` 안 부르면 → 액션 비활성 → 콜백 안 옴 → *입력 안 먹힘* (가장 흔한 버그)
- `OnDisable` 에서 `Disable()` + `-=` 안 하면 → GameObject 꺼져도 콜백 계속 → 죽은 객체 호출 → 크래시

### 셀프 체크

> 알바트로스에서 *멀티터치 핀치 줌* (두 손가락으로 줌인/아웃) 을 구현하려면 New Input System 어디를 봐야 하나? Action 1개? 2개?

(답: `Touchscreen.current.touches[0]` 와 `[1]` 의 거리 변화를 매 프레임 읽는 패턴. Action 으로도 가능하지만 *두 손가락 좌표를 동시에 알아야 함* — 보통 `EnhancedTouchSupport` + `Touch.activeTouches` 로 직접 읽는 게 자연스러움.)

---

## 6. 좌표계 — *포팅 가장 큰 함정*

### 한 줄

**부호도 반대, 원점도 다르고, 단위도 다름.** TS Phaser (화면 픽셀, Y+ 아래) → Unity World (world unit, Y+ 위).

### Unity 의 4가지 좌표계

| 좌표계 | 원점 / 단위 | Y 방향 | 어디서 쓰나 |
|---|---|---|---|
| **World Space** | 월드 원점 / world unit | Y+ 위 | `transform.position`, 모든 GameObject |
| **Screen Space** | 좌**하**단 / px | Y+ 위 | raw 입력 좌표 |
| **Viewport Space** | 좌**하**단 / 0~1 | Y+ 위 | 카메라 비율 무관 위치 |
| **Canvas Space** | RectTransform 기준 / px | Y+ 위 | UI 요소 |

**핵심**: Unity 의 *모든* 좌표계가 Y+ 위. Phaser 의 Y+ 아래 (화면 픽셀) 와 반대.

### 변환 함수

```csharp
Vector2 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
```

### 포팅 3대 함정

**1. vy 부호 반전**
```
TS:    ball.velocity = (0, -5)  // 위로
Unity: rb.velocity   = (0, +5)  // 위로
```
모든 방향 벡터의 y 성분 반전. cos/sin 사용한 발사각도 점검.

**2. 원점 위치 다름**
```
TS:    paddle.y = 1100  // 좌상단 (0,0) 기준
Unity: paddle.y = -4    // 카메라 중앙 (0,0) 기준
```
TS 좌표 그대로 복사 → 화면 밖.

**3. 단위 다름**
- TS: 1 = 1 px
- Unity world: 1 = 1 m (보통)

`paddle.y = 1100` 을 그대로 옮기면 *1100 m 떨어진 곳* — 카메라 frustum (±5 m) 의 220배 너머.

### 알바트로스 권장 세팅

모바일 세로 (9:16):
```
Camera (Orthographic)
 ├── orthographicSize = 5      // 카메라 가시 세로 절반 = 5 m (총 10 m)
 ├── playfield 가로 ≈ 5.6 m    // 9:16 비율
 └── 원점 (0,0) = playfield 중앙

World 좌표 예:
 - playfield 상단:  y = +5
 - playfield 하단:  y = -5
 - 패들:            y = -4
 - 공 발사 방향:    (0, +5)
```

### UI 는 별도 좌표계

스코어/일시정지 버튼 같은 UI 는 **Canvas 좌표계** — World 와 *섞이지 않음*. 알바트로스 TS 의 UI 카메라 분리가 Unity 에선 Canvas 로 풀려요.

### 셀프 체크

> 알바트로스 TS 에서 화면 좌상단 (10, 10) 에 스코어 텍스트를 띄웠다. Unity 로 옮길 때 이걸 World 좌표로 변환해서 GameObject 로 만들어야 하나? 다른 방법?

(답: World 로 변환하지 말 것. UI 는 **Canvas + Text 컴포넌트** 로 별도 처리 — Screen Space - Overlay 모드 Canvas 에 RectTransform 으로 좌상단 anchor. UI 와 World 는 *섞이지 않게* 분리하는 게 Unity 의 권장 패턴.)

---

## 7. Camera / Canvas (Phase 0 마지막)

### 한 줄

**Camera 는 World 를 렌더, Canvas 는 UI 를 렌더. 둘은 분리되어 합성됨.** 알바트로스 TS multi-camera 의 Unity 대응.

### 3종 합성 옵션

| 옵션 | 구성 | 적합 케이스 |
|---|---|---|
| **A. 단일 카메라 + Overlay Canvas** | Main Camera + Canvas (Screen Space - Overlay) | *가장 단순.* 카메라 효과 = world 만, UI 자동 분리. **알바트로스 추천**. |
| **B. 단일 카메라 + Camera Canvas** | Main Camera + Canvas (Screen Space - Camera) | UI 에도 카메라 후처리 적용하고 싶을 때 |
| **C. Camera Stack (URP)** | Base Camera + Overlay Camera | TS multi-camera 와 *문법적으로 가장 가까움* |

### Canvas Render Mode 3종

| Mode | 특성 | 비유 |
|---|---|---|
| **Screen Space - Overlay** | 항상 화면 위. 카메라 무관. | 화면에 *스티커* |
| **Screen Space - Camera** | 특정 카메라에 묶임. shake/zoom 같이 받음. | 카메라 *렌즈에* 종이 한 장 |
| **World Space** | Canvas 가 GameObject 처럼 3D world 안에. | 게임 월드 *간판* (체력바 머리 위) |

### 알바트로스 권장 — 옵션 A

```
Main Camera (Orthographic)
 ├── orthographicSize = 5
 ├── Clear Flags = Solid Color    ← 모바일은 Skybox 성능 낭비
 └── playfield 렌더 (Ball/Paddle/Brick)

Canvas (Screen Space - Overlay)
 ├── Canvas Scaler = Scale With Screen Size  ← 폰마다 UI 크기 다르게 보이지 않게
 ├── Reference Resolution = 1080 × 1920
 └── 자식: Score Text, Pause Button, Game Over Panel
```

→ **Camera shake 가 playfield 만 흔들고 UI 는 그대로** 가 *자동*. 옵션 A 의 매력.

### 꼭 챙길 세팅 2개

- **Canvas Scaler** = `Scale With Screen Size` + ref `1080×1920`. 안 하면 폰마다 UI 크기 다르게 보임.
- **Main Camera Clear Flags** = `Solid Color`. 모바일에서 Skybox 는 성능 낭비.

### TS multi-camera → Unity 매핑

| TS 알바트로스 | Unity (옵션 A) |
|---|---|
| Main camera (playfield 좌표계) | Main Camera (Orthographic) |
| UI camera (canvas 좌표계) | Canvas (Screen Space - Overlay) |
| Camera shake (playfield 만) | `Camera.main.transform` 흔들기. UI 영향 없음 |
| HUD 스코어/시간 | Canvas 자식 Text/TMP |
| 일시정지 반투명 오버레이 | Canvas 자식 Image (alpha) |

### 셀프 체크

> 알바트로스에 *플레이필드 zoom-in 효과* (보스 등장 시 카메라가 확대) 가 있고, *UI 는 그대로* 유지하고 싶다. 어떤 세팅?

(답: 옵션 A. Main Camera 의 `orthographicSize` 를 줄이면 zoom-in. Canvas 는 Screen Space - Overlay 라 카메라와 무관 → UI 자동 그대로. 옵션 B 라면 UI 도 같이 확대됨.)

---

# Phase 0 완료

7개 개념:
1. ✅ GameObject / Component / Transform
2. ✅ Scene / Prefab / ScriptableObject
3. ✅ MonoBehaviour 라이프사이클
4. ✅ Coroutine vs async/await
5. ✅ Input System (new)
6. ✅ 좌표계 (포팅 가장 큰 함정)
7. ✅ Camera / Canvas

다음: **Phase 1 — gameplay/ 코어 포팅** (C# NUnit 968 테스트, Unity 의존 0, POCO).

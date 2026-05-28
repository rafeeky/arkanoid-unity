# 03 Troubleshooting Log — Safe Mode 탈출기

TS+Phaser → Unity 포팅 후 첫 Unity Editor 오픈 시 발생한 시행착오와 해결법 (2026-05-25).

## 큰 그림

Unity 가 **Safe Mode** 로 부팅됨 → 컴파일 에러 누적 때문 → 패키지/메뉴 (`Window > MCP for Unity` 등) 다 로드 안 됨 → 콘솔에러부터 잡아야 정상 모드 진입.

에러 카테고리 → 해결법 순.

---

## 1. asmdef 가 Unity 패키지 참조 누락

### 증상

```
error CS0234: 'InputSystem' does not exist in namespace 'UnityEngine'
error CS0246: 'TMPro' could not be found
error CS0246: 'TMP_Text' could not be found
```

`Packages/manifest.json` 에는 `com.unity.inputsystem`, `com.unity.ugui` 둘 다 설치되어 있는데도 에러.

### 원인

`Arkanoid.Presentation.asmdef` 가 `"autoReferenced": false` 로 되어 있어서, asmdef 의 `references` 배열에 **명시적으로 적힌 어셈블리만** 컴파일 시 보임. Unity 6 부터 TextMeshPro 는 `com.unity.ugui` 패키지에 흡수됐고, 어셈블리 이름은 여전히 `Unity.TextMeshPro`.

### 해결

`Assets/Scripts/Presentation/Arkanoid.Presentation.asmdef` 의 `references` 에 추가:

```json
"references": [
    ...,
    "Unity.InputSystem",
    "Unity.TextMeshPro"
]
```

### 교훈

- `autoReferenced: false` asmdef 는 패키지를 명시 참조해야 함
- Unity 6+ TextMeshPro 별도 패키지 설치 불필요 (UGUI 에 통합)
- 패키지 설치 ≠ 컴파일러가 볼 수 있음. asmdef 가 게이트.

---

## 2. `DefaultConfig` 이름 충돌 (40+ 에러)

### 증상

```
error CS0229: Ambiguity between 'TestHelpers.DefaultConfig' and 'DefaultConfig'
```

테스트 파일 거의 모든 곳에서.

### 원인

`Assets/Scripts/Definitions/Types/PhysicsConfig.cs` 에 사용되지 않는 정적 클래스:

```csharp
public static class DefaultConfig {
    public static readonly PhysicsConfig Physics = new(...);
}
```

테스트는 `using Arkanoid.Definitions;` (이 클래스 보임) + `using static Arkanoid.Gameplay.Tests.TestHelpers;` (이 안의 `DefaultConfig` 정적 프로퍼티 보임) → 이름 충돌.

검색 결과 production 의 `DefaultConfig` 클래스는 **어디서도 사용 안 됨** (데드 코드).

### 해결

`PhysicsConfig.cs` 에서 `DefaultConfig` 정적 클래스 통째로 삭제.

### 교훈

- C# 의 `using static` 은 강력하지만, 같은 이름 충돌 시 모호성으로 막힘
- 포팅 중 "혹시 쓸지도" 하고 남겨둔 데드 코드가 나중에 충돌 원인
- 첫 빌드 시 미사용 심볼은 즉시 제거

---

## 3. `using System.Linq;` 누락

### 증상

```
error CS1061: 'BlockState[]' does not contain a definition for 'ToArray'
```

### 원인

`BlockState[]` 는 이미 array 인데 `.ToArray()` 호출 — 사본 만들기 위함. 이건 LINQ 확장 메서드. `using System.Linq;` 가 없으면 안 보임.

### 해결

테스트 2개 파일 (`AdjacentBlockMisfireTests.cs`, `TunnelingRootCauseTests.cs`) 맨 위에 `using System.Linq;` 추가.

### 교훈

- TS 에는 `array.slice()` 가 자연스럽지만 C# 의 array → array 사본은 `Array.Copy` / `Linq.ToArray` 중 택1
- 포팅 시 "TS 에서 자연스러운 기능" → "C# 에선 추가 using 필요" 체크리스트 필요

---

## 4. `BlockDefinition` 포팅 버그 — `VisualId` vs `BaseColor`

### 증상

```
error CS1061: 'BlockDefinition' does not contain a definition for 'BaseColor'
```

`BlocksRenderer.cs:50` 에서 `def.BaseColor` 호출 중.

### 원인

설계 문서 `docs/unity-port/02_editor_setup.md` L68 에 명시:

> Block_blue, Block_red ... DefinitionId / **BaseColor (Hex)** / Hits / Score / DropItem

그런데 실제 `BlockDefinition` 타입은:

```csharp
public readonly record struct BlockDefinition(
    string DefinitionId,
    int MaxHits,
    int Score,
    ItemType? DropItemType,
    string VisualId);   // ← 설계와 다름!
```

`VisualId` 는 정의/SO 두 곳 외 **어디서도 안 쓰임** — 포팅 시 잘못 들어간 필드.

### 해결

1. `BlockDefinition.cs`: `string VisualId` → `int BaseColor`
2. `BlockDefinitionSO.cs`: `string visualId = "block_basic"` → `int baseColor = 0xCCCCCC`
3. 테스트 5개 (`CollisionResolutionServiceTests`, `GameplayControllerTests`, `LaserSystemTests`, `StageRuntimeFactoryTests`, `SpinnerSystemTests`) 의 더미값 `"v"` / `"block_normal_visual"` → `0xCCCCCC`

### 교훈

- **포팅 버그는 설계 문서 vs 코드 vs 사용처 (renderer) 3 자 비교로 찾음**
- 사용처가 있고, 설계 문서가 있으면 둘 다 같은 방향 가리키는 게 맞음. 타입만 외톨이면 타입이 버그
- 미사용 필드는 빨간 깃발

---

## 5. CS8632 nullable 어노테이션 경고 (28개)

### 증상

```
warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
```

코드 곳곳에서 `string?`, `int?` 같은 nullable 어노테이션 사용 중인데, 프로젝트에 nullable 컨텍스트가 꺼져 있어서 경고.

### 해결

`Assets/csc.rsp` 에 한 줄 추가:

```
-langversion:10
-nullable:enable
```

→ 프로젝트 전체에 nullable reference types 컨텍스트 활성화 → 모든 CS8632 경고 사라짐.

### 교훈

- `csc.rsp` = Unity 의 C# 컴파일러 옵션 (response file). asmdef 마다 따로 둘 수도 있지만 `Assets/` 루트에 두면 전체 적용
- TS 의 strictNullChecks 처럼, nullable 어노테이션 (`?`) 은 컨텍스트 켜야 의미 있음

---

## 6. MCP for Unity Bridge 환경 세팅

### 증상 1 — `Window > MCP for Unity` 메뉴 안 보임

→ Safe Mode 때문. 위 1~4 컴파일 에러부터 해결.

### 증상 2 — Bridge 켜려니 `'uvx'은 내부 또는 외부 명령... 아닙니다`

`uvx` = Astral 사의 `uv` Python 패키지 매니저에 포함된 도구. MCP for Unity Python 서버를 `uvx` 로 실행함.

**해결:**

```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

설치 위치: `C:\Users\rimse\.local\bin\` (uv.exe, uvx.exe, uvw.exe). PATH 자동 등록.

### 증상 3 — Bridge UI 에 `Python Not Found`

MCP Bridge 는 `python.exe` 자체를 PATH 에서 찾음 (uvx 만으로는 부족).

**해결:**

```powershell
winget install --id Python.Python.3.12 -e --silent
```

### 증상 4 — 설치 후에도 Unity 가 인식 못 함

Unity 는 **에디터 시작 시점의 PATH 를 캐시**함. 새로 설치한 도구 인식하려면 **Unity 재시작 필수**.

### 교훈

- MCP for Unity 의존성: `uv` (uvx) + `python.exe` 둘 다 PATH 에 있어야 함
- 환경 변수 (PATH) 갱신 후엔 Unity 재시작
- "Server Started" vs "Client Configured" 는 다름:
  - **Server** (= 구버전 "Bridge"): Unity 내부 TCP 리스너 → 매번 켜야 함. UI: `Window > MCP for Unity > Toggle Mcp Window` → `Connect` 탭 → `Server` → `Start Server`
  - **Client Configuration**: Claude Code 같은 외부 클라이언트의 MCP 설정 자동 등록 → 일회성

---

---

## 7. "게임이 안 돌아간다" — 모든 텍스트 안 보임 (2026-05-29)

### 증상

Play 모드에서 배경/버튼은 보이는데 **모든 TMP 텍스트가 안 보임** — 로고("ALBATROSS")/버튼 라벨("NORMAL PLAY")/HUD/스토리 글자 전멸. 콘솔 에러 0, 컴파일 OK, EditMode 372/372 통과. 게임 Flow(Title→스토리→InGame)는 정상 작동.

### 오진 경로 (참고 — 시간 낭비한 가설들)

1. **폰트 atlas 깨짐 가설** — `DNFBitBitv2 SDF` / `MalgunGothic SDF` 의 `m_AtlasTextures` UnassignedReferenceException → 폰트 SDF 재생성 시도. 부분적으로 맞지만 근본 원인 아님.
2. **TMP Essential Resources 손상 가설** — LiberationSans SDF도 atlas 없는 듯해서 재임포트 시도. 실제로 TMP Settings/shader/LiberationSans 모두 정상이었음.

### 진짜 근본 원인

**씬의 모든 `TextMeshProUGUI` 컴포넌트의 `enabled = false`.** GameObject 는 active 인데 컴포넌트만 비활성 → mesh(vertexCount) 0 → 글자 안 그려짐.

진단 결정타 (`ArkanoidFix/D` 메뉴):
```
DNF glyphs=96 chars=96 lookup=107 hasN=True atlas=1024x1024 readable=True shader supported=True  ← 폰트 완전 정상
Label text='NORMAL PLAY' enabled=False charCount=0  ← 컴포넌트가 disabled!
DYNAMIC TMP (DNF) charCount=7 vertexCount=32  ← 동적 생성 텍스트는 정상 렌더
```

폰트/RectTransform/shader/TMP Settings 다 정상인데 charCount=0 이면 → **컴포넌트 enabled 의심**.

### 해결

1. `DNFBitBitv2 Dynamic SDF` 생성 — `TMP_FontAsset.CreateFontAsset(Dynamic)` + `TryAddCharacters(ASCII)` 로 atlas 굽기 (`ReadFontAssetDefinition()` 로 lookup 재구축). Static 모드는 `TryAddCharacters` 거부하므로 Dynamic 유지.
2. **Canvas 하위 모든 `TMP_Text.enabled = true`** 일괄 설정 (`ArkanoidFix/E` 메뉴, 46개). ← 이게 실제 해결.
3. 코드엔 `.enabled = false` 호출 없음 → 씬에 저장된 상태였음. 한 번 켜면 재발 안 함.

### 교훈

- **"텍스트 안 보임" + 폰트 정상 = 컴포넌트 `enabled` 부터 확인.** charCount=0 인데 폰트 멀쩡하면 거의 컴포넌트/계층 비활성.
- Scene View 는 TMP dynamic atlas 를 종종 안 그림 — Game View(또는 동적 생성 테스트)로 검증.
- `manage_camera screenshot` 의 카메라 캡처는 Screen Space Overlay UI 를 **제외** → UI 디버깅엔 `scene_view` 캡처나 사용자 Game View 확인.
- `execute_code` MCP 툴은 이 환경에서 mono 실행 경로 길이 초과로 깨짐 → Editor 스크립트 + `execute_menu_item` 패턴으로 우회.

---

## 8. 폰트 자산 구조 정리 (2026-05-29)

- **`Assets/TextMesh Pro/`** = Unity TMP 패키지 시스템 리소스 (shader, TMP Settings, LiberationSans 기본 폰트). **게임 폰트 아님 / 삭제 금지** — TMP 작동 필수 인프라.
- **`Assets/Fonts/`** = 게임 전용 폰트. 실제 사용 = **`DNFBitBitv2 Dynamic SDF.asset`** (94 참조), 한글 fallback = `MalgunGothic Dynamic SDF.asset`.
- 삭제한 미사용 폰트: `malgun SDF.asset`(102MB, 0참조), `DNFBitBitv2 SDF.asset`(구 깨진 SDF, 0참조).
- 폰트 SDF 재생성이 필요하면 `Window > TextMeshPro > Font Asset Creator` (GUI) 가 가장 확실. 코드 생성은 Dynamic 모드 + `TryAddCharacters` + `ReadFontAssetDefinition` 조합 필요.

---

## 9. 라운드 배경 bg_stage → bg_pixel 교체 + stage 별 전환 (2026-05-29)

- 기존: InGamePanel/RoundIntroPanel 의 `Background` Image 가 `bg_stage_01` 고정 (stage 전환 없음).
- 변경: `bg_pixel_01~03` 로 교체 + **stage index 별 배경 전환** 추가.
  - `GameManager.ApplyStageBackground(stageIndex)` — `stageBackgroundSprites[]`(bg_pixel_01~03) 를 `stageBackgroundImages[]`(InGame/RoundIntro Background) 에 적용. `BindViews` 의 showGameplay 블록에서 매 프레임 호출.
- 삭제: `Assets/Sprites/Backgrounds/bg_stage_01~03.png`. (V2/bg 폴더의 `bg_stage01~03` 는 별개 파일이라 보존.)
- 에디터 스크립트(`ArkanoidPortingFinish3`, `ArkanoidParityFix`)의 bg_stage 참조도 bg_pixel 로 변경.

---

## 한 줄 요약 체크리스트 (다음에 새 Unity 환경에서 같은 프로젝트 열 때)

1. ☐ asmdef 패키지 참조 누락 없는지 (`Unity.InputSystem`, `Unity.TextMeshPro`)
2. ☐ `csc.rsp` 에 `-nullable:enable` 있는지
3. ☐ `uv` 설치 (`uvx --version` 확인)
4. ☐ Python 설치 (`python --version` 확인)
5. ☐ Unity 재시작 후 `Window > MCP for Unity > Toggle Mcp Window` → `Connect` → `Server` → `Start Server`
6. ☐ TMP 텍스트 안 보이면 → `TextMeshProUGUI.enabled` 부터 확인 (§7)
7. ☐ 게임 폰트 = `Assets/Fonts/DNFBitBitv2 Dynamic SDF` (§8)

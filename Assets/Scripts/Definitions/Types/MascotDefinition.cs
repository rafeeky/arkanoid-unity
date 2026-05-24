using System.Collections.Generic;

namespace Arkanoid.Definitions
{
    // 응원 픽셀아트 캐릭터 정의. Title 선택/잠금 해제 + InGame 우측하단 4프레임 댄스 애니.
    // Unity 매핑: ScriptableObject (MascotData) — id 별 sprite 4장 + 이름 + 비용.
    public readonly record struct MascotDefinition(
        string Id,                 // 고유 ID — 'albatross', 'kongming', 'snowrabbit' 등.
        string DisplayName,        // Title/InGame 표시 이름 (영문 대문자 권장).
        string Subtitle,           // 한국어 부제.
        int UnlockCost,            // 0 이면 시작부터 해제 (default: albatross).
        int PlaceholderColor,      // 16진수 RGB. Unity 포팅 시 sprite 로 교체.
        int PlaceholderStrokeColor,
        // 4프레임 댄스 애니의 sprite ID 배열. 현재 placeholder 단계 — Unity 시 실제 sprite 연결.
        IReadOnlyList<string> SpriteFrameIds);
}

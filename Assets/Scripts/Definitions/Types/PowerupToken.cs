namespace Arkanoid.Definitions
{
    // 파워업 시각 토큰 — 색·아이콘·라벨. Unity 매핑: ScriptableObject 한 장 (PowerupSO).
    // PowerupId 는 ItemType (Expand/Magnet/Laser) 와 동일 도메인 — ItemType 재사용.
    public readonly record struct PowerupToken(
        // 16진수 RGB (Phaser Graphics.fillStyle 용 number — C# 에선 int 사용).
        int Color,
        // 아이콘 텍스처 키 — renderBlocks.ts:ensureIconTextures 의 generateTexture key.
        string IconKey,
        // 라벨 (대문자, 느낌표 없음). 토스트는 "{label}!" 식으로 표시.
        string Label);
}

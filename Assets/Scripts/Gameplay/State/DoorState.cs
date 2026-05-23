namespace Arkanoid.Gameplay
{
    // 상단 테두리 위에 배치되는 게이트.
    // Closed   → BorderBlock 처럼 작동. 공/스피너 모두 차단.
    // Opening  → 왼쪽 슬라이드 애니. 공 차단, 스피너 spawn 전.
    // Opened   → 사라진 상태. 공 차단, 스피너 통과 가능.
    public enum DoorPhase { Closed, Opening, Opened }

    public readonly record struct DoorState(
        string Id,
        float X,
        float Y,
        DoorPhase Phase,
        // Opening 시작 후 경과 시간 (ms). 0 이면 시작 안 됨.
        float OpeningElapsedMs,
        // 열리면 어떤 스피너가 spawn 될지.
        string SpinnerDefinitionId,
        // Opened 후 생성된 spinner 의 id. 아직 없으면 null.
        string? SpawnedSpinnerId);
}

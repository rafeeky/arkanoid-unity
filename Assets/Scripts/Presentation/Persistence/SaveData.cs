using System.Collections.Generic;

namespace Arkanoid.Presentation
{
    // 영속 저장 데이터. PlayerPrefs / 파일 직렬화 대상.
    public readonly record struct SaveData(
        int HighScore,
        int Gold,
        IReadOnlyList<string> UnlockedMascots,
        string SelectedMascot)
    {
        // DEFAULT_MASCOT_ID — MascotTable 의 첫 entry (현재 "albatross").
        public const string DefaultMascotId = "albatross";

        public static SaveData CreateDefault() =>
            new(0, 0, new[] { DefaultMascotId }, DefaultMascotId);
    }
}

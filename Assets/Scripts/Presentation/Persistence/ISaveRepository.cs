namespace Arkanoid.Presentation
{
    // SaveData 로드/저장. PlayerPrefs sync API 기반 → sync interface.
    // 호출자 책임: 변경 시 Load → 수정 → Save (full SaveData).
    // 미래 파일 IO 도입 시 Task-based async 로 확장 가능.
    public interface ISaveRepository
    {
        SaveData Load();
        void Save(SaveData data);
    }
}

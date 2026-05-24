namespace Arkanoid.Presentation
{
    // 메모리 내 SaveData 보관. 테스트 / placeholder 용. 프로세스 재시작 시 초기화.
    public sealed class InMemorySaveRepository : ISaveRepository
    {
        private SaveData _data;

        public InMemorySaveRepository(SaveData? initial = null)
        {
            _data = initial ?? SaveData.CreateDefault();
        }

        public SaveData Load() => _data;
        public void Save(SaveData data) => _data = data;
    }
}

using System.Collections.Generic;
using Arkanoid.Definitions;

namespace Arkanoid.Presentation
{
    // eventType → AudioCueEntry[] 매핑 조회.
    // 현재 테이블은 1:1 매핑이지만 미래 1:N 확장 위해 배열 반환.
    // Unity 0의존 POCO. AudioBridge MonoBehaviour 에서 주입받아 사용.
    public sealed class AudioCueResolver
    {
        private readonly Dictionary<string, List<AudioCueEntry>> _byEventType = new();

        public AudioCueResolver(IReadOnlyList<AudioCueEntry> table)
        {
            foreach (var entry in table)
            {
                if (!_byEventType.TryGetValue(entry.EventType, out var list))
                {
                    list = new List<AudioCueEntry>();
                    _byEventType[entry.EventType] = list;
                }
                list.Add(entry);
            }
        }

        // eventType 에 해당하는 모든 AudioCueEntry 반환. 매핑 없으면 빈 배열.
        public IReadOnlyList<AudioCueEntry> Resolve(string eventType) =>
            _byEventType.TryGetValue(eventType, out var list)
                ? list
                : System.Array.Empty<AudioCueEntry>();
    }
}

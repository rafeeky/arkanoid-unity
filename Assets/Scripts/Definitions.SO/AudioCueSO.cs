using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    // AudioCueTable — 19 cue 의 컨테이너. Inspector 에서 List<Entry> 편집.
    [CreateAssetMenu(fileName = "AudioCueTable", menuName = "Arkanoid/Presentation/Audio Cue Table")]
    public sealed class AudioCueSO : ScriptableObject
    {
        [SerializeField] private List<SerializableEntry> entries = new();

        public IReadOnlyList<AudioCueEntry> Data
        {
            get
            {
                var list = new List<AudioCueEntry>(entries.Count);
                foreach (var e in entries)
                {
                    list.Add(new AudioCueEntry(
                        CueId: e.cueId,
                        EventType: e.eventType,
                        ResourceId: e.resourceId,
                        PlaybackType: e.playbackType,
                        Pitch: e.pitch < 0f ? (float?)null : e.pitch,
                        PlayDurationMs: e.playDurationMs < 0f ? (float?)null : e.playDurationMs,
                        Volume: e.volume < 0f ? (float?)null : e.volume));
                }
                return list;
            }
        }

        [System.Serializable]
        public class SerializableEntry
        {
            public string cueId = "";
            public string eventType = "";
            public string resourceId = "";
            public PlaybackType playbackType = PlaybackType.Sfx;
            public float pitch = -1f;          // sentinel: 음수 = null
            public float playDurationMs = -1f;
            public float volume = -1f;
        }
    }
}

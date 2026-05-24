using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "IntroSequenceTable", menuName = "Arkanoid/Presentation/Intro Sequence Table")]
    public sealed class IntroSequenceSO : ScriptableObject
    {
        [SerializeField] private List<SerializableEntry> entries = new();

        public IReadOnlyList<IntroSequenceEntry> Data
        {
            get
            {
                var list = new List<IntroSequenceEntry>(entries.Count);
                foreach (var e in entries)
                    list.Add(new IntroSequenceEntry(e.pageIndex, e.text, e.typingSpeedMs, e.holdDurationMs, e.eraseSpeedMs));
                return list;
            }
        }

        [System.Serializable]
        public class SerializableEntry
        {
            public int pageIndex;
            [TextArea] public string text = "";
            public float typingSpeedMs = 40f;
            public float holdDurationMs = 1500f;
            public float eraseSpeedMs = 20f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "UITextTable", menuName = "Arkanoid/Presentation/UI Text Table")]
    public sealed class UITextSO : ScriptableObject
    {
        [SerializeField] private List<SerializableEntry> entries = new();

        public IReadOnlyList<UITextEntry> Data
        {
            get
            {
                var list = new List<UITextEntry>(entries.Count);
                foreach (var e in entries) list.Add(new UITextEntry(e.textId, e.value));
                return list;
            }
        }

        [System.Serializable]
        public class SerializableEntry
        {
            public string textId = "";
            [TextArea] public string value = "";
        }
    }
}

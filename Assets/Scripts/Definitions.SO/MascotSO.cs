using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "MascotTable", menuName = "Arkanoid/Presentation/Mascot Table")]
    public sealed class MascotSO : ScriptableObject
    {
        [SerializeField] private List<SerializableEntry> entries = new();

        public IReadOnlyList<MascotDefinition> Data
        {
            get
            {
                var list = new List<MascotDefinition>(entries.Count);
                foreach (var e in entries)
                {
                    list.Add(new MascotDefinition(
                        Id: e.id,
                        DisplayName: e.displayName,
                        Subtitle: e.subtitle,
                        UnlockCost: e.unlockCost,
                        PlaceholderColor: e.placeholderColor,
                        PlaceholderStrokeColor: e.placeholderStrokeColor,
                        SpriteFrameIds: e.spriteFrameIds));
                }
                return list;
            }
        }

        [System.Serializable]
        public class SerializableEntry
        {
            public string id = "";
            public string displayName = "";
            public string subtitle = "";
            public int unlockCost = 0;
            public int placeholderColor;        // 16진수 RGB
            public int placeholderStrokeColor;
            public string[] spriteFrameIds = new string[0];
        }
    }
}

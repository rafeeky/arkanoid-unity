using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    // POWERUP_TABLE — ItemType 별 PowerupToken (색·아이콘·라벨).
    [CreateAssetMenu(fileName = "PowerupTable", menuName = "Arkanoid/Presentation/Powerup Table")]
    public sealed class PowerupSO : ScriptableObject
    {
        [SerializeField] private SerializableEntry expand = new();
        [SerializeField] private SerializableEntry magnet = new();
        [SerializeField] private SerializableEntry laser = new();

        public IReadOnlyDictionary<ItemType, PowerupToken> Data => new Dictionary<ItemType, PowerupToken>
        {
            [ItemType.Expand] = new(expand.color, expand.iconKey, expand.label),
            [ItemType.Magnet] = new(magnet.color, magnet.iconKey, magnet.label),
            [ItemType.Laser] = new(laser.color, laser.iconKey, laser.label),
        };

        [System.Serializable]
        public class SerializableEntry
        {
            public int color;        // 16진수 RGB
            public string iconKey = "";
            public string label = "";
        }
    }
}

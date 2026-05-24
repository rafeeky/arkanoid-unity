using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Arkanoid/Gameplay/Item Definition")]
    public sealed class ItemDefinitionSO : ScriptableObject
    {
        [SerializeField] private ItemType itemType = ItemType.Expand;
        [SerializeField] private string displayNameTextId = "";
        [SerializeField] private string descriptionTextId = "";
        [SerializeField] private string iconId = "";
        [SerializeField] private float fallSpeed = 160f;
        [SerializeField] private ItemType effectType = ItemType.Expand;

        // Optional 값들 — sentinel < 0 = null.
        [Header("Optional (음수면 미설정)")]
        [SerializeField] private float expandMultiplier = -1f;
        [SerializeField] private float magnetDurationMs = -1f;
        [SerializeField] private int magnetUseCount = -1;
        [SerializeField] private float laserCooldownMs = -1f;
        [SerializeField] private int laserShotCount = -1;
        [SerializeField] private float laserDurationMs = -1f;

        public ItemDefinition Data => new(
            ItemType: itemType,
            DisplayNameTextId: displayNameTextId,
            DescriptionTextId: descriptionTextId,
            IconId: iconId,
            FallSpeed: fallSpeed,
            EffectType: effectType,
            ExpandMultiplier: expandMultiplier < 0f ? (float?)null : expandMultiplier,
            MagnetDurationMs: magnetDurationMs < 0f ? (float?)null : magnetDurationMs,
            MagnetUseCount: magnetUseCount < 0 ? (int?)null : magnetUseCount,
            LaserCooldownMs: laserCooldownMs < 0f ? (float?)null : laserCooldownMs,
            LaserShotCount: laserShotCount < 0 ? (int?)null : laserShotCount,
            LaserDurationMs: laserDurationMs < 0f ? (float?)null : laserDurationMs);
    }
}

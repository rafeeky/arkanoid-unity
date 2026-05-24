using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "BlockDefinition", menuName = "Arkanoid/Gameplay/Block Definition")]
    public sealed class BlockDefinitionSO : ScriptableObject
    {
        [SerializeField] private string definitionId = "basic";
        [SerializeField] private int maxHits = 1;
        [SerializeField] private int score = 10;
        [SerializeField] private DropItemKind dropItem = DropItemKind.None;
        [SerializeField] private string visualId = "block_basic";

        public BlockDefinition Data => new(
            DefinitionId: definitionId,
            MaxHits: maxHits,
            Score: score,
            DropItemType: dropItem.ToItemTypeOrNull(),
            VisualId: visualId);
    }
}

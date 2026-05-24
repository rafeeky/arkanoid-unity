using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Arkanoid/Gameplay/Difficulty Config")]
    public sealed class DifficultyConfigSO : ScriptableObject
    {
        [SerializeField] private int initialLives = 5;
        [SerializeField] private bool spinnersEnabled = false;

        public DifficultyConfig Data => new(initialLives, spinnersEnabled);
    }
}

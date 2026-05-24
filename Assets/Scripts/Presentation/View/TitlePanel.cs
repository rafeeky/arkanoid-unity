using TMPro;
using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Presentation.View
{
    // Title 화면 — StartText, HighScore, 선택된 Difficulty 표시.
    public sealed class TitlePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text startText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text difficultyText;

        public void Bind(TitleScreenViewModel vm)
        {
            if (startText != null) startText.text = vm.StartText;
            if (highScoreText != null) highScoreText.text = $"HIGH SCORE  {vm.HighScore}";
            if (difficultyText != null)
                difficultyText.text = vm.SelectedDifficulty == DifficultyKind.Hard ? "[HARD]" : "[NORMAL]";
        }
    }
}

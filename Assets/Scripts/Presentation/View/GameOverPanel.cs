using TMPro;
using UnityEngine;

namespace Arkanoid.Presentation.View
{
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text gameOverLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private TMP_Text retryText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color newHiColor = new(1f, 0.9f, 0.2f, 1f);

        public void Bind(GameOverViewModel vm)
        {
            if (gameOverLabel != null) gameOverLabel.text = vm.GameOverLabel;
            if (finalScoreLabel != null) finalScoreLabel.text = vm.FinalScoreLabel;
            if (highScoreLabel != null)
            {
                highScoreLabel.text = vm.HighScoreLabel;
                highScoreLabel.color = vm.IsNewHighScore ? newHiColor : normalColor;
            }
            if (retryText != null) retryText.text = vm.RetryText;
        }
    }
}

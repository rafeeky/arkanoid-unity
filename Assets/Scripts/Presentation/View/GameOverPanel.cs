using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.Presentation.View
{
    // GameOver — label/score 표시 + 4 frame mascot 애니 (200ms 주기).
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text gameOverLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private TMP_Text retryText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color newHiColor = new(1f, 0.9f, 0.2f, 1f);

        [Header("Mascot (gameover_frame_1~4)")]
        [SerializeField] private Image mascotImage;
        [SerializeField] private Sprite[] mascotFrames = new Sprite[4];
        [SerializeField] private float frameIntervalSec = 0.2f;

        private float _accumTime;
        private int _frameIdx;

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

        private void Update()
        {
            if (mascotImage == null || mascotFrames == null || mascotFrames.Length == 0) return;
            _accumTime += Time.deltaTime;
            if (_accumTime >= frameIntervalSec)
            {
                _accumTime = 0f;
                _frameIdx = (_frameIdx + 1) % mascotFrames.Length;
                var f = mascotFrames[_frameIdx];
                if (f != null)
                {
                    mascotImage.sprite = f;
                    mascotImage.enabled = true;
                }
            }
        }
    }
}

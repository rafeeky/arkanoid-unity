using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Flow;

namespace Arkanoid.Presentation.View
{
    // GameOver — label/score + 마스코트 3개 (각 4-frame anim) + Retry/Title 두 버튼.
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text gameOverLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private TMP_Text retryText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color newHiColor = new(1f, 0.9f, 0.2f, 1f);

        [Header("Mascot — 나란히 3개, 각 4 frame anim (gameover_frame_1~4)")]
        [SerializeField] private Image[] mascotImages = new Image[3];
        [SerializeField] private Sprite[] mascotFrames = new Sprite[4];
        [SerializeField] private float frameIntervalSec = 0.2f;
        // 호환 — 옛 단일 mascot 참조 보존 (씬 wire 호환).
        [SerializeField] private Image mascotImage;

        [Header("Retry/Title 두 버튼 — onClick GameFlowController")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private GameManager gameManager;

        private float _accumTime;
        private int _frameIdx;

        private void OnEnable()
        {
            WireBtn(retryButton, () => gameManager?.RequestRetry());
            WireBtn(titleButton, () => gameManager?.RequestQuitToTitle());
        }
        private static void WireBtn(Button b, UnityEngine.Events.UnityAction onClick)
        {
            if (b == null) return;
            b.onClick.RemoveListener(onClick);
            b.onClick.AddListener(onClick);
        }

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
            if (mascotFrames == null || mascotFrames.Length == 0) return;
            _accumTime += Time.deltaTime;
            if (_accumTime >= frameIntervalSec)
            {
                _accumTime = 0f;
                _frameIdx = (_frameIdx + 1) % mascotFrames.Length;
                var f = mascotFrames[_frameIdx];
                if (f == null) return;
                // 3개 (또는 옛 단일 mascotImage) 모두 같은 프레임 sprite 동기 표시
                if (mascotImages != null)
                {
                    foreach (var img in mascotImages)
                        if (img != null) { img.sprite = f; img.enabled = true; }
                }
                if (mascotImage != null) { mascotImage.sprite = f; mascotImage.enabled = true; }
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Flow;

namespace Arkanoid.Presentation.View
{
    // GameClear — headline/score + 마스코트 3개 (4 frame anim) + Retry/Title 두 버튼.
    public sealed class GameClearPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text headlineLabel;
        [SerializeField] private TMP_Text finalScoreLabel;
        [SerializeField] private TMP_Text highScoreLabel;
        [SerializeField] private TMP_Text retryText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color newHiColor = new(1f, 0.9f, 0.2f, 1f);

        [Header("Mascot — 나란히 3개, 각 4 frame anim")]
        [SerializeField] private Image[] mascotImages = new Image[3];
        [SerializeField] private Sprite[] mascotFrames = new Sprite[4];
        [SerializeField] private float frameIntervalSec = 0.2f;

        [Header("Retry/Title 두 버튼")]
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

        public void Bind(GameClearViewModel vm)
        {
            if (headlineLabel != null) headlineLabel.text = vm.Headline;
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
                if (f == null || mascotImages == null) return;
                foreach (var img in mascotImages)
                    if (img != null) { img.sprite = f; img.enabled = true; }
            }
        }
    }
}

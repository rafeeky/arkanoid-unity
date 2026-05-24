using TMPro;
using UnityEngine;

namespace Arkanoid.Presentation.View
{
    // Round X / READY! 표시. IntroProgress 로 fade.
    public sealed class RoundIntroPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text readyLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Bind(RoundIntroViewModel vm)
        {
            if (roundLabel != null) roundLabel.text = vm.RoundLabel;
            if (readyLabel != null) readyLabel.text = vm.ReadyLabel;

            // IntroProgress 0~1 — 0.0~0.2 fade-in, 0.8~1.0 fade-out.
            if (canvasGroup != null)
            {
                float a = 1f;
                if (vm.IntroProgress < 0.2f) a = vm.IntroProgress / 0.2f;
                else if (vm.IntroProgress > 0.8f) a = (1f - vm.IntroProgress) / 0.2f;
                canvasGroup.alpha = Mathf.Clamp01(a);
            }
        }
    }
}

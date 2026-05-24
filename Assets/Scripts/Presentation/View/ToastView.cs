using System.Collections;
using TMPro;
using UnityEngine;

namespace Arkanoid.Presentation.View
{
    // 짧은 메시지 토스트 — fade-in → hold → fade-out (Coroutine + AnimationCurve).
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInSec = 0.3f;
        [SerializeField] private float holdSec = 1.2f;
        [SerializeField] private float fadeOutSec = 0.5f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine _running;

        public void Show(string text)
        {
            if (label != null) label.text = text;
            if (_running != null) StopCoroutine(_running);
            gameObject.SetActive(true);
            _running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (canvasGroup == null)
            {
                yield return new WaitForSeconds(fadeInSec + holdSec + fadeOutSec);
                gameObject.SetActive(false);
                yield break;
            }

            float t = 0f;
            while (t < fadeInSec)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = fadeCurve.Evaluate(Mathf.Clamp01(t / fadeInSec));
                yield return null;
            }
            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(holdSec);

            t = 0f;
            while (t < fadeOutSec)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = 1f - fadeCurve.Evaluate(Mathf.Clamp01(t / fadeOutSec));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _running = null;
        }
    }
}

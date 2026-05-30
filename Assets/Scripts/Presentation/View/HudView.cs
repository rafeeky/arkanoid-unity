using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // HUD — TextMeshPro 4개 + Effect + 픽셀 하트 (TS renderHud HEART_PATTERN 7x6 동치).
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text livesText;       // 더 이상 사용 안 함 (하트로 대체) — 호환 위해 유지
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text effectText;

        [Header("Lives — 픽셀 하트 (TS HEART_PATTERN 7x6 동치)")]
        [SerializeField] private Transform livesContainer;
        [SerializeField] private Color heartColor = new(1f, 0.2f, 0.27f, 1f); // #ff3344
        [SerializeField] private float heartGapPx = 14f;

        private const int HEART_PIXEL = 6;
        private const int HEART_W = 7 * HEART_PIXEL;   // 42
        private const int HEART_H = 6 * HEART_PIXEL;   // 36
        private static readonly int[,] HEART_PATTERN = new int[6, 7] {
            {0,1,1,0,1,1,0},
            {1,1,1,1,1,1,1},
            {1,1,1,1,1,1,1},
            {0,1,1,1,1,1,0},
            {0,0,1,1,1,0,0},
            {0,0,0,1,0,0,0},
        };
        private static Sprite _heartSprite;
        private readonly List<GameObject> _hearts = new();

        public void Bind(HudViewModel vm)
        {
            if (scoreText != null) scoreText.text = $"SCORE  {vm.Score}";
            if (highScoreText != null) highScoreText.text = $"HIGH SCORE  {vm.HighScore}";
            if (livesText != null) livesText.text = "";   // 텍스트 비움 (하트로 대체)
            if (roundText != null) roundText.text = $"ROUND  {vm.Round}";
            UpdateEffectAlert(vm);
            UpdateHearts(vm.Lives);
        }

        // C9: EXPAND!/MAGNET/LASER 알림 — effect 변경 순간 노출, 2.5초 후 자동 사라짐.
        private BarEffect _prevEffect = BarEffect.None;
        private float _effectAlertRemainingSec;
        private const float EFFECT_ALERT_DURATION_SEC = 2.5f;

        private void UpdateEffectAlert(HudViewModel vm)
        {
            if (effectText == null) return;
            // effect 가 바뀌는 순간 알림 표시
            if (vm.ActiveEffect != _prevEffect && vm.ActiveEffect != BarEffect.None)
            {
                _effectAlertRemainingSec = EFFECT_ALERT_DURATION_SEC;
                effectText.text = vm.ActiveEffect switch
                {
                    BarEffect.Expand => "EXPAND!",
                    BarEffect.Magnet => "MAGNET!",
                    BarEffect.Laser => "LASER!",
                    _ => "",
                };
            }
            _prevEffect = vm.ActiveEffect;
            // timer 감소
            if (_effectAlertRemainingSec > 0f)
            {
                _effectAlertRemainingSec -= Time.deltaTime;
                if (_effectAlertRemainingSec <= 0f) effectText.text = "";
            }
        }

        private void UpdateHearts(int lives)
        {
            if (livesContainer == null) return;
            EnsureHeartCount(lives);
            for (int i = 0; i < _hearts.Count; i++)
                _hearts[i].SetActive(i < lives);
        }

        private void EnsureHeartCount(int n)
        {
            while (_hearts.Count < n)
            {
                var go = new GameObject($"Heart{_hearts.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(livesContainer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(HEART_W, HEART_H);
                rt.anchoredPosition = new Vector2(_hearts.Count * (HEART_W + heartGapPx), 0f);
                var img = go.GetComponent<Image>();
                img.sprite = GetHeartSprite();
                img.color = heartColor;
                img.raycastTarget = false;
                _hearts.Add(go);
            }
        }

        private static Sprite GetHeartSprite()
        {
            if (_heartSprite != null) return _heartSprite;
            int w = HEART_W, h = HEART_H;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[w * h];
            for (int row = 0; row < 6; row++)
            for (int col = 0; col < 7; col++)
            {
                bool on = HEART_PATTERN[row, col] == 1;
                for (int dy = 0; dy < HEART_PIXEL; dy++)
                for (int dx = 0; dx < HEART_PIXEL; dx++)
                {
                    int px = col * HEART_PIXEL + dx;
                    int py = (5 - row) * HEART_PIXEL + dy;   // Texture y=0 bottom
                    pixels[py * w + px] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _heartSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0f), 1f);
            return _heartSprite;
        }
    }
}

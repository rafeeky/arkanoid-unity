using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.Presentation.View
{
    // Pause 오버레이 — TS renderPauseOverlay 동치: 배경음/효과음 toggle + Resume/Title.
    public sealed class PauseOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text helpText;

        [Header("4 버튼 — TS renderPauseOverlay 동치")]
        [SerializeField] private Button bgmButton;        // 배경음 toggle (label 변경 BGM ON/OFF)
        [SerializeField] private TMP_Text bgmLabel;
        [SerializeField] private Button sfxButton;        // 효과음 toggle
        [SerializeField] private TMP_Text sfxLabel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private GameManager gameManager;

        private const string KeyBgm = "arkanoid.bgmOn";
        private const string KeySfx = "arkanoid.sfxOn";

        private void OnEnable()
        {
            Wire(bgmButton, ToggleBgm);
            Wire(sfxButton, ToggleSfx);
            Wire(resumeButton, OnResume);
            Wire(titleButton, OnQuitToTitle);
            RefreshLabels();
        }

        private static void Wire(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b == null) return;
            b.onClick.RemoveListener(a);
            b.onClick.AddListener(a);
        }

        public void Bind(string labelText = "PAUSED", string help = "")
        {
            if (label != null) label.text = labelText;
            if (helpText != null) helpText.text = help;
        }

        private void ToggleBgm()
        {
            var on = PlayerPrefs.GetInt(KeyBgm, 1) != 0;
            PlayerPrefs.SetInt(KeyBgm, on ? 0 : 1);
            PlayerPrefs.Save();
            RefreshLabels();
        }
        private void ToggleSfx()
        {
            var on = PlayerPrefs.GetInt(KeySfx, 1) != 0;
            PlayerPrefs.SetInt(KeySfx, on ? 0 : 1);
            PlayerPrefs.Save();
            RefreshLabels();
        }
        private void RefreshLabels()
        {
            if (bgmLabel != null) bgmLabel.text = $"배경음\n{(PlayerPrefs.GetInt(KeyBgm, 1) != 0 ? "ON" : "OFF")}";
            if (sfxLabel != null) sfxLabel.text = $"효과음\n{(PlayerPrefs.GetInt(KeySfx, 1) != 0 ? "ON" : "OFF")}";
        }

        // Pause/Resume Flow 상태는 별도 enum 미정 — 일단 overlay 만 토글 (TS 와 동일)
        private void OnResume() => gameObject.SetActive(false);
        private void OnQuitToTitle() => gameManager?.RequestQuitToTitle();
    }
}

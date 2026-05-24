using TMPro;
using UnityEngine;

namespace Arkanoid.Presentation.View
{
    // Pause 오버레이 — 어두운 배경 + "PAUSED" + 안내 텍스트.
    public sealed class PauseOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text helpText;

        public void Bind(string labelText = "PAUSED", string help = "PRESS P TO RESUME")
        {
            if (label != null) label.text = labelText;
            if (helpText != null) helpText.text = help;
        }
    }
}

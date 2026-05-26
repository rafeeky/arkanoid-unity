using TMPro;
using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // HUD — TextMeshPro 4개 (Score / HiScore / Lives / Round) + Effect 라벨.
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text effectText;

        public void Bind(HudViewModel vm)
        {
            if (scoreText != null) scoreText.text = $"SCORE  {vm.Score}";
            if (highScoreText != null) highScoreText.text = $"HI  {vm.HighScore}";
            // ♥ (U+2665) 는 기본 폰트에 없는 케이스 많아 텍스트로 폴백.
            if (livesText != null) livesText.text = $"LIVES x{vm.Lives}";
            if (roundText != null) roundText.text = $"ROUND  {vm.Round}";
            if (effectText != null)
            {
                effectText.text = vm.ActiveEffect switch
                {
                    BarEffect.Expand => "EXPAND",
                    BarEffect.Magnet => $"MAGNET ({vm.MagnetRemainingUses ?? 0})",
                    BarEffect.Laser => "LASER",
                    _ => "",
                };
            }
        }
    }
}

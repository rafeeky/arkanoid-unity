using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // 좌측 세로 슬라이더. Magnet 잔여 횟수 + Laser 잔여 시간 두 게이지.
    // UI.Slider 또는 Image fillAmount 패턴.
    public sealed class SliderView : MonoBehaviour
    {
        [SerializeField] private Image magnetFill;
        [SerializeField] private Image laserFill;
        [SerializeField] private GameObject magnetGroup;
        [SerializeField] private GameObject laserGroup;

        [SerializeField] private float magnetMaxUses = 5f;        // Magnet 최대 사용 횟수.
        [SerializeField] private float laserMaxDurationMs = 8000f;  // Laser 최대 지속.

        public void Bind(HudViewModel vm)
        {
            var showMagnet = vm.ActiveEffect == BarEffect.Magnet && (vm.MagnetRemainingUses ?? 0) > 0;
            if (magnetGroup != null) magnetGroup.SetActive(showMagnet);
            if (showMagnet && magnetFill != null)
                magnetFill.fillAmount = Mathf.Clamp01((vm.MagnetRemainingUses ?? 0) / magnetMaxUses);

            var showLaser = vm.ActiveEffect == BarEffect.Laser && (vm.LaserRemainingMs ?? 0f) > 0f;
            if (laserGroup != null) laserGroup.SetActive(showLaser);
            if (showLaser && laserFill != null)
                laserFill.fillAmount = Mathf.Clamp01((vm.LaserRemainingMs ?? 0f) / laserMaxDurationMs);
        }
    }
}

using UnityEngine;

namespace Arkanoid.Presentation.View
{
    // InGame — Renderer 들의 컨테이너이자 HUD/Slider 호스트.
    // 이 패널은 단순 SetActive 토글 + Bind 라우팅.
    public sealed class InGamePanel : MonoBehaviour
    {
        [SerializeField] private HudView hudView;
        [SerializeField] private SliderView sliderView;

        public void Bind(HudViewModel vm)
        {
            if (hudView != null) hudView.Bind(vm);
            if (sliderView != null) sliderView.Bind(vm);
        }
    }
}

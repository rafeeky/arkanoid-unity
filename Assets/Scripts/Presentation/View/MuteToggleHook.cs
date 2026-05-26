using TMPro;
using UnityEngine;
using Arkanoid.Presentation;

namespace Arkanoid.Presentation.View
{
    // MuteButton 의 OnClick 핸들러. BGM + SFX 같이 토글.
    public sealed class MuteToggleHook : MonoBehaviour
    {
        [SerializeField] private UnityAudio audioRef;
        [SerializeField] private TMP_Text label;

        private bool _muted;

        public void Toggle()
        {
            if (audioRef == null) return;
            _muted = !_muted;
            audioRef.SetBgmMuted(_muted);
            audioRef.SetSfxMuted(_muted);
            if (label != null) label.text = _muted ? "UNMUTE" : "MUTE";
        }
    }
}

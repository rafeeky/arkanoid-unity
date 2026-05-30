using UnityEngine;
using Arkanoid.Flow;

namespace Arkanoid.Presentation.View
{
    // FlowStateKind 에 따라 Panel SetActive 토글.
    // GameManager Update 의 4단계 후 호출. Pause 는 별도 (FlowState.Pause 면 InGame 도 표시 + PauseOverlay 위에).
    public sealed class ScreenRouter : MonoBehaviour
    {
        [SerializeField] private TitlePanel titlePanel;
        [SerializeField] private IntroStoryPanel introStoryPanel;
        [SerializeField] private RoundIntroPanel roundIntroPanel;
        [SerializeField] private InGamePanel inGamePanel;
        [SerializeField] private GameOverPanel gameOverPanel;
        [SerializeField] private GameClearPanel gameClearPanel;
        [SerializeField] private PauseOverlay pauseOverlay;
        [SerializeField] private GameObject pauseButton;     // 우상단 일시정지 버튼 (InGame/RoundIntro 에서만 보임)
        [SerializeField] private GameObject playfieldBlackFill; // 라운드(InGame/RoundIntro) 검은 플레이필드

        public void Apply(FlowStateKind kind)
        {
            SafeSetActive(titlePanel, kind == FlowStateKind.Title);
            SafeSetActive(introStoryPanel, kind == FlowStateKind.IntroStory);
            SafeSetActive(roundIntroPanel, kind == FlowStateKind.RoundIntro);
            bool inRound = kind == FlowStateKind.InGame || kind == FlowStateKind.RoundIntro;
            // TS SceneRenderer.render 와 동일 — RoundIntro 동안에도 InGame 뷰 (HUD/Bar/Ball/Blocks) 표시.
            SafeSetActive(inGamePanel, inRound);
            SafeSetActive(gameOverPanel, kind == FlowStateKind.GameOver);
            SafeSetActive(gameClearPanel, kind == FlowStateKind.GameClear);
            // PauseOverlay 는 Pause flow 상태가 별도 enum 으로 생긴 뒤 활성화.
            SafeSetActive(pauseOverlay, false);
            // 라운드 한정 표시
            if (pauseButton != null) pauseButton.SetActive(inRound);
            if (playfieldBlackFill != null) playfieldBlackFill.SetActive(inRound);
        }

        private static void SafeSetActive(MonoBehaviour mb, bool active)
        {
            if (mb != null) mb.gameObject.SetActive(active);
        }
    }
}

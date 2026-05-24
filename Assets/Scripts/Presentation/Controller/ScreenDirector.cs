using System;
using Arkanoid.Flow;

namespace Arkanoid.Presentation
{
    // ScreenState 소유 + 시간 기반 갱신. flowState.kind 변화에 따라 currentScreen 동기화,
    // RoundIntro 타이머 감소, VisualEffectController.Update 호출.
    public sealed class ScreenDirector
    {
        private readonly ScreenState _screenState;
        private readonly float _roundIntroDurationMs;
        private FlowStateKind? _prevFlowKind = null;
        private readonly VisualEffectController _visualEffectController;

        public ScreenDirector(float roundIntroDurationMs, VisualEffectController visualEffectController)
        {
            _roundIntroDurationMs = roundIntroDurationMs;
            _visualEffectController = visualEffectController;
            _screenState = ScreenState.CreateInitial(roundIntroDurationMs);
        }

        public void Update(GameFlowState flowState, float deltaMs, Action<PresentationEvent> emitPresentationEvent)
        {
            var kind = flowState.Kind;

            // introStory 진입 첫 프레임 → Intro 시퀀스 시작.
            if (kind == FlowStateKind.IntroStory && _prevFlowKind != FlowStateKind.IntroStory)
                _visualEffectController.StartIntroSequence();

            // RoundIntro 진입 → 타이머 리셋.
            if (kind == FlowStateKind.RoundIntro && _prevFlowKind != FlowStateKind.RoundIntro)
                _screenState.RoundIntroRemainingTime = _roundIntroDurationMs;

            // currentScreen 동기화.
            _screenState.CurrentScreen = kind;

            // RoundIntro 중 타이머 감소.
            if (kind == FlowStateKind.RoundIntro)
            {
                var next = _screenState.RoundIntroRemainingTime - deltaMs;
                _screenState.RoundIntroRemainingTime = next < 0f ? 0f : next;
            }

            // VisualEffectController 갱신 → ScreenState 반영.
            _visualEffectController.Update(deltaMs, emitPresentationEvent);

            _screenState.BlockHitFlashBlockIds = _visualEffectController.GetFlashingBlockIds();
            _screenState.IsBarBreaking = _visualEffectController.IsBarBreaking();
            _screenState.IntroPageIndex = _visualEffectController.GetIntroPageIndex();
            _screenState.IntroTypingProgress = _visualEffectController.GetIntroTypingProgress();
            _screenState.IntroPhase = _visualEffectController.GetIntroPhase();

            _prevFlowKind = kind;
        }

        public ScreenState GetScreenState() => _screenState;
        public VisualEffectController GetVisualEffectController() => _visualEffectController;
    }
}

using System;
using System.Collections.Generic;
using Arkanoid.Definitions;
using Arkanoid.Flow;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    // 시간 기반 시각 연출 타이머. 순수 C# 클래스 (MonoBehaviour 아님).
    // ScreenDirector.Update 가 매 틱 Update 호출.
    public sealed class VisualEffectController
    {
        private readonly GameplayConfig _config;
        private readonly IReadOnlyList<IntroSequenceEntry> _introPages;

        // blockId → 남은 플래시 시간 (ms).
        private readonly Dictionary<string, float> _flashingBlocks = new();
        private float _barBreakRemainingMs;

        // Intro 시퀀스 내부 상태.
        private bool _introActive;
        private int _introPageIndex;
        private IntroPhase _introPhase = IntroPhase.Typing;
        private float _introPhaseElapsedMs;
        private bool _introFinishedEmitted;

        public VisualEffectController(GameplayConfig config, IReadOnlyList<IntroSequenceEntry> introPages)
        {
            _config = config;
            _introPages = introPages;
        }

        // ─── Intro 시퀀스 ───

        public void StartIntroSequence()
        {
            _introActive = true;
            _introPageIndex = 0;
            _introPhase = IntroPhase.Typing;
            _introPhaseElapsedMs = 0f;
            _introFinishedEmitted = false;
        }

        public int GetIntroPageIndex() => _introPageIndex;

        public float GetIntroTypingProgress()
        {
            if (!_introActive) return 0f;
            if (_introPageIndex < 0 || _introPageIndex >= _introPages.Count) return 0f;
            var page = _introPages[_introPageIndex];
            return _introPhase switch
            {
                IntroPhase.Typing => page.Text.Length * page.TypingSpeedMs == 0f
                    ? 1f
                    : Math.Min(1f, _introPhaseElapsedMs / (page.Text.Length * page.TypingSpeedMs)),
                IntroPhase.Hold => 1f,
                IntroPhase.Erasing => page.Text.Length * page.EraseSpeedMs == 0f
                    ? 0f
                    : Math.Max(0f, 1f - _introPhaseElapsedMs / (page.Text.Length * page.EraseSpeedMs)),
                _ => 0f,
            };
        }

        public IntroPhase GetIntroPhase() => _introPhase;

        // dev 모드 사용자가 space 로 intro 스킵 시 호출. 이미 emit 됐으면 no-op.
        public void SkipIntroSequence(Action<PresentationEvent> emitPresentationEvent)
        {
            if (!_introActive) return;
            if (_introFinishedEmitted) return;
            _introPhase = IntroPhase.Done;
            _introPhaseElapsedMs = 0f;
            _introFinishedEmitted = true;
            emitPresentationEvent(new IntroSequenceFinishedEvent());
        }

        // ─── Gameplay 이벤트 핸들러 ───

        // BlockHit → 플래시 타이머. BlockDestroyed → 제거. LifeLost → 바 파괴 타이머.
        public void HandleGameplayEvent(GameplayEvent ev)
        {
            switch (ev)
            {
                case BlockHitEvent bh:
                    _flashingBlocks[bh.BlockId] = _config.BlockHitFlashDurationMs;
                    break;
                case BlockDestroyedEvent bd:
                    _flashingBlocks.Remove(bd.BlockId);
                    break;
                case LifeLostEvent:
                    _barBreakRemainingMs = _config.BarBreakDurationMs;
                    break;
            }
        }

        // ─── Update ───

        public void Update(float deltaMs, Action<PresentationEvent> emitPresentationEvent)
        {
            // 블록 플래시 타이머 감소.
            var toRemove = new List<string>();
            var keys = new List<string>(_flashingBlocks.Keys);
            foreach (var id in keys)
            {
                var next = _flashingBlocks[id] - deltaMs;
                if (next <= 0f) toRemove.Add(id);
                else _flashingBlocks[id] = next;
            }
            foreach (var id in toRemove) _flashingBlocks.Remove(id);

            // 바 파괴 타이머 + 경계.
            if (_barBreakRemainingMs > 0f)
            {
                _barBreakRemainingMs -= deltaMs;
                if (_barBreakRemainingMs <= 0f)
                {
                    _barBreakRemainingMs = 0f;
                    emitPresentationEvent(new LifeLostPresentationFinishedEvent());
                }
            }

            // Intro 시퀀스 진행.
            if (_introActive && _introPhase != IntroPhase.Done)
                TickIntro(deltaMs, emitPresentationEvent);
        }

        private void TickIntro(float deltaMs, Action<PresentationEvent> emitPresentationEvent)
        {
            if (_introPageIndex < 0 || _introPageIndex >= _introPages.Count)
            {
                _introPhase = IntroPhase.Done;
                return;
            }
            var page = _introPages[_introPageIndex];
            _introPhaseElapsedMs += deltaMs;

            switch (_introPhase)
            {
                case IntroPhase.Typing:
                    {
                        var typingDur = page.Text.Length * page.TypingSpeedMs;
                        if (_introPhaseElapsedMs >= typingDur)
                        {
                            _introPhase = IntroPhase.Hold;
                            _introPhaseElapsedMs = 0f;
                        }
                        break;
                    }
                case IntroPhase.Hold:
                    if (_introPhaseElapsedMs >= page.HoldDurationMs)
                    {
                        // Erasing skip — 즉시 다음 페이지 typing 또는 done.
                        if (_introPageIndex < _introPages.Count - 1)
                        {
                            _introPageIndex++;
                            _introPhase = IntroPhase.Typing;
                            _introPhaseElapsedMs = 0f;
                        }
                        else
                        {
                            _introPhase = IntroPhase.Done;
                            _introPhaseElapsedMs = 0f;
                            if (!_introFinishedEmitted)
                            {
                                _introFinishedEmitted = true;
                                emitPresentationEvent(new IntroSequenceFinishedEvent());
                            }
                        }
                    }
                    break;
                case IntroPhase.Erasing:
                    // 현재는 hold → 다음 페이지 직접 전이. erasing 미사용 (호환 case 만).
                    break;
                case IntroPhase.Done:
                    break;
            }
        }

        // ─── Getters ───

        public IReadOnlyList<string> GetFlashingBlockIds()
        {
            var list = new List<string>(_flashingBlocks.Count);
            foreach (var id in _flashingBlocks.Keys) list.Add(id);
            return list;
        }

        public bool IsBarBreaking() => _barBreakRemainingMs > 0f;

        // 0.0(완료) ~ 1.0(시작). renderer 의 opacity/scale 애니용. duration=0 면 0.
        public float GetBarBreakProgress()
        {
            if (_config.BarBreakDurationMs == 0f) return 0f;
            var ratio = _barBreakRemainingMs / _config.BarBreakDurationMs;
            return Math.Max(0f, Math.Min(1f, ratio));
        }
    }
}

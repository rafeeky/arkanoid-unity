using System;
using System.Collections.Generic;
using Arkanoid.Flow;

namespace Arkanoid.Presentation
{
    public enum IntroPhase { Typing, Hold, Erasing, Done }

    // ScreenDirector 가 소유 + 갱신. mutable class.
    public sealed class ScreenState
    {
        public FlowStateKind CurrentScreen { get; set; }
        public float RoundIntroRemainingTime { get; set; }
        public IReadOnlyList<string> BlockHitFlashBlockIds { get; set; } = Array.Empty<string>();
        public bool IsBarBreaking { get; set; }
        // 현재 재생 중인 intro 페이지 인덱스 (0..N-1).
        public int IntroPageIndex { get; set; }
        // 현재 페이지 내부 표시 진행률 0~1.
        public float IntroTypingProgress { get; set; }
        public IntroPhase IntroPhase { get; set; }

        public static ScreenState CreateInitial(float roundIntroDurationMs) =>
            new()
            {
                CurrentScreen = FlowStateKind.Title,
                RoundIntroRemainingTime = roundIntroDurationMs,
                BlockHitFlashBlockIds = Array.Empty<string>(),
                IsBarBreaking = false,
                IntroPageIndex = 0,
                IntroTypingProgress = 0f,
                IntroPhase = IntroPhase.Typing,
            };
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;
using Arkanoid.Flow;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation
{
    // AppContext + GameManager 합쳐서 — 모든 service wire + tick loop.
    // Inspector 에서 SO 들을 [SerializeField] 로 받음 (D2.2 Inspector 직접 참조).
    // Renderer / Panel 와이어링은 추후 추가.
    public sealed class GameManager : MonoBehaviour
    {
        // ─── Inspector wired SO ───

        [Header("Gameplay SO")]
        [SerializeField] private GameplayConfigSO gameplayConfigSO;
        [SerializeField] private DifficultyConfigSO normalDifficultySO;
        [SerializeField] private DifficultyConfigSO hardDifficultySO;
        [SerializeField] private List<BlockDefinitionSO> blockDefinitionSOs = new();
        [SerializeField] private ItemDefinitionSO expandItemSO;
        [SerializeField] private ItemDefinitionSO magnetItemSO;
        [SerializeField] private ItemDefinitionSO laserItemSO;
        [SerializeField] private List<SpinnerDefinitionSO> spinnerDefinitionSOs = new();
        [SerializeField] private List<StageDefinitionSO> stageDefinitionSOs = new();

        [Header("Presentation SO")]
        [SerializeField] private UITextSO uiTextSO;
        [SerializeField] private IntroSequenceSO introSequenceSO;
        [SerializeField] private AudioCueSO audioCueSO;
        [SerializeField] private LayoutConfigSO layoutConfigSO;

        [Header("Camera + UI roots")]
        [SerializeField] private Camera mainCamera;

        // ─── Runtime services ───

        private GameplayConfig _config;
        private GameplayController _gameplayController;
        private GameFlowController _gameFlowController;
        private HUDPresenter _hudPresenter;
        private ScreenPresenter _screenPresenter;
        private ScreenDirector _screenDirector;
        private VisualEffectController _visualEffectController;
        private AudioCueResolver _audioCueResolver;
        private IArkanoidAudio _audio;
        private UnityInputSnapshotBuilder _inputBuilder;
        private PointerToPlayfield _pointerToPlayfield;

        // 이벤트 router — listener 들에게 분배.
        private readonly List<Action<GameplayEvent>> _gameplayListeners = new();
        private readonly List<Action<FlowEvent>> _flowListeners = new();
        private readonly List<Action<PresentationEvent>> _presentationListeners = new();

        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _config = gameplayConfigSO.Data;

            // 정의 테이블 dictionary 변환.
            var blockDefs = new Dictionary<string, BlockDefinition>();
            foreach (var so in blockDefinitionSOs)
            {
                if (so == null) continue;
                var d = so.Data;
                blockDefs[d.DefinitionId] = d;
            }

            var itemDefs = new Dictionary<ItemType, ItemDefinition>();
            if (expandItemSO != null) itemDefs[ItemType.Expand] = expandItemSO.Data;
            if (magnetItemSO != null) itemDefs[ItemType.Magnet] = magnetItemSO.Data;
            if (laserItemSO != null) itemDefs[ItemType.Laser] = laserItemSO.Data;

            var spinnerDefs = new Dictionary<string, SpinnerDefinition>();
            foreach (var so in spinnerDefinitionSOs)
            {
                if (so == null) continue;
                var d = so.Data;
                spinnerDefs[d.DefinitionId] = d;
            }

            // Audio + UI text.
            _audioCueResolver = new AudioCueResolver(audioCueSO != null ? audioCueSO.Data : new List<AudioCueEntry>());
            _audio = new NoopAudio();  // Phase 3 단계 — UnityAudio 구현 시 교체.

            // Initial gameplay state — stage 0.
            GameplayRuntimeState initialState;
            if (stageDefinitionSOs.Count > 0 && stageDefinitionSOs[0] != null)
            {
                initialState = StageRuntimeFactory.CreateGameplayRuntimeFromStageDefinition(
                    stageDefinitionSOs[0].Data,
                    _config,
                    blockDefs,
                    _config.InitialLives,
                    normalDifficultySO != null ? normalDifficultySO.Data : (DifficultyConfig?)null);
            }
            else
            {
                initialState = new GameplayRuntimeState();
            }

            var deps = new GameplayController.Dependencies(blockDefs, itemDefs, _config, spinnerDefs);
            _gameplayController = new GameplayController(initialState, deps);
            _gameFlowController = new GameFlowController(OnFlowEvent, stageDefinitionSOs.Count);
            _hudPresenter = new HUDPresenter();
            _screenPresenter = new ScreenPresenter();
            _visualEffectController = new VisualEffectController(
                _config,
                introSequenceSO != null ? introSequenceSO.Data : new List<IntroSequenceEntry>());
            _screenDirector = new ScreenDirector(_config.RoundIntroDurationMs, _visualEffectController);

            _inputBuilder = new UnityInputSnapshotBuilder();
            _pointerToPlayfield = mainCamera != null ? new PointerToPlayfield(mainCamera) : null;
        }

        private void Update()
        {
            if (!_initialized) return;

            var dt = Time.deltaTime;
            var dtMs = dt * 1000f;

            // 1. 입력 build.
            var pointerX = _pointerToPlayfield?.GetPlayfieldX();
            var input = _inputBuilder.Build(pointerX);

            // 2. Flow 입력 처리 (비인게임 상태).
            _gameFlowController.HandleInput(input);

            // 3. InGame 일 때만 Gameplay tick.
            if (_gameFlowController.GetState().Kind == FlowStateKind.InGame)
            {
                var events = _gameplayController.Tick(input, dt);
                foreach (var e in events)
                {
                    _visualEffectController.HandleGameplayEvent(e);
                    _gameFlowController.HandleGameplayEvent(e);
                    foreach (var listener in _gameplayListeners) listener(e);
                }
            }

            // 4. ScreenDirector update — VisualEffectController + PresentationEvent.
            _screenDirector.Update(_gameFlowController.GetState(), dtMs, OnPresentationEvent);
        }

        // ─── Event routing ───

        private void OnFlowEvent(FlowEvent e)
        {
            foreach (var l in _flowListeners) l(e);
        }

        private void OnPresentationEvent(PresentationEvent e)
        {
            _gameFlowController.HandlePresentationEvent(e);
            foreach (var l in _presentationListeners) l(e);
        }

        // ─── External subscriber API (Renderer/Panel 이 구독) ───

        public void SubscribeGameplay(Action<GameplayEvent> listener) => _gameplayListeners.Add(listener);
        public void SubscribeFlow(Action<FlowEvent> listener) => _flowListeners.Add(listener);
        public void SubscribePresentation(Action<PresentationEvent> listener) => _presentationListeners.Add(listener);

        // ─── State 조회 (Renderer/Panel 이 매 프레임 폴링) ───

        public GameplayRuntimeState GetGameplayState() => _gameplayController?.GetState();
        public GameFlowState GetFlowState() => _gameFlowController?.GetState();
        public ScreenState GetScreenState() => _screenDirector?.GetScreenState();
        public HUDPresenter HudPresenter => _hudPresenter;
        public ScreenPresenter ScreenPresenter => _screenPresenter;
        public IArkanoidAudio Audio => _audio;
    }
}

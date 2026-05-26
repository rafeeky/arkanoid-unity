using System;
using System.Collections.Generic;
using UnityEngine;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;
using Arkanoid.Flow;
using Arkanoid.Gameplay;
using Arkanoid.Presentation.View;

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

        [Header("View — Gameplay 렌더러 (Bind 매 프레임)")]
        [SerializeField] private BarRenderer barRenderer;
        [SerializeField] private BallsRenderer ballsRenderer;
        [SerializeField] private BlocksRenderer blocksRenderer;
        [SerializeField] private BordersRenderer bordersRenderer;
        [SerializeField] private DoorsRenderer doorsRenderer;
        [SerializeField] private SpinnersRenderer spinnersRenderer;
        [SerializeField] private ItemsRenderer itemsRenderer;
        [SerializeField] private LaserShotsRenderer laserShotsRenderer;
        [SerializeField] private BallTrailRenderer ballTrailRenderer;
        [SerializeField] private MascotRenderer mascotRenderer;

        [Header("View — Panel + Router")]
        [SerializeField] private ScreenRouter screenRouter;
        [SerializeField] private TitlePanel titlePanel;
        [SerializeField] private IntroStoryPanel introStoryPanel;
        [SerializeField] private RoundIntroPanel roundIntroPanel;
        [SerializeField] private InGamePanel inGamePanel;
        [SerializeField] private GameOverPanel gameOverPanel;
        [SerializeField] private GameClearPanel gameClearPanel;
        [SerializeField] private PauseOverlay pauseOverlay;
        [SerializeField] private ToastView toastView;

        [Header("Audio — UnityAudio 컴포넌트 있으면 NoopAudio 대신 사용")]
        [SerializeField] private UnityAudio unityAudio;

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

        // SaveData (PlayerPrefs 기반)
        private ISaveRepository _saveRepo;
        private SaveData _save;

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
            _audio = unityAudio != null ? (IArkanoidAudio)unityAudio : new NoopAudio();

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

            // SaveData 로드 → 초기 session.HighScore 에 반영.
            _saveRepo = new PlayerPrefsSaveRepository();
            _save = _saveRepo.Load();
            initialState.Session = initialState.Session with { HighScore = _save.HighScore };

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
            _pointerToPlayfield = mainCamera != null ? new PointerToPlayfield(mainCamera, layoutConfigSO) : null;
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
                    RouteGameplayAudio(e);   // TS FlowEventRouter.routeGameplayAudio
                    foreach (var listener in _gameplayListeners) listener(e);
                }
            }

            // 4. ScreenDirector update — VisualEffectController + PresentationEvent.
            _screenDirector.Update(_gameFlowController.GetState(), dtMs, OnPresentationEvent);

            // 5. View bind — Renderer/Panel 매 프레임 동기화.
            BindViews();

            // 6. HighScore 갱신 시 PlayerPrefs save.
            TrySaveHighScore();
        }

        private void TrySaveHighScore()
        {
            if (_saveRepo == null) return;
            var session = _gameplayController.GetState().Session;
            if (session.HighScore > _save.HighScore)
            {
                _save = _save with { HighScore = session.HighScore };
                _saveRepo.Save(_save);
            }
        }

        private void BindViews()
        {
            var flowState = _gameFlowController.GetState();
            var screenState = _screenDirector.GetScreenState();
            var gameplay = _gameplayController.GetState();
            var uiTexts = uiTextSO != null ? uiTextSO.Data : new List<UITextEntry>();

            if (screenRouter != null) screenRouter.Apply(flowState.Kind);

            // Gameplay 렌더러 — InGame/RoundIntro 일 때 표시.
            var showGameplay = flowState.Kind == FlowStateKind.InGame
                            || flowState.Kind == FlowStateKind.RoundIntro;
            if (showGameplay)
            {
                if (barRenderer != null) barRenderer.Bind(gameplay.Bar);
                if (ballsRenderer != null) ballsRenderer.Bind(gameplay.Balls);
                if (blocksRenderer != null) blocksRenderer.Bind(gameplay.Blocks, screenState.BlockHitFlashBlockIds);
                if (bordersRenderer != null) bordersRenderer.Bind(gameplay.Borders);
                if (doorsRenderer != null) doorsRenderer.Bind(gameplay.Doors);
                if (spinnersRenderer != null) spinnersRenderer.Bind(gameplay.SpinnerStates);
                if (itemsRenderer != null) itemsRenderer.Bind(gameplay.ItemDrops);
                if (laserShotsRenderer != null) laserShotsRenderer.Bind(gameplay.LaserShots);
                if (ballTrailRenderer != null) ballTrailRenderer.Bind(gameplay.Balls, gameplay.CurrentTrailStyle);
                // Mascot 캐릭터 swap — PlayerPrefs 의 selectedMascot 또는 기본값 albatross.
                // TS renderInGameScreen 의 cheerMascotFromState(selectedMascotId) 와 동등.
                if (mascotRenderer != null)
                {
                    var mascotId = UnityEngine.PlayerPrefs.GetString("arkanoid.selectedMascot", "albatross");
                    if (string.IsNullOrEmpty(mascotId)) mascotId = "albatross";
                    mascotRenderer.SetMascot(mascotId);
                }
            }

            // Panel binds.
            switch (flowState.Kind)
            {
                case FlowStateKind.Title:
                    if (titlePanel != null)
                        titlePanel.Bind(_screenPresenter.BuildTitleViewModel(gameplay.Session, uiTexts, flowState.SelectedDifficulty));
                    break;
                case FlowStateKind.IntroStory:
                    if (introStoryPanel != null && introSequenceSO != null)
                        introStoryPanel.Bind(_screenPresenter.BuildIntroScreenViewModel(
                            screenState.IntroPageIndex, screenState.IntroTypingProgress,
                            screenState.IntroPhase, introSequenceSO.Data));
                    break;
                case FlowStateKind.RoundIntro:
                    if (roundIntroPanel != null)
                        roundIntroPanel.Bind(_screenPresenter.BuildRoundIntroViewModel(
                            gameplay.Session, uiTexts, screenState.RoundIntroRemainingTime, _config.RoundIntroDurationMs));
                    // TS 와 동일 — RoundIntro 동안 HUD 도 표시.
                    if (inGamePanel != null)
                        inGamePanel.Bind(_hudPresenter.BuildHudViewModel(gameplay));
                    break;
                case FlowStateKind.InGame:
                    if (inGamePanel != null)
                        inGamePanel.Bind(_hudPresenter.BuildHudViewModel(gameplay));
                    break;
                case FlowStateKind.GameOver:
                    if (gameOverPanel != null)
                        gameOverPanel.Bind(_screenPresenter.BuildGameOverViewModel(gameplay.Session, uiTexts));
                    break;
                case FlowStateKind.GameClear:
                    if (gameClearPanel != null)
                        gameClearPanel.Bind(_screenPresenter.BuildGameClearViewModel(gameplay.Session, uiTexts));
                    break;
            }
        }

        // ─── Event routing ───

        private void OnFlowEvent(FlowEvent e)
        {
            RouteFlowAudio(e);                  // TS FlowEventRouter.routeFlowAudio
            HandleEnteredResultGoldSave(e);     // TS FlowEventRouter.handleEnteredResult
            foreach (var l in _flowListeners) l(e);
        }

        // ─── Audio routing (TS FlowEventRouter 동치) ───

        private static string EventTypeKey(object e)
        {
            var name = e.GetType().Name;
            return name.EndsWith("Event") ? name.Substring(0, name.Length - 5) : name;
        }

        private void RouteGameplayAudio(GameplayEvent e)
        {
            if (_audioCueResolver == null || _audio == null) return;
            var cues = _audioCueResolver.Resolve(EventTypeKey(e));
            foreach (var cue in cues) _audio.Play(cue);
        }

        private void RouteFlowAudio(FlowEvent e)
        {
            if (_audioCueResolver == null || _audio == null) return;

            // Phase 2: InGame 진입 시 RoundIntro 짧은 jingle 정지.
            if (e is EnteredInGameEvent) _audio.Stop("cue_round_intro_jingle");

            if (e is EnteredRoundIntroEvent && e.From == FlowStateKind.IntroStory)
            {
                foreach (var c in _audioCueResolver.Resolve("EnteredRoundIntro")) _audio.Play(c);
                foreach (var c in _audioCueResolver.Resolve("UiConfirm")) _audio.Play(c);
                return;
            }
            if (e is EnteredTitleEvent && e.From != FlowStateKind.Title)
            {
                foreach (var c in _audioCueResolver.Resolve("UiConfirm")) _audio.Play(c);
                foreach (var c in _audioCueResolver.Resolve("EnteredTitle")) _audio.Play(c);
                return;
            }

            foreach (var c in _audioCueResolver.Resolve(EventTypeKey(e))) _audio.Play(c);
        }

        // ─── Gold 적립 + HighScore save (TS FlowEventRouter.handleEnteredResult) ───

        private void HandleEnteredResultGoldSave(FlowEvent e)
        {
            if (!(e is EnteredGameOverEvent || e is EnteredGameClearEvent)) return;
            if (_saveRepo == null || _gameplayController == null) return;

            var session = _gameplayController.GetState().Session;
            var newHighScore = System.Math.Max(_save.HighScore, session.Score);
            _save = _save with {
                HighScore = newHighScore,
                Gold = _save.Gold + session.Score,   // TS addGoldFromScore: 1:1 변환
            };
            _saveRepo.Save(_save);

            // PlayerPrefs key 도 동기화 (TitlePanel 이 직접 PlayerPrefs 읽으므로)
            UnityEngine.PlayerPrefs.SetInt("arkanoid.gold", _save.Gold);
            UnityEngine.PlayerPrefs.Save();
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

        // Title NORMAL/HARD 버튼 onClick → 난이도 + 게임 시작.
        public void RequestStartGame(DifficultyKind difficulty) =>
            _gameFlowController?.RequestStartGame(difficulty);
    }
}

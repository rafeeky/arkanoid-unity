using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Arkanoid.Definitions;
using Arkanoid.Presentation;

namespace Arkanoid.Presentation.View
{
    // Title 화면.
    // TS renderTitleScreen.ts 동작 매칭:
    // - A/D 키 cycling — Input System (Keyboard.current) 으로. legacy Input.GetKeyDown 사용 시 침묵 실패.
    // - selected portrait 1장만 visible (carousel) — TS 는 `portrait2.<id>` 텍스처 1개 swap.
    // - U 키 or UnlockButton 클릭 → 잠금 해제 (PlayerPrefs gold 차감).
    // - NORMAL/HARD 버튼 onClick → GameManager.RequestStartGame.
    public sealed class TitlePanel : MonoBehaviour
    {
        [Header("Title 로고 / 텍스트")]
        [SerializeField] private TMP_Text titleText;          // "ALBATROSS" 큰 로고 (y=260)
        [SerializeField] private TMP_Text startText;           // (TS 미사용 — 보존)
        [SerializeField] private TMP_Text highScoreText;       // y=1137
        [SerializeField] private TMP_Text difficultyText;      // (TS 미사용 — 보존)

        [Header("Mascot Portrait (4 frame anim) — 작은 한 마리 미리보기")]
        [SerializeField] private Image mascotImage;
        [SerializeField] private Image mascotPortraitFrame;     // 흰 4px stroke 정사각형
        [SerializeField] private GameObject prevArrow;          // "<" 좌측 (cursor 첫 위치면 dim)
        [SerializeField] private GameObject nextArrow;          // ">" 우측 (cursor 마지막이면 dim)
        [SerializeField] private TMP_Text prevArrowText;        // dim 색 처리용
        [SerializeField] private TMP_Text nextArrowText;
        [SerializeField] private Sprite[] mascotFrames = new Sprite[4];
        [SerializeField] private float frameIntervalSec = 0.2f;

        [Header("Mascot 선택 / 잠금 해제")]
        [SerializeField] private MascotPortrait[] portraits;
        [SerializeField] private TMP_Text mascotNameText;
        [SerializeField] private TMP_Text mascotCostText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Button unlockButton;

        [Header("NORMAL / HARD 버튼 — onClick 으로 게임 시작")]
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private GameManager gameManager;

        [Header("POWERUPS 영역 (TS renderTitleScreen.ts POWERUPS — 3 아이템)")]
        [SerializeField] private TMP_Text powerupsTitle;        // "POWERUPS" y=1250
        [SerializeField] private PowerupSlot[] powerupSlots;    // 3개: EXPAND/MAGNET/LASER

        [Header("Info Panels (반투명 배경 카드)")]
        [SerializeField] private Image mascotInfoPanel;         // cy=985 w=620 h=160
        [SerializeField] private Image powerupsInfoPanel;       // cy=1340 w=1010 h=260

        [System.Serializable]
        public struct PowerupSlot
        {
            public Image IconBlock;        // 블록 모양 배경 (block_*_drop)
            public Image IconOverlay;      // 흰 아이콘 (icon_expand 등)
            public TMP_Text Name;          // EXPAND/MAGNET/LASER
            public TMP_Text Description;   // 설명문
        }

        [System.Serializable]
        public struct MascotPortrait
        {
            public string MascotId;
            public Image Portrait;
            public int UnlockCost;
            public string DisplayName;
            public Sprite[] AnimFrames;
        }

        private float _accumTime;
        private int _frameIdx;
        private int _selectedIdx;

        private const string KeyGold = "arkanoid.gold";
        private const string KeyUnlocked = "arkanoid.unlockedMascots";
        private const string KeySelected = "arkanoid.selectedMascot";

        private void OnEnable()
        {
            SyncSelectedIndexFromSave();
            RefreshAll();
            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveListener(TryUnlockSelected);
                unlockButton.onClick.AddListener(TryUnlockSelected);
            }
            if (normalButton != null)
            {
                normalButton.onClick.RemoveListener(OnNormalClicked);
                normalButton.onClick.AddListener(OnNormalClicked);
            }
            if (hardButton != null)
            {
                hardButton.onClick.RemoveListener(OnHardClicked);
                hardButton.onClick.AddListener(OnHardClicked);
            }
            WireArrow(prevArrow, OnPrevArrowClicked);
            WireArrow(nextArrow, OnNextArrowClicked);
        }

        private static void WireArrow(GameObject arrowGo, UnityEngine.Events.UnityAction onClick)
        {
            if (arrowGo == null) return;
            var btn = arrowGo.GetComponent<Button>();
            if (btn == null) btn = arrowGo.AddComponent<Button>();
            btn.onClick.RemoveListener(onClick);
            btn.onClick.AddListener(onClick);
        }

        private void OnPrevArrowClicked()
        {
            if (portraits == null || portraits.Length == 0) return;
            _selectedIdx = (_selectedIdx - 1 + portraits.Length) % portraits.Length;
            OnSelectionChanged();
        }

        private void OnNextArrowClicked()
        {
            if (portraits == null || portraits.Length == 0) return;
            _selectedIdx = (_selectedIdx + 1) % portraits.Length;
            OnSelectionChanged();
        }

        public void Bind(TitleScreenViewModel vm)
        {
            // TS renderTitleScreen.ts: startText 는 사용 안 함 (NORMAL/HARD 버튼이 시작).
            // 기존 호환 위해 텍스트는 채워두지만 GameObject 가시성은 변경 안 함.
            if (startText != null) startText.text = vm.StartText;
            if (titleText != null && string.IsNullOrEmpty(titleText.text)) titleText.text = "ALBATROSS";
            if (highScoreText != null) highScoreText.text = $"HIGH SCORE  {vm.HighScore}";
            if (difficultyText != null)
                difficultyText.text = vm.SelectedDifficulty == DifficultyKind.Hard ? "[HARD]" : "[NORMAL]";
        }

        private void Update()
        {
            // 4 frame 미리보기 애니 (선택 mascot 만).
            if (mascotImage != null && mascotFrames != null && mascotFrames.Length > 0)
            {
                _accumTime += Time.deltaTime;
                if (_accumTime >= frameIntervalSec)
                {
                    _accumTime = 0f;
                    _frameIdx = (_frameIdx + 1) % mascotFrames.Length;
                    var f = mascotFrames[_frameIdx];
                    if (f != null)
                    {
                        mascotImage.sprite = f;
                        mascotImage.enabled = true;
                    }
                }
            }

            // A / D / U — Input System (legacy Input.GetKeyDown 은 Input System 전환 후 침묵 실패).
            var kb = Keyboard.current;
            if (kb == null || portraits == null || portraits.Length == 0) return;
            if (kb.aKey.wasPressedThisFrame)
            {
                _selectedIdx = (_selectedIdx - 1 + portraits.Length) % portraits.Length;
                OnSelectionChanged();
            }
            else if (kb.dKey.wasPressedThisFrame)
            {
                _selectedIdx = (_selectedIdx + 1) % portraits.Length;
                OnSelectionChanged();
            }
            else if (kb.uKey.wasPressedThisFrame)
            {
                TryUnlockSelected();
            }
        }

        private void SyncSelectedIndexFromSave()
        {
            if (portraits == null || portraits.Length == 0) return;
            var selectedId = PlayerPrefs.GetString(KeySelected, "albatross");
            for (int i = 0; i < portraits.Length; i++)
            {
                if (portraits[i].MascotId == selectedId)
                {
                    _selectedIdx = i;
                    return;
                }
            }
            _selectedIdx = 0;
        }

        private void OnSelectionChanged()
        {
            if (portraits == null || _selectedIdx < 0 || _selectedIdx >= portraits.Length) return;
            var p = portraits[_selectedIdx];

            if (IsUnlocked(p.MascotId))
            {
                PlayerPrefs.SetString(KeySelected, p.MascotId);
                PlayerPrefs.Save();
            }

            if (p.AnimFrames != null && p.AnimFrames.Length >= 4)
            {
                mascotFrames = p.AnimFrames;
                _frameIdx = 0;
                _accumTime = 0f;
                if (mascotImage != null && p.AnimFrames[0] != null)
                {
                    mascotImage.sprite = p.AnimFrames[0];
                    mascotImage.enabled = true;
                }
            }
            RefreshAll();
        }

        private void TryUnlockSelected()
        {
            if (portraits == null || _selectedIdx < 0 || _selectedIdx >= portraits.Length) return;
            var p = portraits[_selectedIdx];
            if (IsUnlocked(p.MascotId)) return;
            var gold = PlayerPrefs.GetInt(KeyGold, 0);
            if (gold < p.UnlockCost) return;

            PlayerPrefs.SetInt(KeyGold, gold - p.UnlockCost);
            var unlockedRaw = PlayerPrefs.GetString(KeyUnlocked, "albatross");
            if (string.IsNullOrEmpty(unlockedRaw)) unlockedRaw = "albatross";
            unlockedRaw = unlockedRaw + "," + p.MascotId;
            PlayerPrefs.SetString(KeyUnlocked, unlockedRaw);
            PlayerPrefs.SetString(KeySelected, p.MascotId);
            PlayerPrefs.Save();

            RefreshAll();
        }

        private bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id == "albatross") return true;
            var raw = PlayerPrefs.GetString(KeyUnlocked, "albatross");
            if (string.IsNullOrEmpty(raw)) return false;
            foreach (var s in raw.Split(','))
            {
                if (s == id) return true;
            }
            return false;
        }

        private void RefreshAll()
        {
            if (portraits == null) return;
            // TS 와 동일 — selected 1장만 보임. 나머지는 hidden.
            for (int i = 0; i < portraits.Length; i++)
            {
                var p = portraits[i];
                if (p.Portrait == null) continue;
                bool selected = i == _selectedIdx;
                p.Portrait.gameObject.SetActive(selected);

                if (selected)
                {
                    bool unlocked = IsUnlocked(p.MascotId);
                    // 잠금 시 dim (TS setTint 0x444444)
                    p.Portrait.color = unlocked ? Color.white : new Color(0.27f, 0.27f, 0.27f, 1f);
                    p.Portrait.rectTransform.localScale = Vector3.one;
                }
            }

            if (_selectedIdx >= 0 && _selectedIdx < portraits.Length)
            {
                var p = portraits[_selectedIdx];
                if (mascotNameText != null) mascotNameText.text = p.DisplayName;
                if (mascotCostText != null)
                    mascotCostText.text = IsUnlocked(p.MascotId)
                        ? "UNLOCKED"
                        : $"LOCKED  UNLOCK [U]: {p.UnlockCost}G";
                if (unlockButton != null) unlockButton.gameObject.SetActive(!IsUnlocked(p.MascotId));
            }
            if (goldText != null)
                goldText.text = $"GOLD: {PlayerPrefs.GetInt(KeyGold, 0)}";

            // TS renderTitleScreen — cursor 가 끝(첫/마지막) 에 있으면 그 방향 화살표만 dimmed.
            // 첫 albatross(0): ←dim →white | 마지막 seraphin: ←white →dim
            UpdateArrowColors();
        }

        private void UpdateArrowColors()
        {
            if (portraits == null || portraits.Length == 0) return;
            bool isFirst = _selectedIdx == 0;
            bool isLast = _selectedIdx == portraits.Length - 1;
            var dim = new Color(0.53f, 0.53f, 0.53f, 1f);  // #888888
            if (prevArrowText != null) prevArrowText.color = isFirst ? dim : Color.white;
            if (nextArrowText != null) nextArrowText.color = isLast ? dim : Color.white;
        }

        private void OnNormalClicked()
        {
            if (gameManager != null) gameManager.RequestStartGame(DifficultyKind.Normal);
        }

        private void OnHardClicked()
        {
            if (gameManager != null) gameManager.RequestStartGame(DifficultyKind.Hard);
        }
    }
}

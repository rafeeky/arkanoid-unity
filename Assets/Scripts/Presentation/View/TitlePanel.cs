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
        [SerializeField] private TMP_Text startText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text difficultyText;

        [Header("Mascot Portrait (4 frame anim) — 작은 한 마리 미리보기")]
        [SerializeField] private Image mascotImage;
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
        }

        public void Bind(TitleScreenViewModel vm)
        {
            if (startText != null) startText.text = vm.StartText;
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

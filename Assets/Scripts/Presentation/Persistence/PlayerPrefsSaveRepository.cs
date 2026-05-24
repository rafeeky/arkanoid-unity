using System.Collections.Generic;
using UnityEngine;

namespace Arkanoid.Presentation
{
    // PlayerPrefs 기반 SaveData 저장. TS LocalSaveRepository 의 Unity 대응체.
    // 각 field 별 PlayerPrefs key 사용. unlockedMascots 는 *콤마 구분 string* 으로 직렬화.
    public sealed class PlayerPrefsSaveRepository : ISaveRepository
    {
        private readonly string _keyHighScore;
        private readonly string _keyGold;
        private readonly string _keyUnlockedMascots;
        private readonly string _keySelectedMascot;

        public PlayerPrefsSaveRepository(string keyPrefix = "arkanoid")
        {
            _keyHighScore = $"{keyPrefix}.highScore";
            _keyGold = $"{keyPrefix}.gold";
            _keyUnlockedMascots = $"{keyPrefix}.unlockedMascots";
            _keySelectedMascot = $"{keyPrefix}.selectedMascot";
        }

        public SaveData Load()
        {
            // 처음 호출 시 (key 없음) → default. PlayerPrefs.GetInt(key, default) 가 자동 처리.
            var defaults = SaveData.CreateDefault();
            var highScore = PlayerPrefs.GetInt(_keyHighScore, defaults.HighScore);
            var gold = PlayerPrefs.GetInt(_keyGold, defaults.Gold);

            var unlockedRaw = PlayerPrefs.GetString(_keyUnlockedMascots, "");
            IReadOnlyList<string> unlocked = string.IsNullOrEmpty(unlockedRaw)
                ? defaults.UnlockedMascots
                : unlockedRaw.Split(',');

            var selectedMascot = PlayerPrefs.GetString(_keySelectedMascot, defaults.SelectedMascot);

            return new SaveData(highScore, gold, unlocked, selectedMascot);
        }

        public void Save(SaveData data)
        {
            PlayerPrefs.SetInt(_keyHighScore, data.HighScore);
            PlayerPrefs.SetInt(_keyGold, data.Gold);

            var unlockedJoined = string.Join(",", data.UnlockedMascots);
            PlayerPrefs.SetString(_keyUnlockedMascots, unlockedJoined);
            PlayerPrefs.SetString(_keySelectedMascot, data.SelectedMascot);
            PlayerPrefs.Save();
        }
    }
}

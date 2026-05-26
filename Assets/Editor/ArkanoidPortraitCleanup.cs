using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// Portrait 중복 GameObject cleanup.
    /// TitlePanel.portraits[].Portrait reference 가 wire 된 GameObject 만 남기고
    /// 동일 이름의 다른 GameObject 는 destroy.
    /// </summary>
    public static class ArkanoidPortraitCleanup
    {
        [MenuItem("Arkanoid/Cleanup/Portrait 중복 제거")]
        public static void CleanupDuplicatePortraits()
        {
            var tp = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (tp == null) { Debug.LogError("[Cleanup] TitlePanel not found"); return; }

            var so = new SerializedObject(tp);
            var portraits = so.FindProperty("portraits");
            var wired = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < portraits.arraySize; i++)
            {
                var p = portraits.GetArrayElementAtIndex(i);
                var img = p.FindPropertyRelative("Portrait").objectReferenceValue as Image;
                if (img != null) wired.Add(img.gameObject.GetInstanceID());
            }

            string[] mascotIds = { "albatross", "kongming", "snowrabbit", "reaper", "seraphin" };
            int destroyed = 0;
            foreach (var id in mascotIds)
            {
                var allWithName = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(g => g.name == $"Portrait_{id}" && !EditorUtility.IsPersistent(g))
                    .ToArray();
                foreach (var go in allWithName)
                {
                    if (wired.Contains(go.GetInstanceID())) continue;
                    Object.DestroyImmediate(go);
                    destroyed++;
                    Debug.Log($"[Cleanup] Destroyed duplicate Portrait_{id} (id={go.GetInstanceID()})");
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Cleanup] Total destroyed: {destroyed}");
        }

        // GoldText 중복 cleanup — TitlePanel.goldText wire 된 것만 남기고 다른 것 destroy
        [MenuItem("Arkanoid/Cleanup/GoldText 중복 제거")]
        public static void CleanupGoldText()
        {
            var tp = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (tp == null) { Debug.LogError("[Cleanup] TitlePanel not found"); return; }

            var so = new SerializedObject(tp);
            var wiredGoldText = so.FindProperty("goldText").objectReferenceValue as TMPro.TMP_Text;
            int wiredId = wiredGoldText != null ? wiredGoldText.gameObject.GetInstanceID() : 0;

            var all = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(g => g.name == "GoldText" && !EditorUtility.IsPersistent(g))
                .ToArray();
            int destroyed = 0;
            foreach (var go in all)
            {
                if (go.GetInstanceID() == wiredId) continue;
                Object.DestroyImmediate(go);
                destroyed++;
                Debug.Log($"[Cleanup] Destroyed duplicate GoldText (id={go.GetInstanceID()})");
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Cleanup] GoldText destroyed: {destroyed}");
        }

        // 전수 중복 audit — Canvas/TitlePanel/* 의 같은 이름 자식 dump
        [MenuItem("Arkanoid/Cleanup/Audit All Duplicates")]
        public static void AuditAllDuplicates()
        {
            string[] panels = { "TitlePanel", "IntroStoryPanel", "RoundIntroPanel", "InGamePanel",
                                "GameOverPanel", "GameClearPanel", "PauseOverlay" };
            foreach (var panelName in panels)
            {
                var panel = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(g => g.name == panelName && !EditorUtility.IsPersistent(g));
                if (panel == null) continue;
                var nameCount = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var t in panel.GetComponentsInChildren<Transform>(true))
                {
                    if (t == panel.transform) continue;
                    nameCount.TryGetValue(t.name, out var c);
                    nameCount[t.name] = c + 1;
                }
                foreach (var kv in nameCount.Where(kv => kv.Value > 1))
                    Debug.LogWarning($"[Dup] {panelName}/{kv.Key} × {kv.Value}");
            }
        }

        // 통합 cleanup
        [MenuItem("Arkanoid/Cleanup/Cleanup All Duplicates")]
        public static void CleanupAll()
        {
            CleanupDuplicatePortraits();
            CleanupGoldText();
            AuditAllDuplicates();
        }
    }
}

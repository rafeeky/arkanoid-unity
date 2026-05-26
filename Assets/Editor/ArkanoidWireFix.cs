using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// 전수조사 (Wire Audit) 결과의 8개 누락 fix.
    /// Idempotent — 이미 wire 된 건 그대로, null 만 채움.
    /// </summary>
    public static class ArkanoidWireFix
    {
        [MenuItem("Arkanoid/Wire Fix/Run All")]
        public static void RunAll()
        {
            FixTitlePanelMissing();
            FixItemsRendererPoolRoot();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[WireFix] Run All done.");
        }

        // TitlePanel 6 missing fields wire
        [MenuItem("Arkanoid/Wire Fix/A. TitlePanel 6 wire")]
        public static void FixTitlePanelMissing()
        {
            var tp = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (tp == null) { Debug.LogError("[WireFix] TitlePanel not found"); return; }

            var titleGo = tp.gameObject;

            // 자식에서 찾기 (TitlePanel 의 직접 자식 우선)
            var mascotNameText = FindChildTextDeep(titleGo, "MascotNameText");
            var mascotCostText = FindChildTextDeep(titleGo, "MascotCostText");
            // GoldText: TitlePanel 직접 자식 (MascotSelector 안에 있는 것 X)
            var goldText = titleGo.transform.Cast<Transform>()
                .FirstOrDefault(t => t.name == "GoldText")?.GetComponent<TMP_Text>()
                ?? FindChildTextDeep(titleGo, "GoldText");
            var normalBtn = FindChild(titleGo, "NormalButton")?.GetComponent<Button>();
            var hardBtn = FindChild(titleGo, "HardButton")?.GetComponent<Button>();
            var gm = Resources.FindObjectsOfTypeAll<GameManager>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));

            var so = new SerializedObject(tp);
            int wired = 0;
            if (TrySet(so, "mascotNameText", mascotNameText)) wired++;
            if (TrySet(so, "mascotCostText", mascotCostText)) wired++;
            if (TrySet(so, "goldText", goldText)) wired++;
            if (TrySet(so, "normalButton", normalBtn)) wired++;
            if (TrySet(so, "hardButton", hardBtn)) wired++;
            if (TrySet(so, "gameManager", gm)) wired++;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tp);

            Debug.Log($"[WireFix] (A) TitlePanel 6 wire — {wired}/6 OK " +
                $"(mascotName={mascotNameText!=null}, mascotCost={mascotCostText!=null}, gold={goldText!=null}, " +
                $"normalBtn={normalBtn!=null}, hardBtn={hardBtn!=null}, gm={gm!=null})");
        }

        // ItemsRenderer.poolRoot = self transform fallback
        [MenuItem("Arkanoid/Wire Fix/B. ItemsRenderer poolRoot")]
        public static void FixItemsRendererPoolRoot()
        {
            var ir = Resources.FindObjectsOfTypeAll<ItemsRenderer>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (ir == null) { Debug.LogError("[WireFix] ItemsRenderer not found"); return; }
            var so = new SerializedObject(ir);
            var prop = so.FindProperty("poolRoot");
            if (prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = ir.transform;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ir);
                Debug.Log("[WireFix] (B) ItemsRenderer.poolRoot = self transform");
            }
            else Debug.Log("[WireFix] (B) ItemsRenderer.poolRoot already wired");
        }

        // ============ Helpers ============
        private static bool TrySet(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return false;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return false;
            if (prop.objectReferenceValue == value) return true;
            prop.objectReferenceValue = value;
            return true;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var c = parent.transform.GetChild(i);
                if (c.name == name) return c.gameObject;
            }
            return null;
        }

        private static TMP_Text FindChildTextDeep(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if (t.gameObject.name == name) return t;
            return null;
        }
    }
}

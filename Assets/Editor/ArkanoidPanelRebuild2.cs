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
    /// 2차 fix:
    /// - (G) 모든 TMP_Text 에 DNFBitBitv2 SDF font 적용
    /// - (H) IntroStory 글자를 최상단으로 (사용자 의도)
    /// - (I) TitlePanel 의 NormalButton/HardButton/GameManager SerializeField wire
    /// - (J) POWERUPS panel sibling order 정리 (panel 뒤로, text 앞으로)
    /// </summary>
    public static class ArkanoidPanelRebuild2
    {
        private const float CX = 540f;
        private const float CY = 960f;
        private const string SDF_PATH = "Assets/Fonts/DNFBitBitv2 SDF.asset";

        [MenuItem("Arkanoid/Panel Rebuild 2/Run All")]
        public static void RunAll()
        {
            RegenerateSdfFontAsset();   // K — Atlas missing fix
            WireAllFonts();              // G — 새 SDF GUID 로 wire
            FixIntroStoryTop();
            WireTitleButtons();
            FixPowerupsSibling();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PanelRebuild2] Run All done.");
        }

        // (K) DNFBitBitv2 SDF Font Asset 재생성 — atlas texture missing fix.
        // 기존 asset 삭제 → .ttf 에서 Dynamic SDF 새로 생성 → 같은 경로에 저장.
        // GUID 변경되므로 (G) 다시 실행해서 wire.
        [MenuItem("Arkanoid/Panel Rebuild 2/K. Regenerate DNFBitBitv2 SDF")]
        public static void RegenerateSdfFontAsset()
        {
            // 기존 broken SDF 삭제
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SDF_PATH) != null)
                AssetDatabase.DeleteAsset(SDF_PATH);

            // .ttf 로드 (또는 .otf fallback)
            var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/DNFBitBitv2.ttf")
                   ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/DNFBitBitv2.otf");
            if (ttf == null)
            {
                Debug.LogError("[PanelRebuild2] (K) DNFBitBitv2.ttf/.otf not found in Assets/Fonts");
                return;
            }

            // Dynamic SDF — atlas 4096, on-demand glyph 생성 (한글 자모 포함).
            var newSdf = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                atlasWidth: 4096,
                atlasHeight: 4096,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            AssetDatabase.CreateAsset(newSdf, SDF_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PanelRebuild2] (K) DNFBitBitv2 SDF 재생성 — Dynamic 4096, GUID changed, run (G) again");
        }

        // (G) DNFBitBitv2 SDF 를 모든 TMP_Text 에 적용 — fallback 으로 LiberationSans 유지.
        [MenuItem("Arkanoid/Panel Rebuild 2/G. Wire All Fonts")]
        public static void WireAllFonts()
        {
            var sdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SDF_PATH);
            if (sdf == null)
            {
                Debug.LogError($"[PanelRebuild2] (G) DNFBitBitv2 SDF not found at {SDF_PATH}");
                return;
            }

            int count = 0;
            foreach (var tmp in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (EditorUtility.IsPersistent(tmp)) continue;
                if (tmp.font == sdf) { count++; continue; }
                var so = new SerializedObject(tmp);
                so.FindProperty("m_fontAsset").objectReferenceValue = sdf;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmp);
                count++;
            }
            // 3D TMP (SpriteRenderer 가 아닌 TextMeshPro) 도 있다면
            foreach (var tmp in Resources.FindObjectsOfTypeAll<TextMeshPro>())
            {
                if (EditorUtility.IsPersistent(tmp)) continue;
                var so = new SerializedObject(tmp);
                so.FindProperty("m_fontAsset").objectReferenceValue = sdf;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"[PanelRebuild2] (G) DNFBitBitv2 SDF wired to {count} TMP texts");
        }

        // (H) IntroStory 글자를 최상단으로 (사용자 의도: "최상단에 와야 한다").
        // TS PANEL_CY=1500 (하단) 이지만 사용자가 명시적으로 최상단 원함.
        [MenuItem("Arkanoid/Panel Rebuild 2/H. IntroStory 글자 최상단")]
        public static void FixIntroStoryTop()
        {
            var p = FindRoot("IntroStoryPanel");
            if (p == null) { Debug.LogError("[PanelRebuild2] (H) IntroStoryPanel not found"); return; }

            // PANEL_CY = 200 (상단). 사용자 의도 — TS 하단(1500)이 아닌 상단.
            float panelCy = 200f;
            float panelW = 1050f;
            float panelH = 270f;

            // Backdrop 위치 정정
            var bd = FindChild(p, "StoryTextBackdrop");
            if (bd != null) SetCenterPos(bd.GetComponent<RectTransform>(), CX, panelCy, panelW, panelH);

            // bodyText 위치 정정
            var bt = FindChild(p, "BodyText") ?? FindChild(p, "StoryText");
            if (bt != null)
            {
                SetCenterPos(bt.GetComponent<RectTransform>(), CX, panelCy, 960, 230);
                var tmp = bt.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 42;
                    tmp.color = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                }
            }
            Debug.Log($"[PanelRebuild2] (H) IntroStory glasses → 최상단 (cy={panelCy})");
        }

        // (I) TitlePanel 의 NormalButton/HardButton/GameManager SerializeField wire.
        [MenuItem("Arkanoid/Panel Rebuild 2/I. Title NORMAL/HARD wire")]
        public static void WireTitleButtons()
        {
            var titlePanel = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (titlePanel == null) { Debug.LogError("[PanelRebuild2] (I) TitlePanel not found"); return; }

            var titleGo = titlePanel.gameObject;
            var normalGo = FindChild(titleGo, "NormalButton");
            var hardGo = FindChild(titleGo, "HardButton");
            var normalBtn = normalGo != null ? normalGo.GetComponent<Button>() : null;
            var hardBtn = hardGo != null ? hardGo.GetComponent<Button>() : null;

            var gm = Resources.FindObjectsOfTypeAll<GameManager>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));

            var so = new SerializedObject(titlePanel);
            if (normalBtn != null) so.FindProperty("normalButton").objectReferenceValue = normalBtn;
            if (hardBtn != null)   so.FindProperty("hardButton").objectReferenceValue = hardBtn;
            if (gm != null)        so.FindProperty("gameManager").objectReferenceValue = gm;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(titlePanel);

            Debug.Log($"[PanelRebuild2] (I) TitlePanel wired — NormalBtn={normalBtn != null}, HardBtn={hardBtn != null}, GM={gm != null}");
        }

        // (J) POWERUPS panel/text sibling order — panel 가장 뒤, text 들 앞.
        [MenuItem("Arkanoid/Panel Rebuild 2/J. POWERUPS sibling order")]
        public static void FixPowerupsSibling()
        {
            var titlePanel = FindRoot("TitlePanel");
            if (titlePanel == null) return;

            // Panel 2개 (MascotInfoPanel, PowerupsPanel) 을 background 로 — sibling index 0~1
            var mip = FindChild(titlePanel, "MascotInfoPanel");
            var pup = FindChild(titlePanel, "PowerupsPanel");
            if (mip != null) mip.transform.SetSiblingIndex(0);
            if (pup != null) pup.transform.SetSiblingIndex(1);

            // POWERUPS title + 3 items 의 Name/Desc 를 panel 보다 뒤 sibling 로 (UI 앞에 그려짐)
            string[] names = { "PowerupsTitle",
                "Powerup0_Icon", "Powerup0_Name", "Powerup0_Desc",
                "Powerup1_Icon", "Powerup1_Name", "Powerup1_Desc",
                "Powerup2_Icon", "Powerup2_Name", "Powerup2_Desc",
                "NormalButton", "HardButton",
                "LogoText", "HighScoreText", "GoldText",
                "PrevArrow", "NextArrow",
            };
            int siblingStart = 2;
            int n = 0;
            foreach (var name in names)
            {
                var c = FindChild(titlePanel, name);
                if (c != null)
                {
                    c.transform.SetSiblingIndex(siblingStart + n);
                    n++;
                }
            }
            Debug.Log($"[PanelRebuild2] (J) Title sibling order — panels back, {n} elements front");
        }

        // ============ Helpers ============
        private static GameObject FindRoot(string name) =>
            Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == name && !EditorUtility.IsPersistent(g));

        private static GameObject FindChild(GameObject parent, string name)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var c = parent.transform.GetChild(i);
                if (c.name == name) return c.gameObject;
            }
            return null;
        }

        private static void SetCenterPos(RectTransform rt, float tsX, float tsY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(tsX - CX, CY - tsY);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}

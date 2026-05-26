using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// 3차 fix (사용자 스샷 2026-05-27 기반):
    /// - (L) RoundIntroPanel/Background image 비활성 — overlay (TS 동작)
    /// - (M) TitlePanel.portraits[] auto-wire — Portrait_<id> 자식 5개 찾아서 매핑
    /// - (N) portrait 5장 모두 동일 위치 (CX=540, Y=600, 380×380) — RefreshAll 의 selected 만 visible 동작
    /// - (O) Static SDF 재생성 + ASCII/한글 자모 사전 generate (Dynamic 이 안 보일 경우 대비)
    /// - (P) 모든 panel 의 Background 자식 sprite 검증 — RoundIntro 등 잘못된 sprite 진단
    /// </summary>
    public static class ArkanoidPanelRebuild3
    {
        private const float CX = 540f;
        private const float CY = 960f;
        private const string SDF_PATH = "Assets/Fonts/DNFBitBitv2 SDF.asset";

        private static readonly string[] MASCOT_IDS = { "albatross", "kongming", "snowrabbit", "reaper", "seraphin" };
        private static readonly string[] MASCOT_NAMES = { "ALBATROSS", "KONGMING", "SNOW RABBIT", "REAPER", "SERAPHIN" };
        private static readonly int[] MASCOT_COSTS = { 0, 100, 300, 600, 1000 };

        [MenuItem("Arkanoid/Panel Rebuild 3/Run All")]
        public static void RunAll()
        {
            FixSeraphinSpriteMode();      // T — sprite sheet → Single
            HideRoundIntroBackground();   // L
            DiagnoseBackgroundSprites();  // P
            WirePortraitsArray();         // M — portrait wire
            SwapPortraitSprites();        // V — portrait Image.sprite swap (Single mode 후)
            UnifyPortraitPositions();     // N
            WireLiberationSansFallback(); // U — font 가 안 보이면 LiberationSans 로
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PanelRebuild3] Run All done.");
        }

        // (T) seraphin.png spriteMode = Multiple → Single + reimport.
        // 사용자 화면에 "음표 아이콘" 보이는 정체 — Multiple 의 첫 sub-sprite (seraphin_0=39×46) 가 잘못 wire.
        [MenuItem("Arkanoid/Panel Rebuild 3/T. Fix seraphin spriteMode")]
        public static void FixSeraphinSpriteMode()
        {
            const string path = "Assets/Sprites/Portraits2/seraphin.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[PanelRebuild3] (T) seraphin TextureImporter null");
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Debug.Log("[PanelRebuild3] (T) seraphin.png spriteMode → Single, reimported");
        }

        // (V) portrait Image 5장의 sprite 를 portraits2/<id>.png 로 강제 swap.
        // (T) seraphin Single 변환 후 main sprite 가 갱신됐으므로 다시 wire.
        [MenuItem("Arkanoid/Panel Rebuild 3/V. Portrait sprite swap")]
        public static void SwapPortraitSprites()
        {
            int swapped = 0;
            foreach (var id in MASCOT_IDS)
            {
                var go = FindByName($"Portrait_{id}") ?? FindByName(id);
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img == null) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Portraits2/{id}.png");
                if (sprite == null) { Debug.LogWarning($"[PanelRebuild3] (V) Portraits2/{id}.png main sprite null"); continue; }
                var so = new SerializedObject(img);
                so.FindProperty("m_Sprite").objectReferenceValue = sprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(img);
                swapped++;
            }
            Debug.Log($"[PanelRebuild3] (V) Portrait sprite swap — {swapped}/5");
        }

        // (U) LiberationSans SDF (TMP 기본) 로 모든 TMP wire — DNFBitBitv2 atlas 가 broken 이면 LiberationSans 가 표시.
        // 한글 안 보일 수 있지만 영어/숫자는 보임 (사용자가 글자 보임 검증 후 한글 font 별도).
        [MenuItem("Arkanoid/Panel Rebuild 3/U. LiberationSans fallback")]
        public static void WireLiberationSansFallback()
        {
            // Unity TMP 기본 font 경로
            string[] candidates = {
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
                "Assets/TextMeshPro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            };
            TMP_FontAsset libsans = null;
            foreach (var p in candidates)
            {
                libsans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
                if (libsans != null) break;
            }
            if (libsans == null)
            {
                Debug.LogError("[PanelRebuild3] (U) LiberationSans SDF not found");
                return;
            }

            int count = 0;
            foreach (var tmp in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (EditorUtility.IsPersistent(tmp)) continue;
                var so = new SerializedObject(tmp);
                so.FindProperty("m_fontAsset").objectReferenceValue = libsans;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"[PanelRebuild3] (U) LiberationSans wired to {count} TMP texts");
        }

        // (L) RoundIntroPanel/Background image 비활성 — TS 는 overlay (게임 월드 보여야)
        [MenuItem("Arkanoid/Panel Rebuild 3/L. RoundIntro Background hide")]
        public static void HideRoundIntroBackground()
        {
            var panel = FindRoot("RoundIntroPanel");
            if (panel == null) { Debug.LogError("[PanelRebuild3] (L) RoundIntroPanel not found"); return; }
            var bg = FindChild(panel, "Background");
            if (bg != null)
            {
                var img = bg.GetComponent<Image>();
                if (img != null) img.enabled = false;
                Debug.Log("[PanelRebuild3] (L) RoundIntroPanel/Background image disabled");
            }
            else Debug.LogWarning("[PanelRebuild3] (L) RoundIntroPanel/Background not found");
        }

        // (P) 모든 panel 의 Background sprite 진단 로그 — 어떤 sprite 가 잘못 wire 됐는지
        [MenuItem("Arkanoid/Panel Rebuild 3/P. Diagnose Background sprites")]
        public static void DiagnoseBackgroundSprites()
        {
            string[] panels = { "TitlePanel", "IntroStoryPanel", "RoundIntroPanel", "InGamePanel", "GameOverPanel", "GameClearPanel" };
            foreach (var panelName in panels)
            {
                var panel = FindRoot(panelName);
                if (panel == null) continue;
                var bg = FindChild(panel, "Background");
                if (bg == null) continue;
                var img = bg.GetComponent<Image>();
                if (img == null) continue;
                var spritePath = img.sprite != null ? AssetDatabase.GetAssetPath(img.sprite) : "(null)";
                Debug.Log($"[PanelRebuild3] (P) {panelName}/Background sprite = {spritePath} (enabled={img.enabled})");
            }
        }

        // (M) TitlePanel.portraits[5] auto-wire — Portrait_<id> 자식 Image 찾아서 매핑
        [MenuItem("Arkanoid/Panel Rebuild 3/M. Wire portraits array")]
        public static void WirePortraitsArray()
        {
            var titlePanel = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (titlePanel == null) { Debug.LogError("[PanelRebuild3] (M) TitlePanel not found"); return; }

            var so = new SerializedObject(titlePanel);
            var portraits = so.FindProperty("portraits");
            portraits.arraySize = MASCOT_IDS.Length;

            int wired = 0;
            for (int i = 0; i < MASCOT_IDS.Length; i++)
            {
                var id = MASCOT_IDS[i];
                // Portrait Image — Hierarchy 어딘가의 "Portrait_<id>" 또는 "<id>"
                var imgGo = FindByName($"Portrait_{id}") ?? FindByName(id);
                Image portraitImg = imgGo != null ? imgGo.GetComponent<Image>() : null;

                var entry = portraits.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("MascotId").stringValue = id;
                entry.FindPropertyRelative("DisplayName").stringValue = MASCOT_NAMES[i];
                entry.FindPropertyRelative("UnlockCost").intValue = MASCOT_COSTS[i];
                entry.FindPropertyRelative("Portrait").objectReferenceValue = portraitImg;

                // AnimFrames — Sprites/Mascots/<id>/frame0~3.png
                var anim = entry.FindPropertyRelative("AnimFrames");
                anim.arraySize = 4;
                for (int f = 0; f < 4; f++)
                {
                    anim.GetArrayElementAtIndex(f).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Mascots/{id}/frame{f}.png");
                }
                if (portraitImg != null) wired++;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(titlePanel);
            Debug.Log($"[PanelRebuild3] (M) TitlePanel.portraits[] wired — {wired}/5 portrait Image found");
        }

        // (N) Portrait_<id> 5장 모두 동일 위치 (CX=540, Y=600, 380×380) — TS 의 1장 swap 흉내
        // TS: portrait 1장 (CX=540, MASCOT_PLACEHOLDER_Y=600, SIZE=380). Unity 5장을 같은 자리에 두고 RefreshAll 이 selected 만 visible.
        [MenuItem("Arkanoid/Panel Rebuild 3/N. Unify portrait positions")]
        public static void UnifyPortraitPositions()
        {
            int n = 0;
            foreach (var id in MASCOT_IDS)
            {
                var go = FindByName($"Portrait_{id}") ?? FindByName(id);
                if (go == null) continue;
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 360);   // TS Y=600 → Unity 960-600=360
                rt.sizeDelta = new Vector2(380, 380);
                rt.localScale = Vector3.one;
                n++;
            }
            Debug.Log($"[PanelRebuild3] (N) {n} portraits unified to (540,600) 380×380");
        }

        // (O) Static SDF 재생성 — ASCII + 한글 자모 사전 generate. Dynamic 의 런타임 glyph 생성 실패 대비.
        [MenuItem("Arkanoid/Panel Rebuild 3/O. Regenerate Static SDF")]
        public static void RegenerateStaticSdf()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SDF_PATH) != null)
                AssetDatabase.DeleteAsset(SDF_PATH);

            var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/DNFBitBitv2.ttf")
                   ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/DNFBitBitv2.otf");
            if (ttf == null)
            {
                Debug.LogError("[PanelRebuild3] (O) DNFBitBitv2 font not found");
                return;
            }

            // Dynamic mode (multi-atlas) — TryAddCharacters 로 사전 generate 가능
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

            // 사전 generate — ASCII + 자주 쓰는 한글 + 기호
            var preset = " !\"#$%&'()*+,-./0123456789:;<=>?@" +
                         "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
                         "abcdefghijklmnopqrstuvwxyz{|}~" +
                         "알바트로스콩밍이햄스터눈토끼저승해골세라핀분홍시렌" +
                         "마스코트선택언락구입게임시작레디라운드클리어오버" +
                         "하이스코어골드플레이노말하드일시정지퍼즐";
            newSdf.TryAddCharacters(preset);

            EditorUtility.SetDirty(newSdf);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PanelRebuild3] (O) SDF 재생성 + 사전 generate {preset.Length} chars");
        }

        // ============ Helpers ============
        private static GameObject FindRoot(string name) =>
            Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == name && !EditorUtility.IsPersistent(g));

        private static GameObject FindByName(string name) =>
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
    }
}

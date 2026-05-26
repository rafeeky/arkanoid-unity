using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// 04 누락 분석 + 05 plan 의 나머지 fix 일괄. Run All 한 번이면 끝.
    /// </summary>
    public static class ArkanoidPortingFinish
    {
        [MenuItem("Arkanoid/Porting Finish/Run All")]
        public static void RunAll()
        {
            ApplySpriteSettings();
            CreateFontSDF();
            CreateDifficultyHard();
            WireSceneSprites();
            WireIntroPages();
            ApplyPlayerSettings();
            FixCameraAndPlayfield();
            FixHudPositions();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PortingFinish] Run All done.");
        }

        // ============ G. Camera + PlayfieldRoot 좌표 (TS portrait 9:16 매칭) ============
        // 카메라 (360, -450, -10), ortho 640. PlayfieldRoot (0, 0, 0), scale (1, -1, 1).
        // 매핑: game (X, Y) → world (X, -Y). game 코트 (0~720, 0~900) → world (0~720, 0~-900).
        // Camera view: y [-1090, 190], x [-300, 1020] (9:16 aspect 가정). 화면에 game 영역 정확히 fit.
        [MenuItem("Arkanoid/Porting Finish/G. Camera+PlayfieldRoot")]
        public static void FixCameraAndPlayfield()
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO != null)
            {
                var cam = camGO.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 640f; // PlayfieldWidth 720 이 9:16 portrait 의 width 에 fit (1280 * 9/16 = 720)
                camGO.transform.position = new Vector3(360, -450, -10);
                Debug.Log("[PortingFinish] (G) Camera pos (360, -450, -10), ortho 640");
            }
            var playfield = GameObject.Find("PlayfieldRoot");
            if (playfield != null)
            {
                playfield.transform.position = Vector3.zero;
                playfield.transform.localScale = new Vector3(1, -1, 1);
                Debug.Log("[PortingFinish] (G) PlayfieldRoot pos (0,0,0), scale (1,-1,1)");
            }
        }

        // ============ H. HUD positions (5 텍스트 reset) ============
        [MenuItem("Arkanoid/Porting Finish/H. HUD Positions")]
        public static void FixHudPositions()
        {
            // Canvas Reference Resolution 1080×1920 가정. HUD 5 텍스트 화면 가장자리 배치.
            // (TextName, anchor (min, max), anchoredPosition, sizeDelta, alignment)
            var entries = new (string Name, Vector2 AnchorMin, Vector2 AnchorMax, Vector2 Pivot, Vector2 Pos, Vector2 Size, TMPro.TextAlignmentOptions Align)[]
            {
                ("ScoreText",     new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),   new Vector2( 40, -40), new Vector2(400, 80), TMPro.TextAlignmentOptions.TopLeft),
                ("HighScoreText", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(  0, -40), new Vector2(500, 80), TMPro.TextAlignmentOptions.Top),
                ("RoundText",     new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),   new Vector2(-40, -40), new Vector2(400, 80), TMPro.TextAlignmentOptions.TopRight),
                ("LivesText",     new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),   new Vector2( 40,  40), new Vector2(400, 80), TMPro.TextAlignmentOptions.BottomLeft),
                ("EffectText",    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(  0, -140), new Vector2(600, 80), TMPro.TextAlignmentOptions.Top),
            };
            int fixed_ = 0;
            foreach (var e in entries)
            {
                var tmpros = Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>();
                var t = tmpros.FirstOrDefault(x => x.gameObject.name == e.Name);
                if (t == null) { Debug.LogWarning($"[PortingFinish] (H) {e.Name} not found"); continue; }
                var rt = t.rectTransform;
                rt.anchorMin = e.AnchorMin;
                rt.anchorMax = e.AnchorMax;
                rt.pivot = e.Pivot;
                rt.anchoredPosition = e.Pos;
                rt.sizeDelta = e.Size;
                t.alignment = e.Align;
                t.fontSize = 42;
                t.color = Color.white;
                t.outlineWidth = 0.2f;
                t.outlineColor = new Color32(0, 0, 0, 200);
                fixed_++;
            }
            Debug.Log($"[PortingFinish] (H) HUD positions reset: {fixed_}");
        }

        // ============ F. Player Settings ============
        [MenuItem("Arkanoid/Porting Finish/F. Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            Debug.Log("[PortingFinish] (F) Player Settings: Portrait lock");
        }

        // ============ A. Sprite import settings ============
        [MenuItem("Arkanoid/Porting Finish/A. Sprite Settings")]
        public static void ApplySpriteSettings()
        {
            AssetDatabase.Refresh();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
            int updated = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spritePixelsPerUnit != 1f) { importer.spritePixelsPerUnit = 1f; changed = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (changed) { importer.SaveAndReimport(); updated++; }
            }
            Debug.Log($"[PortingFinish] (A) Sprite settings updated: {updated}");
        }

        // ============ B. Font SDF ============
        [MenuItem("Arkanoid/Porting Finish/B. Font SDF")]
        public static void CreateFontSDF()
        {
            var fontPath = "Assets/Fonts/DNFBitBitv2.ttf";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) { Debug.LogError("[PortingFinish] (B) DNFBitBitv2.ttf not found"); return; }

            var sdfPath = "Assets/Fonts/DNFBitBitv2 SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
            TMP_FontAsset fontAsset;
            if (existing != null)
            {
                Debug.Log("[PortingFinish] (B) DNFBitBitv2 SDF already exists — reusing");
                fontAsset = existing;
            }
            else
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(fontAsset, sdfPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PortingFinish] (B) DNFBitBitv2 SDF created at {sdfPath}");
            }

            // TMP Default Font Asset 으로 set
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    Debug.Log("[PortingFinish] (B) TMP Default Font = DNFBitBitv2 SDF");
                }
            }
        }

        // ============ C. Difficulty_Hard ============
        [MenuItem("Arkanoid/Porting Finish/C. Difficulty Hard")]
        public static void CreateDifficultyHard()
        {
            var normalPath = "Assets/Data/Gameplay/Difficulty_Normal.asset";
            var hardPath = "Assets/Data/Gameplay/Difficulty_Hard.asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(hardPath) != null)
            {
                Debug.Log("[PortingFinish] (C) Difficulty_Hard already exists");
                return;
            }
            if (AssetDatabase.CopyAsset(normalPath, hardPath))
            {
                Debug.Log("[PortingFinish] (C) Difficulty_Hard.asset created (copy of Normal — tune in Inspector)");
            }
        }

        // ============ D. Scene wire (sprite arrays) ============
        [MenuItem("Arkanoid/Porting Finish/D. Scene Wire")]
        public static void WireSceneSprites()
        {
            // BordersRenderer
            var borders = FindFirstOfType<BordersRenderer>();
            if (borders != null)
            {
                var so = new SerializedObject(borders);
                Set(so, "verticalSprite", LoadSprite("Assets/Sprites/Borders/border_vertical.png"));
                Set(so, "horizontalSprite", LoadSprite("Assets/Sprites/Borders/border_horizontal.png"));
                so.ApplyModifiedProperties();
                Debug.Log("[PortingFinish] (D) BordersRenderer sprite wired");
            }
            else Debug.LogWarning("[PortingFinish] (D) BordersRenderer not found in scene");

            // DoorsRenderer
            var doors = FindFirstOfType<DoorsRenderer>();
            if (doors != null)
            {
                var so = new SerializedObject(doors);
                Set(so, "closedSprite", LoadSprite("Assets/Sprites/Borders/door_closed.png"));
                var framesProp = so.FindProperty("openingFrames");
                framesProp.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue =
                        LoadSprite($"Assets/Sprites/Borders/door_opening_frame{i}.png");
                }
                so.ApplyModifiedProperties();
                Debug.Log("[PortingFinish] (D) DoorsRenderer 5 frames wired");
            }
            else Debug.LogWarning("[PortingFinish] (D) DoorsRenderer not found in scene");

            // ItemsRenderer
            var items = FindFirstOfType<ItemsRenderer>();
            if (items != null)
            {
                var so = new SerializedObject(items);
                Set(so, "expandSprite", LoadSprite("Assets/Sprites/Gameplay/item_expand.png"));
                Set(so, "magnetSprite", LoadSprite("Assets/Sprites/Gameplay/item_magnet.png"));
                Set(so, "laserSprite", LoadSprite("Assets/Sprites/Gameplay/item_laser.png"));
                so.ApplyModifiedProperties();
                Debug.Log("[PortingFinish] (D) ItemsRenderer 3 sprites wired");
            }
            else Debug.LogWarning("[PortingFinish] (D) ItemsRenderer not found in scene");

            // MascotRenderer — 5종 entry
            var mascot = FindFirstOfType<MascotRenderer>();
            if (mascot != null)
            {
                var so = new SerializedObject(mascot);
                var entriesProp = so.FindProperty("mascotSprites");
                string[] ids = { "albatross", "kongming", "snowrabbit", "reaper", "seraphin" };
                entriesProp.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var id = ids[i];
                    var ep = entriesProp.GetArrayElementAtIndex(i);
                    ep.FindPropertyRelative("MascotId").stringValue = id;
                    ep.FindPropertyRelative("Frame0").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame0.png");
                    ep.FindPropertyRelative("Frame1").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame1.png");
                    ep.FindPropertyRelative("Frame2").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame2.png");
                    ep.FindPropertyRelative("Frame3").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame3.png");
                }
                so.ApplyModifiedProperties();
                Debug.Log($"[PortingFinish] (D) MascotRenderer {ids.Length} entries wired");
            }
            else Debug.LogWarning("[PortingFinish] (D) MascotRenderer not found in scene");
        }

        // ============ E. Intro pages ============
        [MenuItem("Arkanoid/Porting Finish/E. Intro Pages")]
        public static void WireIntroPages()
        {
            var intro = FindFirstOfType<IntroStoryPanel>();
            if (intro == null) { Debug.LogError("[PortingFinish] (E) IntroStoryPanel not found"); return; }
            var so = new SerializedObject(intro);
            var pagesProp = so.FindProperty("pages");
            pagesProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var ep = pagesProp.GetArrayElementAtIndex(i);
                ep.FindPropertyRelative("PageIndex").intValue = i;
                ep.FindPropertyRelative("Illustration").objectReferenceValue =
                    LoadSprite($"Assets/Sprites/Intro/intro_story_0{i + 1}.png");
            }
            so.ApplyModifiedProperties();
            Debug.Log("[PortingFinish] (E) IntroStoryPanel 4 pages wired");
        }

        // ============ Helpers ============
        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Set(SerializedObject so, string propName, Object value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[PortingFinish] property not found: {propName}");
        }

        private static T FindFirstOfType<T>() where T : Component
        {
            // inactive 도 찾기 위해 Resources.FindObjectsOfTypeAll 사용.
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => !UnityEditor.EditorUtility.IsPersistent(c.transform.root.gameObject)
                                     && !(c.hideFlags == HideFlags.NotEditable || c.hideFlags == HideFlags.HideAndDontSave));
        }
    }
}

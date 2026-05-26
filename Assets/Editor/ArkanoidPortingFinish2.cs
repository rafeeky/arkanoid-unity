// 05 plan + 04 누락 fix batch 2 — I~P 메뉴.
// 기존 ArkanoidPortingFinish.cs 와 별도 파일로 관리. Run All 2 한 번이면 끝.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;
using Arkanoid.Definitions;
using Arkanoid.Definitions.SO;

namespace ArkanoidEditor
{
    public static class ArkanoidPortingFinish2
    {
        // ========================================================
        // Run All 2 — I~P 일괄.
        // ========================================================
        [MenuItem("Arkanoid/Porting Finish/Run All 2 (I-P)")]
        public static void RunAll2()
        {
            ApplyAudioImportSettings();
            WireFontFallback();
            RewriteUITextTable();
            WireUnityAudioClipTable();
            ConvertIntroIllustrationToImage();
            CreateOrWireTitleMascotImage();
            CreateOrWireGameOverMascotAnim();
            FixHudHighScorePosition();
            CreateMascotSelectorUI();
            CreateMuteSettingsUI();
            EnsureInputActionsAsset();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PortingFinish2] Run All 2 done.");
        }

        // ============ I. Audio import settings ============
        // BGM (bgm_*) → Vorbis + Streaming. SFX/jingle → ADPCM + DecompressOnLoad.
        [MenuItem("Arkanoid/Porting Finish/I. Audio Import Settings")]
        public static void ApplyAudioImportSettings()
        {
            int bgm = 0, sfx = 0;
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                var fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool isBgm = fileName.StartsWith("bgm_");
                var settings = importer.defaultSampleSettings;
                if (isBgm)
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                    bgm++;
                }
                else
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.ADPCM;
                    sfx++;
                }
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.SaveAndReimport();
            }
            Debug.Log($"[PortingFinish2] (I) Audio settings: BGM={bgm}, SFX/jingle={sfx}");
        }

        // ============ J. Font Fallback (MalgunGothic → DNFBitBitv2) ============
        [MenuItem("Arkanoid/Porting Finish/J. Font Fallback")]
        public static void WireFontFallback()
        {
            var malgunSdfPath = "Assets/Fonts/MalgunGothic SDF.asset";
            var malgun = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(malgunSdfPath);
            if (malgun == null)
            {
                // 생성 시도
                var ttfPath = "Assets/Fonts/malgun.ttf";
                var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
                if (ttf == null) { Debug.LogError("[PortingFinish2] (J) malgun.ttf not found"); return; }
                malgun = TMP_FontAsset.CreateFontAsset(
                    ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(malgun, malgunSdfPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PortingFinish2] (J) MalgunGothic SDF created at {malgunSdfPath}");
            }

            var dnfPath = "Assets/Fonts/DNFBitBitv2 SDF.asset";
            var dnf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(dnfPath);
            if (dnf == null) { Debug.LogError("[PortingFinish2] (J) DNFBitBitv2 SDF not found"); return; }

            // Add MalgunGothic 을 DNF 의 fallback list 에 (중복 방지).
            var so = new SerializedObject(dnf);
            var arr = so.FindProperty("m_FallbackFontAssetTable");
            bool exists = false;
            for (int i = 0; i < arr.arraySize; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == malgun) { exists = true; break; }
            }
            if (!exists)
            {
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = malgun;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dnf);
                Debug.Log("[PortingFinish2] (J) MalgunGothic added to DNFBitBitv2 fallback");
            }
            else
            {
                Debug.Log("[PortingFinish2] (J) MalgunGothic already in fallback list");
            }
        }

        // ============ K. UITextTable rewrite (TS 스펙 21 entries) ============
        [MenuItem("Arkanoid/Porting Finish/K. UITextTable Rewrite (TS spec)")]
        public static void RewriteUITextTable()
        {
            var path = "Assets/Data/Presentation/UITextTable.asset";
            var so = AssetDatabase.LoadAssetAtPath<UITextSO>(path);
            if (so == null) { Debug.LogError("[PortingFinish2] (K) UITextTable.asset not found"); return; }
            var sObj = new SerializedObject(so);
            var entries = sObj.FindProperty("entries");
            // TS UITextTable.ts 21 entries (한글 ↔ 영문 혼합 — TS 와 동일하게 영문).
            var data = new (string id, string val)[]
            {
                ("txt_title_start",            "PRESS SPACE TO START"),
                ("txt_title_highscore",        "HIGH SCORE {0}"),
                ("txt_round_01",               "ROUND 1"),
                ("txt_ready",                  "READY"),
                ("txt_gameover",               "GAME OVER"),
                ("txt_retry",                  "TAP TO RETRY"),
                ("txt_item_expand_name",       "EXPAND"),
                ("txt_item_expand_desc",       "BAR LENGTH x1.5"),
                ("txt_intro_page_01",          "LONG AGO, THE BRICKS FELL FROM THE SKY."),
                ("txt_intro_page_02",          "ONLY THE BAR AND THE BALL REMAIN."),
                ("txt_intro_page_03",          "RECLAIM THE STAGES."),
                ("txt_round_02",               "ROUND 2"),
                ("txt_round_03",               "ROUND 3"),
                ("txt_gameclear",              "CONGRATULATIONS"),
                ("txt_gameclear_final_score",  "FINAL SCORE {0}"),
                ("txt_gameclear_retry",        "TAP TO RETRY"),
                ("txt_gameover_final_score",   "FINAL SCORE {0}"),
                ("txt_item_magnet_name",       "MAGNET"),
                ("txt_item_magnet_desc",       "BALL STICKS TO BAR"),
                ("txt_item_laser_name",        "LASER"),
                ("txt_item_laser_desc",        "SPACE FIRES 2 SHOTS"),
            };
            entries.arraySize = data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("textId").stringValue = data[i].id;
                e.FindPropertyRelative("value").stringValue = data[i].val;
            }
            sObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
            Debug.Log($"[PortingFinish2] (K) UITextTable rewritten: {data.Length} entries (TS spec)");
        }

        // ============ L. UnityAudio.clipTable wire (19 entries) ============
        [MenuItem("Arkanoid/Porting Finish/L. UnityAudio ClipTable Wire")]
        public static void WireUnityAudioClipTable()
        {
            var ua = FindFirstOfType<UnityAudio>();
            if (ua == null) { Debug.LogError("[PortingFinish2] (L) UnityAudio not found in scene"); return; }

            // resourceId → file path
            var map = new Dictionary<string, string>
            {
                { "bgm_title",            "Assets/Audio/bgm_title.wav" },
                { "jingle_round_start",   "Assets/Audio/jingle_round_start.wav" },
                { "jingle_gameover",      "Assets/Audio/jingle_gameover.wav" },
                { "jingle_gameclear",     "Assets/Audio/jingle_gameclear.wav" },
                { "sfx_block_hit",        "Assets/Audio/sfx_block_hit.wav" },
                { "sfx_block_destroyed",  "Assets/Audio/sfx_block_destroyed.wav" },
                { "sfx_item_collected",   "Assets/Audio/sfx_item_collected.wav" },
                { "sfx_life_lost",        "Assets/Audio/sfx_life_lost.wav" },
                { "sfx_ui_confirm",       "Assets/Audio/sfx_ui_confirm.wav" },
                { "sfx_ball_attached",    "Assets/Audio/sfx_ball_attached.wav" },
                { "sfx_balls_released",   "Assets/Audio/sfx_balls_released.wav" },
                { "sfx_laser_fired",      "Assets/Audio/sfx_laser_fired.wav" },
            };

            var so = new SerializedObject(ua);
            var arr = so.FindProperty("clipTable");
            arr.arraySize = map.Count;
            int i = 0, missing = 0;
            foreach (var kv in map)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("ResourceId").stringValue = kv.Key;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(kv.Value);
                if (clip == null) { Debug.LogWarning($"[PortingFinish2] (L) clip missing: {kv.Value}"); missing++; }
                elem.FindPropertyRelative("Clip").objectReferenceValue = clip;
                i++;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ua);
            Debug.Log($"[PortingFinish2] (L) UnityAudio clipTable wired: {map.Count - missing}/{map.Count}");
        }

        // ============ M. Intro illustration SpriteRenderer → Image ============
        // IntroStoryPanel.cs 이미 Image 필드로 바꿈. 여기는 hierarchy 의 GameObject 정리.
        [MenuItem("Arkanoid/Porting Finish/M. Intro Illustration → Image")]
        public static void ConvertIntroIllustrationToImage()
        {
            var intro = FindFirstOfType<IntroStoryPanel>();
            if (intro == null) { Debug.LogError("[PortingFinish2] (M) IntroStoryPanel not found"); return; }

            // intro 의 자식 중 "Illustration" 이름 GameObject 찾기.
            Transform illusT = null;
            foreach (Transform child in intro.transform)
            {
                if (child.name == "Illustration" || child.name == "illustration")
                {
                    illusT = child;
                    break;
                }
            }
            if (illusT == null)
            {
                // 없으면 새 자식 생성 (Canvas 의 자식 RectTransform)
                var go = new GameObject("Illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(intro.transform, false);
                illusT = go.transform;
                Debug.Log("[PortingFinish2] (M) Created Illustration child");
            }

            // SpriteRenderer 가 있으면 제거.
            var sr = illusT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Object.DestroyImmediate(sr);
                Debug.Log("[PortingFinish2] (M) Removed SpriteRenderer");
            }

            // RectTransform 없으면 추가.
            var rt = illusT.GetComponent<RectTransform>();
            if (rt == null)
            {
                // SpriteRenderer 가 있었다면 transform 이 RectTransform 이 아닐 수 있음. 새로 생성.
                var go = new GameObject("Illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(intro.transform, false);
                Object.DestroyImmediate(illusT.gameObject);
                illusT = go.transform;
                rt = (RectTransform)illusT;
            }

            // Image 컴포넌트 보장.
            var img = illusT.GetComponent<Image>();
            if (img == null) img = illusT.gameObject.AddComponent<Image>();

            // RectTransform: 화면 상단 1/3 영역에 중앙 배치 (대략).
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 200f);
            rt.sizeDelta = new Vector2(600f, 600f);

            img.preserveAspect = true;
            img.raycastTarget = false;
            // 초기 sprite — page1 (Bind 가 매 프레임 갱신).
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Intro/intro_story_01.png");

            // IntroStoryPanel.illustration 필드 wire.
            var so = new SerializedObject(intro);
            so.FindProperty("illustration").objectReferenceValue = img;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(intro);

            Debug.Log("[PortingFinish2] (M) Intro illustration wired as Image");
        }

        // ============ N. Title mascot Image (4 frame anim) ============
        [MenuItem("Arkanoid/Porting Finish/N. Title Mascot Image")]
        public static void CreateOrWireTitleMascotImage()
        {
            var title = FindFirstOfType<TitlePanel>();
            if (title == null) { Debug.LogError("[PortingFinish2] (N) TitlePanel not found"); return; }

            // 자식 "MascotImage" 찾기.
            Transform mascotT = null;
            foreach (Transform child in title.transform)
            {
                if (child.name == "MascotImage") { mascotT = child; break; }
            }
            if (mascotT == null)
            {
                var go = new GameObject("MascotImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(title.transform, false);
                mascotT = go.transform;
                Debug.Log("[PortingFinish2] (N) Created MascotImage child");
            }
            var rt = (RectTransform)mascotT;
            var img = mascotT.GetComponent<Image>() ?? mascotT.gameObject.AddComponent<Image>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 100f);
            rt.sizeDelta = new Vector2(240f, 240f);
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 4 frame sprite array (albatross 기본).
            var frames = new Sprite[4];
            for (int i = 0; i < 4; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Mascots/albatross/frame{i}.png");

            img.sprite = frames[0];

            var so = new SerializedObject(title);
            so.FindProperty("mascotImage").objectReferenceValue = img;
            var arr = so.FindProperty("mascotFrames");
            arr.arraySize = 4;
            for (int i = 0; i < 4; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(title);

            Debug.Log("[PortingFinish2] (N) Title MascotImage wired (albatross 4 frames)");
        }

        // ============ O. GameOver mascot 4 frame anim ============
        [MenuItem("Arkanoid/Porting Finish/O. GameOver Mascot Anim")]
        public static void CreateOrWireGameOverMascotAnim()
        {
            var gameOver = FindFirstOfType<GameOverPanel>();
            if (gameOver == null) { Debug.LogError("[PortingFinish2] (O) GameOverPanel not found"); return; }

            Transform mascotT = null;
            foreach (Transform child in gameOver.transform)
            {
                if (child.name == "MascotImage") { mascotT = child; break; }
            }
            if (mascotT == null)
            {
                var go = new GameObject("MascotImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(gameOver.transform, false);
                mascotT = go.transform;
                Debug.Log("[PortingFinish2] (O) Created MascotImage child");
            }
            var rt = (RectTransform)mascotT;
            var img = mascotT.GetComponent<Image>() ?? mascotT.gameObject.AddComponent<Image>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 200f);
            rt.sizeDelta = new Vector2(300f, 300f);
            img.preserveAspect = true;
            img.raycastTarget = false;

            // gameover_frame_1~4
            var frames = new Sprite[4];
            for (int i = 0; i < 4; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Mascots/GameOver/gameover_frame_{i + 1}.png");

            img.sprite = frames[0];

            var so = new SerializedObject(gameOver);
            so.FindProperty("mascotImage").objectReferenceValue = img;
            var arr = so.FindProperty("mascotFrames");
            arr.arraySize = 4;
            for (int i = 0; i < 4; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameOver);

            Debug.Log("[PortingFinish2] (O) GameOver MascotImage wired (gameover_frame_1~4)");
        }

        // ============ P. HUD HighScore reposition + outline 0.3 ============
        [MenuItem("Arkanoid/Porting Finish/P. HighScore Position + Outline")]
        public static void FixHudHighScorePosition()
        {
            // HighScoreText 를 더 위쪽으로 (배경 빈 영역). 모든 HUD outline width 0.3.
            var tmpros = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var t in tmpros)
            {
                if (t == null) continue;
                var name = t.gameObject.name;
                if (name == "ScoreText" || name == "HighScoreText" || name == "RoundText"
                    || name == "LivesText" || name == "EffectText")
                {
                    t.outlineWidth = 0.3f;
                    t.outlineColor = new Color32(0, 0, 0, 220);
                    EditorUtility.SetDirty(t);
                }
                if (name == "HighScoreText")
                {
                    var rt = t.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -120f); // 화면 위 1/8 영역
                    rt.sizeDelta = new Vector2(600f, 80f);
                    t.alignment = TextAlignmentOptions.Top;
                    t.fontSize = 36;
                }
            }
            Debug.Log("[PortingFinish2] (P) HUD outline 0.3 + HighScore repositioned");
        }

        // ============ Q. Mascot selector UI (Title 자식) ============
        [MenuItem("Arkanoid/Porting Finish/Q. Mascot Selector UI")]
        public static void CreateMascotSelectorUI()
        {
            var title = FindFirstOfType<TitlePanel>();
            if (title == null) { Debug.LogError("[PortingFinish2] (Q) TitlePanel not found"); return; }

            // 자식 "MascotSelector" 보장 (단순 시각 — 잠금 상태 표시. 실제 cycling 은 차후).
            Transform sel = null;
            foreach (Transform child in title.transform) if (child.name == "MascotSelector") { sel = child; break; }
            if (sel != null)
            {
                Debug.Log("[PortingFinish2] (Q) MascotSelector already exists — skip");
                return;
            }

            var root = new GameObject("MascotSelector", typeof(RectTransform));
            root.transform.SetParent(title.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 200f);
            rootRt.sizeDelta = new Vector2(720f, 140f);

            string[] ids = { "albatross", "kongming", "snowrabbit", "reaper", "seraphin" };
            const float w = 120f, gap = 20f;
            float startX = -((w + gap) * (ids.Length - 1)) * 0.5f;
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var go = new GameObject($"Portrait_{id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(startX + i * (w + gap), 0f);
                rt.sizeDelta = new Vector2(w, w);
                var img = go.GetComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Portraits/{id}.png");
                img.preserveAspect = true;
                img.raycastTarget = false;
                // 첫 번째 (albatross, unlocked) 외에는 어둡게 — locked 표시 (placeholder).
                img.color = i == 0 ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            Debug.Log("[PortingFinish2] (Q) MascotSelector created (5 portraits, only albatross unlocked visual)");
        }

        // ============ R. Mute settings (간단 토글) ============
        // PauseOverlay 또는 Canvas 의 자식으로 MuteButton 추가. UnityAudio.SetBgmMuted/SetSfxMuted 호출.
        [MenuItem("Arkanoid/Porting Finish/R. Mute Settings UI")]
        public static void CreateMuteSettingsUI()
        {
            // Canvas 찾기.
            var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            Canvas root = null;
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (UnityEditor.EditorUtility.IsPersistent(c.transform.root.gameObject)) continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera) { root = c; break; }
            }
            if (root == null) { Debug.LogWarning("[PortingFinish2] (R) No Canvas found — skip"); return; }

            // 이미 있는지 검사
            foreach (Transform t in root.transform)
            {
                if (t.name == "MuteButton") { Debug.Log("[PortingFinish2] (R) MuteButton already exists — skip"); return; }
            }

            var go = new GameObject("MuteButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(root.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(120f, 60f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // 라벨 자식 (TMP)
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labRt = (RectTransform)labelGo.transform;
            labRt.anchorMin = Vector2.zero;
            labRt.anchorMax = Vector2.one;
            labRt.offsetMin = Vector2.zero;
            labRt.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "MUTE";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
            tmp.color = Color.white;

            // OnClick → UnityAudio 토글. 직접 PersistentCall 등록은 복잡 — 별도 MuteToggle 컴포넌트 만들기.
            var muter = go.AddComponent<MuteToggleHook>();
            var ua = FindFirstOfType<UnityAudio>();
            if (ua != null)
            {
                var so = new SerializedObject(muter);
                so.FindProperty("audioRef").objectReferenceValue = ua;
                so.FindProperty("label").objectReferenceValue = tmp;
                so.ApplyModifiedProperties();
            }
            var btn = go.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, muter.Toggle);

            Debug.Log("[PortingFinish2] (R) MuteButton created (top-right)");
        }

        // ============ S. InputActions asset (간단) ============
        [MenuItem("Arkanoid/Porting Finish/S. InputActions Asset")]
        public static void EnsureInputActionsAsset()
        {
            // InputSystem_Actions.inputactions 이미 Assets 루트에 존재 — 이동 + 검증만.
            var src = "Assets/InputSystem_Actions.inputactions";
            var dst = "Assets/Input/ArkanoidInput.inputactions";

            if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
            {
                Debug.Log("[PortingFinish2] (S) ArkanoidInput.inputactions already exists — skip");
                return;
            }
            if (!System.IO.Directory.Exists("Assets/Input"))
            {
                AssetDatabase.CreateFolder("Assets", "Input");
            }
            if (AssetDatabase.LoadAssetAtPath<Object>(src) != null)
            {
                // copy instead of move (원본은 default 자산이므로 유지).
                AssetDatabase.CopyAsset(src, dst);
                Debug.Log("[PortingFinish2] (S) Copied InputSystem_Actions → Input/ArkanoidInput.inputactions");
            }
            else
            {
                // 빈 .inputactions 파일 생성 (Unity 가 import 시 빈 IndependentAction map).
                var empty = "{\n  \"name\": \"ArkanoidInput\",\n  \"maps\": [],\n  \"controlSchemes\": []\n}";
                System.IO.File.WriteAllText(dst, empty);
                AssetDatabase.Refresh();
                Debug.Log("[PortingFinish2] (S) Created empty ArkanoidInput.inputactions");
            }
        }

        // ============ Helpers ============
        private static T FindFirstOfType<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => !UnityEditor.EditorUtility.IsPersistent(c.transform.root.gameObject)
                                     && !(c.hideFlags == HideFlags.NotEditable || c.hideFlags == HideFlags.HideAndDontSave));
        }
    }
}

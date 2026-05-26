// 새 자산 (V2) 적용 + UX fix batch 3.
// 기존 Finish/Finish2 와 별도 파일. Run All 3 한 번이면 끝.
//
// 항목 (T~Z + 2):
//   T. V2 sprite import settings (PPU=1, Point, Uncompressed) + 4frames sprite sheet slice
//   U. Sprite rename — V2 자산을 우리 명명 규칙에 맞게 복사 (Assets/Sprites/V2Resolved/ 로)
//   V. MascotRenderer 5종 4 frame 재배선 (V2 frames)
//   W. TitlePanel mascot portraits 5종 wire (cycling/unlock)
//   X. Bar/Block/Border/Door V2 sprite swap
//   Y. InGamePanel background image 추가 + RoundIntroPanel 도 동일 BG
//   Z. IntroStoryPanel illustration sizeDelta 화면 전체 + body backdrop 추가
//   AA. GameOver/GameClear 마스코트 sprite swap (V2 단일 또는 4 frame)
//   AB. Slider track/knob image (단순 시각 — 동작은 PointerToPlayfield)
//   AC. Title 음소거 버튼 위치/크기 조정 (Title 에서는 hide, InGame Pause 에서 표시)
//   AD. RoundIntroPanel 배경 빈 영역 fix — Round/Ready label 위치 보정

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    public static class ArkanoidPortingFinish3
    {
        // ─── V2 자산 경로 상수 ───
        private const string V2 = "Assets/Sprites/V2";
        private const string V2_OUT = "Assets/Sprites/V2Resolved";   // 우리 규칙으로 복사된 자산 (선택)

        [MenuItem("Arkanoid/Porting Finish/Run All 3 (T-AD)")]
        public static void RunAll3()
        {
            ApplyV2ImportSettings();        // T
            SliceV2SpriteSheets();          // T2 — 4×1 grid slice
            WireMascotV2Frames();           // V
            WireTitlePortraitsV2();          // W
            SwapV2BlockBarBorder();         // X
            EnsureScreenBackgrounds();      // Y
            FixIntroStoryLayout();          // Z
            SwapGameOverClearMascot();      // AA
            EnsureSliderTrackKnob();        // AB
            FixMuteButtonPlacement();       // AC
            FixRoundIntroLabelPlacement();  // AD

            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PortingFinish3] Run All 3 done.");
        }

        // ============ T. V2 import settings ============
        [MenuItem("Arkanoid/Porting Finish/T1. V2 Import Settings (PPU=1, Point)")]
        public static void ApplyV2ImportSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { V2 });
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
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (importer.alphaIsTransparency != true) { importer.alphaIsTransparency = true; changed = true; }
                if (changed) { importer.SaveAndReimport(); updated++; }
            }
            Debug.Log($"[PortingFinish3] (T1) V2 sprite settings updated: {updated}");
        }

        // ============ T2. Sprite sheet slice (4×1 grid) ============
        // V2/<id>_4frames.png 형식의 PNG 들을 4×1 grid 로 자동 slice.
        // 슬라이스 결과 sprite 들의 이름: <id>_4frames_0, _1, _2, _3.
        [MenuItem("Arkanoid/Porting Finish/T2. Slice V2 Sprite Sheets")]
        public static void SliceV2SpriteSheets()
        {
            string[] sheets =
            {
                $"{V2}/albatross_4frames.png",
                $"{V2}/snowrabbit_4frames.png",
                $"{V2}/reaper_4frames.png",
                $"{V2}/seraphin_4frames.png",
                $"{V2}/hamster_4frames.png",
                $"{V2}/4frames/albatross_4frames-removebg-preview.png",
                $"{V2}/4frames/snowrabbit_4frames-removebg-preview.png",
                $"{V2}/4frames/reaper_4frames-removebg-preview.png",
                $"{V2}/4frames/seraphin_4frames-removebg-preview.png",
                $"{V2}/4frames/hamster_4frames-removebg-preview.png",
            };
            int sliced = 0;
            foreach (var path in sheets)
            {
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) continue;
                if (SliceAs4x1(path)) sliced++;
            }
            Debug.Log($"[PortingFinish3] (T2) sliced {sliced} sprite sheets (4×1)");
        }

        private static bool SliceAs4x1(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return false;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) return false;

            // readability 임시 켜기 — 굳이 필요 없지만 isReadable=true 가 안전.
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 1f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            int w = tex.width, h = tex.height;
            int cellW = w / 4;
            int cellH = h;
            var name = Path.GetFileNameWithoutExtension(assetPath);
            var metas = new List<SpriteMetaData>();
            for (int i = 0; i < 4; i++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"{name}_{i}",
                    rect = new Rect(i * cellW, 0, cellW, cellH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
            }
            // 신 API
#pragma warning disable CS0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return true;
        }

        // ============ V. MascotRenderer 5종 V2 frames 재배선 ============
        [MenuItem("Arkanoid/Porting Finish/V. MascotRenderer V2 frames")]
        public static void WireMascotV2Frames()
        {
            var mascot = FindFirstOfType<MascotRenderer>();
            if (mascot == null) { Debug.LogError("[PortingFinish3] (V) MascotRenderer not found"); return; }

            var so = new SerializedObject(mascot);
            var entriesProp = so.FindProperty("mascotSprites");

            // 5종 매핑 — V2 의 분리 frame 또는 slice 된 sprite sheet.
            var entries = new (string id, Sprite[] frames)[]
            {
                ("albatross", LoadAlbatrossFrames()),
                ("kongming", LoadKongmingFrames()),
                ("snowrabbit", LoadFramesFromSheet($"{V2}/snowrabbit_4frames.png", "snowrabbit_4frames")),
                ("reaper", LoadFramesFromSheet($"{V2}/reaper_4frames.png", "reaper_4frames")),
                ("seraphin", LoadFramesFromSheet($"{V2}/seraphin_4frames.png", "seraphin_4frames")),
            };

            entriesProp.arraySize = entries.Length;
            int missing = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var (id, frames) = entries[i];
                var ep = entriesProp.GetArrayElementAtIndex(i);
                ep.FindPropertyRelative("MascotId").stringValue = id;
                ep.FindPropertyRelative("Frame0").objectReferenceValue = frames != null && frames.Length > 0 ? frames[0] : null;
                ep.FindPropertyRelative("Frame1").objectReferenceValue = frames != null && frames.Length > 1 ? frames[1] : null;
                ep.FindPropertyRelative("Frame2").objectReferenceValue = frames != null && frames.Length > 2 ? frames[2] : null;
                ep.FindPropertyRelative("Frame3").objectReferenceValue = frames != null && frames.Length > 3 ? frames[3] : null;
                if (frames == null || frames.Any(f => f == null)) { Debug.LogWarning($"[PortingFinish3] (V) {id}: missing frame(s)"); missing++; }
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mascot);
            Debug.Log($"[PortingFinish3] (V) MascotRenderer wired (V2 frames) — {entries.Length} mascots, {missing} with missing");
        }

        private static Sprite[] LoadAlbatrossFrames()
        {
            // 우선 dance_frame_1~4 (이미 분리), 없으면 sliced 4frames.
            var dance = new Sprite[4];
            bool allOk = true;
            for (int i = 0; i < 4; i++)
            {
                dance[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/4frames/dance_frame_{i + 1}.png");
                if (dance[i] == null) { allOk = false; break; }
            }
            if (allOk) return dance;
            return LoadFramesFromSheet($"{V2}/albatross_4frames.png", "albatross_4frames");
        }

        private static Sprite[] LoadKongmingFrames()
        {
            var arr = new Sprite[4];
            for (int i = 0; i < 4; i++)
                arr[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/kongming_frame{i + 1}.png");
            // 미존재 시 hamster_4frames sheet fallback.
            if (arr.Any(s => s == null))
                return LoadFramesFromSheet($"{V2}/hamster_4frames.png", "hamster_4frames");
            return arr;
        }

        // sliced sprite sheet → 4장 sub-sprite 로드.
        private static Sprite[] LoadFramesFromSheet(string assetPath, string namePrefix)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var frames = new Sprite[4];
            foreach (var o in all)
            {
                if (o is Sprite sp)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (sp.name == $"{namePrefix}_{i}") frames[i] = sp;
                    }
                }
            }
            // fallback: 단일 sprite (slice 안 된 경우) → 0번 만 채움.
            if (frames.All(f => f == null))
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (single != null) { frames[0] = single; frames[1] = single; frames[2] = single; frames[3] = single; }
            }
            return frames;
        }

        // ============ W. TitlePanel — 5종 portrait + 4 frame anim wire ============
        [MenuItem("Arkanoid/Porting Finish/W. Title Mascot Portraits + Cycling")]
        public static void WireTitlePortraitsV2()
        {
            var title = FindFirstOfType<TitlePanel>();
            if (title == null) { Debug.LogError("[PortingFinish3] (W) TitlePanel not found"); return; }

            // MascotSelector container 보장.
            Transform sel = null;
            foreach (Transform child in title.transform)
                if (child.name == "MascotSelector") { sel = child; break; }
            if (sel == null)
            {
                var root = new GameObject("MascotSelector", typeof(RectTransform));
                root.transform.SetParent(title.transform, false);
                sel = root.transform;
            }
            var selRt = (RectTransform)sel;
            selRt.anchorMin = new Vector2(0.5f, 0f);
            selRt.anchorMax = new Vector2(0.5f, 0f);
            selRt.pivot = new Vector2(0.5f, 0f);
            selRt.anchoredPosition = new Vector2(0f, 240f);
            selRt.sizeDelta = new Vector2(880f, 200f);

            // 5종 매핑 — id, portrait V2 파일, displayName, unlockCost, animFrames.
            var defs = new (string id, string portrait, string display, int cost)[]
            {
                ("albatross", $"{V2}/해금_알바트로스2.png", "ALBATROSS", 0),
                ("kongming",  $"{V2}/해금_햄스터2.png",   "KONGMING",   100),
                ("snowrabbit", $"{V2}/해금_눈토끼2.png",  "SNOW RABBIT", 300),
                ("reaper",    $"{V2}/해금_사신2.png",     "REAPER",     600),
                ("seraphin",  $"{V2}/해금_세라핀2.png",   "SERAPHIN",   1000),
            };

            // 기존 자식 portrait 정리 — Portrait_<id> 와 라벨 외에 다 보존.
            foreach (Transform child in sel)
            {
                if (child.name.StartsWith("Portrait_")) Object.DestroyImmediate(child.gameObject);
            }

            const float w = 140f, gap = 22f;
            float startX = -((w + gap) * (defs.Length - 1)) * 0.5f;
            var portraits = new TitlePanel.MascotPortrait[defs.Length];

            for (int i = 0; i < defs.Length; i++)
            {
                var (id, portraitPath, display, cost) = defs[i];
                var go = new GameObject($"Portrait_{id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(sel, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(startX + i * (w + gap), 0f);
                rt.sizeDelta = new Vector2(w, w);
                var img = go.GetComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                if (img.sprite == null) Debug.LogWarning($"[PortingFinish3] (W) portrait missing: {portraitPath}");
                img.preserveAspect = true;
                img.raycastTarget = false;

                Sprite[] frames = id switch
                {
                    "albatross" => LoadAlbatrossFrames(),
                    "kongming" => LoadKongmingFrames(),
                    "snowrabbit" => LoadFramesFromSheet($"{V2}/snowrabbit_4frames.png", "snowrabbit_4frames"),
                    "reaper" => LoadFramesFromSheet($"{V2}/reaper_4frames.png", "reaper_4frames"),
                    "seraphin" => LoadFramesFromSheet($"{V2}/seraphin_4frames.png", "seraphin_4frames"),
                    _ => null,
                };
                portraits[i] = new TitlePanel.MascotPortrait
                {
                    MascotId = id,
                    Portrait = img,
                    UnlockCost = cost,
                    DisplayName = display,
                    AnimFrames = frames,
                };
            }

            // mascotName / mascotCost / gold / unlockButton 보장. TMP MissingReference 가능 — try-catch.
            TMP_Text nameText = null, costText = null, goldText = null;
            Button unlockBtn = null;
            try { nameText = EnsureChildTmp(sel, "MascotNameText", new Vector2(0f, -110f), 32); }
            catch (System.Exception ex) { Debug.LogWarning($"[PortingFinish3] (W) nameText create skipped: {ex.GetType().Name}"); }
            try { costText = EnsureChildTmp(sel, "MascotCostText", new Vector2(0f, -150f), 24); }
            catch (System.Exception ex) { Debug.LogWarning($"[PortingFinish3] (W) costText create skipped: {ex.GetType().Name}"); }
            try { goldText = EnsureChildTmp(sel, "GoldText", new Vector2(0f, 110f), 24); }
            catch (System.Exception ex) { Debug.LogWarning($"[PortingFinish3] (W) goldText create skipped: {ex.GetType().Name}"); }
            try { unlockBtn = EnsureUnlockButton(sel); }
            catch (System.Exception ex) { Debug.LogWarning($"[PortingFinish3] (W) unlockBtn create skipped: {ex.GetType().Name}"); }

            // mascotImage (작은 미리보기 — 4 frame 댄스) — 사용자 보고: 중앙에 안 나와야 함 → 우측 상단으로 이동.
            Transform mascotImageT = null;
            foreach (Transform child in title.transform)
                if (child.name == "MascotImage") { mascotImageT = child; break; }
            Image mascotImg = null;
            if (mascotImageT != null)
            {
                mascotImg = mascotImageT.GetComponent<Image>();
                var rt = (RectTransform)mascotImageT;
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-30f, -30f);
                rt.sizeDelta = new Vector2(160f, 160f);
            }

            // TitlePanel SerializedObject 에 wire.
            var soTitle = new SerializedObject(title);
            soTitle.FindProperty("mascotImage").objectReferenceValue = mascotImg;
            // mascotFrames 는 albatross 기본.
            var mfArr = soTitle.FindProperty("mascotFrames");
            var defaultFrames = LoadAlbatrossFrames();
            mfArr.arraySize = 4;
            for (int i = 0; i < 4; i++)
                mfArr.GetArrayElementAtIndex(i).objectReferenceValue = defaultFrames[i];

            // portraits 배열.
            var pArr = soTitle.FindProperty("portraits");
            pArr.arraySize = portraits.Length;
            for (int i = 0; i < portraits.Length; i++)
            {
                var ep = pArr.GetArrayElementAtIndex(i);
                ep.FindPropertyRelative("MascotId").stringValue = portraits[i].MascotId;
                ep.FindPropertyRelative("Portrait").objectReferenceValue = portraits[i].Portrait;
                ep.FindPropertyRelative("UnlockCost").intValue = portraits[i].UnlockCost;
                ep.FindPropertyRelative("DisplayName").stringValue = portraits[i].DisplayName;
                var afArr = ep.FindPropertyRelative("AnimFrames");
                afArr.arraySize = 4;
                for (int j = 0; j < 4; j++)
                    afArr.GetArrayElementAtIndex(j).objectReferenceValue =
                        portraits[i].AnimFrames != null && j < portraits[i].AnimFrames.Length ? portraits[i].AnimFrames[j] : null;
            }
            var mnt = soTitle.FindProperty("mascotNameText");
            if (mnt != null && nameText != null) mnt.objectReferenceValue = nameText;
            var mct = soTitle.FindProperty("mascotCostText");
            if (mct != null && costText != null) mct.objectReferenceValue = costText;
            var gt = soTitle.FindProperty("goldText");
            if (gt != null && goldText != null) gt.objectReferenceValue = goldText;
            var ubt = soTitle.FindProperty("unlockButton");
            if (ubt != null && unlockBtn != null) ubt.objectReferenceValue = unlockBtn;
            soTitle.ApplyModifiedProperties();
            EditorUtility.SetDirty(title);

            // [NORMAL] / [HARD] 표시 — difficultyText 위치/크기 fix.
            FixDifficultyTextPlacement(title);

            Debug.Log("[PortingFinish3] (W) Title MascotSelector 5 portraits wired + cycling/unlock");
        }

        private static TMP_Text EnsureChildTmp(Transform parent, string name, Vector2 anchorPos, int fontSize)
        {
            Transform t = null;
            foreach (Transform c in parent) if (c.name == name) { t = c; break; }
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(parent, false);
                t = go.transform;
                // TMP AddComponent 가 손상된 fontAsset 참조로 throw 할 수 있음 — 명시적 fontAsset 후 add.
                AddTmpSafely(go);
            }
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchorPos;
            rt.sizeDelta = new Vector2(500f, 60f);
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) AddTmpSafely(t.gameObject);
            tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return null;
            // 명시적으로 안정된 fontAsset 할당 (defaultFontAsset 미손상 시).
            var fa = ResolveSafeFontAsset();
            if (fa != null) tmp.font = fa;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 220);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddTmpSafely(GameObject go)
        {
            try { go.AddComponent<TextMeshProUGUI>(); }
            catch (System.Exception ex) { Debug.LogWarning($"[PortingFinish3] AddTmp throw: {ex.GetType().Name} — retry once"); try { go.AddComponent<TextMeshProUGUI>(); } catch { } }
        }

        private static TMP_FontAsset _cachedFont;
        private static TMP_FontAsset ResolveSafeFontAsset()
        {
            if (_cachedFont != null) return _cachedFont;
            // 우선 DNFBitBitv2 SDF, 없으면 MalgunGothic SDF, 없으면 TMP_Settings.defaultFontAsset.
            _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DNFBitBitv2 SDF.asset")
                ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/MalgunGothic SDF.asset")
                ?? TMP_Settings.defaultFontAsset;
            return _cachedFont;
        }

        private static Button EnsureUnlockButton(Transform parent)
        {
            Transform t = null;
            foreach (Transform c in parent) if (c.name == "UnlockButton") { t = c; break; }
            if (t == null)
            {
                var go = new GameObject("UnlockButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                t = go.transform;
                var labGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                labGo.transform.SetParent(go.transform, false);
                AddTmpSafely(labGo);
                var labRt = (RectTransform)labGo.transform;
                labRt.anchorMin = Vector2.zero;
                labRt.anchorMax = Vector2.one;
                labRt.offsetMin = Vector2.zero;
                labRt.offsetMax = Vector2.zero;
                var lab = labGo.GetComponent<TextMeshProUGUI>();
                if (lab != null)
                {
                    var fa = ResolveSafeFontAsset();
                    if (fa != null) lab.font = fa;
                    lab.text = "UNLOCK";
                    lab.alignment = TextAlignmentOptions.Center;
                    lab.fontSize = 22;
                    lab.color = Color.white;
                }
            }
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -190f);
            rt.sizeDelta = new Vector2(180f, 50f);
            var img = t.GetComponent<Image>();
            img.color = new Color(0.15f, 0.4f, 0.7f, 0.9f);
            return t.GetComponent<Button>();
        }

        private static void FixDifficultyTextPlacement(TitlePanel title)
        {
            // TitlePanel 의 difficultyText 가 보이도록 — 화면 가운데 위쪽 (HighScore 아래).
            var so = new SerializedObject(title);
            var prop = so.FindProperty("difficultyText");
            if (prop == null || prop.objectReferenceValue == null)
            {
                // 자동 탐색 — 자식 중 "DifficultyText" 이름.
                Transform dt = null;
                foreach (Transform c in title.transform)
                    if (c.name == "DifficultyText" || c.name.ToLowerInvariant().Contains("difficulty")) { dt = c; break; }
                if (dt == null)
                {
                    var go = new GameObject("DifficultyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    go.transform.SetParent(title.transform, false);
                    dt = go.transform;
                }
                var tmp = dt.GetComponent<TextMeshProUGUI>() ?? dt.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = "[NORMAL]";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 48;
                tmp.color = new Color(1f, 0.95f, 0.3f, 1f);
                tmp.outlineWidth = 0.3f;
                tmp.outlineColor = new Color32(0, 0, 0, 230);
                tmp.raycastTarget = false;
                prop.objectReferenceValue = tmp;
            }
            var dTmp = prop.objectReferenceValue as TMP_Text;
            if (dTmp != null)
            {
                var rt = dTmp.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -200f);
                rt.sizeDelta = new Vector2(500f, 80f);
                dTmp.fontSize = 48;
                dTmp.alignment = TextAlignmentOptions.Center;
                dTmp.color = new Color(1f, 0.95f, 0.3f, 1f);
                dTmp.outlineWidth = 0.3f;
                dTmp.outlineColor = new Color32(0, 0, 0, 230);
                EditorUtility.SetDirty(dTmp);
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(title);
        }

        // ============ X. Bar/Block/Border/Door V2 sprite swap ============
        [MenuItem("Arkanoid/Porting Finish/X. Bar/Block/Border/Door V2 swap")]
        public static void SwapV2BlockBarBorder()
        {
            // BarRenderer — bar_normal 로 swap.
            var bar = FindFirstOfType<BarRenderer>();
            if (bar != null)
            {
                var so = new SerializedObject(bar);
                var bodyProp = so.FindProperty("body");
                if (bodyProp != null && bodyProp.objectReferenceValue is SpriteRenderer body)
                {
                    var sp = LoadFirstExisting(
                        $"{V2}/bar_sprites/public/assets/sprites/bar_normal.png",
                        "Assets/Sprites/Gameplay/bar_normal.png");
                    if (sp != null) body.sprite = sp;
                    EditorUtility.SetDirty(body);
                }
                so.ApplyModifiedProperties();
                Debug.Log("[PortingFinish3] (X) BarRenderer body sprite = bar_normal");
            }

            // BordersRenderer — V2 의 borders 로 swap.
            var bordersComp = FindFirstOfType<BordersRenderer>();
            if (bordersComp != null)
            {
                var so = new SerializedObject(bordersComp);
                SetIfNonNull(so, "verticalSprite",
                    LoadFirstExisting($"{V2}/border_door_sprites/public/assets/sprites/borders/border_vertical.png",
                                       "Assets/Sprites/Borders/border_vertical.png"));
                SetIfNonNull(so, "horizontalSprite",
                    LoadFirstExisting($"{V2}/border_door_sprites/public/assets/sprites/borders/border_horizontal.png",
                                       "Assets/Sprites/Borders/border_horizontal.png"));
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(bordersComp);
                Debug.Log("[PortingFinish3] (X) BordersRenderer V2 sprites wired");
            }

            // DoorsRenderer — V2 5 frame swap.
            var doorsComp = FindFirstOfType<DoorsRenderer>();
            if (doorsComp != null)
            {
                var so = new SerializedObject(doorsComp);
                SetIfNonNull(so, "closedSprite",
                    LoadFirstExisting($"{V2}/border_door_sprites/public/assets/sprites/borders/door_closed.png",
                                       "Assets/Sprites/Borders/door_closed.png"));
                var fp = so.FindProperty("openingFrames");
                fp.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    var s = LoadFirstExisting(
                        $"{V2}/border_door_sprites/public/assets/sprites/borders/door_opening_frame{i}.png",
                        $"Assets/Sprites/Borders/door_opening_frame{i}.png");
                    fp.GetArrayElementAtIndex(i).objectReferenceValue = s;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(doorsComp);
                Debug.Log("[PortingFinish3] (X) DoorsRenderer V2 5 frames wired");
            }

            // BlocksRenderer — V2 block sprites 사용 (없을 수 있으니 best-effort).
            // ItemsRenderer — V2 items 로 swap.
            var items = FindFirstOfType<ItemsRenderer>();
            if (items != null)
            {
                var so = new SerializedObject(items);
                SetIfNonNull(so, "expandSprite", LoadFirstExisting(
                    $"{V2}/ball_item_sprites/public/assets/sprites/item_expand.png",
                    $"{V2}/items/item_expand.png",
                    "Assets/Sprites/Gameplay/item_expand.png"));
                SetIfNonNull(so, "magnetSprite", LoadFirstExisting(
                    $"{V2}/ball_item_sprites/public/assets/sprites/item_magnet.png",
                    $"{V2}/items/item_magnet.png",
                    "Assets/Sprites/Gameplay/item_magnet.png"));
                SetIfNonNull(so, "laserSprite", LoadFirstExisting(
                    $"{V2}/ball_item_sprites/public/assets/sprites/item_laser.png",
                    $"{V2}/items/item_laser.png",
                    "Assets/Sprites/Gameplay/item_laser.png"));
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(items);
                Debug.Log("[PortingFinish3] (X) ItemsRenderer V2 sprites wired");
            }
        }

        // ============ Y. Screen backgrounds ============
        // 각 Panel 에 BG Image 자식 추가 (sibling index 0 — 가장 뒤).
        [MenuItem("Arkanoid/Porting Finish/Y. Screen Backgrounds")]
        public static void EnsureScreenBackgrounds()
        {
            // Title
            var title = FindFirstOfType<TitlePanel>();
            if (title != null)
                EnsureBackgroundChild(title.transform, "Background", "Assets/Sprites/Backgrounds/bg_title.png");

            // InGamePanel — bg_stage_01 기본.
            var inGame = FindFirstOfType<InGamePanel>();
            if (inGame != null)
                EnsureBackgroundChild(inGame.transform, "Background", "Assets/Sprites/Backgrounds/bg_stage_01.png");

            // RoundIntroPanel — 동일 stage BG.
            var ri = FindFirstOfType<RoundIntroPanel>();
            if (ri != null)
                EnsureBackgroundChild(ri.transform, "Background", "Assets/Sprites/Backgrounds/bg_stage_01.png");

            // IntroStory — bg_pixel_01 (스토리 분위기).
            var intro = FindFirstOfType<IntroStoryPanel>();
            if (intro != null)
                EnsureBackgroundChild(intro.transform, "Background", "Assets/Sprites/Backgrounds/bg_pixel_01.png");

            // GameOver / GameClear
            var gOver = FindFirstOfType<GameOverPanel>();
            if (gOver != null)
                EnsureBackgroundChild(gOver.transform, "Background", "Assets/Sprites/Backgrounds/bg_gameover.png");

            var gClear = FindFirstOfType<GameClearPanel>();
            if (gClear != null)
                EnsureBackgroundChild(gClear.transform, "Background", "Assets/Sprites/Backgrounds/bg_gameclear.png");

            Debug.Log("[PortingFinish3] (Y) Backgrounds wired across panels");
        }

        private static void EnsureBackgroundChild(Transform parent, string name, string spritePath)
        {
            Transform t = null;
            foreach (Transform c in parent) if (c.name == name) { t = c; break; }
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            // 가장 뒤로 (자식 0).
            t.SetSiblingIndex(0);
            var rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null) Debug.LogWarning($"[PortingFinish3] (Y) BG sprite missing: {spritePath}");
            img.sprite = sp;
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.color = Color.white;
            EditorUtility.SetDirty(img);
        }

        // ============ Z. IntroStory layout fix ============
        [MenuItem("Arkanoid/Porting Finish/Z. IntroStory Layout Fix")]
        public static void FixIntroStoryLayout()
        {
            var intro = FindFirstOfType<IntroStoryPanel>();
            if (intro == null) { Debug.LogError("[PortingFinish3] (Z) IntroStoryPanel not found"); return; }

            // Illustration 자식 — 화면 전체 비율로 확대 (top half) + Body backdrop 추가.
            Transform illusT = null;
            foreach (Transform c in intro.transform)
                if (c.name == "Illustration" || c.name == "illustration") { illusT = c; break; }
            if (illusT != null)
            {
                var rt = (RectTransform)illusT;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 250f);
                rt.sizeDelta = new Vector2(1000f, 800f);  // 화면 전체 ~ 상단 80%
                var img = illusT.GetComponent<Image>();
                if (img != null) img.preserveAspect = true;
            }

            // BodyText backdrop — 같은 부모에 반투명 Image 형제 (BodyText 뒤로).
            // BodyText 찾기.
            TMP_Text bodyText = null;
            foreach (Transform c in intro.transform)
            {
                var tmp = c.GetComponent<TMP_Text>();
                if (tmp != null && (c.name == "BodyText" || c.name.ToLowerInvariant().Contains("body"))) { bodyText = tmp; break; }
            }
            if (bodyText != null)
            {
                Transform backdrop = null;
                foreach (Transform c in intro.transform)
                    if (c.name == "BodyBackdrop") { backdrop = c; break; }
                if (backdrop == null)
                {
                    var go = new GameObject("BodyBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(intro.transform, false);
                    backdrop = go.transform;
                }
                // BodyText 의 RectTransform 영역 + 약간 패딩.
                var bRt = (RectTransform)backdrop;
                var tRt = bodyText.rectTransform;
                bRt.anchorMin = tRt.anchorMin;
                bRt.anchorMax = tRt.anchorMax;
                bRt.pivot = tRt.pivot;
                bRt.anchoredPosition = tRt.anchoredPosition;
                bRt.sizeDelta = tRt.sizeDelta + new Vector2(60f, 40f);
                var img = backdrop.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.6f);
                img.raycastTarget = false;
                // BodyText 보다 뒤로.
                backdrop.SetSiblingIndex(Mathf.Max(0, bodyText.transform.GetSiblingIndex() - 1));
                if (bodyText.transform.GetSiblingIndex() <= backdrop.GetSiblingIndex())
                {
                    bodyText.transform.SetSiblingIndex(backdrop.GetSiblingIndex() + 1);
                }
                EditorUtility.SetDirty(img);
            }
            EditorUtility.SetDirty(intro);
            Debug.Log("[PortingFinish3] (Z) IntroStory illustration enlarged + body backdrop added");
        }

        // ============ AA. GameOver / GameClear mascot V2 swap ============
        [MenuItem("Arkanoid/Porting Finish/AA. GameOver/Clear Mascot V2")]
        public static void SwapGameOverClearMascot()
        {
            var gOver = FindFirstOfType<GameOverPanel>();
            if (gOver != null)
            {
                Transform mImg = null;
                foreach (Transform c in gOver.transform) if (c.name == "MascotImage") { mImg = c; break; }
                if (mImg == null)
                {
                    var go = new GameObject("MascotImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(gOver.transform, false);
                    mImg = go.transform;
                }
                var rt = (RectTransform)mImg;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 50f);
                rt.sizeDelta = new Vector2(600f, 600f);
                var img = mImg.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                // V2/gameover_albatros.png 단일.
                var single = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/gameover_albatros.png");
                if (single != null) img.sprite = single;
                else Debug.LogWarning("[PortingFinish3] (AA) gameover_albatros.png missing");

                // 4 frame anim 도 가능 — V2/4frames/gameover_frame_1~4.
                var frames = new Sprite[4];
                bool anyFrame = false;
                for (int i = 0; i < 4; i++)
                {
                    frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/4frames/gameover_frame_{i + 1}.png");
                    if (frames[i] != null) anyFrame = true;
                }
                if (anyFrame && single == null) img.sprite = frames[0];

                var so = new SerializedObject(gOver);
                so.FindProperty("mascotImage").objectReferenceValue = img;
                var arr = so.FindProperty("mascotFrames");
                arr.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i] ?? single;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gOver);
                Debug.Log("[PortingFinish3] (AA) GameOver mascot V2 (single + 4 frames)");
            }

            var gClear = FindFirstOfType<GameClearPanel>();
            if (gClear != null)
            {
                Transform mImg = null;
                foreach (Transform c in gClear.transform) if (c.name == "MascotImage") { mImg = c; break; }
                if (mImg == null)
                {
                    var go = new GameObject("MascotImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(gClear.transform, false);
                    mImg = go.transform;
                }
                var rt = (RectTransform)mImg;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 50f);
                rt.sizeDelta = new Vector2(600f, 600f);
                var img = mImg.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                var sp = LoadFirstExisting(
                    $"{V2}/gameclear_albatros.png",
                    $"{V2}/4frames/gameclear_albatros.png",
                    $"{V2}/4frames/gameclear_albatros2.png");
                if (sp != null) img.sprite = sp;
                else Debug.LogWarning("[PortingFinish3] (AA) gameclear_albatros.png missing");

                EditorUtility.SetDirty(gClear);
                Debug.Log("[PortingFinish3] (AA) GameClear mascot V2");
            }
        }

        // ============ AB. Slider track + knob (단순 시각) ============
        [MenuItem("Arkanoid/Porting Finish/AB. Slider Track/Knob")]
        public static void EnsureSliderTrackKnob()
        {
            var inGame = FindFirstOfType<InGamePanel>();
            if (inGame == null) { Debug.LogError("[PortingFinish3] (AB) InGamePanel not found"); return; }

            Transform sliderRoot = null;
            foreach (Transform c in inGame.transform) if (c.name == "BarSlider") { sliderRoot = c; break; }
            if (sliderRoot == null)
            {
                var go = new GameObject("BarSlider", typeof(RectTransform));
                go.transform.SetParent(inGame.transform, false);
                sliderRoot = go.transform;
            }
            var sRt = (RectTransform)sliderRoot;
            // 하단 가로 배치 (Bar 위치 컨트롤 영역 — 시각 only, 실제 입력은 PointerToPlayfield).
            sRt.anchorMin = new Vector2(0.5f, 0f);
            sRt.anchorMax = new Vector2(0.5f, 0f);
            sRt.pivot = new Vector2(0.5f, 0f);
            sRt.anchoredPosition = new Vector2(0f, 60f);
            sRt.sizeDelta = new Vector2(700f, 60f);

            // Track Image.
            Transform trackT = null;
            foreach (Transform c in sliderRoot) if (c.name == "Track") { trackT = c; break; }
            if (trackT == null)
            {
                var go = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(sliderRoot, false);
                trackT = go.transform;
            }
            var tRt = (RectTransform)trackT;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var tImg = trackT.GetComponent<Image>();
            tImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/slider_track.png");
            if (tImg.sprite == null) Debug.LogWarning($"[PortingFinish3] (AB) slider_track.png missing");
            tImg.preserveAspect = false;
            tImg.raycastTarget = false;
            tImg.color = new Color(1f, 1f, 1f, 0.7f);
            tImg.type = Image.Type.Sliced;

            // Knob Image (center, 작은 사각).
            Transform knobT = null;
            foreach (Transform c in sliderRoot) if (c.name == "Knob") { knobT = c; break; }
            if (knobT == null)
            {
                var go = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(sliderRoot, false);
                knobT = go.transform;
            }
            var kRt = (RectTransform)knobT;
            kRt.anchorMin = new Vector2(0.5f, 0.5f);
            kRt.anchorMax = new Vector2(0.5f, 0.5f);
            kRt.pivot = new Vector2(0.5f, 0.5f);
            kRt.anchoredPosition = new Vector2(0f, 0f);
            kRt.sizeDelta = new Vector2(80f, 80f);
            var kImg = knobT.GetComponent<Image>();
            kImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{V2}/slider_knob.png");
            if (kImg.sprite == null) Debug.LogWarning($"[PortingFinish3] (AB) slider_knob.png missing");
            kImg.preserveAspect = true;
            kImg.raycastTarget = false;

            Debug.Log("[PortingFinish3] (AB) Slider track+knob created (visual only — input uses PointerToPlayfield)");
        }

        // ============ AC. MuteButton placement fix ============
        [MenuItem("Arkanoid/Porting Finish/AC. MuteButton Placement")]
        public static void FixMuteButtonPlacement()
        {
            var muters = Resources.FindObjectsOfTypeAll<MuteToggleHook>();
            foreach (var m in muters)
            {
                if (m == null) continue;
                if (UnityEditor.EditorUtility.IsPersistent(m.transform.root.gameObject)) continue;
                var rt = m.GetComponent<RectTransform>();
                if (rt == null) continue;
                // 우상단 안쪽으로 살짝, 사이즈 확대.
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-40f, -40f);
                rt.sizeDelta = new Vector2(180f, 80f);
                var img = m.GetComponent<Image>();
                if (img != null) img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                var label = m.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.fontSize = 36;
                    label.color = Color.white;
                }
                EditorUtility.SetDirty(m.gameObject);
                Debug.Log($"[PortingFinish3] (AC) MuteButton placed top-right (180×80)");
            }
        }

        // ============ AD. RoundIntro Round/Ready label placement ============
        [MenuItem("Arkanoid/Porting Finish/AD. RoundIntro Label Placement")]
        public static void FixRoundIntroLabelPlacement()
        {
            var ri = FindFirstOfType<RoundIntroPanel>();
            if (ri == null) { Debug.LogError("[PortingFinish3] (AD) RoundIntroPanel not found"); return; }

            // Round / Ready label 자동 탐색 + 크게 + 중앙 배치.
            var so = new SerializedObject(ri);
            var rl = so.FindProperty("roundLabel").objectReferenceValue as TMP_Text;
            var ready = so.FindProperty("readyLabel").objectReferenceValue as TMP_Text;

            if (rl != null)
            {
                var rt = rl.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 100f);
                rt.sizeDelta = new Vector2(700f, 120f);
                rl.fontSize = 88;
                rl.alignment = TextAlignmentOptions.Center;
                rl.color = new Color(1f, 0.95f, 0.3f, 1f);
                rl.outlineWidth = 0.35f;
                rl.outlineColor = new Color32(0, 0, 0, 230);
                EditorUtility.SetDirty(rl);
            }
            if (ready != null)
            {
                var rt = ready.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -50f);
                rt.sizeDelta = new Vector2(500f, 100f);
                ready.fontSize = 64;
                ready.alignment = TextAlignmentOptions.Center;
                ready.color = Color.white;
                ready.outlineWidth = 0.3f;
                ready.outlineColor = new Color32(0, 0, 0, 230);
                EditorUtility.SetDirty(ready);
            }
            EditorUtility.SetDirty(ri);
            Debug.Log("[PortingFinish3] (AD) RoundIntro labels repositioned (center large)");
        }

        // ============ Helpers ============
        private static T FindFirstOfType<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => !UnityEditor.EditorUtility.IsPersistent(c.transform.root.gameObject)
                                     && !(c.hideFlags == HideFlags.NotEditable || c.hideFlags == HideFlags.HideAndDontSave));
        }

        private static Sprite LoadFirstExisting(params string[] paths)
        {
            foreach (var p in paths)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sp != null) return sp;
            }
            return null;
        }

        private static void SetIfNonNull(SerializedObject so, string propName, Object value)
        {
            if (value == null) return;
            var p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = value;
        }
    }
}

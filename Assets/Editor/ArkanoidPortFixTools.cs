using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ArkanoidPortFix
{
    public static class ArkanoidPortFixTools
    {
        // ── P2 본격: 폰트 SDF 동적 재생성 (Font Asset Creator GUI 없이) ──
        // 기존 DNFBitBitv2 SDF / MalgunGothic SDF 의 atlas 가 깨져서 글자 안 보임.
        // CreateFontAsset(Dynamic) 으로 새로 만들면 atlas 가 런타임에 자동으로 채워짐.
        private const string DnfDynPath = "Assets/Fonts/DNFBitBitv2 Dynamic SDF.asset";
        private const string MalgunDynPath = "Assets/Fonts/MalgunGothic Dynamic SDF.asset";

        [MenuItem("ArkanoidFix/0. Regenerate Dynamic SDF fonts + apply to all TMP")]
        public static void RegenerateDynamicFonts()
        {
            var dnfTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/DNFBitBitv2.ttf");
            var malgunTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/malgun.ttf");
            if (dnfTtf == null) { Debug.LogError("[ArkanoidFix] DNFBitBitv2.ttf not found"); return; }

            var dnf = MakeDynamicSdf(dnfTtf, "DNFBitBitv2 Dynamic SDF", DnfDynPath, 90, 9, 1024, 1024);
            if (dnf == null) return;

            TMP_FontAsset malgun = null;
            if (malgunTtf != null)
            {
                malgun = MakeDynamicSdf(malgunTtf, "MalgunGothic Dynamic SDF", MalgunDynPath, 80, 9, 2048, 2048);
                if (malgun != null)
                {
                    if (dnf.fallbackFontAssetTable == null)
                        dnf.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    dnf.fallbackFontAssetTable.Clear();
                    dnf.fallbackFontAssetTable.Add(malgun);
                    EditorUtility.SetDirty(dnf);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 모든 Canvas TMP_Text 에 적용
            var canvas = GameObject.Find("Canvas");
            int applied = 0;
            if (canvas != null)
            {
                foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.font = dnf;
                    EditorUtility.SetDirty(t);
                    applied++;
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Dynamic SDF regenerated (DNF+{(malgun != null ? "Malgun fallback" : "no Malgun")}), applied to {applied} TMP_Text");
        }

        // ASCII printable (0x20~0x7E) — Title/HUD 영문/숫자/구두점 전부 커버.
        private static string AsciiPrintable()
        {
            var sb = new System.Text.StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);
            return sb.ToString();
        }

        private static TMP_FontAsset MakeDynamicSdf(Font ttf, string assetName, string path, int sampling, int padding, int w, int h)
        {
            // Dynamic 모드로 생성 후 TryAddCharacters 로 atlas 에 즉시 굽기.
            // (TryAddCharacters 는 Dynamic 모드에서만 동작. 구운 뒤 Static 으로 바꿔 고정.)
            var sdf = TMP_FontAsset.CreateFontAsset(
                ttf, sampling, padding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                w, h, AtlasPopulationMode.Dynamic, false);
            if (sdf == null) { Debug.LogError($"[ArkanoidFix] CreateFontAsset failed for {assetName}"); return null; }
            sdf.name = assetName;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(sdf, path);

            // 글자 굽기 (Dynamic 모드 유지 — sourceFontFile 이 ttf asset 이라 런타임에도 보충 가능)
            sdf.TryAddCharacters(AsciiPrintable(), out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[ArkanoidFix] {assetName} missing chars: {missing}");
            // lookup table 재구축 (도메인 리로드 후 character→glyph 매핑 보장)
            sdf.ReadFontAssetDefinition();

            // atlas texture + material 을 sub-asset 으로 묶어야 .asset 에 저장됨
            if (sdf.atlasTextures != null)
            {
                for (int i = 0; i < sdf.atlasTextures.Length; i++)
                {
                    var tex = sdf.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = $"{assetName} Atlas {i}";
                    if (!AssetDatabase.Contains(tex)) AssetDatabase.AddObjectToAsset(tex, sdf);
                }
            }
            if (sdf.material != null)
            {
                sdf.material.name = $"{assetName} Material";
                if (!AssetDatabase.Contains(sdf.material)) AssetDatabase.AddObjectToAsset(sdf.material, sdf);
            }
            EditorUtility.SetDirty(sdf);
            int glyphCount = sdf.glyphTable != null ? sdf.glyphTable.Count : 0;
            Debug.Log($"[ArkanoidFix] Created Static SDF: {path} (glyphs baked: {glyphCount})");
            return sdf;
        }

        private const string DnfFontPath = "Assets/Fonts/DNFBitBitv2 SDF.asset";
        private const string MalgunFontPath = "Assets/Fonts/MalgunGothic SDF.asset";

        private static bool FontIsUsable(TMP_FontAsset f)
        {
            if (f == null) return false;
            try
            {
                // SerializedObject 로 m_AtlasTextures array 직접 확인 (managed reference 안전).
                var so = new SerializedObject(f);
                var arr = so.FindProperty("m_AtlasTextures");
                if (arr == null || !arr.isArray || arr.arraySize == 0) return false;
                var first = arr.GetArrayElementAtIndex(0);
                if (first == null || first.objectReferenceValue == null) return false;
                return true;
            }
            catch { return false; }
        }

        private static TMP_FontAsset PickPrimaryFont()
        {
            // 동적 재생성된 SDF 우선 (메뉴 0 으로 생성). 없으면 LiberationSans fallback.
            var dyn = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DnfDynPath);
            if (dyn != null) { Debug.Log("[ArkanoidFix] Using DNFBitBitv2 Dynamic SDF"); return dyn; }
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (liberation != null)
            {
                Debug.Log("[ArkanoidFix] Using LiberationSans SDF (Dynamic SDF 아직 생성 안 됨 — 메뉴 0 실행 권장)");
                return liberation;
            }
            Debug.LogError("[ArkanoidFix] No usable font. Run menu 0 to regenerate, or import TMP Essential Resources");
            return null;
        }

        [MenuItem("ArkanoidFix/1. Replace TMP Fonts (auto-pick DNF or Malgun)")]
        public static void ReplaceTmpFonts()
        {
            var font = PickPrimaryFont();
            if (font == null) { Debug.LogError("[ArkanoidFix] No usable font asset found"); return; }

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            var texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            int replaced = 0;
            foreach (var t in texts)
            {
                if (t.font != font)
                {
                    t.font = font;
                    EditorUtility.SetDirty(t);
                    replaced++;
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Font replaced: {replaced}/{texts.Length} using {font.name}");
        }

        [MenuItem("ArkanoidFix/2. Cleanup duplicate Portrait_* under MascotSelector")]
        public static void CleanupDuplicatePortraits()
        {
            var titlePanelGO = GameObject.Find("Canvas/TitlePanel");
            if (titlePanelGO == null) { Debug.LogError("[ArkanoidFix] Canvas/TitlePanel not found"); return; }

            System.Type tpType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                tpType = asm.GetType("Arkanoid.Presentation.View.TitlePanel");
                if (tpType != null) break;
            }
            if (tpType == null) { Debug.LogError("[ArkanoidFix] TitlePanel type not found"); return; }

            var titlePanel = titlePanelGO.GetComponent(tpType);
            var portraitsField = tpType.GetField("portraits", BindingFlags.NonPublic | BindingFlags.Instance);
            if (portraitsField == null) { Debug.LogError("[ArkanoidFix] portraits field not found"); return; }
            var arr = portraitsField.GetValue(titlePanel) as System.Array;

            var keep = new HashSet<GameObject>();
            if (arr != null)
            {
                foreach (var p in arr)
                {
                    var portraitFld = p.GetType().GetField("Portrait");
                    if (portraitFld != null)
                    {
                        var img = portraitFld.GetValue(p) as Image;
                        if (img != null) keep.Add(img.gameObject);
                    }
                }
            }
            Debug.Log($"[ArkanoidFix] Keeping {keep.Count} portraits wired to TitlePanel.portraits");

            var sel = GameObject.Find("Canvas/TitlePanel/MascotSelector");
            if (sel == null) { Debug.LogError("[ArkanoidFix] MascotSelector not found"); return; }

            var toDelete = new List<GameObject>();
            foreach (Transform child in sel.transform)
            {
                if (child.name.StartsWith("Portrait_") && !keep.Contains(child.gameObject))
                    toDelete.Add(child.gameObject);
            }
            foreach (var go in toDelete)
            {
                Debug.Log($"[ArkanoidFix] Deleting duplicate: {go.name} (instID={go.GetInstanceID()})");
                Object.DestroyImmediate(go);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Deleted {toDelete.Count} duplicate portraits");
        }

        [MenuItem("ArkanoidFix/3. List Canvas children with active state")]
        public static void ListCanvasChildren()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            int idx = 0;
            foreach (Transform child in canvas.transform)
            {
                var componentNames = string.Join(",", child.GetComponents<Component>().Select(c => c?.GetType().Name ?? "null"));
                Debug.Log($"[ArkanoidFix] Canvas[{idx++}] {child.name} active={child.gameObject.activeSelf} components=[{componentNames}]");
            }
        }

        // ── P5: Item.prefab / Laser.prefab 생성 (Block/Ball 복제 + 비율 수정) ──
        [MenuItem("ArkanoidFix/5. Create Item.prefab + Laser.prefab + rewire renderers")]
        public static void CreateItemAndLaserPrefabs()
        {
            // Item.prefab — 32×32 사각형, Block.prefab 기반 (SpriteRenderer만 있는 단순 prefab)
            CreateSimpleSpritePrefab("Assets/Prefabs/Item.prefab", "Item", new Vector2(32f, 32f));
            // Laser.prefab — 6×24 세로 막대
            CreateSimpleSpritePrefab("Assets/Prefabs/Laser.prefab", "Laser", new Vector2(6f, 24f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ItemsRenderer.itemPrefab / LaserShotsRenderer.laserPrefab 재와이어링
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item.prefab");
            var laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Laser.prefab");

            RewirePrefabField("ItemRenderer", "Arkanoid.Presentation.View.ItemsRenderer", "itemPrefab", itemPrefab);
            RewirePrefabField("LaserShotsRenderer", "Arkanoid.Presentation.View.LaserShotsRenderer", "laserPrefab", laserPrefab);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] Item.prefab/Laser.prefab created and renderers rewired");
        }

        private static void CreateSimpleSpritePrefab(string assetPath, string name, Vector2 sizePx)
        {
            // 임시 GameObject 생성 → prefab 으로 저장 → 임시 GO 파괴
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            // 흰색 1×1 sprite (placeholder) — 추후 ItemsRenderer 가 swap.
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.color = Color.white;
            go.transform.localScale = new Vector3(sizePx.x, sizePx.y, 1f);
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[ArkanoidFix] Created prefab {assetPath} ({sizePx.x}x{sizePx.y})");
        }

        private static void RewirePrefabField(string goName, string typeFullName, string fieldName, GameObject newPrefab)
        {
            var go = GameObject.Find(goName);
            if (go == null) { Debug.LogWarning($"[ArkanoidFix] {goName} not found in scene"); return; }
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType(typeFullName); if (t != null) break; }
            if (t == null) { Debug.LogWarning($"[ArkanoidFix] type {typeFullName} not found"); return; }
            var comp = go.GetComponent(t);
            if (comp == null) { Debug.LogWarning($"[ArkanoidFix] component {typeFullName} not on {goName}"); return; }
            var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogWarning($"[ArkanoidFix] field {fieldName} not on {typeFullName}"); return; }
            f.SetValue(comp, newPrefab);
            EditorUtility.SetDirty(comp);
            Debug.Log($"[ArkanoidFix] Rewired {goName}.{fieldName} = {newPrefab.name}");
        }

        // ── P5b: Difficulty_Hard.asset 생성 (Difficulty_Normal 복제 + 와이어링) ──
        [MenuItem("ArkanoidFix/5b. Create Difficulty_Hard.asset + wire to GameManager")]
        public static void CreateDifficultyHardAsset()
        {
            const string normalPath = "Assets/Data/Gameplay/Difficulty_Normal.asset";
            const string hardPath = "Assets/Data/Gameplay/Difficulty_Hard.asset";

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(hardPath) == null)
            {
                if (!AssetDatabase.CopyAsset(normalPath, hardPath))
                {
                    Debug.LogError("[ArkanoidFix] Failed to copy Difficulty_Normal to Difficulty_Hard");
                    return;
                }
                Debug.Log($"[ArkanoidFix] Created {hardPath} as copy of Normal");
            }
            AssetDatabase.Refresh();

            var hard = AssetDatabase.LoadAssetAtPath<ScriptableObject>(hardPath);
            if (hard == null) { Debug.LogError("[ArkanoidFix] Hard asset reload failed"); return; }

            // GameManager.hardDifficultySO 재와이어링
            var gm = GameObject.Find("GameManager");
            if (gm == null) { Debug.LogError("[ArkanoidFix] GameManager not found"); return; }
            System.Type gmType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { gmType = asm.GetType("Arkanoid.Presentation.GameManager"); if (gmType != null) break; }
            if (gmType == null) { Debug.LogError("[ArkanoidFix] GameManager type not found"); return; }
            var comp = gm.GetComponent(gmType);
            var f = gmType.GetField("hardDifficultySO", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogError("[ArkanoidFix] hardDifficultySO field not found"); return; }
            f.SetValue(comp, hard);
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] GameManager.hardDifficultySO rewired to Difficulty_Hard.asset");
        }

        // ── P3: TS 좌표 1:1 로 누락 GameObject 11개 생성 + TitlePanel 와이어링 ──
        // TS 캔버스(1080×1920, top-left=0,0, Y+ down) → Unity Canvas(center pivot, Y+ up) 좌표 변환:
        //   ts(x,y) → unity(x-540, 960-y)
        [MenuItem("ArkanoidFix/6. Build missing Title UI elements (logo/arrows/powerups/panels)")]
        public static void BuildMissingTitleUi()
        {
            var titlePanelGO = GameObject.Find("Canvas/TitlePanel");
            if (titlePanelGO == null) { Debug.LogError("[ArkanoidFix] TitlePanel not found"); return; }
            System.Type tpType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { tpType = asm.GetType("Arkanoid.Presentation.View.TitlePanel"); if (tpType != null) break; }
            if (tpType == null) { Debug.LogError("[ArkanoidFix] TitlePanel type not found"); return; }
            var titlePanel = titlePanelGO.GetComponent(tpType);

            var font = PickPrimaryFont();
            int created = 0;

            // 1. mascotInfoPanel — 반투명 카드 cy=985 w=620 h=160 → Unity (0, -25)
            var mascotInfoPanel = CreateImageIfMissing(titlePanelGO.transform, "MascotInfoPanel",
                new Vector2(0, -25), new Vector2(620, 160), new Color(0, 0, 0, 0.5f));
            SetTpField(tpType, titlePanel, "mascotInfoPanel", mascotInfoPanel.GetComponent<Image>());
            mascotInfoPanel.transform.SetSiblingIndex(0); // 뒤에
            created++;

            // 2. powerupsInfoPanel — TS cy=1340 w=1010 h=260 → Unity (0, -380)
            var powerupsPanel = CreateImageIfMissing(titlePanelGO.transform, "PowerupsInfoPanel",
                new Vector2(0, -380), new Vector2(1010, 260), new Color(0, 0, 0, 0.5f));
            SetTpField(tpType, titlePanel, "powerupsInfoPanel", powerupsPanel.GetComponent<Image>());
            powerupsPanel.transform.SetSiblingIndex(0);
            created++;

            // 3. titleText "ALBATROSS" — TS (540, 260) → Unity (0, 700), fontSize 110
            var titleText = CreateTextIfMissing(titlePanelGO.transform, "TitleText",
                new Vector2(0, 700), new Vector2(1000, 130), "ALBATROSS", 110f, font, Color.white);
            SetTpField(tpType, titlePanel, "titleText", titleText);
            created++;

            // 4. prevArrow "<" — TS (110, 600) → Unity (-430, 360), fontSize 88
            var prevArrow = CreateTextIfMissing(titlePanelGO.transform, "PrevArrow",
                new Vector2(-430, 360), new Vector2(100, 100), "<", 88f, font, Color.white);
            var prevGo = prevArrow.transform.parent.gameObject == titlePanelGO ? prevArrow.gameObject : prevArrow.transform.parent.gameObject;
            // 화살표 자체에 Button 부착 — wire 는 TitlePanel.OnEnable 에서 처리.
            if (prevArrow.gameObject.GetComponent<Button>() == null) prevArrow.gameObject.AddComponent<Button>();
            SetTpField(tpType, titlePanel, "prevArrow", prevArrow.gameObject);
            SetTpField(tpType, titlePanel, "prevArrowText", prevArrow);
            created++;

            // 5. nextArrow ">"
            var nextArrow = CreateTextIfMissing(titlePanelGO.transform, "NextArrow",
                new Vector2(430, 360), new Vector2(100, 100), ">", 88f, font, Color.white);
            if (nextArrow.gameObject.GetComponent<Button>() == null) nextArrow.gameObject.AddComponent<Button>();
            SetTpField(tpType, titlePanel, "nextArrow", nextArrow.gameObject);
            SetTpField(tpType, titlePanel, "nextArrowText", nextArrow);
            created++;

            // 6. mascotPortraitFrame — Image (흰 stroke 만, fill 투명) cy=600 size 380x380 → Unity (0, 360)
            var frameGO = CreateRectImage(titlePanelGO.transform, "MascotPortraitFrame",
                new Vector2(0, 360), new Vector2(380, 380), new Color(1, 1, 1, 0));
            var frameOutline = frameGO.gameObject.GetComponent<UnityEngine.UI.Outline>() ?? frameGO.gameObject.AddComponent<UnityEngine.UI.Outline>();
            frameOutline.effectColor = Color.white;
            frameOutline.effectDistance = new Vector2(4, 4);
            SetTpField(tpType, titlePanel, "mascotPortraitFrame", frameGO.GetComponent<Image>());
            created++;

            // 7. powerupsTitle "POWERUPS" — TS (540, 1250) → Unity (0, -290), fontSize 40
            var powerupsTitle = CreateTextIfMissing(titlePanelGO.transform, "PowerupsTitle",
                new Vector2(0, -290), new Vector2(600, 60), "POWERUPS", 40f, font, Color.white);
            SetTpField(tpType, titlePanel, "powerupsTitle", powerupsTitle);
            created++;

            // 8-10. PowerupSlots × 3 (EXPAND/MAGNET/LASER) — TS coords:
            // i=0..2, itemCx = 540 + (i-1)*320 → Unity x = itemCx - 540 = (i-1)*320 = -320, 0, +320
            // iconCenterX = itemCx - 70 → Unity x = (i-1)*320 - 70 = -390, -70, +250
            // iconCenterY = 1340 → Unity y = -380
            // descY = 1425 → Unity y = -465
            var slots = new System.Collections.Generic.List<object>();
            string[] names = { "EXPAND", "MAGNET", "LASER" };
            string[] descs = { "Bar grows wider\nfor easier rebound", "Catches ball.\nSPACE to launch", "Bar fires lasers.\nSPACE to shoot" };
            for (int i = 0; i < 3; i++)
            {
                float itemCx = (i - 1) * 320f;
                float iconCx = (i - 1) * 320f - 70f;
                float nameCx = iconCx + 48f + 32f + 30f; // iconRightX + gap + nameWidth/2 추정

                var iconBlockGO = CreateRectImage(titlePanelGO.transform, $"PowerupSlot_{names[i]}_Block",
                    new Vector2(iconCx, -380), new Vector2(96, 36), new Color(0.7f, 0.7f, 0.7f, 1f));
                var iconOverlayGO = CreateRectImage(titlePanelGO.transform, $"PowerupSlot_{names[i]}_Overlay",
                    new Vector2(iconCx, -380), new Vector2(54, 26), new Color(1, 1, 1, 1));
                var nameText = CreateTextIfMissing(titlePanelGO.transform, $"PowerupSlot_{names[i]}_Name",
                    new Vector2(nameCx, -380), new Vector2(180, 40), names[i], 28f, font, Color.white);
                nameText.alignment = TextAlignmentOptions.MidlineLeft;
                var descText = CreateTextIfMissing(titlePanelGO.transform, $"PowerupSlot_{names[i]}_Desc",
                    new Vector2(itemCx - 60, -465), new Vector2(260, 80), descs[i], 20f, font, Color.white);
                descText.alignment = TextAlignmentOptions.Center;

                var slotType = tpType.GetNestedType("PowerupSlot");
                var slot = System.Activator.CreateInstance(slotType);
                slotType.GetField("IconBlock").SetValue(slot, iconBlockGO.GetComponent<Image>());
                slotType.GetField("IconOverlay").SetValue(slot, iconOverlayGO.GetComponent<Image>());
                slotType.GetField("Name").SetValue(slot, nameText);
                slotType.GetField("Description").SetValue(slot, descText);
                slots.Add(slot);
                created += 4;
            }
            // powerupSlots = slots.ToArray() with PowerupSlot type
            var slotArr = System.Array.CreateInstance(tpType.GetNestedType("PowerupSlot"), 3);
            for (int i = 0; i < 3; i++) slotArr.SetValue(slots[i], i);
            SetTpField(tpType, titlePanel, "powerupSlots", slotArr);

            EditorUtility.SetDirty(titlePanel);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Built/refreshed {created} Title UI GameObjects, wired to TitlePanel");
        }

        private static void SetTpField(System.Type t, object instance, string fieldName, object value)
        {
            var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogWarning($"[ArkanoidFix] field {fieldName} not found"); return; }
            f.SetValue(instance, value);
        }

        private static GameObject CreateImageIfMissing(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            return CreateRectImage(parent, name, anchoredPos, size, color);
        }

        private static GameObject CreateRectImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var existing = parent.Find(name);
            if (existing != null) {
                var rt = existing.GetComponent<RectTransform>();
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
                var img = existing.GetComponent<Image>();
                if (img != null) img.color = color;
                return existing.gameObject;
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TMP_Text CreateTextIfMissing(Transform parent, string name, Vector2 anchoredPos, Vector2 size, string text, float fontSize, TMP_FontAsset font, Color color)
        {
            var existing = parent.Find(name);
            TMP_Text t;
            if (existing != null)
            {
                t = existing.GetComponent<TMP_Text>();
                if (t == null) t = existing.gameObject.AddComponent<TextMeshProUGUI>();
                var rt = existing.GetComponent<RectTransform>();
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                t = go.AddComponent<TextMeshProUGUI>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }
            t.text = text;
            t.fontSize = fontSize;
            if (font != null) t.font = font;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        // TMP Essential Resources 재임포트 (LiberationSans/shader 손상 시 복구)
        [MenuItem("ArkanoidFix/T. Import TMP Essential Resources (reflection)")]
        public static void ImportTmpEssentials()
        {
            System.Type importer = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                importer = asm.GetType("TMPro.TMP_PackageResourceImporter");
                if (importer != null) break;
            }
            if (importer == null) { Debug.LogError("[ArkanoidFix] TMP_PackageResourceImporter not found"); return; }
            var m = importer.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (m == null) { Debug.LogError("[ArkanoidFix] ImportResources method not found"); return; }
            // signature: ImportResources(bool importEssentials, bool importExamples, bool clean)
            m.Invoke(null, new object[] { true, false, false });
            AssetDatabase.Refresh();
            Debug.Log("[ArkanoidFix] TMP Essential Resources import requested");
        }

        // 폰트 atlas texture 의 실제 readable/glyph 상태 진단
        [MenuItem("ArkanoidFix/D. Diagnose font + Label mesh")]
        public static void DiagnoseFont()
        {
            var dnf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DnfDynPath);
            if (dnf == null) { Debug.LogError("[ArkanoidFix] DNF Dynamic SDF not found"); return; }
            int glyphs = dnf.glyphTable != null ? dnf.glyphTable.Count : -1;
            int chars = dnf.characterTable != null ? dnf.characterTable.Count : -1;
            var atlas = (dnf.atlasTextures != null && dnf.atlasTextures.Length > 0) ? dnf.atlasTextures[0] : null;
            string atlasInfo = atlas != null ? $"{atlas.width}x{atlas.height} fmt={atlas.format} readable={atlas.isReadable}" : "NULL";
            var mat = dnf.material;
            string shaderName = mat != null && mat.shader != null ? $"{mat.shader.name} supported={mat.shader.isSupported}" : "NULL";
            dnf.ReadFontAssetDefinition();
            int lookupCount = dnf.characterLookupTable != null ? dnf.characterLookupTable.Count : -1;
            bool hasN = dnf.HasCharacter('N');
            Debug.Log($"[ArkanoidFix][DIAG] DNF glyphs={glyphs} chars={chars} lookup={lookupCount} hasN={hasN} atlas={atlasInfo} shader={shaderName}");

            var lbl = GameObject.Find("Canvas/TitlePanel/NormalButton/Label");
            if (lbl != null)
            {
                var tmp = lbl.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.ForceMeshUpdate(true, true);
                    var ti = tmp.textInfo;
                    Debug.Log($"[ArkanoidFix][DIAG] Label text='{tmp.text}' enabled={tmp.enabled} charCount={ti.characterCount} font={(tmp.font != null ? tmp.font.name : "NULL")}");
                }
            }

            // 결정적 실험: 임시 Canvas + TMP_Text 동적 생성 후 charCount 측정
            var tmpGo = new GameObject("DIAG_TMP_Test", typeof(RectTransform));
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo != null) tmpGo.transform.SetParent(canvasGo.transform, false);
            var testTmp = tmpGo.AddComponent<TextMeshProUGUI>();
            testTmp.font = dnf;
            testTmp.text = "TEST123";
            testTmp.fontSize = 40;
            (testTmp.transform as RectTransform).sizeDelta = new Vector2(400, 100);
            testTmp.ForceMeshUpdate(true, true);
            Debug.Log($"[ArkanoidFix][DIAG] DYNAMIC TMP (DNF) charCount={testTmp.textInfo.characterCount} vertexCount={testTmp.mesh.vertexCount}");
            var lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            testTmp.font = lib;
            testTmp.ForceMeshUpdate(true, true);
            Debug.Log($"[ArkanoidFix][DIAG] DYNAMIC TMP (LiberationSans) charCount={testTmp.textInfo.characterCount} libFont={(lib != null ? lib.name : "NULL")}");
            Object.DestroyImmediate(tmpGo);
        }

        // 근본 원인: 모든 TMP_Text.enabled=false 라 글자 mesh 안 그려짐 → 일괄 활성화
        [MenuItem("ArkanoidFix/E. Enable all TMP_Text components")]
        public static void EnableAllTmpText()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            int enabled = 0, already = 0;
            foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!t.enabled) { t.enabled = true; EditorUtility.SetDirty(t); enabled++; }
                else already++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Enabled {enabled} TMP_Text (already on: {already})");
        }

        // A2: stage 배경을 bg_pixel_01~03 으로 교체 + GameManager 에 wire
        [MenuItem("ArkanoidFix/A2. Wire stage backgrounds (bg_pixel_01~03)")]
        public static void WireStageBackgrounds()
        {
            var px1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_pixel_01.png");
            var px2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_pixel_02.png");
            var px3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_pixel_03.png");
            if (px1 == null || px2 == null || px3 == null) { Debug.LogError("[ArkanoidFix] bg_pixel_01~03 sprite 일부 없음"); return; }

            // InGame/RoundIntro 의 Background Image 찾기
            var inGameBg = GameObject.Find("Canvas/InGamePanel/Background");
            var roundIntroBg = GameObject.Find("Canvas/RoundIntroPanel/Background");
            var bgImages = new List<Image>();
            if (inGameBg != null) { var i = inGameBg.GetComponent<Image>(); if (i != null) { i.sprite = px1; EditorUtility.SetDirty(i); bgImages.Add(i); } }
            if (roundIntroBg != null) { var i = roundIntroBg.GetComponent<Image>(); if (i != null) { i.sprite = px1; EditorUtility.SetDirty(i); bgImages.Add(i); } }

            // GameManager 에 stageBackgroundSprites + stageBackgroundImages wire
            var gm = GameObject.Find("GameManager");
            System.Type gmType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { gmType = asm.GetType("Arkanoid.Presentation.GameManager"); if (gmType != null) break; }
            if (gm != null && gmType != null)
            {
                var comp = gm.GetComponent(gmType);
                var spritesF = gmType.GetField("stageBackgroundSprites", BindingFlags.NonPublic | BindingFlags.Instance);
                var imagesF = gmType.GetField("stageBackgroundImages", BindingFlags.NonPublic | BindingFlags.Instance);
                if (spritesF != null) spritesF.SetValue(comp, new Sprite[] { px1, px2, px3 });
                if (imagesF != null) imagesF.SetValue(comp, bgImages.ToArray());
                EditorUtility.SetDirty(comp);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Stage backgrounds wired: {bgImages.Count} bg images + GameManager sprite[3]");
        }

        [MenuItem("ArkanoidFix/9. Save active scene")]
        public static void SaveScene()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] Scene saved");
        }
    }
}

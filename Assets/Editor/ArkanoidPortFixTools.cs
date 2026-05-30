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

            // 한글 음절 11172 포함 → atlas 4096 + multi-atlas. sampling 48 (한글 다량 + 픽셀 폰트라 충분).
            var dnf = MakeDynamicSdf(dnfTtf, "DNFBitBitv2 Dynamic SDF", DnfDynPath, 48, 6, 4096, 4096);
            if (dnf == null) return;

            TMP_FontAsset malgun = null;
            if (malgunTtf != null)
            {
                malgun = MakeDynamicSdf(malgunTtf, "MalgunGothic Dynamic SDF", MalgunDynPath, 48, 6, 4096, 4096);
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

        // ASCII printable (0x20~0x7E) + 한글 음절 (가-힣, 0xAC00~0xD7A3).
        private static string CharsToBake()
        {
            var sb = new System.Text.StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);        // ASCII
            for (int c = 0xAC00; c <= 0xD7A3; c++) sb.Append((char)c);    // 한글 가-힣 (11172)
            return sb.ToString();
        }

        private static TMP_FontAsset MakeDynamicSdf(Font ttf, string assetName, string path, int sampling, int padding, int w, int h)
        {
            // Dynamic 모드로 생성 후 TryAddCharacters 로 atlas 에 즉시 굽기 (한글 많아 multi-atlas 허용).
            var sdf = TMP_FontAsset.CreateFontAsset(
                ttf, sampling, padding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                w, h, AtlasPopulationMode.Dynamic, true);
            if (sdf == null) { Debug.LogError($"[ArkanoidFix] CreateFontAsset failed for {assetName}"); return null; }
            sdf.name = assetName;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(sdf, path);

            // 글자 굽기 (Dynamic 모드 유지 — sourceFontFile 이 ttf asset 이라 런타임에도 보충 가능)
            sdf.TryAddCharacters(CharsToBake(), out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning($"[ArkanoidFix] {assetName} missing {missing.Length} chars (폰트에 없는 글자)");
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

        // P3 에서 잘못 중복 생성한 Title UI GameObject 제거 (기존 LogoText/Powerup0~2/PowerupsPanel 사용).
        [MenuItem("ArkanoidFix/C. Remove duplicate Title UI (TitleText/PowerupSlot_*/PowerupsInfoPanel)")]
        public static void RemoveDuplicateTitleUi()
        {
            var titlePanel = GameObject.Find("Canvas/TitlePanel");
            if (titlePanel == null) { Debug.LogError("[ArkanoidFix] TitlePanel not found"); return; }
            string[] dupNames = {
                "TitleText",
                "PowerupsInfoPanel",
                "PowerupSlot_EXPAND_Block", "PowerupSlot_EXPAND_Overlay", "PowerupSlot_EXPAND_Name", "PowerupSlot_EXPAND_Desc",
                "PowerupSlot_MAGNET_Block", "PowerupSlot_MAGNET_Overlay", "PowerupSlot_MAGNET_Name", "PowerupSlot_MAGNET_Desc",
                "PowerupSlot_LASER_Block", "PowerupSlot_LASER_Overlay", "PowerupSlot_LASER_Name", "PowerupSlot_LASER_Desc",
            };
            int removed = 0;
            foreach (var n in dupNames)
            {
                var child = titlePanel.transform.Find(n);
                if (child != null) { Debug.Log($"[ArkanoidFix] Removing duplicate: {n}"); Object.DestroyImmediate(child.gameObject); removed++; }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Removed {removed} duplicate Title UI GameObjects");
        }

        // 모든 Canvas 패널의 자식 sibling 순서(=렌더 순서) 로깅
        [MenuItem("ArkanoidFix/L. Log all panel children order")]
        public static void LogPanelChildrenOrder()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            foreach (Transform panel in canvas.transform)
            {
                if (panel.childCount == 0) { Debug.Log($"[ArkanoidFix][LAYER] {panel.name}: (자식 없음)"); continue; }
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < panel.childCount; i++)
                {
                    var c = panel.GetChild(i);
                    string kind = "";
                    if (c.GetComponent<TMP_Text>() != null) kind = "Text";
                    else if (c.GetComponent<Button>() != null) kind = "Button";
                    else if (c.GetComponent<Image>() != null) kind = "Image";
                    sb.Append($"[{i}]{c.name}({kind}) ");
                }
                Debug.Log($"[ArkanoidFix][LAYER] {panel.name}: {sb}");
            }
        }

        // 각 패널에서 이름에 "Background" 가 든 자식을 맨 뒤(sibling index 0)로 이동
        [MenuItem("ArkanoidFix/L2. Fix layering — Background to back in all panels")]
        public static void FixBackgroundLayering()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            int moved = 0;
            foreach (Transform panel in canvas.transform)
            {
                for (int i = 0; i < panel.childCount; i++)
                {
                    var c = panel.GetChild(i);
                    if (c.name.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (c.GetSiblingIndex() != 0)
                        {
                            Debug.Log($"[ArkanoidFix] {panel.name}/{c.name}: sibling {c.GetSiblingIndex()} → 0");
                            c.SetSiblingIndex(0);
                            moved++;
                        }
                        break; // 패널당 Background 1개 가정
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] Moved {moved} Background(s) to back");
        }

        // IntroStory 자식 컴포넌트 enabled + InGame 배경/플레이필드 관계 진단
        [MenuItem("ArkanoidFix/D2. Diagnose IntroStory + Playfield")]
        public static void DiagnoseIntroAndPlayfield()
        {
            // IntroStory 자식들 enabled / alpha
            var intro = GameObject.Find("Canvas/IntroStoryPanel");
            if (intro != null)
            {
                foreach (Transform c in intro.transform)
                {
                    var img = c.GetComponent<Image>();
                    var tmp = c.GetComponent<TMP_Text>();
                    string info = $"active={c.gameObject.activeSelf}";
                    if (img != null) info += $" Image.enabled={img.enabled} a={img.color.a:F2}";
                    if (tmp != null) info += $" TMP.enabled={tmp.enabled} text='{tmp.text}'";
                    Debug.Log($"[ArkanoidFix][D2-Intro] {c.name}: {info}");
                }
                var cg = intro.GetComponent<CanvasGroup>();
                if (cg != null) Debug.Log($"[ArkanoidFix][D2-Intro] IntroStoryPanel CanvasGroup alpha={cg.alpha}");
            }

            // InGamePanel Background — Overlay 불투명 풀스크린이면 world space PlayfieldRoot 를 가림
            var inGameBg = GameObject.Find("Canvas/InGamePanel/Background");
            if (inGameBg != null)
            {
                var img = inGameBg.GetComponent<Image>();
                var rt = inGameBg.GetComponent<RectTransform>();
                Debug.Log($"[ArkanoidFix][D2-InGame] Background Image.enabled={img?.enabled} alpha={img?.color.a:F2} size={rt.rect.width}x{rt.rect.height}");
            }

            // Canvas renderMode (Overlay 면 world space 를 무조건 가림)
            var canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (canvas != null) Debug.Log($"[ArkanoidFix][D2] Canvas renderMode={canvas.renderMode} sortingOrder={canvas.sortingOrder}");

            // PlayfieldRoot 자식 렌더러 (world space)
            var pf = GameObject.Find("PlayfieldRoot");
            if (pf != null) Debug.Log($"[ArkanoidFix][D2] PlayfieldRoot childCount={pf.transform.childCount} z={pf.transform.position.z}");
        }

        // IntroStory Illustration(풀스크린 불투명)을 Background 바로 뒤(index 1)로 — 텍스트/backdrop 가림 해결
        [MenuItem("ArkanoidFix/L3. Fix IntroStory Illustration layer")]
        public static void FixIntroIllustrationLayer()
        {
            var ill = GameObject.Find("Canvas/IntroStoryPanel/Illustration");
            if (ill == null) { Debug.LogError("[ArkanoidFix] IntroStoryPanel/Illustration not found"); return; }
            ill.transform.SetSiblingIndex(1);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] IntroStory Illustration → sibling 1 (Background 다음, backdrop/text 앞)");
        }

        // 이슈 2: InGame/RoundIntro 배경을 world space 로 — Overlay 풀스크린이 PlayfieldRoot(world) 게임요소를 가리는 문제 해결.
        // world 배경 SpriteRenderer(sortingOrder -100) 생성 + 카메라 뷰에 맞춤. Overlay Background 는 비활성화.
        [MenuItem("ArkanoidFix/L4. Move stage background to world space (fix playfield hidden)")]
        public static void MoveStageBackgroundToWorld()
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[ArkanoidFix] Main Camera not found"); return; }
            var px1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds/bg_pixel_01.png");
            if (px1 == null) { Debug.LogError("[ArkanoidFix] bg_pixel_01 not found"); return; }

            // WorldBackground GameObject (루트, PlayfieldRoot 밖)
            var existing = GameObject.Find("WorldBackground");
            var bg = existing != null ? existing : new GameObject("WorldBackground", typeof(SpriteRenderer));
            var sr = bg.GetComponent<SpriteRenderer>();
            sr.sprite = px1;
            sr.sortingOrder = -100; // 게임요소(기본 0)보다 뒤
            // 카메라 뷰 중앙에 배치 + 뷰 전체 덮게 scale
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            bg.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            var sb = sr.sprite.bounds.size; // world 단위 sprite 크기 (PPU 반영)
            if (sb.x > 0 && sb.y > 0)
                bg.transform.localScale = new Vector3(viewW / sb.x, viewH / sb.y, 1f);

            // Overlay InGame/RoundIntro Background 비활성화
            foreach (var path in new[] { "Canvas/InGamePanel/Background", "Canvas/RoundIntroPanel/Background" })
            {
                var o = GameObject.Find(path);
                if (o != null) { var img = o.GetComponent<Image>(); if (img != null) { img.enabled = false; EditorUtility.SetDirty(img); } }
            }

            // GameManager.stageBackgroundImages 비우고, world SpriteRenderer 를 새 필드로 연결
            var gm = GameObject.Find("GameManager");
            System.Type gmType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { gmType = asm.GetType("Arkanoid.Presentation.GameManager"); if (gmType != null) break; }
            if (gm != null && gmType != null)
            {
                var comp = gm.GetComponent(gmType);
                var srF = gmType.GetField("stageBackgroundRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (srF != null) { srF.SetValue(comp, sr); EditorUtility.SetDirty(comp); }
                else Debug.LogWarning("[ArkanoidFix] GameManager.stageBackgroundRenderer 필드 없음 — 코드 추가 필요");
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] WorldBackground 생성(sortingOrder -100, scale {bg.transform.localScale}), Overlay 배경 비활성화");
        }

        // B2: TitlePanel.toast → Canvas/ToastView 와이어링
        [MenuItem("ArkanoidFix/B2. Wire TitlePanel.toast")]
        public static void WireTitleToast()
        {
            var tp = GameObject.Find("Canvas/TitlePanel");
            var toastGo = GameObject.Find("Canvas/ToastView");
            if (tp == null || toastGo == null) { Debug.LogError("[ArkanoidFix] TitlePanel/ToastView not found"); return; }
            System.Type tpType = null, toastType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                tpType ??= asm.GetType("Arkanoid.Presentation.View.TitlePanel");
                toastType ??= asm.GetType("Arkanoid.Presentation.View.ToastView");
            }
            var toastComp = toastGo.GetComponent(toastType);
            var f = tpType.GetField("toast", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogError("[ArkanoidFix] TitlePanel.toast field not found"); return; }
            f.SetValue(tp.GetComponent(tpType), toastComp);
            EditorUtility.SetDirty(tp.GetComponent(tpType));
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] TitlePanel.toast → ToastView wired");
        }

        // B7 진단: Play 중 게임 시작 (Title → IntroStory). 여러 번 누르면 다음 단계로.
        [MenuItem("ArkanoidFix/P. (Play) Request start game (Normal)")]
        public static void RequestStartGameNormal()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[ArkanoidFix] Play 모드에서만 동작"); return; }
            var gm = GameObject.Find("GameManager");
            System.Type gmType = null, dkType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                gmType ??= asm.GetType("Arkanoid.Presentation.GameManager");
                dkType ??= asm.GetType("Arkanoid.Definitions.DifficultyKind");
            }
            if (gm == null || gmType == null || dkType == null) { Debug.LogError("[ArkanoidFix] GameManager/DifficultyKind type 없음"); return; }
            var comp = gm.GetComponent(gmType);
            var m = gmType.GetMethod("RequestStartGame");
            var normal = System.Enum.Parse(dkType, "Normal");
            m.Invoke(comp, new[] { normal });
            Debug.Log("[ArkanoidFix] RequestStartGame(Normal) 호출");
        }

        // B8: IntroStory BodyBackdrop 삭제 (불필요한 반투명 — StoryTextBackdrop 만 유지)
        [MenuItem("ArkanoidFix/B8. Remove IntroStory BodyBackdrop")]
        public static void RemoveBodyBackdrop()
        {
            var bd = GameObject.Find("Canvas/IntroStoryPanel/BodyBackdrop");
            if (bd == null) { Debug.LogWarning("[ArkanoidFix] BodyBackdrop not found (이미 삭제됨?)"); return; }
            Object.DestroyImmediate(bd);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] IntroStory BodyBackdrop 삭제");
        }

        // B2: ToastView 를 Canvas 마지막 sibling 으로 (맨 앞 렌더 — 패널 위에 토스트 보이게)
        [MenuItem("ArkanoidFix/B2b. ToastView to front (last sibling)")]
        public static void ToastViewToFront()
        {
            var toast = GameObject.Find("Canvas/ToastView");
            if (toast == null) { Debug.LogError("[ArkanoidFix] ToastView not found"); return; }
            toast.transform.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] ToastView → last sibling (index {toast.transform.GetSiblingIndex()})");
        }

        // B7: GameManager.playfieldRoot 와이어링 (pointer 입력 좌표 변환에 필요)
        [MenuItem("ArkanoidFix/B7. Wire GameManager.playfieldRoot")]
        public static void WirePlayfieldRoot()
        {
            var gm = GameObject.Find("GameManager");
            var pf = GameObject.Find("PlayfieldRoot");
            if (gm == null || pf == null) { Debug.LogError("[ArkanoidFix] GameManager/PlayfieldRoot not found"); return; }
            System.Type gmType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { gmType = asm.GetType("Arkanoid.Presentation.GameManager"); if (gmType != null) break; }
            var comp = gm.GetComponent(gmType);
            var f = gmType.GetField("playfieldRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogError("[ArkanoidFix] GameManager.playfieldRoot field not found"); return; }
            f.SetValue(comp, pf.transform);
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] GameManager.playfieldRoot → PlayfieldRoot ({pf.transform.position})");
        }

        // B6 진단: BarRenderer + 자식 5개 의 transform/SR 상태 정밀 로깅
        [MenuItem("ArkanoidFix/B6d. Diagnose BarRenderer hierarchy")]
        public static void DiagnoseBarRenderer()
        {
            var br = GameObject.Find("PlayfieldRoot/BarRenderer") ?? GameObject.Find("BarRenderer");
            if (br == null) { Debug.LogError("[ArkanoidFix] BarRenderer not found"); return; }
            Debug.Log($"[ArkanoidFix][B6d] BarRenderer activeSelf={br.activeSelf} activeInHierarchy={br.activeInHierarchy} childCount={br.transform.childCount} localPos={br.transform.localPosition} worldPos={br.transform.position}");
            foreach (Transform c in br.transform)
            {
                var sr = c.GetComponent<SpriteRenderer>();
                string srInfo = "N/A";
                if (sr != null)
                {
                    var b = sr.bounds;
                    srInfo = $"enabled={sr.enabled} sprite={(sr.sprite!=null?sr.sprite.name:"NULL")} color={sr.color} sortLayer={sr.sortingLayerName} sortOrder={sr.sortingOrder} bounds={b.size}";
                }
                Debug.Log($"[ArkanoidFix][B6d]   {c.name}: active={c.gameObject.activeSelf} localPos={c.localPosition} localScale={c.localScale} worldPos={c.position} SR[{srInfo}]");
            }
        }

        // B6 수정: BarRenderer + 자식 SemiL/SemiR/Base/StripL/StripR 활성화 + SpriteRenderer enable
        [MenuItem("ArkanoidFix/B6. Enable BarRenderer + children")]
        public static void EnableBarRenderer()
        {
            var br = GameObject.Find("PlayfieldRoot/BarRenderer") ?? GameObject.Find("BarRenderer");
            if (br == null) { Debug.LogError("[ArkanoidFix] BarRenderer not found"); return; }
            if (!br.activeSelf) { br.SetActive(true); Debug.Log("[ArkanoidFix] BarRenderer GameObject 활성화"); }
            int enabled = 0;
            foreach (Transform c in br.transform)
            {
                if (!c.gameObject.activeSelf) { c.gameObject.SetActive(true); enabled++; }
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr != null && !sr.enabled) { sr.enabled = true; EditorUtility.SetDirty(sr); enabled++; }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] BarRenderer 자식 활성화: {enabled}");
        }

        // B3: PlayfieldRoot 자식으로 검은 사각형 추가 (TS 라운드 플레이필드 배경)
        [MenuItem("ArkanoidFix/B3. Add black playfield fill")]
        public static void AddBlackPlayfieldFill()
        {
            var pf = GameObject.Find("PlayfieldRoot");
            if (pf == null) { Debug.LogError("[ArkanoidFix] PlayfieldRoot not found"); return; }
            var existing = pf.transform.Find("PlayfieldBlackFill");
            GameObject fill;
            if (existing != null) fill = existing.gameObject;
            else
            {
                fill = new GameObject("PlayfieldBlackFill", typeof(SpriteRenderer));
                fill.transform.SetParent(pf.transform, false);
            }
            fill.transform.SetSiblingIndex(0); // PlayfieldRoot 자식 중 맨 앞 — 다른 게임요소보다 뒤 렌더
            var sr = fill.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.color = Color.black;
            sr.sortingOrder = -50; // WorldBackground(-100) 위, 게임요소(0+) 아래
            // PlayfieldRoot scale.y=-1, 자식 좌표는 gameplay (0~720, 0~900). 중심 (360, 450).
            fill.transform.localPosition = new Vector3(360f, 450f, 0f);
            fill.transform.localScale = new Vector3(720f, 900f, 1f);
            EditorUtility.SetDirty(sr);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] PlayfieldBlackFill 생성 (sortingOrder -50, 720x900)");
        }

        // B4: HudView.livesContainer 와이어링 + 위치 설정 (LayoutConfig livesBar)
        [MenuItem("ArkanoidFix/B4. Wire HudView.livesContainer")]
        public static void WireLivesContainer()
        {
            var hud = GameObject.Find("HudView") ?? GameObject.Find("Canvas/InGamePanel/HudView");
            if (hud == null) { Debug.LogError("[ArkanoidFix] HudView not found"); return; }
            // LivesContainer 자식 생성 (RectTransform)
            var existing = hud.transform.Find("LivesContainer");
            GameObject lc;
            if (existing != null) lc = existing.gameObject;
            else
            {
                lc = new GameObject("LivesContainer", typeof(RectTransform));
                lc.transform.SetParent(hud.transform, false);
            }
            // Canvas 좌표 (anchor center). LayoutConfig livesBar.startX=80, y=1700.
            // canvas 1080×1920 center 기준: x=80-540=-460, y=960-1700=-740.
            var rt = lc.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(-460f, -740f);
            rt.sizeDelta = new Vector2(600f, 40f);
            // HudView.livesContainer 필드 설정
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.View.HudView"); if (t != null) break; }
            var comp = hud.GetComponent(t);
            var f = t.GetField("livesContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(comp, lc.transform);
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] HudView.livesContainer wired (-460, -740)");
        }

        // B6e: BarRenderer 컴포넌트 .enabled = true 강제 (씬에 저장 → Awake 보장)
        [MenuItem("ArkanoidFix/B6e. Force BarRenderer.enabled = true")]
        public static void ForceBarRendererEnabled()
        {
            var br = GameObject.Find("PlayfieldRoot/BarRenderer") ?? GameObject.Find("BarRenderer");
            if (br == null) { Debug.LogError("[ArkanoidFix] BarRenderer not found"); return; }
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.View.BarRenderer"); if (t != null) break; }
            var comp = br.GetComponent(t) as Behaviour;
            if (comp == null) { Debug.LogError("[ArkanoidFix] BarRenderer component not found"); return; }
            if (!comp.enabled)
            {
                comp.enabled = true;
                EditorUtility.SetDirty(comp);
                Debug.Log("[ArkanoidFix] BarRenderer.enabled was false → true (씬 저장 필요)");
            }
            else Debug.Log("[ArkanoidFix] BarRenderer.enabled 이미 true");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // E1: 토스트 검은 박스 + 흰 테두리 (글자 잘 보이게)
        [MenuItem("ArkanoidFix/E1. Toast box (black bg + white outline)")]
        public static void AddToastBox()
        {
            var toast = GameObject.Find("Canvas/ToastView");
            if (toast == null) { Debug.LogError("[ArkanoidFix] ToastView not found"); return; }
            var label = toast.transform.Find("Label");
            if (label == null) { Debug.LogError("[ArkanoidFix] ToastView/Label not found"); return; }
            var existing = toast.transform.Find("Background");
            GameObject bg = existing != null ? existing.gameObject
                : new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(toast.transform, false);
            bg.transform.SetSiblingIndex(0); // Label 뒤
            var bgRt = bg.GetComponent<RectTransform>();
            var labelRt = label.GetComponent<RectTransform>();
            bgRt.anchorMin = labelRt.anchorMin;
            bgRt.anchorMax = labelRt.anchorMax;
            bgRt.pivot = labelRt.pivot;
            bgRt.anchoredPosition = labelRt.anchoredPosition;
            bgRt.sizeDelta = new Vector2(560f, 110f);
            var img = bg.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var outline = bg.GetComponent<Outline>() ?? bg.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(4f, 4f);
            EditorUtility.SetDirty(bg);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] Toast Background 추가 (검정 + 흰 테두리 4px)");
        }

        // B5d: PlayfieldBlackFill + WorldBackground 진단
        [MenuItem("ArkanoidFix/B5d. Diagnose PlayfieldBlackFill")]
        public static void DiagnoseBlackFill()
        {
            var fill = GameObject.Find("PlayfieldBlackFill");
            if (fill != null)
            {
                var sr = fill.GetComponent<SpriteRenderer>();
                Debug.Log($"[ArkanoidFix][B5d] PlayfieldBlackFill: worldPos={fill.transform.position} scale={fill.transform.lossyScale} color={(sr!=null?sr.color.ToString():"")} sortLayer={(sr!=null?sr.sortingLayerName:"")} sortOrder={(sr!=null?sr.sortingOrder:0)} sprite={(sr?.sprite!=null?sr.sprite.name:"NULL")} bounds={(sr!=null?sr.bounds.ToString():"")}");
            }
            else Debug.LogWarning("[ArkanoidFix][B5d] PlayfieldBlackFill not found");

            var bg = GameObject.Find("WorldBackground");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                Debug.Log($"[ArkanoidFix][B5d] WorldBackground: worldPos={bg.transform.position} scale={bg.transform.lossyScale} sortLayer={sr.sortingLayerName} sortOrder={sr.sortingOrder} bounds={sr.bounds}");
            }
        }

        // E2: MuteButton 라벨 → "II" (Pause). 라운드 내에서만 보이게 GameManager 가 SetActive 제어 필요 (별도 작업).
        [MenuItem("ArkanoidFix/E2. MuteButton → PauseButton (label)")]
        public static void MuteToPauseLabel()
        {
            var mute = GameObject.Find("Canvas/MuteButton");
            if (mute == null) { Debug.LogError("[ArkanoidFix] MuteButton not found"); return; }
            mute.name = "PauseButton";
            var tmp = mute.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = "II"; tmp.fontStyle = FontStyles.Bold; EditorUtility.SetDirty(tmp); }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] MuteButton → PauseButton, label='II'");
        }

        // E3: ScreenRouter 에 pauseButton + playfieldBlackFill 와이어 (InGame/RoundIntro 한정 active)
        [MenuItem("ArkanoidFix/E3. Wire ScreenRouter.pauseButton + playfieldBlackFill")]
        public static void WireScreenRouterExtras()
        {
            var sr = GameObject.Find("ScreenRouter");
            if (sr == null) { Debug.LogError("[ArkanoidFix] ScreenRouter not found"); return; }
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.View.ScreenRouter"); if (t != null) break; }
            var comp = sr.GetComponent(t);
            var pause = GameObject.Find("Canvas/PauseButton") ?? GameObject.Find("Canvas/MuteButton");
            var fill = GameObject.Find("PlayfieldBlackFill");
            var fPause = t.GetField("pauseButton", BindingFlags.NonPublic | BindingFlags.Instance);
            var fFill = t.GetField("playfieldBlackFill", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pause != null) fPause.SetValue(comp, pause);
            if (fill != null) fFill.SetValue(comp, fill);
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] ScreenRouter wired — pauseButton={(pause!=null?pause.name:"NULL")} playfieldBlackFill={(fill!=null?"OK":"NULL")}");
        }

        // E4: ToastView 초기 비활성 (Show 호출 시에만 보이도록)
        [MenuItem("ArkanoidFix/E4. ToastView 초기 비활성")]
        public static void ToastInitInactive()
        {
            var toast = GameObject.Find("Canvas/ToastView");
            if (toast == null) { Debug.LogError("[ArkanoidFix] ToastView not found"); return; }
            toast.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] ToastView SetActive(false) — Show 호출 시에만 표시");
        }

        // E5: SpinnersRenderer.spriteEntries 와이어 (spinner_cube/spinner_triangle)
        [MenuItem("ArkanoidFix/E5. Wire SpinnersRenderer.spriteEntries")]
        public static void WireSpinnerSprites()
        {
            var sr = GameObject.Find("SpinnersRenderer") ?? GameObject.Find("PlayfieldRoot/SpinnersRenderer");
            if (sr == null) { Debug.LogError("[ArkanoidFix] SpinnersRenderer not found"); return; }
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.View.SpinnersRenderer"); if (t != null) break; }
            var comp = sr.GetComponent(t);
            var entryType = t.GetNestedType("SpriteEntry");
            var cube = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/V2/spinner_cube.png");
            var tri = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/V2/spinner_triangle.png");
            if (cube == null || tri == null) { Debug.LogError("[ArkanoidFix] spinner sprites not found"); return; }
            var arr = System.Array.CreateInstance(entryType, 2);
            var e0 = System.Activator.CreateInstance(entryType);
            entryType.GetField("DefinitionId").SetValue(e0, "spinner_cube");
            entryType.GetField("Sprite").SetValue(e0, cube);
            arr.SetValue(e0, 0);
            var e1 = System.Activator.CreateInstance(entryType);
            entryType.GetField("DefinitionId").SetValue(e1, "spinner_triangle");
            entryType.GetField("Sprite").SetValue(e1, tri);
            arr.SetValue(e1, 1);
            var f = t.GetField("spriteEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(comp, arr);
            EditorUtility.SetDirty(comp);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] SpinnersRenderer.spriteEntries wired (cube, triangle)");
        }

        // E6: PlayfieldBlackFill 추가 진단 (런타임 active 상태)
        [MenuItem("ArkanoidFix/E6. Diagnose PlayfieldBlackFill runtime state")]
        public static void DiagnoseBlackFillRuntime()
        {
            var fill = GameObject.Find("PlayfieldBlackFill");
            if (fill == null) { Debug.LogWarning("[ArkanoidFix][E6] PlayfieldBlackFill not active in hierarchy (SetActive false?)"); }
            else
            {
                var sr = fill.GetComponent<SpriteRenderer>();
                Debug.Log($"[ArkanoidFix][E6] PlayfieldBlackFill activeSelf={fill.activeSelf} activeInHierarchy={fill.activeInHierarchy} SR.enabled={(sr!=null?sr.enabled.ToString():"NULL")} sprite={(sr?.sprite!=null?"OK":"NULL")} parent={fill.transform.parent?.name}");
            }
            // PlayfieldRoot 활성 체크
            var pf = GameObject.Find("PlayfieldRoot");
            if (pf != null)
            {
                Debug.Log($"[ArkanoidFix][E6] PlayfieldRoot activeSelf={pf.activeSelf} childCount={pf.transform.childCount}");
                foreach (Transform c in pf.transform)
                {
                    if (c.name.Contains("BlackFill") || c.name.Contains("Background"))
                        Debug.Log($"[ArkanoidFix][E6]   {c.name}: active={c.gameObject.activeSelf}");
                }
            }
        }

        // E7: PlayfieldBlackFill 을 PlayfieldRoot 밖(루트)으로 옮겨 음의 scale 제거 — WorldBackground 옆
        [MenuItem("ArkanoidFix/E7. Move PlayfieldBlackFill out of PlayfieldRoot (positive scale)")]
        public static void MoveBlackFillToRoot()
        {
            var fill = GameObject.Find("PlayfieldBlackFill");
            if (fill == null)
            {
                // 없으면 새로 생성
                fill = new GameObject("PlayfieldBlackFill", typeof(SpriteRenderer));
            }
            else
            {
                fill.transform.SetParent(null, true); // 루트로
            }
            var sr = fill.GetComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.color = Color.black;
            sr.sortingOrder = -50;
            // PlayfieldRoot 가 (0,0) + scale.y=-1 로 자식이 world (0..720, 0..-900) 차지.
            // 같은 영역을 검정으로 덮으려면 world 중심 (360, -450), scale 720x900 (양수).
            fill.transform.position = new Vector3(360f, -450f, 0f);
            fill.transform.localScale = new Vector3(720f, 900f, 1f);
            EditorUtility.SetDirty(sr);

            // ScreenRouter.playfieldBlackFill 재와이어 (참조 유효 보장)
            var rt = GameObject.Find("ScreenRouter");
            if (rt != null)
            {
                System.Type t = null;
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.View.ScreenRouter"); if (t != null) break; }
                var comp = rt.GetComponent(t);
                var f = t.GetField("playfieldBlackFill", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) { f.SetValue(comp, fill); EditorUtility.SetDirty(comp); }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] PlayfieldBlackFill → root (parent=null, world pos {fill.transform.position}, scale {fill.transform.localScale})");
        }

        // E8: 런타임 입력 흐름 진단 (FlowState, bar.X, pointer)
        [MenuItem("ArkanoidFix/E8. Diagnose input flow runtime")]
        public static void DiagnoseInputRuntime()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[ArkanoidFix][E8] Play 모드 전용"); return; }
            var gm = GameObject.Find("GameManager");
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.GameManager"); if (t != null) break; }
            var comp = gm.GetComponent(t);
            // GetFlowState() / GetGameplayState()
            var mFlow = t.GetMethod("GetFlowState");
            var flow = mFlow.Invoke(comp, null);
            var flowKind = flow.GetType().GetProperty("Kind")?.GetValue(flow);
            Debug.Log($"[ArkanoidFix][E8] FlowState.Kind={flowKind}");
            var mGame = t.GetMethod("GetGameplayState");
            var game = mGame.Invoke(comp, null);
            if (game != null)
            {
                var bar = game.GetType().GetField("Bar", BindingFlags.Public | BindingFlags.Instance)?.GetValue(game)
                       ?? game.GetType().GetProperty("Bar")?.GetValue(game);
                if (bar != null)
                {
                    var x = bar.GetType().GetField("X")?.GetValue(bar) ?? bar.GetType().GetProperty("X")?.GetValue(bar);
                    var y = bar.GetType().GetField("Y")?.GetValue(bar) ?? bar.GetType().GetProperty("Y")?.GetValue(bar);
                    Debug.Log($"[ArkanoidFix][E8] bar.X={x} bar.Y={y}");
                }
            }
            // Pointer/Keyboard 는 reflection 으로 (InputSystem asmdef 참조 회피)
            try
            {
                var pointerType = System.Type.GetType("UnityEngine.InputSystem.Pointer, Unity.InputSystem");
                var p = pointerType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                Debug.Log($"[ArkanoidFix][E8] Pointer.current={(p != null ? "OK" : "NULL")}");
                var kbType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
                var k = kbType?.GetProperty("current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                Debug.Log($"[ArkanoidFix][E8] Keyboard.current={(k != null ? "OK" : "NULL")}");
            }
            catch (System.Exception e) { Debug.LogWarning($"[ArkanoidFix][E8] input reflection: {e.Message}"); }
        }

        // C1: TitlePanel StartText 비활성 (transform.Find — 비활성 자식도 검색)
        [MenuItem("ArkanoidFix/C1. Hide TitlePanel StartText")]
        public static void HideStartText()
        {
            var canvas = GameObject.Find("Canvas");
            var tp = canvas != null ? canvas.transform.Find("TitlePanel") : null;
            var st = tp != null ? tp.Find("StartText") : null;
            if (st == null) { Debug.LogWarning("[ArkanoidFix] StartText not found"); return; }
            st.gameObject.SetActive(false);
            EditorUtility.SetDirty(st.gameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] StartText 비활성");
        }

        // C2: HUD score/highScore 좌측 정렬 (같은 X). TS LayoutConfig hud.leftX=80 기준 → Unity anchor center 변환.
        [MenuItem("ArkanoidFix/C2. HUD score/highScore 좌측 정렬")]
        public static void AlignHudLeft()
        {
            // Canvas 1080×1920 anchor center → x = 80 - 540 = -460
            // SCORE 윗줄 (y=100 → 960-100=860), HIGH SCORE 아랫줄 또는 옆에
            var score = GameObject.Find("Canvas/InGamePanel/HudView/ScoreText") ?? GameObject.Find("HudView/ScoreText");
            var high = GameObject.Find("Canvas/InGamePanel/HudView/HighScoreText") ?? GameObject.Find("HudView/HighScoreText");
            if (score == null || high == null) { Debug.LogError("[ArkanoidFix] HUD Score/HighScoreText not found"); return; }
            int adjusted = 0;
            foreach (var (go, anchored) in new[] {
                (score, new Vector2(-460f, 770f)),    // SCORE 첫 줄 (y=190 → 960-190=770)
                (high,  new Vector2(-460f, 660f)),    // HIGH SCORE 둘째 줄 (y=300 → 660)
            })
            {
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 1f); // 좌상단 pivot — 시작 글자 같은 X
                rt.anchoredPosition = anchored;
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.TopLeft;
                EditorUtility.SetDirty(go);
                adjusted++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] HUD {adjusted}개 좌측 정렬 (-460, 770/660)");
        }

        // C10: 모든 Canvas TMP_Text 에 검은 얇은 외곽선 (TMP outline)
        [MenuItem("ArkanoidFix/C10. 글자 외곽선 일괄 (검정 얇게)")]
        public static void AddTextOutline()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[ArkanoidFix] Canvas not found"); return; }
            int n = 0;
            foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                t.outlineColor = new Color32(0, 0, 0, 255);
                t.outlineWidth = 0.2f;
                EditorUtility.SetDirty(t);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[ArkanoidFix] {n} TMP_Text 외곽선 추가 (검정 0.2)");
        }

        // C7: 디버깅용 — 현재 stage 강제 클리어 (Play 중)
        [MenuItem("ArkanoidFix/C7. (Play) Debug — force stage clear (next stage)")]
        public static void DebugForceStageClear()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[ArkanoidFix] Play 전용"); return; }
            var gm = GameObject.Find("GameManager");
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType("Arkanoid.Presentation.GameManager"); if (t != null) break; }
            var comp = gm?.GetComponent(t);
            t?.GetMethod("DebugForceStageClear")?.Invoke(comp, null);
            Debug.Log("[ArkanoidFix] DebugForceStageClear 호출");
        }

        // PauseButton 클릭 시 PauseOverlay 표시 (간단 — Flow 상태 미정이라 UI 토글만)
        [MenuItem("ArkanoidFix/C6w. Wire PauseButton → PauseOverlay toggle")]
        public static void WirePauseButton()
        {
            var pauseBtn = GameObject.Find("Canvas/PauseButton") ?? GameObject.Find("Canvas/MuteButton");
            if (pauseBtn == null) { Debug.LogError("[ArkanoidFix] PauseButton not found"); return; }
            var overlay = GameObject.Find("Canvas/PauseOverlay");
            if (overlay == null)
            {
                var canvas = GameObject.Find("Canvas");
                var poTr = canvas.transform.Find("PauseOverlay");
                overlay = poTr != null ? poTr.gameObject : null;
            }
            if (overlay == null) { Debug.LogError("[ArkanoidFix] PauseOverlay not found"); return; }
            var btn = pauseBtn.GetComponent<Button>();
            if (btn == null) { Debug.LogError("[ArkanoidFix] PauseButton has no Button"); return; }
            btn.onClick.RemoveAllListeners();
            var capturedOverlay = overlay;
            btn.onClick.AddListener(() => capturedOverlay.SetActive(true));
            EditorUtility.SetDirty(btn);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] PauseButton onClick → PauseOverlay.SetActive(true) wired");
        }

        // ── D1: 전수 자동 wire + 누락 UI 생성 ──
        [MenuItem("ArkanoidFix/D1. 전수조사 + 자동 wire + 누락 UI 생성")]
        public static void D1AutoWire()
        {
            var canvas = GameObject.Find("Canvas");
            var gm = GameObject.Find("GameManager");
            if (canvas == null || gm == null) { Debug.LogError("[D1] Canvas/GameManager not found"); return; }
            var gmComp = gm.GetComponent(FindType("Arkanoid.Presentation.GameManager"));

            // 1) 모든 패널 gameManager 자동 wire
            foreach (var path in new[] { "TitlePanel", "IntroStoryPanel", "GameOverPanel", "GameClearPanel", "PauseOverlay" })
            {
                var go = canvas.transform.Find(path)?.gameObject;
                if (go == null) continue;
                AutoWireGameManager(go, gmComp);
            }

            // 2) GameOver / GameClear UI 생성
            EnsureGameOverPanel(canvas, gmComp);
            EnsureGameClearPanel(canvas, gmComp);

            // 3) IntroStory SkipButton
            EnsureIntroStorySkip(canvas, gmComp);

            // 4) PauseOverlay 4 버튼
            EnsurePauseOverlayButtons(canvas, gmComp);

            // 5) 전체 null SerializeField 진단
            DiagnoseNullSerializeFields(canvas);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[D1] 자동 wire + UI 생성 완료");
        }

        private static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static void AutoWireGameManager(GameObject panel, Component gmComp)
        {
            foreach (var mb in panel.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var f = mb.GetType().GetField("gameManager", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.GetValue(mb) == null)
                {
                    f.SetValue(mb, gmComp);
                    EditorUtility.SetDirty(mb);
                    Debug.Log($"[D1] {panel.name}.{mb.GetType().Name}.gameManager wired");
                }
            }
        }

        private static GameObject EnsureChildButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, 3);

            var labelGo = go.transform.Find("Label")?.gameObject ??
                          new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.GetComponent<TMP_Text>() ?? labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 36f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineColor = Color.black;
            tmp.outlineWidth = 0.2f;
            var lFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DnfDynPath);
            if (lFont != null) tmp.font = lFont;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(go);
            return go;
        }

        private static GameObject EnsureChildImage(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static void EnsureGameOverPanel(GameObject canvas, Component gmComp)
        {
            var panel = canvas.transform.Find("GameOverPanel");
            if (panel == null) return;
            var t = FindType("Arkanoid.Presentation.View.GameOverPanel");
            var comp = panel.GetComponent(t);

            // Retry/Title 버튼
            var retry = EnsureChildButton(panel, "RetryButton", new Vector2(-180, -400), new Vector2(280, 80), "TAP TO RETRY", new Color(0.27f, 0.67f, 0.27f, 1f));
            var title = EnsureChildButton(panel, "TitleButton", new Vector2(180, -400), new Vector2(280, 80), "TAP TO TITLE", new Color(0.27f, 0.27f, 0.67f, 1f));
            t.GetField("retryButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, retry.GetComponent<Button>());
            t.GetField("titleButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, title.GetComponent<Button>());

            // 마스코트 3개 + 4프레임
            var frames = new Sprite[] {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_3.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_4.png"),
            };
            var imgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var m = EnsureChildImage(panel, $"Mascot{i}", new Vector2(-200 + i * 200, 100), new Vector2(180, 180));
                var img = m.GetComponent<Image>();
                if (frames[0] != null) img.sprite = frames[0];
                img.preserveAspect = true;
                imgs[i] = img;
            }
            t.GetField("mascotImages", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, imgs);
            t.GetField("mascotFrames", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, frames);
            EditorUtility.SetDirty(comp);
            Debug.Log("[D1] GameOverPanel UI 생성 + wired");
        }

        private static void EnsureGameClearPanel(GameObject canvas, Component gmComp)
        {
            var panel = canvas.transform.Find("GameClearPanel");
            if (panel == null) return;
            var t = FindType("Arkanoid.Presentation.View.GameClearPanel");
            var comp = panel.GetComponent(t);

            var retry = EnsureChildButton(panel, "RetryButton", new Vector2(-180, -400), new Vector2(280, 80), "TAP TO RETRY", new Color(0.27f, 0.67f, 0.27f, 1f));
            var title = EnsureChildButton(panel, "TitleButton", new Vector2(180, -400), new Vector2(280, 80), "TAP TO TITLE", new Color(0.27f, 0.27f, 0.67f, 1f));
            t.GetField("retryButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, retry.GetComponent<Button>());
            t.GetField("titleButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, title.GetComponent<Button>());

            // 마스코트 (GameOver sprite 재사용 — gameclear 전용 sprite 없음)
            var frames = new Sprite[] {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_3.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mascots/GameOver/gameover_frame_4.png"),
            };
            var imgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var m = EnsureChildImage(panel, $"Mascot{i}", new Vector2(-200 + i * 200, 100), new Vector2(180, 180));
                var img = m.GetComponent<Image>();
                if (frames[0] != null) img.sprite = frames[0];
                img.preserveAspect = true;
                imgs[i] = img;
            }
            t.GetField("mascotImages", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, imgs);
            t.GetField("mascotFrames", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, frames);
            EditorUtility.SetDirty(comp);
            Debug.Log("[D1] GameClearPanel UI 생성 + wired");
        }

        private static void EnsureIntroStorySkip(GameObject canvas, Component gmComp)
        {
            var panel = canvas.transform.Find("IntroStoryPanel");
            if (panel == null) return;
            var t = FindType("Arkanoid.Presentation.View.IntroStoryPanel");
            var comp = panel.GetComponent(t);
            var btn = EnsureChildButton(panel, "SkipButton", new Vector2(400, 800), new Vector2(160, 60), "SKIP", new Color(0.2f, 0.2f, 0.2f, 0.8f));
            t.GetField("skipButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, btn.GetComponent<Button>());
            EditorUtility.SetDirty(comp);
            Debug.Log("[D1] IntroStory SkipButton 생성 + wired");
        }

        private static void EnsurePauseOverlayButtons(GameObject canvas, Component gmComp)
        {
            var panel = canvas.transform.Find("PauseOverlay");
            if (panel == null) return;
            var t = FindType("Arkanoid.Presentation.View.PauseOverlay");
            var comp = panel.GetComponent(t);
            var bgm = EnsureChildButton(panel, "BgmButton", new Vector2(-180, 100), new Vector2(280, 100), "배경음\nON", new Color(0.1f, 0.16f, 0.23f, 1f));
            var sfx = EnsureChildButton(panel, "SfxButton", new Vector2(180, 100), new Vector2(280, 100), "효과음\nON", new Color(0.1f, 0.16f, 0.23f, 1f));
            var resume = EnsureChildButton(panel, "ResumeButton", new Vector2(-180, -100), new Vector2(280, 100), "RESUME", new Color(0.27f, 0.67f, 0.27f, 1f));
            var title = EnsureChildButton(panel, "QuitToTitleButton", new Vector2(180, -100), new Vector2(280, 100), "QUIT", new Color(0.8f, 0.27f, 0.27f, 1f));
            t.GetField("bgmButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, bgm.GetComponent<Button>());
            t.GetField("bgmLabel", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, bgm.GetComponentInChildren<TMP_Text>());
            t.GetField("sfxButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, sfx.GetComponent<Button>());
            t.GetField("sfxLabel", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, sfx.GetComponentInChildren<TMP_Text>());
            t.GetField("resumeButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, resume.GetComponent<Button>());
            t.GetField("titleButton", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(comp, title.GetComponent<Button>());
            EditorUtility.SetDirty(comp);
            Debug.Log("[D1] PauseOverlay 4버튼 생성 + wired");
        }

        private static void DiagnoseNullSerializeFields(GameObject canvas)
        {
            int nullCount = 0;
            foreach (var mb in canvas.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var typeName = mb.GetType().FullName ?? "";
                if (!typeName.StartsWith("Arkanoid.")) continue;
                foreach (var f in mb.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!f.IsDefined(typeof(SerializeField), false)) continue;
                    var v = f.GetValue(mb);
                    bool isNull = v is Object uo ? uo == null : v == null;
                    if (isNull) { Debug.LogWarning($"[D1-null] {mb.gameObject.name}/{mb.GetType().Name}.{f.Name}"); nullCount++; }
                }
            }
            Debug.Log($"[D1] Null SerializeField 합계: {nullCount}");
        }

        [MenuItem("ArkanoidFix/9. Save active scene")]
        public static void SaveScene()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[ArkanoidFix] Scene saved");
        }
    }
}

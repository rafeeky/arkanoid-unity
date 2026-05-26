// SPDX-License-Identifier: MIT
// ArkanoidParityFix — TS Phaser parity 작업. 사용자 8가지 지적 fix 일괄 적용.
// Menu: Arkanoid > Parity Fix (Run All)
//
// 작업 목록:
//   1. 모든 sprite PNG 의 import settings (Sprite, PPU=1, Bilinear/Point) set
//   2. Block prefab 의 SpriteRenderer 색 reset (white) + scale (1,1,1)
//   3. Bar prefab 의 body sprite native size 별 referenceWidthPx 자동 fit (코드 default=1 사용)
//   4. BlocksRenderer 의 spriteEntries 자동 wiring (block_basic .. block_tough)
//   5. MascotRenderer 의 mascotSprites 자동 wiring (albatross 4 frame)
//   6. MascotRenderer 를 PlayfieldRoot 밖으로 reparent (Canvas world space 좌표로)
//   7. IntroSequenceSO asset 의 4 entries 채움 (TS IntroSequenceTable 그대로)
//   8. Canvas 안 TitlePanel 의 배경 Image source 를 bg_title sprite 로 set
//   9. RoundIntroPanel 의 RoundLabel / ReadyLabel anchoredPosition fix
//   10. HudView 자식 5 텍스트의 anchoredPosition + anchor 를 LayoutConfig 좌표로 fix
//   11. TitlePanel 의 4 텍스트 폰트 size + 위치 + (옵션) DNFBitBitv2 폰트 적용
//   12. (옵션) 마스코트 좌/우 토글 UI 를 Title 에 추가

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Arkanoid.Definitions.SO;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

internal static class SpriteLoader
{
    /// <summary>
    /// LoadAssetAtPath&lt;Sprite&gt;() 가 fileID 21300000 (texture) 을 반환하는 경우가 있어
    /// LoadAllAssetsAtPath 로 Sprite sub-asset 을 명시적으로 찾는다.
    /// </summary>
    public static Sprite Load(string assetPath)
    {
        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (direct != null) return direct;
        var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var a in all)
        {
            if (a is Sprite sp) return sp;
        }
        return null;
    }
}

public static class ArkanoidParityFix
{
    [MenuItem("Arkanoid/Parity Fix (Run All)")]
    public static void RunAll()
    {
        FixSpriteImporters();
        FixBlockPrefab();
        FixBlocksRenderer();
        FixMascotRenderer();
        FixMascotPosition();
        FillIntroSequence();
        FixTitlePanelBackgroundAndFonts();
        FixRoundIntroPanel();
        FixHudPositions();
        AssetDatabase.SaveAssets();

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[ArkanoidParityFix] Done.");
    }

    // ── 1. Sprite importer ─────────────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/1. Sprite Importers")]
    public static void FixSpriteImporters()
    {
        var paths = new (string path, FilterMode filter)[]
        {
            ("Assets/Sprites/Blocks/block_basic.png",       FilterMode.Bilinear),
            ("Assets/Sprites/Blocks/block_basic_drop.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Blocks/block_magnet_drop.png", FilterMode.Bilinear),
            ("Assets/Sprites/Blocks/block_laser_drop.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Blocks/block_tough.png",       FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_title.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_stage_01.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_stage_02.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_stage_03.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_gameover.png",  FilterMode.Bilinear),
            ("Assets/Sprites/Backgrounds/bg_gameclear.png", FilterMode.Bilinear),
            ("Assets/Sprites/Mascots/albatross/frame0.png", FilterMode.Point),
            ("Assets/Sprites/Mascots/albatross/frame1.png", FilterMode.Point),
            ("Assets/Sprites/Mascots/albatross/frame2.png", FilterMode.Point),
            ("Assets/Sprites/Mascots/albatross/frame3.png", FilterMode.Point),
            ("Assets/Sprites/Gameplay/item_expand.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Gameplay/item_magnet.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Gameplay/item_laser.png",      FilterMode.Bilinear),
            ("Assets/Sprites/Intro/intro_story_01.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Intro/intro_story_02.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Intro/intro_story_03.png",     FilterMode.Bilinear),
            ("Assets/Sprites/Intro/intro_story_04.png",     FilterMode.Bilinear),
        };
        foreach (var (p, fm) in paths)
        {
            if (!File.Exists(p)) continue;
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1f;
            imp.filterMode = fm;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
        Debug.Log("[ArkanoidParityFix] (1) Sprite importers set.");
    }

    // ── 2. Block prefab + Bar prefab + Ball prefab scale reset ─────────────

    [MenuItem("Arkanoid/Parity Fix/2. Prefab Scales")]
    public static void FixBlockPrefab()
    {
        FixSinglePrefabScale("Assets/Prefabs/Block.prefab", true);
        FixSinglePrefabScale("Assets/Prefabs/Bar.prefab", false); // bar 는 body 자식 SR 사용
        // Ball 은 16×16 fix scale 유지 (BallsRenderer 가 scale 안 건드림).
        Debug.Log("[ArkanoidParityFix] (2) Prefab scales reset.");
    }

    static void FixSinglePrefabScale(string path, bool resetSpriteColor)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) { Debug.LogWarning($"[ArkanoidParityFix] {path} not found."); return; }
        var contents = PrefabUtility.LoadPrefabContents(path);
        if (resetSpriteColor)
        {
            var sr = contents.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.color = Color.white;
        }
        // root scale (1,1,1) — Renderer 코드가 매 프레임 sprite native size 기반 scale 산출.
        contents.transform.localScale = Vector3.one;
        // body 자식이 있으면 그 scale 도 (1,1,1) — BarRenderer 가 body.transform.localScale 만 사용.
        foreach (Transform child in contents.transform)
        {
            child.localScale = Vector3.one;
        }
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ── 3. BlocksRenderer wiring ───────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/3. BlocksRenderer Wiring")]
    public static void FixBlocksRenderer()
    {
        var pf = GameObject.Find("PlayfieldRoot");
        if (pf == null) return;
        var br = pf.GetComponentInChildren<BlocksRenderer>(true);
        if (br == null) { Debug.LogWarning("[ArkanoidParityFix] BlocksRenderer not found."); return; }

        var pairs = new (string defId, string spritePath)[]
        {
            ("basic",       "Assets/Sprites/Blocks/block_basic.png"),
            ("basic_drop",  "Assets/Sprites/Blocks/block_basic_drop.png"),
            ("magnet_drop", "Assets/Sprites/Blocks/block_magnet_drop.png"),
            ("laser_drop",  "Assets/Sprites/Blocks/block_laser_drop.png"),
            ("tough",       "Assets/Sprites/Blocks/block_tough.png"),
        };

        var so = new SerializedObject(br);
        var arr = so.FindProperty("spriteEntries");
        arr.arraySize = pairs.Length;
        for (int i = 0; i < pairs.Length; i++)
        {
            var (defId, path) = pairs[i];
            var sp = SpriteLoader.Load(path);
            var elem = arr.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("DefinitionId").stringValue = defId;
            elem.FindPropertyRelative("Sprite").objectReferenceValue = sp;
        }
        so.FindProperty("blockWidthPx").floatValue = 64f;
        so.FindProperty("blockHeightPx").floatValue = 24f;
        so.ApplyModifiedProperties();
        Debug.Log("[ArkanoidParityFix] (3) BlocksRenderer wired.");
    }

    // ── 4. MascotRenderer wiring ───────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/4. MascotRenderer Wiring")]
    public static void FixMascotRenderer()
    {
        var mr = Object.FindFirstObjectByType<MascotRenderer>();
        if (mr == null) { Debug.LogWarning("[ArkanoidParityFix] MascotRenderer not found."); return; }

        var so = new SerializedObject(mr);
        var arr = so.FindProperty("mascotSprites");
        arr.arraySize = 1; // 일단 albatross 만. 나머지 마스코트 sprite 추가 시 늘리면 됨.

        var elem = arr.GetArrayElementAtIndex(0);
        elem.FindPropertyRelative("MascotId").stringValue = "albatross";
        elem.FindPropertyRelative("Frame0").objectReferenceValue = SpriteLoader.Load("Assets/Sprites/Mascots/albatross/frame0.png");
        elem.FindPropertyRelative("Frame1").objectReferenceValue = SpriteLoader.Load("Assets/Sprites/Mascots/albatross/frame1.png");
        elem.FindPropertyRelative("Frame2").objectReferenceValue = SpriteLoader.Load("Assets/Sprites/Mascots/albatross/frame2.png");
        elem.FindPropertyRelative("Frame3").objectReferenceValue = SpriteLoader.Load("Assets/Sprites/Mascots/albatross/frame3.png");

        so.FindProperty("frameIntervalSec").floatValue = 0.2f;
        so.FindProperty("mascotSizePx").floatValue = 200f;
        so.FindProperty("flipX").boolValue = true;
        so.ApplyModifiedProperties();
        Debug.Log("[ArkanoidParityFix] (4) MascotRenderer wired.");
    }

    // ── 5. MascotRenderer reparent (PlayfieldRoot → world, canvas 좌표) ────

    [MenuItem("Arkanoid/Parity Fix/5. Mascot Position")]
    public static void FixMascotPosition()
    {
        var mr = Object.FindFirstObjectByType<MascotRenderer>();
        if (mr == null) return;
        var t = mr.transform;

        // Canvas 좌상단 (0,0)→ world (0, canvas.height). World 좌표계는 1 unit = 1 px.
        // TS LayoutConfig: mascotCenterX=900, mascotCenterY=200 (캔버스 좌표, y down).
        // World y = canvas.height(1920) - 200 = 1720.
        // PlayfieldRoot 에서 떼서 root 에 둠 — PlayfieldRoot scale.y=-1 영향 차단.
        if (t.parent != null && t.parent.name == "PlayfieldRoot")
        {
            t.SetParent(null, worldPositionStays: false);
        }
        t.localPosition = new Vector3(900f, 1720f, 0f);
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;

        // SpriteRenderer 의 sortingOrder 를 높여서 background 위에 그려지도록.
        var sr = mr.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
        }
        Debug.Log("[ArkanoidParityFix] (5) Mascot repositioned to (900, 1720).");
    }

    // ── 6. IntroSequenceSO entries ─────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/6. Intro Sequence")]
    public static void FillIntroSequence()
    {
        var so = AssetDatabase.LoadAssetAtPath<IntroSequenceSO>("Assets/Data/Presentation/IntroSequenece_Mini.asset");
        if (so == null) { Debug.LogWarning("[ArkanoidParityFix] IntroSequenece_Mini.asset not found."); return; }

        var serSO = new SerializedObject(so);
        var entries = serSO.FindProperty("entries");
        var texts = new[]
        {
            "알바트로스는 날개를 펼치면 3.5미터에 이르는 놀라운 비행 능력을 가진 새입니다.",
            "알바트로스는 바다 위를 수천 킬로미터 비행하며 먹이를 찾습니다.",
            "일부 알바트로스는 먹이를 찾아 한 번에 지구를 한 바퀴 돌기도 합니다.",
            "알바트로스의 놀라운 비행 능력을 기억해 주세요!",
        };
        entries.arraySize = texts.Length;
        for (int i = 0; i < texts.Length; i++)
        {
            var e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("pageIndex").intValue = i;
            e.FindPropertyRelative("text").stringValue = texts[i];
            e.FindPropertyRelative("typingSpeedMs").floatValue = 40f;
            e.FindPropertyRelative("holdDurationMs").floatValue = 1800f;
            e.FindPropertyRelative("eraseSpeedMs").floatValue = 20f;
        }
        serSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(so);
        Debug.Log("[ArkanoidParityFix] (6) IntroSequence 4 entries filled.");
    }

    // ── 7. TitlePanel background + fonts ───────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/7. Title Panel")]
    public static void FixTitlePanelBackgroundAndFonts()
    {
        var titlePanelGO = GameObject.Find("TitlePanel");
        if (titlePanelGO == null) { Debug.LogWarning("[ArkanoidParityFix] TitlePanel not found."); return; }

        // 배경 Image → bg_title sprite. SerializedObject 통해 set + SetDirty 명시.
        var img = titlePanelGO.GetComponent<Image>();
        if (img != null)
        {
            var bg = SpriteLoader.Load("Assets/Sprites/Backgrounds/bg_title.png");
            if (bg == null) Debug.LogWarning("[ArkanoidParityFix] bg_title sprite not loaded.");
            else
            {
                var imgSO = new SerializedObject(img);
                imgSO.FindProperty("m_Sprite").objectReferenceValue = bg;
                imgSO.FindProperty("m_PreserveAspect").boolValue = false;
                imgSO.FindProperty("m_Color").colorValue = Color.white;
                imgSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(img);
                Debug.Log($"[ArkanoidParityFix] bg_title sprite set: {bg.name} ({bg.rect.width}x{bg.rect.height})");
            }
        }
        // RectTransform 을 full-stretch 로
        var rt = titlePanelGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // 폰트 — DNFBitBitv2 TMP asset 이 있으면 사용, 없으면 그대로 (Malgun fallback).
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DNFBitBitv2 SDF.asset");

        // 텍스트 위치/크기 — TS renderTitleScreen 참조.
        // canvas 1080×1920 기준. RectTransform anchor 가 중심 (0.5, 0.5) 일 때 (0,0) = 캔버스 중심 (540, 960).
        // 따라서 anchoredPosition.y = canvasCenterY - targetY = 960 - targetY.
        ApplyTitleText(titlePanelGO, "LogoText",      "ALBATROSS",       110, 960 - 260,   font, Color.white);
        ApplyTitleText(titlePanelGO, "HighScoreText", "HIGH SCORE  0",   36,  960 - 1137,  font, new Color(1f, 1f, 0f, 1f));
        ApplyTitleText(titlePanelGO, "DifficultyText","[NORMAL]  ←→  HARD", 44, 960 - 1655, font, Color.white);
        ApplyTitleText(titlePanelGO, "StartText",     "PRESS SPACE TO START", 36, 960 - 1830, font, new Color(0.8f, 0.8f, 0.8f, 1f));

        Debug.Log("[ArkanoidParityFix] (7) TitlePanel bg + fonts applied.");
    }

    static void ApplyTitleText(GameObject parent, string name, string defaultText, int fontSize, float anchoredY, TMP_FontAsset font, Color color)
    {
        var t = parent.transform.Find(name);
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);
            rt.sizeDelta = new Vector2(1000f, fontSize * 1.5f);
        }
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            if (!string.IsNullOrEmpty(defaultText) && string.IsNullOrEmpty(tmp.text)) tmp.text = defaultText;
        }
    }

    // ── 8. RoundIntroPanel ────────────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/8. RoundIntro Panel")]
    public static void FixRoundIntroPanel()
    {
        var go = GameObject.Find("RoundIntroPanel");
        if (go == null) return;
        var rtPanel = go.GetComponent<RectTransform>();
        if (rtPanel != null)
        {
            rtPanel.anchorMin = new Vector2(0f, 0f);
            rtPanel.anchorMax = new Vector2(1f, 1f);
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;
        }
        // ROUND 라벨 — TS 좌표 cy=1080 → anchoredY = 960 - 1080 = -120.
        ApplyAnchoredPos(go, "RoundLabel", new Vector2(0f, -120f), 72, Color.white);
        // READY 라벨 — TS cy=1180 → anchoredY = 960 - 1180 = -220.
        ApplyAnchoredPos(go, "ReadyLabel", new Vector2(0f, -220f), 48, new Color(0.53f, 0.8f, 1f, 1f));
        Debug.Log("[ArkanoidParityFix] (8) RoundIntroPanel positioned.");
    }

    static void ApplyAnchoredPos(GameObject parent, string name, Vector2 pos, int fontSize, Color color)
    {
        var t = parent.transform.Find(name);
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1000f, fontSize * 1.5f);
        }
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
        }
    }

    // ── 9. HudView 좌표 ────────────────────────────────────────────────────

    [MenuItem("Arkanoid/Parity Fix/9. HUD Positions")]
    public static void FixHudPositions()
    {
        var hudGO = GameObject.Find("HudView");
        if (hudGO == null) { Debug.LogWarning("[ArkanoidParityFix] HudView not found."); return; }

        // InGamePanel 자체를 full-stretch 로 (없으면 HUD 자식이 0 위치에서 시작 안 함).
        var inGameGO = GameObject.Find("InGamePanel");
        if (inGameGO != null)
        {
            var rt = inGameGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
        var hudRT = hudGO.GetComponent<RectTransform>();
        if (hudRT != null)
        {
            hudRT.anchorMin = new Vector2(0f, 0f);
            hudRT.anchorMax = new Vector2(1f, 1f);
            hudRT.offsetMin = Vector2.zero;
            hudRT.offsetMax = Vector2.zero;
        }

        // TS LayoutConfig (Canvas 좌표, y-down):
        //   SCORE      leftX=80,  valueY=170  (label top, value below)
        //   HIGH SCORE centerX=360, valueY=170
        //   ROUND      rightX=660, valueY=170
        // anchor = (0,1) top-left  → anchoredPosition (x, -y) in pixels from top-left.
        // 단순화: anchor = (0.5, 0.5) 중심 + 각 텍스트 별 offset 산출.
        // Canvas 중심 = (540, 960).
        //   ScoreText:    target canvas (80+200, 100+40) ≈ (280, 140) (label+value 합성).
        //                 anchored = (280-540, 960-140) = (-260, 820).
        //   HighScore:    target (540, 140). anchored = (0, 820).
        //   RoundText:    target (1000, 140). anchored = (460, 820).
        //   LivesText:    target (160, 1700). anchored = (-380, -740).
        //   EffectText:   target (540, 648). anchored = (0, 312).
        ApplyHudText(hudGO, "ScoreText",      36, new Vector2(-380f, 820f),  TextAlignmentOptions.Left, Color.white);
        ApplyHudText(hudGO, "HighScoreText",  36, new Vector2(0f,    820f),  TextAlignmentOptions.Center, new Color(1f, 0.2f, 0.2f));
        ApplyHudText(hudGO, "RoundText",      36, new Vector2(380f,  820f),  TextAlignmentOptions.Right, Color.white);
        ApplyHudText(hudGO, "LivesText",      32, new Vector2(-380f, -740f), TextAlignmentOptions.Left,  Color.white);
        ApplyHudText(hudGO, "EffectText",     28, new Vector2(0f,    312f),  TextAlignmentOptions.Center, new Color(0.53f, 0.8f, 1f));

        Debug.Log("[ArkanoidParityFix] (9) HUD positioned.");
    }

    static void ApplyHudText(GameObject parent, string name, int fontSize, Vector2 anchored, TextAlignmentOptions align, Color color)
    {
        var t = parent.transform.Find(name);
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(900f, fontSize * 2f);
        }
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
        }
    }
}

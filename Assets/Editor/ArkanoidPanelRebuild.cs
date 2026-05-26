using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArkanoidEditor
{
    /// <summary>
    /// TS renderTitleScreen.ts / renderIntroStoryScreen.ts / renderRoundIntroScreen.ts
    /// 1:1 매칭으로 누락된 UI element 들 build + 위치 정정.
    ///
    /// 좌표 변환: TS canvas (1080×1920), Unity Canvas pivot (0.5, 0.5) at (540, 960).
    /// Canvas anchor (0,1)=topLeft → anchoredPosition = (TS_x, -TS_y)
    /// Canvas anchor (0.5,0.5)=center → anchoredPosition = (TS_x - 540, 960 - TS_y)
    ///
    /// 여기서는 모든 RectTransform 을 TitlePanel 자식 (center 0.5,0.5) 기준으로 함:
    ///   anchoredPosition.x = TS_x - 540
    ///   anchoredPosition.y = 960 - TS_y
    /// </summary>
    public static class ArkanoidPanelRebuild
    {
        // TS canvasLayout: 1080×1920, CX=540
        private const float CX = 540f;
        private const float CY = 960f;

        [MenuItem("Arkanoid/Panel Rebuild/Run All")]
        public static void RunAll()
        {
            RebuildTitle();
            FixIntroStory();
            FixRoundIntro();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PanelRebuild] Run All done.");
        }

        // ============ Title: 누락된 element build + 위치 정정 ============
        // 누락: POWERUPS 3, NORMAL/HARD 버튼 2, prev/next 화살표 2, MascotInfoPanel/PowerupsPanel 반투명
        // 위치 정정: LogoText (540, 260) font 110, HighScoreText (540, 1137) font 36 yellow, GOLD 추가
        [MenuItem("Arkanoid/Panel Rebuild/A. Title")]
        public static void RebuildTitle()
        {
            var titlePanel = FindRoot("TitlePanel");
            if (titlePanel == null) { Debug.LogError("[PanelRebuild] (A) TitlePanel not found"); return; }

            // 1) LogoText 위치 정정 — (540, 260), font 110 bold white
            var logo = FindChild(titlePanel, "LogoText");
            if (logo != null)
            {
                SetCenterPos(logo.GetComponent<RectTransform>(), CX, 260, 800, 140);
                var tmp = logo.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "ALBATROSS";
                    tmp.fontSize = 110;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // 2) HighScoreText 위치 정정 — (540, 1137), font 36 yellow bold
            var hi = FindChild(titlePanel, "HighScoreText");
            if (hi != null)
            {
                SetCenterPos(hi.GetComponent<RectTransform>(), CX, 1137, 800, 60);
                var tmp = hi.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 36;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color32(0xFF, 0xFF, 0x00, 0xFF);
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // 3) DifficultyText 비활성 (NORMAL/HARD 두 버튼이 대신)
            var dt = FindChild(titlePanel, "DifficultyText");
            if (dt != null) dt.SetActive(false);

            // 4) Title 의 MascotImage 영구 비활성 (carousel 의 portrait 가 마스코트 표시)
            var mi = FindChild(titlePanel, "MascotImage");
            if (mi != null) mi.SetActive(false);

            // 5) MascotInfoPanel 반투명 (540, 985, 620×160) — depth -10 (Image, color black alpha 0.45)
            var mip = EnsureChild(titlePanel, "MascotInfoPanel", out var mipCreated);
            mip.transform.SetSiblingIndex(0);  // 뒤로
            SetCenterPos(mip.GetComponent<RectTransform>() ?? mip.AddComponent<RectTransform>(),
                CX, 985, 620, 160);
            var mipImg = mip.GetComponent<Image>() ?? mip.AddComponent<Image>();
            mipImg.color = new Color(0, 0, 0, 0.45f);
            mipImg.raycastTarget = false;

            // 6) PowerupsPanel 반투명 (540, 1340, 1010×260)
            var pup = EnsureChild(titlePanel, "PowerupsPanel", out _);
            pup.transform.SetSiblingIndex(1);
            SetCenterPos(pup.GetComponent<RectTransform>() ?? pup.AddComponent<RectTransform>(),
                CX, 1340, 1010, 260);
            var pupImg = pup.GetComponent<Image>() ?? pup.AddComponent<Image>();
            pupImg.color = new Color(0, 0, 0, 0.45f);
            pupImg.raycastTarget = false;

            // 7) POWERUPS 제목 (540, 1250) font 40 white bold
            var pt = EnsureChild(titlePanel, "PowerupsTitle", out _);
            SetCenterPos(EnsureRect(pt), CX, 1250, 600, 60);
            var ptTmp = EnsureTmp(pt);
            ptTmp.text = "POWERUPS";
            ptTmp.fontSize = 40;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.color = Color.white;
            ptTmp.alignment = TextAlignmentOptions.Center;

            // 8) POWERUPS 3개 항목 (EXPAND/MAGNET/LASER) — CX + (i-1)*320, y=1340
            string[] names = { "EXPAND", "MAGNET", "LASER" };
            string[] blockPaths = {
                "Assets/Sprites/Blocks/block_basic_drop.png",
                "Assets/Sprites/Blocks/block_magnet_drop.png",
                "Assets/Sprites/Blocks/block_laser_drop.png",
            };
            string[] descs = {
                "Bar grows wider\nfor easier rebound",
                "Catches ball.\nSPACE to launch",
                "Bar fires lasers.\nSPACE to shoot",
            };
            for (int i = 0; i < 3; i++)
            {
                float itemCx = CX + (i - 1) * 320f;
                float iconCx = itemCx - 70f;
                float iconLeftX = iconCx - 48f;
                float iconRightX = iconCx + 48f;

                // Block icon (96×36)
                var icon = EnsureChild(titlePanel, $"Powerup{i}_Icon", out _);
                SetCenterPos(EnsureRect(icon), iconCx, 1340, 96, 36);
                var iconImg = icon.GetComponent<Image>() ?? icon.AddComponent<Image>();
                iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(blockPaths[i]);
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;

                // Name (icon 우측, font 28 white bold)
                var nm = EnsureChild(titlePanel, $"Powerup{i}_Name", out _);
                var nmRt = EnsureRect(nm);
                nmRt.anchorMin = nmRt.anchorMax = new Vector2(0.5f, 0.5f);
                nmRt.pivot = new Vector2(0, 0.5f);
                nmRt.anchoredPosition = new Vector2(iconRightX + 32f - CX, CY - 1340f);
                nmRt.sizeDelta = new Vector2(200, 40);
                var nmTmp = EnsureTmp(nm);
                nmTmp.text = names[i];
                nmTmp.fontSize = 28;
                nmTmp.fontStyle = FontStyles.Bold;
                nmTmp.color = Color.white;
                nmTmp.alignment = TextAlignmentOptions.MidlineLeft;

                // Description (y=1425, font 20 white center)
                var dc = EnsureChild(titlePanel, $"Powerup{i}_Desc", out _);
                SetCenterPos(EnsureRect(dc), itemCx, 1425, 280, 60);
                var dcTmp = EnsureTmp(dc);
                dcTmp.text = descs[i];
                dcTmp.fontSize = 20;
                dcTmp.color = Color.white;
                dcTmp.alignment = TextAlignmentOptions.Center;
            }

            // 9) NORMAL/HARD 버튼 — (NORMAL CX=280, HARD CX=800, cy=1655, 490×170, font 44 bold white)
            CreateNormalHardButton(titlePanel, "NormalButton", "NORMAL PLAY", 280, 1655,
                new Color32(0x33, 0xAA, 0x55, 0xFF), new Color32(0x99, 0xFF, 0xBB, 0xFF));
            CreateNormalHardButton(titlePanel, "HardButton", "HARD PLAY", 800, 1655,
                new Color32(0xCC, 0x44, 0x44, 0xFF), new Color32(0xFF, 0xAA, 0xAA, 0xFF));

            // 10) prev/next 화살표 (CX±430, y=600, font 88 bold gray, MascotSelector 내가 아닌 TitlePanel 자식)
            CreateArrow(titlePanel, "PrevArrow", "<", CX - 430f, 600f);
            CreateArrow(titlePanel, "NextArrow", ">", CX + 430f, 600f);

            // 11) BUY 버튼 위치 정정 (540, 848, 320×60) — 기존 UnlockButton 찾아서
            var buy = FindChild(titlePanel, "UnlockButton");
            if (buy == null)
            {
                // MascotSelector 안에 있을 수도
                var sel = FindChild(titlePanel, "MascotSelector");
                if (sel != null) buy = FindChild(sel, "UnlockButton");
            }
            if (buy != null)
            {
                SetCenterPos(buy.GetComponent<RectTransform>(), CX, 848, 320, 60);
                var buyImg = buy.GetComponent<Image>();
                if (buyImg != null) buyImg.color = new Color32(0xFF, 0x88, 0x33, 0xFF);
            }

            // 12) GOLD 텍스트 (540, 1030, font 28 gold bold)
            var gold = EnsureChild(titlePanel, "GoldText", out _);
            SetCenterPos(EnsureRect(gold), CX, 1030, 400, 40);
            var goldTmp = EnsureTmp(gold);
            goldTmp.text = "GOLD: 0";
            goldTmp.fontSize = 28;
            goldTmp.fontStyle = FontStyles.Bold;
            goldTmp.color = new Color32(0xFF, 0xCC, 0x44, 0xFF);
            goldTmp.alignment = TextAlignmentOptions.Center;

            Debug.Log("[PanelRebuild] (A) Title: POWERUPS 3 + NORMAL/HARD 2 + 화살표 2 + 패널 2 + 로고/HIGH/GOLD 위치");
        }

        // ============ IntroStory: 풀스크린 bg + 텍스트 패널 위치 ============
        [MenuItem("Arkanoid/Panel Rebuild/B. IntroStory")]
        public static void FixIntroStory()
        {
            var p = FindRoot("IntroStoryPanel");
            if (p == null) { Debug.LogError("[PanelRebuild] (B) IntroStoryPanel not found"); return; }

            // 1) Panel 자체 Image (배경) 검정 알파 1 — 풀스크린이지만 illustration 이 위에 있으므로 OK
            var pImg = p.GetComponent<Image>();
            if (pImg != null) pImg.color = new Color(0, 0, 0, 1);

            // 2) Illustration RectTransform — 풀스크린 (1080×1920) at (540, 960)
            var ill = FindChild(p, "Illustration") ?? FindChild(p, "IllustrationImage");
            if (ill != null) SetCenterPos(ill.GetComponent<RectTransform>(), CX, 960, 1080, 1920);

            // 3) 텍스트박스 panel — (540, 1500, 1050×270, black alpha 0.6)
            var tb = EnsureChild(p, "StoryTextBackdrop", out _);
            tb.transform.SetSiblingIndex(1);
            SetCenterPos(EnsureRect(tb), CX, 1500, 1050, 270);
            var tbImg = tb.GetComponent<Image>() ?? tb.AddComponent<Image>();
            tbImg.color = new Color(0, 0, 0, 0.6f);
            tbImg.raycastTarget = false;

            // 4) bodyText 위치 (540, 1500, font 42 wrap 960)
            var bt = FindChild(p, "BodyText") ?? FindChild(p, "StoryText");
            if (bt != null)
            {
                SetCenterPos(bt.GetComponent<RectTransform>(), CX, 1500, 960, 230);
                var tmp = bt.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 42;
                    tmp.color = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableWordWrapping = true;
                }
            }

            Debug.Log("[PanelRebuild] (B) IntroStory: 풀스크린 bg + 반투명 텍스트 패널 + bodyText 정렬");
        }

        // ============ RoundIntro: panel 배경 비활성 (game world 보이게) + ROUND/READY 위치 ============
        [MenuItem("Arkanoid/Panel Rebuild/C. RoundIntro")]
        public static void FixRoundIntro()
        {
            var p = FindRoot("RoundIntroPanel");
            if (p == null) { Debug.LogError("[PanelRebuild] (C) RoundIntroPanel not found"); return; }

            // 1) panel 자체 Image 비활성 (overlay — game world 보여야 함)
            var pImg = p.GetComponent<Image>();
            if (pImg != null) pImg.enabled = false;

            // 2) ROUND label — (540, 1080, font 72 white bold, black stroke)
            var rl = FindChild(p, "RoundLabel");
            if (rl != null)
            {
                SetCenterPos(rl.GetComponent<RectTransform>(), CX, 1080, 800, 100);
                var tmp = rl.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 72;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.outlineColor = Color.black;
                    tmp.outlineWidth = 0.3f;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // 3) READY label — (540, 1180, font 48 sky blue, black stroke)
            var rd = FindChild(p, "ReadyLabel");
            if (rd != null)
            {
                SetCenterPos(rd.GetComponent<RectTransform>(), CX, 1180, 800, 80);
                var tmp = rd.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 48;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color32(0x88, 0xCC, 0xFF, 0xFF);
                    tmp.outlineColor = Color.black;
                    tmp.outlineWidth = 0.3f;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }

            Debug.Log("[PanelRebuild] (C) RoundIntro: panel 배경 비활성 + ROUND/READY 위치");
        }

        // ============ Helpers ============
        private static GameObject FindRoot(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == name && !EditorUtility.IsPersistent(g));
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

        private static GameObject EnsureChild(GameObject parent, string name, out bool created)
        {
            var existing = FindChild(parent, name);
            if (existing != null) { created = false; return existing; }
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            created = true;
            return go;
        }

        private static RectTransform EnsureRect(GameObject go) =>
            go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

        private static TextMeshProUGUI EnsureTmp(GameObject go) =>
            go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();

        private static void SetCenterPos(RectTransform rt, float tsX, float tsY, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(tsX - CX, CY - tsY);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void CreateNormalHardButton(GameObject parent, string name, string label,
            float tsX, float tsY, Color32 fillColor, Color32 strokeColor)
        {
            var go = EnsureChild(parent, name, out var created);
            SetCenterPos(EnsureRect(go), tsX, tsY, 490, 170);

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = fillColor;
            img.raycastTarget = true;

            // Outline (Border) — Image 의 stroke 가 안 됨, Outline component 추가
            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = strokeColor;
            outline.effectDistance = new Vector2(4, 4);

            // Button
            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label 자식
            var labelGo = EnsureChild(go, "Label", out _);
            SetCenterPos(EnsureRect(labelGo), tsX, tsY, 480, 160);
            // Re-center within parent (parent's pivot is center, so child anchored at parent center)
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(480, 160);

            var lt = EnsureTmp(labelGo);
            lt.text = label;
            lt.fontSize = 44;
            lt.fontStyle = FontStyles.Bold;
            lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            lt.raycastTarget = false;
        }

        private static void CreateArrow(GameObject parent, string name, string glyph, float tsX, float tsY)
        {
            var go = EnsureChild(parent, name, out _);
            SetCenterPos(EnsureRect(go), tsX, tsY, 120, 120);
            var tmp = EnsureTmp(go);
            tmp.text = glyph;
            tmp.fontSize = 88;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color32(0x88, 0x88, 0x88, 0xFF);
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}

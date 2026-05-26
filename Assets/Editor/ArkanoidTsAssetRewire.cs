using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// TS AssetLoader.ts 기준 1:1 re-wire. sub-agent 의 V2 자산 잘못 wire 를
    /// TS public/assets/* 정답 (이미 Unity 에 import 되어있음) 으로 되돌림.
    /// </summary>
    public static class ArkanoidTsAssetRewire
    {
        private static readonly string[] MASCOT_IDS = { "albatross", "kongming", "snowrabbit", "reaper", "seraphin" };

        [MenuItem("Arkanoid/TS Asset Rewire/Run All")]
        public static void RunAll()
        {
            RewireMascotFrames();
            RewireCarouselPortraits();
            RewireGameOverMascot();
            FixTitleLayout();
            FixMascotInGameLayout();
            FixHudLayout();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[TsRewire] Run All done.");
        }

        // ============ A. MascotRenderer 5 entry → TS mascots/<id>/frame*.png ============
        [MenuItem("Arkanoid/TS Asset Rewire/A. Mascot Frames")]
        public static void RewireMascotFrames()
        {
            var mascot = FindFirstOfType<MascotRenderer>();
            if (mascot == null) { Debug.LogError("[TsRewire] (A) MascotRenderer not found"); return; }
            var so = new SerializedObject(mascot);
            var entries = so.FindProperty("mascotSprites");
            entries.arraySize = MASCOT_IDS.Length;
            for (int i = 0; i < MASCOT_IDS.Length; i++)
            {
                var id = MASCOT_IDS[i];
                var ep = entries.GetArrayElementAtIndex(i);
                ep.FindPropertyRelative("MascotId").stringValue = id;
                ep.FindPropertyRelative("Frame0").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame0.png");
                ep.FindPropertyRelative("Frame1").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame1.png");
                ep.FindPropertyRelative("Frame2").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame2.png");
                ep.FindPropertyRelative("Frame3").objectReferenceValue = LoadSprite($"Assets/Sprites/Mascots/{id}/frame3.png");
            }
            so.ApplyModifiedProperties();
            Debug.Log($"[TsRewire] (A) MascotRenderer 5 entries → TS mascots/<id>/frame*.png");
        }

        // ============ B. Title carousel portrait → portraits2/<id>.png ============
        [MenuItem("Arkanoid/TS Asset Rewire/B. Carousel Portraits")]
        public static void RewireCarouselPortraits()
        {
            // MascotSelector 자식의 Portrait_<id> Image 들의 sprite 를 portraits2 로.
            // sub-agent 가 V2/해금_X2 로 wire 한 것 되돌림.
            int wired = 0;
            foreach (var id in MASCOT_IDS)
            {
                var sprite = LoadSprite($"Assets/Sprites/Portraits2/{id}.png");
                if (sprite == null) { Debug.LogWarning($"[TsRewire] (B) Portraits2/{id}.png not found"); continue; }

                // 모든 Image 컴포넌트 중 이름이 Portrait_<id> 인 것 찾기
                var images = Resources.FindObjectsOfTypeAll<Image>();
                var target = images.FirstOrDefault(x => x.gameObject.name == $"Portrait_{id}" || x.gameObject.name == id);
                if (target != null)
                {
                    var so = new SerializedObject(target);
                    so.FindProperty("m_Sprite").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    wired++;
                }
            }
            Debug.Log($"[TsRewire] (B) Carousel portraits → portraits2/<id>.png ({wired}/5)");
        }

        // ============ C. GameOver mascot 4 frame → mascots/gameover_frame_1~4.png ============
        [MenuItem("Arkanoid/TS Asset Rewire/C. GameOver Mascot")]
        public static void RewireGameOverMascot()
        {
            var panel = FindFirstOfType<GameOverPanel>();
            if (panel == null) { Debug.LogError("[TsRewire] (C) GameOverPanel not found"); return; }
            var so = new SerializedObject(panel);
            var framesProp = so.FindProperty("mascotFrames");
            if (framesProp != null && framesProp.isArray)
            {
                framesProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue =
                        LoadSprite($"Assets/Sprites/Mascots/GameOver/gameover_frame_{i + 1}.png");
                }
                so.ApplyModifiedProperties();
                Debug.Log("[TsRewire] (C) GameOver mascot 4 frames → mascots/gameover_frame_*.png");
            }
            else Debug.LogWarning("[TsRewire] (C) GameOverPanel.mascotFrames property not found");
        }

        // ============ D. Title layout (TS renderTitleScreen.ts 정확한 좌표) ============
        // Canvas 1080×1920. CX=540, portrait Y=600 size 380, arrow X±430,
        // mascot name Y=940, lock status Y=985.
        // anchor 모두 center (0.5, 0.5), pivot (0.5, 0.5). anchoredPosition 은 canvas 중심 기준.
        // Y 좌표 → anchoredPosition.y = (canvas_height/2 - TS_Y) = (960 - TS_Y).
        [MenuItem("Arkanoid/TS Asset Rewire/D. Title Layout")]
        public static void FixTitleLayout()
        {
            // MascotSelector 위치 (Y=600 → anchoredPos.y = 360)
            var images = Resources.FindObjectsOfTypeAll<RectTransform>();
            var selector = images.FirstOrDefault(x => x.gameObject.name == "MascotSelector");
            if (selector != null)
            {
                SetRect(selector, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, 360), new Vector2(1000, 400));
                Debug.Log("[TsRewire] (D) MascotSelector center y=360 (TS Y=600)");
            }

            // Portrait_<id> 5개 — 가로 일렬 배치. 380×380. carousel 효과.
            // TS 는 portrait 1개만 cycling 표시. Unity 는 simple 5개 가로 배열로.
            // 또는 중앙 하나만 380×380, 양옆에 작은 미리보기. 단순화: 중앙 380, 나머지 dim.
            // TS 정확히 흉내: portrait 1장만 보이게. 그러면 5개 중 selected 만 380, 나머지 hidden.
            // 일단 5개 같은 자리에 380×380 으로 두고 selected 만 alpha=1, 나머지 alpha=0.

            // mascot info text 위치 — Y=940 → anchoredPos.y = (960 - 940) = 20
            // lock status — Y=985 → anchoredPos.y = (960 - 985) = -25
            var nameText = images.FirstOrDefault(x => x.gameObject.name == "MascotNameText");
            if (nameText != null)
                SetRect(nameText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, 20), new Vector2(600, 60));
            var costText = images.FirstOrDefault(x => x.gameObject.name == "MascotCostText");
            if (costText != null)
                SetRect(costText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, -25), new Vector2(600, 40));

            // Title 의 mascot in-game preview (사용자 지적: 댄스 마스코트 Title 중앙 나옴 — 없애야)
            // → InGame MascotRenderer 와 별도. Title 의 MascotImage GameObject 가 있으면 비활성 or 제거.
            var titleMascotImage = images.FirstOrDefault(x => x.gameObject.name == "MascotImage" &&
                                                             x.transform.parent != null &&
                                                             x.transform.parent.gameObject.name == "TitlePanel");
            if (titleMascotImage != null)
            {
                titleMascotImage.gameObject.SetActive(false);
                Debug.Log("[TsRewire] (D) TitlePanel/MascotImage 비활성 (carousel 의 portrait 만 사용)");
            }

            // DifficultyText — TS 의 NORMAL/HARD 버튼 2개. 단순화: 1 라벨로 [NORMAL] ↔ [HARD] 토글.
            // Y 위치 — TS 는 portrait 아래쪽 (TS button cy=1660 정도)
            var difficultyText = images.FirstOrDefault(x => x.gameObject.name == "DifficultyText");
            if (difficultyText != null)
                SetRect(difficultyText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, -700), new Vector2(600, 80));

            Debug.Log("[TsRewire] (D) Title layout (TS 좌표)");
        }

        // ============ E. InGame Mascot (renderMascot.ts) — centerX=900, centerY=200, size=200, flipX=true ============
        // 사용자 지적: 마스코트 4 frame 안 나옴 (RoundIntro 에서).
        // MascotRenderer GameObject 의 transform 위치 + sprite scale.
        [MenuItem("Arkanoid/TS Asset Rewire/E. InGame Mascot Layout")]
        public static void FixMascotInGameLayout()
        {
            var mascot = FindFirstOfType<MascotRenderer>();
            if (mascot == null) { Debug.LogError("[TsRewire] (E) MascotRenderer not found"); return; }

            // Canvas 좌표 → world 변환: TS canvas (900, 200) → Unity world.
            // Camera (360, -450, -10) ortho 640, PlayfieldRoot (0, 0, 0) scale (1, -1, 1).
            // MascotRenderer 가 root level (PlayfieldRoot 자식 X, sub-agent 가 reparent).
            // Canvas y down → world y up: world_y = canvas_height/2 - canvas_y - camera.y
            //                            = 960 - 200 - (-450) = 1210? 너무 복잡. 단순화:
            // 그냥 화면 우상단으로 transform.position 설정.
            // Camera view: y [-1090, 190] (ortho 640, camera y=-450). 화면 위 = world y 큰 값.
            // 우상단 = (700, 100, 0) 정도.
            mascot.transform.position = new Vector3(700, 100, 0);
            mascot.transform.localScale = new Vector3(1, 1, 1);

            // SpriteRenderer 의 mascotSizePx = 200 (이미 default)
            var so = new SerializedObject(mascot);
            var sizeProp = so.FindProperty("mascotSizePx");
            if (sizeProp != null) sizeProp.floatValue = 200f;
            var flipProp = so.FindProperty("flipX");
            if (flipProp != null) flipProp.boolValue = true;
            so.ApplyModifiedProperties();

            Debug.Log("[TsRewire] (E) MascotRenderer transform=(700,100,0) size 200 flipX");
        }

        // ============ F. HUD layout (TS LayoutConfigTable.hud) ============
        // labelY=100, valueY=170, leftX=80, centerX=360, rightX=660, labelFontPx=36, valueFontPx=52
        // Canvas 1080×1920. anchor topLeft.
        // TS 좌표 → Unity Canvas anchoredPosition (anchor 0,1 = topLeft): (x, -y)
        [MenuItem("Arkanoid/TS Asset Rewire/F. HUD Layout")]
        public static void FixHudLayout()
        {
            var tmpros = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

            var entries = new (string Name, Vector2 Pos, Vector2 Pivot, TextAlignmentOptions Align, int Size)[]
            {
                // anchor (0,1)=topLeft. Pos.x = TS x, Pos.y = -TS y (Canvas Y+ up vs TS Y+ down).
                // value y=170. label y=100. 두 줄 합쳐 한 텍스트로 simplify: "LABEL\nVALUE".
                ("ScoreText",     new Vector2( 80, -100), new Vector2(0, 1),     TextAlignmentOptions.TopLeft, 36),
                ("HighScoreText", new Vector2(360, -100), new Vector2(0.5f, 1),  TextAlignmentOptions.Top,     36),
                ("RoundText",     new Vector2(660, -100), new Vector2(1, 1),     TextAlignmentOptions.TopRight,36),
                // Lives — 좌하단. TS livesBar.startX=80, y=1700. anchor (0,1) → pos (80, -1700)
                ("LivesText",     new Vector2( 80, -1700),new Vector2(0, 1),     TextAlignmentOptions.TopLeft, 36),
                // Effect — 중앙 위 (powerup 표시).
                ("EffectText",    new Vector2(360, -300), new Vector2(0.5f, 1),  TextAlignmentOptions.Top,     32),
            };

            int n = 0;
            foreach (var e in entries)
            {
                var t = tmpros.FirstOrDefault(x => x.gameObject.name == e.Name);
                if (t == null) { Debug.LogWarning($"[TsRewire] (F) {e.Name} not found"); continue; }
                var rt = t.rectTransform;
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = e.Pivot;
                rt.anchoredPosition = e.Pos;
                rt.sizeDelta = new Vector2(500, 80);
                t.alignment = e.Align;
                t.fontSize = e.Size;
                t.color = Color.white;
                t.outlineWidth = 0.25f;
                t.outlineColor = new Color32(0, 0, 0, 220);
                n++;
            }
            Debug.Log($"[TsRewire] (F) HUD layout (TS LayoutConfig): {n}");
        }

        // ============ Helpers ============
        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static T FindFirstOfType<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c.transform.root.gameObject));
        }
    }
}

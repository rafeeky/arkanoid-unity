using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

public static class RebuildPanels
{
    static System.Type GetMBType(string fullName)
    {
        foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            if (t.FullName == fullName) return t;
        Debug.LogWarning("[RP] MB type not found: " + fullName);
        return null;
    }

    [MenuItem("Tools/RebuildPanels")]
    public static void Run()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[RebuildPanels] Canvas not found"); return; }
        var ct = canvas.transform;

        var titleT   = GetMBType("Arkanoid.Presentation.View.TitlePanel");
        var introT   = GetMBType("Arkanoid.Presentation.View.IntroStoryPanel");
        var riT      = GetMBType("Arkanoid.Presentation.View.RoundIntroPanel");
        var hudT     = GetMBType("Arkanoid.Presentation.View.HudView");
        var igT      = GetMBType("Arkanoid.Presentation.View.InGamePanel");
        var goT      = GetMBType("Arkanoid.Presentation.View.GameOverPanel");
        var gcT      = GetMBType("Arkanoid.Presentation.View.GameClearPanel");
        var pauseT   = GetMBType("Arkanoid.Presentation.View.PauseOverlay");
        var srT      = GetMBType("Arkanoid.Presentation.View.ScreenRouter");
        var gmT      = GetMBType("Arkanoid.Presentation.GameManager");
        var toastT   = GetMBType("Arkanoid.Presentation.View.ToastView");

        if (titleT == null) { Debug.LogError("[RP] TitlePanel not found — abort"); return; }

        // TitlePanel
        var tGO   = MakeUI("TitlePanel",      ct);
        var stTmp = MakeTMP("StartText",      tGO.transform,  "시작하기");
        var hsTmp = MakeTMP("HighScoreText",  tGO.transform,  "최고기록: 0");
        var dfTmp = MakeTMP("DifficultyText", tGO.transform,  "Normal");
        tGO.AddComponent(titleT);

        // IntroStoryPanel
        var iGO   = MakeUI("IntroStoryPanel", ct);
        var bdTmp = MakeTMP("BodyText",       iGO.transform,  "START");
        if (introT != null) iGO.AddComponent(introT);

        // RoundIntroPanel
        var riGO  = MakeUI("RoundIntroPanel", ct);
        var rlTmp = MakeTMP("RoundLabel",     riGO.transform, "Round 1");
        var rdTmp = MakeTMP("ReadyLabel",     riGO.transform, "READY");
        if (riT != null) riGO.AddComponent(riT);

        // InGamePanel + HudView
        var igGO  = MakeUI("InGamePanel",     ct);
        var hudGO = MakeUI("HudView",         igGO.transform);
        var scTmp = MakeTMP("ScoreText",      hudGO.transform,"0");
        var hbTmp = MakeTMP("HighScoreText",  hudGO.transform,"BEST 0");
        var liTmp = MakeTMP("LivesText",      hudGO.transform,"♥♥♥");
        var rtTmp = MakeTMP("RoundText",      hudGO.transform,"R1");
        var efTmp = MakeTMP("EffectText",     hudGO.transform,"");
        if (hudT != null) hudGO.AddComponent(hudT);
        if (igT  != null) igGO.AddComponent(igT);

        // GameOverPanel
        var goGO  = MakeUI("GameOverPanel",   ct);
        var glTmp = MakeTMP("GameOverLabel",  goGO.transform, "GAME OVER");
        var gfTmp = MakeTMP("FinalScoreLabel",goGO.transform, "최종 점수: 0");
        var ghTmp = MakeTMP("HighScoreLabel", goGO.transform, "최고기록: 0");
        var grTmp = MakeTMP("RetryText",      goGO.transform, "다시 시작");
        if (goT != null) goGO.AddComponent(goT);

        // GameClearPanel
        var gcGO  = MakeUI("GameClearPanel",  ct);
        var gcHl  = MakeTMP("HeadlineLabel",  gcGO.transform, "GAME CLEAR!");
        var gcFi  = MakeTMP("FinalScoreLabel",gcGO.transform, "최종 점수: 0");
        var gcHi  = MakeTMP("HighScoreLabel", gcGO.transform, "최고기록: 0");
        var gcRe  = MakeTMP("RetryText",      gcGO.transform, "다음 라운드");
        if (gcT != null) gcGO.AddComponent(gcT);

        // PauseOverlay
        var pGO   = MakeUI("PauseOverlay",    ct);
        var plTmp = MakeTMP("Label",          pGO.transform,  "PAUSED");
        var phTmp = MakeTMP("HelpText",       pGO.transform,  "P: Resume / Q: Title");
        if (pauseT != null) pGO.AddComponent(pauseT);

        // Wire TMP fields
        Wire(tGO,   titleT,   "startText",      stTmp);
        Wire(tGO,   titleT,   "highScoreText",  hsTmp);
        Wire(tGO,   titleT,   "difficultyText", dfTmp);
        Wire(iGO,   introT,   "bodyText",       bdTmp);
        Wire(riGO,  riT,      "roundLabel",     rlTmp);
        Wire(riGO,  riT,      "readyLabel",     rdTmp);
        Wire(hudGO, hudT,     "scoreText",      scTmp);
        Wire(hudGO, hudT,     "highScoreText",  hbTmp);
        Wire(hudGO, hudT,     "livesText",      liTmp);
        Wire(hudGO, hudT,     "roundText",      rtTmp);
        Wire(hudGO, hudT,     "effectText",     efTmp);
        WireComp(igGO, igT,  "hudView",        hudGO, hudT);
        Wire(goGO,  goT,      "gameOverLabel",   glTmp);
        Wire(goGO,  goT,      "finalScoreLabel", gfTmp);
        Wire(goGO,  goT,      "highScoreLabel",  ghTmp);
        Wire(goGO,  goT,      "retryText",       grTmp);
        Wire(gcGO,  gcT,      "headlineLabel",   gcHl);
        Wire(gcGO,  gcT,      "finalScoreLabel", gcFi);
        Wire(gcGO,  gcT,      "highScoreLabel",  gcHi);
        Wire(gcGO,  gcT,      "retryText",       gcRe);
        Wire(pGO,   pauseT,   "label",           plTmp);
        Wire(pGO,   pauseT,   "helpText",        phTmp);

        // ScreenRouter wiring
        var srGO = GameObject.Find("ScreenRouter");
        if (srGO != null && srT != null)
        {
            WireComp(srGO, srT, "titlePanel",      tGO,  titleT);
            WireComp(srGO, srT, "introStoryPanel", iGO,  introT);
            WireComp(srGO, srT, "roundIntroPanel", riGO, riT);
            WireComp(srGO, srT, "inGamePanel",     igGO, igT);
            WireComp(srGO, srT, "gameOverPanel",   goGO, goT);
            WireComp(srGO, srT, "gameClearPanel",  gcGO, gcT);
            WireComp(srGO, srT, "pauseOverlay",    pGO,  pauseT);
            Debug.Log("[RebuildPanels] ScreenRouter wired");
        }
        else Debug.LogWarning("[RP] ScreenRouter not wired srGO=" + srGO + " srT=" + srT);

        // GameManager wiring
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null && gmT != null)
        {
            WireComp(gmGO, gmT, "screenRouter",    srGO, srT);
            WireComp(gmGO, gmT, "titlePanel",      tGO,  titleT);
            WireComp(gmGO, gmT, "introStoryPanel", iGO,  introT);
            WireComp(gmGO, gmT, "roundIntroPanel", riGO, riT);
            WireComp(gmGO, gmT, "inGamePanel",     igGO, igT);
            WireComp(gmGO, gmT, "gameOverPanel",   goGO, goT);
            WireComp(gmGO, gmT, "gameClearPanel",  gcGO, gcT);
            WireComp(gmGO, gmT, "pauseOverlay",    pGO,  pauseT);
            var toastGO = GameObject.Find("ToastView");
            if (toastGO != null) WireComp(gmGO, gmT, "toastView", toastGO, toastT);
            Debug.Log("[RebuildPanels] GameManager wired");
        }
        else Debug.LogWarning("[RP] GameManager not wired gmGO=" + gmGO + " gmT=" + gmT);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[RebuildPanels] DONE TitlePanel=" + tGO.GetInstanceID()
            + " InGame=" + igGO.GetInstanceID() + " HudView=" + hudGO.GetInstanceID());
    }

    static GameObject MakeUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text)
    {
        var go = MakeUI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        return tmp;
    }

    static void Wire(GameObject go, System.Type ct, string field, Component val)
    {
        if (ct == null || go == null) return;
        var c = go.GetComponent(ct);
        if (c == null) { Debug.LogWarning("[RP] no comp " + ct.Name + " on " + go.name); return; }
        var so = new SerializedObject(c);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[RP] no field " + field + " on " + ct.Name); return; }
        p.objectReferenceValue = val;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireComp(GameObject go, System.Type ct, string field, GameObject tgt, System.Type tgtT)
    {
        if (ct == null || go == null) return;
        var c = go.GetComponent(ct);
        if (c == null) { Debug.LogWarning("[RP] no comp " + ct.Name + " on " + go.name); return; }
        var so = new SerializedObject(c);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning("[RP] no field " + field + " on " + ct.Name); return; }
        if (tgt == null) { p.objectReferenceValue = null; }
        else p.objectReferenceValue = (tgtT != null) ? (UnityEngine.Object)tgt.GetComponent(tgtT) ?? tgt : tgt;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

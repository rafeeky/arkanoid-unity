using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    /// <summary>
    /// 전수조사 — 모든 핵심 MonoBehaviour 의 SerializeField wire 상태 dump.
    /// null reference / 빈 array list → 콘솔에 보고. 사용자 fix 우선순위 결정용.
    /// </summary>
    public static class ArkanoidWireAudit
    {
        [MenuItem("Arkanoid/Wire Audit/Text Content Audit")]
        public static void TextContentAudit()
        {
            Debug.Log("=== Text Content Audit ===");
            string[] panels = { "TitlePanel", "IntroStoryPanel", "RoundIntroPanel", "InGamePanel",
                                "GameOverPanel", "GameClearPanel", "PauseOverlay" };
            foreach (var panelName in panels)
            {
                var go = Resources.FindObjectsOfTypeAll<GameObject>()
                    .FirstOrDefault(g => g.name == panelName && !EditorUtility.IsPersistent(g));
                if (go == null) continue;
                foreach (var tmp in go.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    var text = tmp.text;
                    if (text != null && text.Length > 60) text = text.Substring(0, 60) + "...";
                    var fontName = tmp.font != null ? tmp.font.name : "NULL";
                    Debug.Log($"[Text] {panelName}/{tmp.gameObject.name}: \"{text}\" font={fontName} active={tmp.gameObject.activeInHierarchy}");
                }
            }
        }

        [MenuItem("Arkanoid/Wire Audit/Deep Audit Title portraits")]
        public static void DeepAuditTitlePortraits()
        {
            var tp = Resources.FindObjectsOfTypeAll<TitlePanel>()
                .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (tp == null) { Debug.LogError("[Audit] TitlePanel not found"); return; }
            var so = new SerializedObject(tp);
            var portraits = so.FindProperty("portraits");
            Debug.Log($"[Audit-Deep] portraits.arraySize = {portraits.arraySize}");
            for (int i = 0; i < portraits.arraySize; i++)
            {
                var p = portraits.GetArrayElementAtIndex(i);
                var id = p.FindPropertyRelative("MascotId").stringValue;
                var portraitImg = p.FindPropertyRelative("Portrait").objectReferenceValue;
                var anim = p.FindPropertyRelative("AnimFrames");
                int animNullCount = 0;
                for (int f = 0; f < anim.arraySize; f++)
                    if (anim.GetArrayElementAtIndex(f).objectReferenceValue == null) animNullCount++;
                Debug.Log($"[Audit-Deep] portraits[{i}] id={id} Portrait={(portraitImg!=null?portraitImg.name:"NULL")} AnimFrames={anim.arraySize-animNullCount}/{anim.arraySize}");
            }
            var mf = so.FindProperty("mascotFrames");
            int mfNull = 0;
            for (int f = 0; f < mf.arraySize; f++)
                if (mf.GetArrayElementAtIndex(f).objectReferenceValue == null) mfNull++;
            Debug.Log($"[Audit-Deep] mascotFrames = {mf.arraySize-mfNull}/{mf.arraySize}");
        }

        [MenuItem("Arkanoid/Wire Audit/Audit All")]
        public static void AuditAll()
        {
            Debug.Log("=== Arkanoid Wire Audit ===");

            AuditComponent<GameManager>();
            AuditComponent<ScreenRouter>();
            AuditComponent<TitlePanel>();
            AuditComponent<IntroStoryPanel>();
            AuditComponent<RoundIntroPanel>();
            AuditComponent<InGamePanel>();
            AuditComponent<GameOverPanel>();
            AuditComponent<GameClearPanel>();
            AuditComponent<PauseOverlay>();
            AuditComponent<BarRenderer>();
            AuditComponent<BallsRenderer>();
            AuditComponent<BlocksRenderer>();
            AuditComponent<BordersRenderer>();
            AuditComponent<DoorsRenderer>();
            AuditComponent<SpinnersRenderer>();
            AuditComponent<ItemsRenderer>();
            AuditComponent<LaserShotsRenderer>();
            AuditComponent<BallTrailRenderer>();
            AuditComponent<MascotRenderer>();
            AuditComponent<ToastView>();
        }

        private static void AuditComponent<T>() where T : Component
        {
            var instance = Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => c != null && !EditorUtility.IsPersistent(c));
            if (instance == null)
            {
                Debug.LogWarning($"[Audit] {typeof(T).Name}: ❌ NOT FOUND IN SCENE");
                return;
            }

            var so = new SerializedObject(instance);
            var iter = so.GetIterator();
            bool enterChildren = true;
            var missing = new List<string>();
            var emptyArrays = new List<string>();
            int okCount = 0;

            while (iter.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iter.name == "m_Script") continue;

                if (iter.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (iter.objectReferenceValue == null) missing.Add(iter.name);
                    else okCount++;
                }
                else if (iter.isArray && iter.propertyType != SerializedPropertyType.String)
                {
                    if (iter.arraySize == 0) emptyArrays.Add(iter.name);
                    else
                    {
                        int nullCount = 0;
                        for (int i = 0; i < iter.arraySize; i++)
                        {
                            var elem = iter.GetArrayElementAtIndex(i);
                            if (elem.propertyType == SerializedPropertyType.ObjectReference
                                && elem.objectReferenceValue == null)
                                nullCount++;
                        }
                        if (nullCount > 0) missing.Add($"{iter.name}[{nullCount}/{iter.arraySize}null]");
                        else okCount++;
                    }
                }
            }

            string status = (missing.Count == 0 && emptyArrays.Count == 0) ? "✅" : "❌";
            string msg = $"[Audit] {status} {typeof(T).Name} ({instance.gameObject.name}, active={instance.gameObject.activeSelf})";
            if (missing.Count > 0) msg += $" MISSING: {string.Join(",", missing)}";
            if (emptyArrays.Count > 0) msg += $" EMPTY: {string.Join(",", emptyArrays)}";
            msg += $" OK:{okCount}";
            Debug.Log(msg);
        }
    }
}

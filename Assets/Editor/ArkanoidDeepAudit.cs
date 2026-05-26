using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Arkanoid.Definitions.SO;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

namespace ArkanoidEditor
{
    public static class ArkanoidDeepAudit
    {
        [MenuItem("Arkanoid/Deep Audit/A+B. Renderer prefab + sprite")]
        public static void AuditRenderersDeep()
        {
            Debug.Log("=== Renderer Prefab + Sprite Audit ===");
            AuditRendererRefs<BlocksRenderer>();
            AuditRendererRefs<BallsRenderer>();
            AuditRendererRefs<BordersRenderer>();
            AuditRendererRefs<DoorsRenderer>();
            AuditRendererRefs<SpinnersRenderer>();
            AuditRendererRefs<ItemsRenderer>();
            AuditRendererRefs<LaserShotsRenderer>();
            AuditRendererRefs<BallTrailRenderer>();
            AuditRendererRefs<MascotRenderer>();
        }

        private static void AuditRendererRefs<T>() where T : Component
        {
            var inst = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (inst == null) { Debug.LogWarning($"[Deep] {typeof(T).Name} not found"); return; }
            var so = new SerializedObject(inst);
            var iter = so.GetIterator();
            bool enterChildren = true;
            while (iter.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iter.name == "m_Script") continue;
                if (iter.propertyType == SerializedPropertyType.ObjectReference && iter.objectReferenceValue != null)
                {
                    var path = AssetDatabase.GetAssetPath(iter.objectReferenceValue);
                    Debug.Log($"[Deep] {typeof(T).Name}.{iter.name} = {iter.objectReferenceValue.name} ({path})");
                }
                else if (iter.isArray && iter.propertyType != SerializedPropertyType.String)
                {
                    for (int i = 0; i < iter.arraySize; i++)
                    {
                        var elem = iter.GetArrayElementAtIndex(i);
                        if (elem.propertyType == SerializedPropertyType.ObjectReference && elem.objectReferenceValue != null)
                        {
                            var path = AssetDatabase.GetAssetPath(elem.objectReferenceValue);
                            Debug.Log($"[Deep] {typeof(T).Name}.{iter.name}[{i}] = {elem.objectReferenceValue.name} ({path})");
                        }
                    }
                }
            }
        }

        [MenuItem("Arkanoid/Deep Audit/C+D. SO data values")]
        public static void AuditSOValues()
        {
            Debug.Log("=== SO Data Audit ===");
            var gm = Resources.FindObjectsOfTypeAll<GameManager>().FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (gm == null) { Debug.LogError("[Deep] GameManager not found"); return; }
            var so = new SerializedObject(gm);

            string[] soFields = {
                "gameplayConfigSO", "normalDifficultySO", "hardDifficultySO",
                "expandItemSO", "magnetItemSO", "laserItemSO",
                "uiTextSO", "introSequenceSO", "audioCueSO", "layoutConfigSO",
            };
            foreach (var f in soFields)
            {
                var p = so.FindProperty(f);
                if (p?.objectReferenceValue != null)
                    Debug.Log($"[Deep] {f} = {p.objectReferenceValue.name} ({AssetDatabase.GetAssetPath(p.objectReferenceValue)})");
                else
                    Debug.LogWarning($"[Deep] {f} = NULL");
            }
            string[] soArrays = { "blockDefinitionSOs", "spinnerDefinitionSOs", "stageDefinitionSOs" };
            foreach (var f in soArrays)
            {
                var p = so.FindProperty(f);
                if (p == null) continue;
                Debug.Log($"[Deep] {f}[{p.arraySize}]:");
                for (int i = 0; i < p.arraySize; i++)
                {
                    var elem = p.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue != null)
                        Debug.Log($"[Deep]   [{i}] = {elem.objectReferenceValue.name} ({AssetDatabase.GetAssetPath(elem.objectReferenceValue)})");
                    else
                        Debug.LogWarning($"[Deep]   [{i}] = NULL");
                }
            }

            // UITextSO 의 entry 수 dump
            var uiTextProp = so.FindProperty("uiTextSO");
            if (uiTextProp?.objectReferenceValue is UITextSO uiTextSo)
            {
                Debug.Log($"[Deep] UITextSO.Data.Count = {uiTextSo.Data.Count} (TS UITextTable = 21)");
            }

            // StageDefSO 의 block count dump
            var stageProp = so.FindProperty("stageDefinitionSOs");
            for (int i = 0; i < stageProp.arraySize; i++)
            {
                var stage = stageProp.GetArrayElementAtIndex(i).objectReferenceValue as StageDefinitionSO;
                if (stage != null)
                {
                    var d = stage.Data;
                    Debug.Log($"[Deep]   stage[{i}] {stage.name}: blocks={d.Blocks?.Count ?? 0}, borders={d.Borders?.Count ?? 0}, doors={d.Doors?.Count ?? 0}, spinners={d.Spinners?.Count ?? 0}");
                }
            }
        }

        [MenuItem("Arkanoid/Deep Audit/E. Input System + UnityInputSnapshotBuilder")]
        public static void AuditInputSystem()
        {
            Debug.Log("=== Input System Audit ===");
            var assets = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (var guid in assets)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"[Deep] InputActionAsset: {p}");
            }
            // PointerToPlayfield camera reference 검증
            var gm = Resources.FindObjectsOfTypeAll<GameManager>().FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var camProp = so.FindProperty("mainCamera");
                Debug.Log($"[Deep] GameManager.mainCamera = {(camProp.objectReferenceValue != null ? camProp.objectReferenceValue.name : "NULL")}");
            }
            // EventSystem 검증
            var es = Resources.FindObjectsOfTypeAll<UnityEngine.EventSystems.EventSystem>()
                .FirstOrDefault(e => !EditorUtility.IsPersistent(e));
            if (es != null)
            {
                Debug.Log($"[Deep] EventSystem found: {es.gameObject.name}, components: " +
                    string.Join(",", es.gameObject.GetComponents<Component>().Select(c => c.GetType().Name)));
            }
            else Debug.LogWarning("[Deep] EventSystem NOT FOUND");
        }

        [MenuItem("Arkanoid/Deep Audit/F. Camera + PlayfieldRoot")]
        public static void AuditCameraPlayfield()
        {
            Debug.Log("=== Camera + PlayfieldRoot Audit ===");
            var cam = Camera.main;
            if (cam == null)
            {
                cam = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => !EditorUtility.IsPersistent(c) && c.tag == "MainCamera");
            }
            if (cam == null) { Debug.LogError("[Deep] Main Camera NOT FOUND"); return; }
            Debug.Log($"[Deep] Camera pos={cam.transform.position} ortho={cam.orthographic} orthoSize={cam.orthographicSize} (TS: pos=(360,-450,-10), ortho size=640)");

            var pf = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == "PlayfieldRoot" && !EditorUtility.IsPersistent(g));
            if (pf == null) { Debug.LogError("[Deep] PlayfieldRoot NOT FOUND"); return; }
            Debug.Log($"[Deep] PlayfieldRoot pos={pf.transform.position} scale={pf.transform.localScale} children={pf.transform.childCount} (TS: pos=(0,0,0), scale=(1,-1,1) for Y flip)");

            // Canvas CanvasScaler 검증
            var canvas = Resources.FindObjectsOfTypeAll<Canvas>().FirstOrDefault(c => !EditorUtility.IsPersistent(c) && c.isRootCanvas);
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                Debug.Log($"[Deep] Canvas {canvas.name} renderMode={canvas.renderMode} scaler.uiScaleMode={(scaler!=null?scaler.uiScaleMode.ToString():"NULL")} refResolution={(scaler!=null?scaler.referenceResolution.ToString():"NULL")} (TS: 1080x1920 ScreenSpaceOverlay ConstantPixelSize or ScaleWithScreenSize)");
            }
        }

        [MenuItem("Arkanoid/Deep Audit/All Deep")]
        public static void AllDeep()
        {
            AuditRenderersDeep();
            AuditSOValues();
            AuditInputSystem();
            AuditCameraPlayfield();
        }
    }
}

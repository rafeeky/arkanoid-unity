using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Arkanoid.Definitions.SO;
using Arkanoid.Definitions;

// Menu: Arkanoid > Import Stages from JSON
// Reads Assets/Data/Stages/json/stage1.json .. stage3.json
// and creates Assets/Data/Stages/Stage_01.asset .. Stage_03.asset
public static class StageJsonImporter
{
    [MenuItem("Arkanoid/Import Stages from JSON")]
    public static void ImportStages()
    {
        for (int n = 1; n <= 3; n++)
        {
            string jsonPath = $"Assets/Data/Stages/json/stage{n}.json";
            string fullPath = Path.Combine(Application.dataPath, "../", jsonPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[StageJsonImporter] Not found: {fullPath}");
                continue;
            }
            string json = File.ReadAllText(fullPath);
            var raw = JsonUtility.FromJson<StageJson>(json);
            if (raw == null)
            {
                Debug.LogError($"[StageJsonImporter] Failed to parse {jsonPath}");
                continue;
            }

            string assetPath = $"Assets/Data/Stages/Stage_0{n}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(assetPath);
            StageDefinitionSO so;
            if (existing != null)
                so = existing;
            else
            {
                so = ScriptableObject.CreateInstance<StageDefinitionSO>();
                AssetDatabase.CreateAsset(so, assetPath);
            }

            var s = new SerializedObject(so);
            s.FindProperty("stageId").stringValue = raw.stageId ?? $"stage_0{n}";
            s.FindProperty("barSpawnX").floatValue = raw.barSpawnX;
            s.FindProperty("barSpawnY").floatValue = raw.barSpawnY;

            // TrailStyle mapping
            TrailStyleId trailEnum = TrailStyleId.GoldenSun;
            bool hasTrail = false;
            if (!string.IsNullOrEmpty(raw.trailStyle))
            {
                hasTrail = true;
                trailEnum = raw.trailStyle switch {
                    "golden_sun"  => TrailStyleId.GoldenSun,
                    "blue_meteor" => TrailStyleId.BlueMeteor,
                    "sunset"      => TrailStyleId.Sunset,
                    _             => TrailStyleId.GoldenSun,
                };
            }
            s.FindProperty("useTrailStyle").boolValue = hasTrail;
            s.FindProperty("trailStyle").enumValueIndex = (int)trailEnum;

            // Blocks
            var blocksProp = s.FindProperty("blocks");
            var blocks = raw.blocks ?? new List<BlockJson>();
            blocksProp.arraySize = blocks.Count;
            for (int i = 0; i < blocks.Count; i++)
            {
                var bp = blocksProp.GetArrayElementAtIndex(i);
                bp.FindPropertyRelative("col").intValue = blocks[i].col;
                bp.FindPropertyRelative("row").intValue = blocks[i].row;
                bp.FindPropertyRelative("definitionId").stringValue = blocks[i].definitionId ?? "basic";
            }

            // Borders
            var bordersProp = s.FindProperty("borders");
            var borders = raw.borders ?? new List<BorderJson>();
            bordersProp.arraySize = borders.Count;
            for (int i = 0; i < borders.Count; i++)
            {
                var bp = bordersProp.GetArrayElementAtIndex(i);
                bp.FindPropertyRelative("col").intValue = borders[i].col;
                bp.FindPropertyRelative("row").intValue = borders[i].row;
                // orientation: "horizontal"=0, "vertical"=1
                int ori = borders[i].orientation == "vertical" ? 1 : 0;
                bp.FindPropertyRelative("orientation").enumValueIndex = ori;
            }

            // Doors
            var doorsProp = s.FindProperty("doors");
            var doors = raw.doors ?? new List<DoorJson>();
            doorsProp.arraySize = doors.Count;
            for (int i = 0; i < doors.Count; i++)
            {
                var dp = doorsProp.GetArrayElementAtIndex(i);
                dp.FindPropertyRelative("col").intValue = doors[i].col;
                dp.FindPropertyRelative("spinnerDefinitionId").stringValue = doors[i].spinnerDefinitionId ?? "spinner_cube";
            }

            // Spinners (may be absent)
            var spinnersProp = s.FindProperty("spinners");
            var spinners = raw.spinners ?? new List<SpinnerJson>();
            spinnersProp.arraySize = spinners.Count;
            for (int i = 0; i < spinners.Count; i++)
            {
                var sp = spinnersProp.GetArrayElementAtIndex(i);
                sp.FindPropertyRelative("x").floatValue = spinners[i].x;
                sp.FindPropertyRelative("y").floatValue = spinners[i].y;
                sp.FindPropertyRelative("definitionId").stringValue = spinners[i].definitionId ?? "spinner_cube";
                bool hasAngle = spinners[i].initialAngleRad != 0f;
                sp.FindPropertyRelative("hasInitialAngle").boolValue = hasAngle;
                sp.FindPropertyRelative("initialAngleRad").floatValue = spinners[i].initialAngleRad;
            }

            s.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
            Debug.Log($"[StageJsonImporter] Created/Updated: {assetPath} ({blocks.Count} blocks, {borders.Count} borders, {doors.Count} doors)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StageJsonImporter] Done.");
    }

    // ─── JSON POCOs ──────────────────────────────────────────────────────

    [Serializable]
    class StageJson
    {
        public string stageId;
        public string trailStyle;
        public string displayName;
        public float barSpawnX;
        public float barSpawnY;
        public List<BlockJson> blocks;
        public List<BorderJson> borders;
        public List<DoorJson> doors;
        public List<SpinnerJson> spinners;
    }

    [Serializable] class BlockJson  { public int col; public int row; public string definitionId; }
    [Serializable] class BorderJson { public int col; public int row; public string orientation; }
    [Serializable] class DoorJson   { public int col; public string spinnerDefinitionId; }
    [Serializable] class SpinnerJson{ public float x; public float y; public string definitionId; public float initialAngleRad; }
}

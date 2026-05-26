using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Arkanoid.Definitions.SO;
using Arkanoid.Definitions;

// One-shot Editor script: creates all Data SOs, prefabs, wires GameManager.
// Menu: Arkanoid > Import All Data
public static class ArkanoidDataImporter
{
    [MenuItem("Arkanoid/Import All Data")]
    public static void ImportAll()
    {
        CreateFolders();
        CreateItemSOs();
        CreateSpinnerSOs();
        CreateTrailStyleSO();
        CreateMascotSO();
        CreatePowerupSO();
        FillAudioCueTable();
        CreatePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ArkanoidDataImporter] All data imported successfully.");
    }

    static void CreateFolders()
    {
        EnsureFolder("Assets/Data", "Items");
        EnsureFolder("Assets/Data", "Spinners");
        EnsureFolder("Assets/Data", "Presentation");
        EnsureFolder("Assets", "Prefabs");
    }

    static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    // ─── 5-1: Item SOs ───────────────────────────────────────────────────

    static void CreateItemSOs()
    {
        CreateItemSO("Item_Expand",
            itemType: 0, dnId: "txt_item_expand_name", descId: "txt_item_expand_desc",
            iconId: "icon_item_expand", fallSpeed: 160f, effectType: 0,
            expandMult: 1.5f, magnetMs: -1f, magnetUse: -1,
            laserCd: -1f, laserShot: -1, laserDur: -1f);

        CreateItemSO("Item_Magnet",
            itemType: 1, dnId: "txt_item_magnet_name", descId: "txt_item_magnet_desc",
            iconId: "icon_item_magnet", fallSpeed: 160f, effectType: 1,
            expandMult: -1f, magnetMs: 8000f, magnetUse: 5,
            laserCd: -1f, laserShot: -1, laserDur: -1f);

        CreateItemSO("Item_Laser",
            itemType: 2, dnId: "txt_item_laser_name", descId: "txt_item_laser_desc",
            iconId: "icon_item_laser", fallSpeed: 160f, effectType: 2,
            expandMult: -1f, magnetMs: -1f, magnetUse: -1,
            laserCd: 400f, laserShot: 2, laserDur: 6000f);
    }

    static void CreateItemSO(string name, int itemType, string dnId, string descId, string iconId,
        float fallSpeed, int effectType, float expandMult, float magnetMs, int magnetUse,
        float laserCd, int laserShot, float laserDur)
    {
        string path = $"Assets/Data/Items/{name}.asset";
        DeleteIfExists(path);
        var so = ScriptableObject.CreateInstance<ItemDefinitionSO>();
        AssetDatabase.CreateAsset(so, path);
        var s = new SerializedObject(so);
        s.FindProperty("itemType").enumValueIndex = itemType;
        s.FindProperty("displayNameTextId").stringValue = dnId;
        s.FindProperty("descriptionTextId").stringValue = descId;
        s.FindProperty("iconId").stringValue = iconId;
        s.FindProperty("fallSpeed").floatValue = fallSpeed;
        s.FindProperty("effectType").enumValueIndex = effectType;
        s.FindProperty("expandMultiplier").floatValue = expandMult;
        s.FindProperty("magnetDurationMs").floatValue = magnetMs;
        s.FindProperty("magnetUseCount").intValue = magnetUse;
        s.FindProperty("laserCooldownMs").floatValue = laserCd;
        s.FindProperty("laserShotCount").intValue = laserShot;
        s.FindProperty("laserDurationMs").floatValue = laserDur;
        s.ApplyModifiedProperties();
        Debug.Log($"Created {path}");
    }

    // ─── 5-2: Spinner SOs ────────────────────────────────────────────────

    static void CreateSpinnerSOs()
    {
        CreateSpinnerSO("Spinner_Cube", "spinner_cube", 0, 48f, 1.5f, new float[]{ 0f, 1.5707963f });
        CreateSpinnerSO("Spinner_Triangle", "spinner_triangle", 1, 48f, 1.2f, new float[]{ 0f });
    }

    static void CreateSpinnerSO(string assetName, string defId, int kind, float size,
        float rotSpeed, float[] phases)
    {
        string path = $"Assets/Data/Spinners/{assetName}.asset";
        DeleteIfExists(path);
        var so = ScriptableObject.CreateInstance<SpinnerDefinitionSO>();
        AssetDatabase.CreateAsset(so, path);
        var s = new SerializedObject(so);
        s.FindProperty("definitionId").stringValue = defId;
        s.FindProperty("kind").enumValueIndex = kind;
        s.FindProperty("size").floatValue = size;
        s.FindProperty("rotationSpeedRadPerSec").floatValue = rotSpeed;
        var phasesProp = s.FindProperty("blockCollisionPhases");
        phasesProp.arraySize = phases.Length;
        for (int i = 0; i < phases.Length; i++)
            phasesProp.GetArrayElementAtIndex(i).floatValue = phases[i];
        s.ApplyModifiedProperties();
        Debug.Log($"Created {path}");
    }

    // ─── 5-3: TrailStyle SO ──────────────────────────────────────────────

    static void CreateTrailStyleSO()
    {
        string path = "Assets/Data/Presentation/TrailStyleTable.asset";
        DeleteIfExists(path);
        var so = ScriptableObject.CreateInstance<TrailStyleSO>();
        AssetDatabase.CreateAsset(so, path);
        var s = new SerializedObject(so);

        SetTrailEntry(s, "goldenSun", 16767812, 16737826, 16755251, 16, 0.95f, 9f, 18f);
        SetTrailEntry(s, "blueMeteor", 15663103, 4491007, 6728959, 16, 0.95f, 9f, 18f);
        SetTrailEntry(s, "sunset", 16746086, 16729770, 16737928, 16, 0.95f, 9f, 18f);

        s.ApplyModifiedProperties();
        Debug.Log($"Created {path}");
    }

    static void SetTrailEntry(SerializedObject s, string fieldName, int head, int tail, int glow,
        int segCount, float headAlpha, float segRadius, float pushInterval)
    {
        var p = s.FindProperty(fieldName);
        p.FindPropertyRelative("headColor").intValue = head;
        p.FindPropertyRelative("tailColor").intValue = tail;
        p.FindPropertyRelative("glowColor").intValue = glow;
        p.FindPropertyRelative("segmentCount").intValue = segCount;
        p.FindPropertyRelative("headAlpha").floatValue = headAlpha;
        p.FindPropertyRelative("segmentRadius").floatValue = segRadius;
        p.FindPropertyRelative("pushIntervalMs").floatValue = pushInterval;
    }

    // ─── 5-4: Mascot SO ──────────────────────────────────────────────────

    static void CreateMascotSO()
    {
        string path = "Assets/Data/Presentation/MascotTable.asset";
        DeleteIfExists(path);
        var so = ScriptableObject.CreateInstance<MascotSO>();
        AssetDatabase.CreateAsset(so, path);
        var s = new SerializedObject(so);
        var entries = s.FindProperty("entries");
        entries.arraySize = 5;

        SetMascotEntry(entries.GetArrayElementAtIndex(0), "albatross", "ALBATROSS", "알바트로스", 0,
            16777215, 6710886, new[]{"mascot.albatross.frame0","mascot.albatross.frame1","mascot.albatross.frame2","mascot.albatross.frame3"});
        SetMascotEntry(entries.GetArrayElementAtIndex(1), "kongming", "KONGMING", "콩밍이 (햄스터)", 100,
            16769184, 13144160, new[]{"mascot.kongming.frame0","mascot.kongming.frame1","mascot.kongming.frame2","mascot.kongming.frame3"});
        SetMascotEntry(entries.GetArrayElementAtIndex(2), "snowrabbit", "SNOW RABBIT", "눈토끼", 300,
            15398143, 8956620, new[]{"mascot.snowrabbit.frame0","mascot.snowrabbit.frame1","mascot.snowrabbit.frame2","mascot.snowrabbit.frame3"});
        SetMascotEntry(entries.GetArrayElementAtIndex(3), "reaper", "REAPER", "저승이 (해골)", 600,
            4473958, 11184093, new[]{"mascot.reaper.frame0","mascot.reaper.frame1","mascot.reaper.frame2","mascot.reaper.frame3"});
        SetMascotEntry(entries.GetArrayElementAtIndex(4), "seraphin", "SERAPHIN", "세라핀 (분홍 세이렌)", 1000,
            16756944, 13395609, new[]{"mascot.seraphin.frame0","mascot.seraphin.frame1","mascot.seraphin.frame2","mascot.seraphin.frame3"});

        s.ApplyModifiedProperties();
        Debug.Log($"Created {path}");
    }

    static void SetMascotEntry(SerializedProperty p, string id, string dn, string subtitle,
        int cost, int color, int strokeColor, string[] frames)
    {
        p.FindPropertyRelative("id").stringValue = id;
        p.FindPropertyRelative("displayName").stringValue = dn;
        p.FindPropertyRelative("subtitle").stringValue = subtitle;
        p.FindPropertyRelative("unlockCost").intValue = cost;
        p.FindPropertyRelative("placeholderColor").intValue = color;
        p.FindPropertyRelative("placeholderStrokeColor").intValue = strokeColor;
        var fp = p.FindPropertyRelative("spriteFrameIds");
        fp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            fp.GetArrayElementAtIndex(i).stringValue = frames[i];
    }

    // ─── 5-5: Powerup SO ─────────────────────────────────────────────────

    static void CreatePowerupSO()
    {
        string path = "Assets/Data/Presentation/PowerupTable.asset";
        DeleteIfExists(path);
        var so = ScriptableObject.CreateInstance<PowerupSO>();
        AssetDatabase.CreateAsset(so, path);
        var s = new SerializedObject(so);

        SetPowerupEntry(s, "expand", 16750899, "icon_expand", "EXPAND");
        SetPowerupEntry(s, "magnet", 8964863, "icon_magnet", "MAGNET");
        SetPowerupEntry(s, "laser", 16746376, "icon_laser", "LASER");

        s.ApplyModifiedProperties();
        Debug.Log($"Created {path}");
    }

    static void SetPowerupEntry(SerializedObject s, string field, int color, string icon, string label)
    {
        var p = s.FindProperty(field);
        p.FindPropertyRelative("color").intValue = color;
        p.FindPropertyRelative("iconKey").stringValue = icon;
        p.FindPropertyRelative("label").stringValue = label;
    }

    // ─── 5-6: AudioCueTable ──────────────────────────────────────────────

    static void FillAudioCueTable()
    {
        string path = "Assets/Data/Presentation/AudioCueTable.asset";
        var so = AssetDatabase.LoadAssetAtPath<AudioCueSO>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<AudioCueSO>();
            AssetDatabase.CreateAsset(so, path);
        }
        var s = new SerializedObject(so);
        var entries = s.FindProperty("entries");
        entries.arraySize = 0;

        // playbackType: Bgm=0, Jingle=1, Sfx=2
        AddAudioCue(entries, "cue_title_bgm",          "EnteredTitle",        "bgm_title",          0, -1f, -1f, 0.2f);
        AddAudioCue(entries, "cue_round_intro_jingle", "EnteredRoundIntro",   "jingle_round_start", 1, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_block_hit",          "BlockHit",            "sfx_block_hit",      2, 1.6f, -1f, -1f);
        AddAudioCue(entries, "cue_block_destroyed",    "BlockDestroyed",      "sfx_block_destroyed",2, 1.4f, -1f, -1f);
        AddAudioCue(entries, "cue_item_collected",     "ItemCollected",       "sfx_item_collected", 2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_life_lost",          "LifeLost",            "sfx_life_lost",      2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_gameover_jingle",    "EnteredGameOver",     "jingle_gameover",    1, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_ui_confirm",         "UiConfirm",           "sfx_ui_confirm",     2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_gameclear_jingle",   "EnteredGameClear",    "jingle_gameclear",   1, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_ball_attached",      "BallAttached",        "sfx_ball_attached",  2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_balls_released",     "BallsReleased",       "sfx_balls_released", 2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_laser_fired",        "LaserFired",          "sfx_laser_fired",    2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_ball_launch",        "BallLaunched",        "sfx_balls_released", 2, 1.0f, -1f, -1f);
        AddAudioCue(entries, "cue_ball_hit_bar",       "BallHitBar",          "sfx_balls_released", 2, 1.1f, 60f, -1f);
        AddAudioCue(entries, "cue_item_bar_extend",    "ItemCollected_Expand","sfx_item_collected", 2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_item_laser",         "ItemCollected_Laser", "sfx_laser_fired",    2, -1f, -1f, -1f);
        AddAudioCue(entries, "cue_item_magnet",        "ItemCollected_Magnet","sfx_ball_attached",  2, -1f, -1f, -1f);

        s.ApplyModifiedProperties();
        Debug.Log($"Filled {path} with {entries.arraySize} entries");
    }

    static void AddAudioCue(SerializedProperty entries, string cueId, string eventType,
        string resourceId, int playbackType, float pitch, float playDurMs, float volume)
    {
        int idx = entries.arraySize;
        entries.arraySize = idx + 1;
        var p = entries.GetArrayElementAtIndex(idx);
        p.FindPropertyRelative("cueId").stringValue = cueId;
        p.FindPropertyRelative("eventType").stringValue = eventType;
        p.FindPropertyRelative("resourceId").stringValue = resourceId;
        p.FindPropertyRelative("playbackType").enumValueIndex = playbackType;
        p.FindPropertyRelative("pitch").floatValue = pitch;
        p.FindPropertyRelative("playDurationMs").floatValue = playDurMs;
        p.FindPropertyRelative("volume").floatValue = volume;
    }

    // ─── 6: Prefabs ──────────────────────────────────────────────────────

    static void CreatePrefabs()
    {
        CreateTrailPrefab();
        CreateSpinnerPrefab();
        CreateMascotPrefab();
    }

    static void CreateTrailPrefab()
    {
        string path = "Assets/Prefabs/Trail.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject("Trail");
        go.AddComponent<TrailRenderer>();
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"Created {path}");
    }

    static void CreateSpinnerPrefab()
    {
        string path = "Assets/Prefabs/Spinner.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject("Spinner");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        go.transform.localScale = new Vector3(48f, 48f, 1f);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"Created {path}");
    }

    static void CreateMascotPrefab()
    {
        string path = "Assets/Prefabs/Mascot.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var go = new GameObject("Mascot");
        go.AddComponent<SpriteRenderer>();
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"Created {path}");
    }

    // ─── Util ─────────────────────────────────────────────────────────────

    static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}

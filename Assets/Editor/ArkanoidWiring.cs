using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Arkanoid.Definitions.SO;
using Arkanoid.Presentation;
using Arkanoid.Presentation.View;

// Menu: Arkanoid > Wire Scene
// Wires GameManager inspector refs + creates UnityAudio GameObject + wires AudioCue table.
public static class ArkanoidWiring
{
    [MenuItem("Arkanoid/Wire Scene")]
    public static void WireScene()
    {
        // ─── Find GameManager ─────────────────────────────────────────────
        var gmGO = GameObject.Find("GameManager");
        if (gmGO == null) { Debug.LogError("[ArkanoidWiring] GameManager not found."); return; }
        var gmSO = new SerializedObject(gmGO.GetComponent<GameManager>());

        // ─── Load Item SOs ────────────────────────────────────────────────
        var expandSO   = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>("Assets/Data/Items/Item_Expand.asset");
        var magnetSO   = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>("Assets/Data/Items/Item_Magnet.asset");
        var laserSO    = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>("Assets/Data/Items/Item_Laser.asset");
        var spinnerCube = AssetDatabase.LoadAssetAtPath<SpinnerDefinitionSO>("Assets/Data/Spinners/Spinner_Cube.asset");
        var spinnerTri  = AssetDatabase.LoadAssetAtPath<SpinnerDefinitionSO>("Assets/Data/Spinners/Spinner_Triangle.asset");
        var stage01 = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/Data/Stages/Stage_01.asset");
        var stage02 = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/Data/Stages/Stage_02.asset");
        var stage03 = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/Data/Stages/Stage_03.asset");

        // Wire item SOs
        gmSO.FindProperty("expandItemSO").objectReferenceValue = expandSO;
        gmSO.FindProperty("magnetItemSO").objectReferenceValue = magnetSO;
        gmSO.FindProperty("laserItemSO").objectReferenceValue = laserSO;

        // Wire spinner SOs list
        var spinnerList = gmSO.FindProperty("spinnerDefinitionSOs");
        spinnerList.arraySize = 2;
        spinnerList.GetArrayElementAtIndex(0).objectReferenceValue = spinnerCube;
        spinnerList.GetArrayElementAtIndex(1).objectReferenceValue = spinnerTri;

        // Wire stage SOs list (replace Stage_Mini)
        var stageList = gmSO.FindProperty("stageDefinitionSOs");
        stageList.arraySize = 3;
        stageList.GetArrayElementAtIndex(0).objectReferenceValue = stage01;
        stageList.GetArrayElementAtIndex(1).objectReferenceValue = stage02;
        stageList.GetArrayElementAtIndex(2).objectReferenceValue = stage03;

        // ─── Create/find UnityAudio GameObject ───────────────────────────
        var unityAudioGO = GameObject.Find("UnityAudio");
        if (unityAudioGO == null)
        {
            unityAudioGO = new GameObject("UnityAudio");
        }

        // Add/get UnityAudio component
        var unityAudioComp = unityAudioGO.GetComponent<UnityAudio>();
        if (unityAudioComp == null) unityAudioComp = unityAudioGO.AddComponent<UnityAudio>();

        // Add/get BgmSource
        var sources = unityAudioGO.GetComponents<AudioSource>();
        AudioSource bgmSrc = null, sfxSrc = null;

        // Name-based lookup via child GameObjects for cleaner setup
        var bgmChild = unityAudioGO.transform.Find("BgmSource");
        if (bgmChild == null)
        {
            var bgmGO = new GameObject("BgmSource");
            bgmGO.transform.SetParent(unityAudioGO.transform);
            bgmSrc = bgmGO.AddComponent<AudioSource>();
        }
        else
        {
            bgmSrc = bgmChild.GetComponent<AudioSource>();
            if (bgmSrc == null) bgmSrc = bgmChild.gameObject.AddComponent<AudioSource>();
        }

        var sfxChild = unityAudioGO.transform.Find("SfxSource");
        if (sfxChild == null)
        {
            var sfxGO = new GameObject("SfxSource");
            sfxGO.transform.SetParent(unityAudioGO.transform);
            sfxSrc = sfxGO.AddComponent<AudioSource>();
        }
        else
        {
            sfxSrc = sfxChild.GetComponent<AudioSource>();
            if (sfxSrc == null) sfxSrc = sfxChild.gameObject.AddComponent<AudioSource>();
        }

        // Wire AudioSource refs and clip table on UnityAudio
        var uaSO = new SerializedObject(unityAudioComp);
        uaSO.FindProperty("bgmSource").objectReferenceValue = bgmSrc;
        uaSO.FindProperty("sfxSource").objectReferenceValue = sfxSrc;

        // Build clip table: resourceId → AudioClip
        var clipTable = uaSO.FindProperty("clipTable");
        var clipEntries = new (string id, string wavName)[]
        {
            ("bgm_title",           "bgm_title"),
            ("jingle_round_start",  "jingle_round_start"),
            ("jingle_gameover",     "jingle_gameover"),
            ("jingle_gameclear",    "jingle_gameclear"),
            ("sfx_block_hit",       "sfx_block_hit"),
            ("sfx_block_destroyed", "sfx_block_destroyed"),
            ("sfx_item_collected",  "sfx_item_collected"),
            ("sfx_life_lost",       "sfx_life_lost"),
            ("sfx_ui_confirm",      "sfx_ui_confirm"),
            ("sfx_ball_attached",   "sfx_ball_attached"),
            ("sfx_balls_released",  "sfx_balls_released"),
            ("sfx_laser_fired",     "sfx_laser_fired"),
        };
        clipTable.arraySize = clipEntries.Length;
        for (int i = 0; i < clipEntries.Length; i++)
        {
            var (resId, wav) = clipEntries[i];
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{wav}.wav");
            var ep = clipTable.GetArrayElementAtIndex(i);
            ep.FindPropertyRelative("ResourceId").stringValue = resId;
            ep.FindPropertyRelative("Clip").objectReferenceValue = clip;
        }
        uaSO.ApplyModifiedProperties();

        // Wire unityAudio ref on GameManager
        gmSO.FindProperty("unityAudio").objectReferenceValue = unityAudioComp;

        // ─── Wire Renderers ───────────────────────────────────────────────
        WireRenderers(gmGO, gmSO);

        gmSO.ApplyModifiedProperties();

        // ─── Save scene ───────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(gmGO.scene);
        EditorSceneManager.SaveScene(gmGO.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArkanoidWiring] Wiring complete.");
    }

    static void WireRenderers(GameObject gmGO, SerializedObject gmSO)
    {
        var blockPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Block.prefab");
        var spinnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spinner.prefab");
        var trailPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Trail.prefab");
        var mascotPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mascot.prefab");
        var trailStyleSO  = AssetDatabase.LoadAssetAtPath<TrailStyleSO>("Assets/Data/Presentation/TrailStyleTable.asset");
        var mascotSO      = AssetDatabase.LoadAssetAtPath<MascotSO>("Assets/Data/Presentation/MascotTable.asset");

        // Find PlayfieldRoot renderers
        var playfieldRoot = GameObject.Find("PlayfieldRoot");

        // SpinnersRenderer
        var spinnersRendererGO = FindChildByComponent<SpinnersRenderer>(playfieldRoot);
        if (spinnersRendererGO != null)
        {
            var sr = spinnersRendererGO.GetComponent<SpinnersRenderer>();
            var srSO = new SerializedObject(sr);
            if (spinnerPrefab != null) srSO.FindProperty("spinnerPrefab").objectReferenceValue = spinnerPrefab;
            srSO.ApplyModifiedProperties();
        }

        // BallTrailRenderer
        var trailRendererGO = FindChildByComponent<BallTrailRenderer>(playfieldRoot);
        if (trailRendererGO == null)
        {
            // Try in scene root
            var allTrails = Object.FindObjectsByType<BallTrailRenderer>(FindObjectsSortMode.None);
            if (allTrails.Length > 0) trailRendererGO = allTrails[0].gameObject;
        }
        if (trailRendererGO != null)
        {
            var tr = trailRendererGO.GetComponent<BallTrailRenderer>();
            var trSO = new SerializedObject(tr);
            if (trailPrefab != null) trSO.FindProperty("trailPrefab").objectReferenceValue = trailPrefab;
            if (trailStyleSO != null) trSO.FindProperty("trailStyleSO").objectReferenceValue = trailStyleSO;
            trSO.ApplyModifiedProperties();
        }
        else Debug.LogWarning("[ArkanoidWiring] BallTrailRenderer not found.");

        // ItemsRenderer
        var itemsRendererGO = FindChildByComponent<ItemsRenderer>(playfieldRoot);
        if (itemsRendererGO != null)
        {
            var ir = itemsRendererGO.GetComponent<ItemsRenderer>();
            var irSO = new SerializedObject(ir);
            if (blockPrefab != null) irSO.FindProperty("itemPrefab").objectReferenceValue = blockPrefab;
            irSO.ApplyModifiedProperties();
        }

        // MascotRenderer
        var mascotRendererGOs = Object.FindObjectsByType<MascotRenderer>(FindObjectsSortMode.None);
        foreach (var mr in mascotRendererGOs)
        {
            var mrSO = new SerializedObject(mr);
            if (mascotSO != null) mrSO.FindProperty("mascotSO").objectReferenceValue = mascotSO;
            mrSO.ApplyModifiedProperties();
        }

        // Wire GameManager's ballTrailRenderer field
        if (trailRendererGO != null)
        {
            var btrComp = trailRendererGO.GetComponent<BallTrailRenderer>();
            gmSO.FindProperty("ballTrailRenderer").objectReferenceValue = btrComp;
        }

        Debug.Log("[ArkanoidWiring] Renderer wiring done.");
    }

    static GameObject FindChildByComponent<T>(GameObject root) where T : Component
    {
        if (root == null) return null;
        var comp = root.GetComponentInChildren<T>(true);
        return comp != null ? comp.gameObject : null;
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click builder for the Milestone 1 test scene.
/// Menu: Apex Rush > Milestone 1 > Build Test Scene.
/// Creates a primitive-geometry training range (placeholder art), the Player and the camera,
/// then saves the scene to Assets/_Project/Scenes/TestRange.unity.
/// This file lives in an "Editor" folder, so it is never included in game builds.
/// </summary>
public static class Milestone1SceneBuilder
{
    private const string Root = "Assets/_Project";
    private const string SceneFolder = Root + "/Scenes";
    private const string MaterialFolder = Root + "/Materials";
    private const string ScenePath = SceneFolder + "/TestRange.unity";

    [MenuItem("Apex Rush/Milestone 1/Build Test Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(Root, "Scenes");
        EnsureFolder(Root, "Materials");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove the default camera; the Player gets its own first-person camera.
        Camera defaultCamera = Camera.main;
        if (defaultCamera != null) Object.DestroyImmediate(defaultCamera.gameObject);

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null) sun.shadows = LightShadows.Soft;

        Material ground = MakeMaterial("M_Ground", new Color(0.32f, 0.36f, 0.27f));
        Material concrete = MakeMaterial("M_Concrete", new Color(0.55f, 0.56f, 0.58f));
        Material crate = MakeMaterial("M_Crate", new Color(0.45f, 0.33f, 0.18f));
        Material sand = MakeMaterial("M_Sandbag", new Color(0.62f, 0.55f, 0.38f));
        Material dark = MakeMaterial("M_DarkMetal", new Color(0.2f, 0.22f, 0.24f));
        Material accent = MakeMaterial("M_Accent", new Color(0.85f, 0.45f, 0.1f));

        Transform map = new GameObject("TestRange").transform;

        // ---- Ground and perimeter walls ----
        Box("Ground", new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60), ground, map);
        Box("Wall_N", new Vector3(0, 2, 30.5f), new Vector3(62, 4, 1), concrete, map);
        Box("Wall_S", new Vector3(0, 2, -30.5f), new Vector3(62, 4, 1), concrete, map);
        Box("Wall_E", new Vector3(30.5f, 2, 0), new Vector3(1, 4, 60), concrete, map);
        Box("Wall_W", new Vector3(-30.5f, 2, 0), new Vector3(1, 4, 60), concrete, map);

        // ---- Training building with a 3 m wide doorway (tests walking through gaps) ----
        Transform building = new GameObject("TrainingBuilding").transform;
        building.SetParent(map, false);
        Box("Back", new Vector3(0, 2, 15), new Vector3(12, 4, 0.4f), concrete, building);
        Box("Left", new Vector3(-6, 2, 10), new Vector3(0.4f, 4, 10), concrete, building);
        Box("Right", new Vector3(6, 2, 10), new Vector3(0.4f, 4, 10), concrete, building);
        Box("FrontLeft", new Vector3(-3.75f, 2, 5), new Vector3(4.5f, 4, 0.4f), concrete, building);
        Box("FrontRight", new Vector3(3.75f, 2, 5), new Vector3(4.5f, 4, 0.4f), concrete, building);
        Box("DoorLintel", new Vector3(0, 3.25f, 5), new Vector3(3, 1.5f, 0.4f), concrete, building);
        Box("Roof", new Vector3(0, 4.2f, 10), new Vector3(12.4f, 0.4f, 10.4f), dark, building);
        Box("InsideCrateA", new Vector3(-3.5f, 0.6f, 12.5f), new Vector3(1.2f, 1.2f, 1.2f), crate, building);
        Box("InsideCrateB", new Vector3(3.5f, 0.6f, 13), new Vector3(1.2f, 1.2f, 1.2f), crate, building);

        // ---- Cover: crates, sandbag walls, barriers ----
        Transform cover = new GameObject("Cover").transform;
        cover.SetParent(map, false);
        Box("Crate1", new Vector3(-8, 0.6f, -4), new Vector3(1.2f, 1.2f, 1.2f), crate, cover);
        Box("Crate2", new Vector3(-6.7f, 0.6f, -4), new Vector3(1.2f, 1.2f, 1.2f), crate, cover);
        Box("Crate3", new Vector3(-7.4f, 1.8f, -4), new Vector3(1.2f, 1.2f, 1.2f), crate, cover);
        Box("Sandbags1", new Vector3(6, 0.5f, -6), new Vector3(3.5f, 1f, 0.8f), sand, cover);
        Box("Sandbags2", new Vector3(-4, 0.5f, -10), new Vector3(3.5f, 1f, 0.8f), sand, cover);
        Box("Barrier1", new Vector3(10, 0.6f, -12), new Vector3(4, 1.2f, 0.6f), concrete, cover);
        Box("Barrier2", new Vector3(-12, 0.6f, 8), new Vector3(0.6f, 1.2f, 4), concrete, cover);

        // ---- Movement test pieces ----
        Transform tests = new GameObject("MovementTests").transform;
        tests.SetParent(map, false);

        // Crouch gate: beam bottom is at 1.35 m. Standing (1.8 m) is blocked, crouching (1.1 m) fits.
        Box("CrouchGate_PostL", new Vector3(12.8f, 0.75f, -5), new Vector3(0.4f, 1.5f, 1), accent, tests);
        Box("CrouchGate_PostR", new Vector3(17.2f, 0.75f, -5), new Vector3(0.4f, 1.5f, 1), accent, tests);
        Box("CrouchGate_Beam", new Vector3(15, 1.5f, -5), new Vector3(4.8f, 0.3f, 1), accent, tests);

        // Jump blocks: 0.5 m and 1.0 m high.
        Box("JumpBlock_Low", new Vector3(-15, 0.25f, 0), new Vector3(3, 0.5f, 3), accent, tests);
        Box("JumpBlock_High", new Vector3(-19, 0.5f, 0), new Vector3(3, 1f, 3), accent, tests);

        // Stairs: six 0.2 m steps (Step Offset is 0.3 m, so you can walk up them).
        for (int i = 0; i < 6; i++)
        {
            float h = 0.2f * (i + 1);
            Box("Stair" + (i + 1), new Vector3(15, h * 0.5f, 5 + i * 0.6f), new Vector3(3, h, 0.6f), concrete, tests);
        }

        // Ramp: 20 degree slope (Slope Limit is 45 degrees).
        Box("Ramp", new Vector3(22, 1.3f, 0), new Vector3(4, 0.3f, 8), dark, tests, new Vector3(-20, 0, 0));

        // ---- Player ----
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 0.05f, -20);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.03f;

        GameObject cameraRoot = new GameObject("CameraRoot");
        cameraRoot.transform.SetParent(player.transform, false);
        cameraRoot.transform.localPosition = new Vector3(0, 1.65f, 0);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraRoot.transform, false);
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        cameraObject.AddComponent<AudioListener>();

        PlayerController controller = player.AddComponent<PlayerController>();
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("cameraRoot").objectReferenceValue = cameraRoot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Debug overlay ----
        GameObject debug = new GameObject("DebugOverlay");
        PlayerDebugOverlay overlay = debug.AddComponent<PlayerDebugOverlay>();
        SerializedObject overlaySo = new SerializedObject(overlay);
        overlaySo.FindProperty("player").objectReferenceValue = controller;
        overlaySo.ApplyModifiedPropertiesWithoutUndo();

        // ---- Save ----
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Apex Rush", "Test scene built and saved to:\n" + ScenePath + "\n\nPress Play to test.", "OK");
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("URP Lit shader not found; using Standard. Did you create the project from the Universal 3D template?");
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Vector3 eulerAngles = default)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.Euler(eulerAngles);
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.isStatic = true;
        return go;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == path) return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif

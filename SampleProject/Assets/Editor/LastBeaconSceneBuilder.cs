using Persistly.Unity.LastBeacon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LastBeaconSceneBuilder
{
    [MenuItem("Persistly/Build Last Beacon Scene")]
    public static void BuildScene()
    {
        const string sceneDirectory = "Assets/Scenes";
        const string scenePath = "Assets/Scenes/LastBeacon.unity";

        if (!AssetDatabase.IsValidFolder(sceneDirectory))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.043f, 0.059f, 0.098f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var gameObject = new GameObject("Last Beacon");
        gameObject.AddComponent<LastBeaconController>();

        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void BuildSceneBatchMode()
    {
        BuildScene();
        Debug.Log("Last Beacon scene generated.");
        EditorApplication.Exit(0);
    }
}

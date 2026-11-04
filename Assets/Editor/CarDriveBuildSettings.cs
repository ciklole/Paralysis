#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the driving scene in Build Settings and provides a safe fallback
/// menu for wiring the City car to it.
/// </summary>
public static class CarDriveBuildSettings
{
    private const string DrivingScenePath = "Assets/Scenes/CarDriving.unity";
    private const string CityScenePath = "Assets/Scenes/City.unity";
    private const string DrivingSceneName = "CarDriving";

    [InitializeOnLoadMethod]
    private static void EnsureDrivingSceneInBuildSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DrivingScenePath) == null)
            return;

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == DrivingScenePath)
                return;
        }

        List<EditorBuildSettingsScene> list =
            new List<EditorBuildSettingsScene>(scenes);

        list.Add(new EditorBuildSettingsScene(DrivingScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        Debug.Log(
            "CarDriveBuildSettings: added " + DrivingScenePath +
            " to Build Settings."
        );
    }

    [MenuItem("Paralysis/Setup/Open Car Driving Scene")]
    public static void OpenDrivingScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DrivingScenePath) == null)
        {
            Debug.LogWarning(
                "CarDriveBuildSettings: " + DrivingScenePath +
                " does not exist yet."
            );

            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(DrivingScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Paralysis/Setup/Enable Car Driving From City")]
    public static void EnableCarDrivingFromCity()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CityScenePath) == null)
        {
            Debug.LogWarning(
                "CarDriveBuildSettings: " + CityScenePath + " is missing."
            );

            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(
            CityScenePath,
            OpenSceneMode.Single
        );

        CarInteraction[] cars =
            Object.FindObjectsByType<CarInteraction>();

        if (cars.Length == 0)
        {
            Debug.LogWarning(
                "CarDriveBuildSettings: no CarInteraction found in City."
            );

            return;
        }

        for (int i = 0; i < cars.Length; i++)
        {
            SerializedObject serialized = new SerializedObject(cars[i]);

            SerializedProperty useDriving =
                serialized.FindProperty("useDrivingScene");

            SerializedProperty sceneName =
                serialized.FindProperty("drivingScene");

            if (useDriving != null)
                useDriving.boolValue = true;

            if (sceneName != null)
                sceneName.stringValue = DrivingSceneName;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "CarDriveBuildSettings: City car now enters " +
            DrivingSceneName + " on E."
        );
    }
}
#endif

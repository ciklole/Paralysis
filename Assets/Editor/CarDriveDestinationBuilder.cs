#if UNITY_EDITOR
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds a visible store-arrival marker to the driving scene. The marker uses
/// the car's forward direction and a ground raycast so it lands on a road
/// instead of floating over a roof.
/// </summary>
[NoAutoStaticsCleanup]
public static class CarDriveDestinationBuilder
{
    private const string ScenePath = "Assets/Scenes/CarDriving.unity";
    private const string MaterialFolder = "Assets/Materials/Store";
    private const string MarkerMaterialPath =
        "Assets/Materials/Store/DestinationMarker.mat";

    [MenuItem("Paralysis/Setup/Move Store Destination To Selected Object")]
    public static void MoveToSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: exit Play Mode before moving the " +
                "destination."
            );

            return;
        }

        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: select the object that marks the " +
                "new destination first."
            );

            return;
        }

        Scene scene = selected.scene;

        if (!scene.IsValid() ||
            !string.Equals(
                scene.path,
                ScenePath,
                System.StringComparison.OrdinalIgnoreCase
            ))
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: select an object inside " + ScenePath + "."
            );

            return;
        }

        if (!MoveDestination(scene, selected.transform.position, selected.name))
            return;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Batch-mode entry point. Set PARALYSIS_DESTINATION_TARGET to the name of
    /// the object that marks the new destination before running this method.
    /// </summary>
    public static void MoveToObjectBatch()
    {
        string targetName = System.Environment.GetEnvironmentVariable(
            "PARALYSIS_DESTINATION_TARGET"
        );

        if (string.IsNullOrEmpty(targetName))
        {
            Debug.LogError(
                "CarDriveDestinationBuilder: set PARALYSIS_DESTINATION_TARGET to " +
                "the name of the object that marks the new destination."
            );

            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);

        Transform target = FindTransform(scene, targetName);

        if (target == null)
        {
            Debug.LogError(
                "CarDriveDestinationBuilder: no object named '" + targetName +
                "' in " + ScenePath
            );

            Restore(previous, scene);
            return;
        }

        if (MoveDestination(scene, target.position, target.name))
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        Restore(previous, scene);
    }

    [MenuItem("Paralysis/Setup/Add Car Destination To Driving Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: exit Play Mode before building."
            );

            return;
        }

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath,
            OpenSceneMode.Additive
        );

        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);

        Transform car = FindCar(scene);

        if (car == null)
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: no car found in " + ScenePath
            );

            Restore(previous, scene);
            return;
        }

        Vector3 destination = FindRoadPoint(car);

        GameObject root = FindRoot(scene, "Store Destination");

        if (root == null)
            root = new GameObject("Store Destination");

        root.transform.SetParent(null);
        root.transform.position = destination;
        root.transform.rotation = Quaternion.identity;

        CarDriveDestination component =
            root.GetComponent<CarDriveDestination>();

        if (component == null)
            component = root.AddComponent<CarDriveDestination>();

        BuildMarker(root.transform, destination);
        BuildSign(root.transform, destination, car);

        SerializedObject serialized = new SerializedObject(component);
        SetVector(serialized, "destinationPosition", destination);
        SetFloat(serialized, "triggerRadius", 10f);
        SetFloat(serialized, "verticalTolerance", 6f);
        SetString(serialized, "destinationScene", "Store");
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "CarDriveDestinationBuilder: destination at " +
            destination.ToString("F2") + " in " + ScenePath
        );

        Restore(previous, scene);
    }

    private static void Restore(Scene previous, Scene scene)
    {
        if (previous.IsValid() && previous.isLoaded)
            SceneManager.SetActiveScene(previous);

        EditorSceneManager.CloseScene(scene, true);
    }

    private static Transform FindCar(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name == "Free Racing Car")
                    return all[j].root;
            }
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }

        return null;
    }

    private static Transform FindTransform(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);

            for (int j = 0; j < all.Length; j++)
            {
                if (all[j].name == name)
                    return all[j];
            }
        }

        return null;
    }

    private static bool MoveDestination(
        Scene scene,
        Vector3 position,
        string sourceName)
    {
        GameObject root = FindRoot(scene, "Store Destination");

        if (root == null)
        {
            Debug.LogWarning(
                "CarDriveDestinationBuilder: no Store Destination root found in " +
                ScenePath
            );

            return false;
        }

        root.transform.position = position;

        CarDriveDestination component =
            root.GetComponent<CarDriveDestination>();

        if (component != null)
        {
            SerializedObject serialized = new SerializedObject(component);
            SetVector(serialized, "destinationPosition", position);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        SnapChildren(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            "CarDriveDestinationBuilder: moved Store Destination to " +
            position.ToString("F2") + " (from " + sourceName + ")."
        );

        return true;
    }

    private static void SnapChildren(Transform root)
    {
        Transform marker = root.Find("Destination Marker");

        if (marker != null)
            marker.localPosition = new Vector3(0f, 0.06f, 0f);

        Transform beam = root.Find("Destination Beam");

        if (beam != null)
            beam.localPosition = new Vector3(0f, 4f, 0f);

        Transform sign = root.Find("Destination Sign");

        if (sign != null)
            sign.localPosition = new Vector3(0f, 3.2f, 0f);
    }

    private static Vector3 FindRoadPoint(Transform car)
    {
        float[] distances = { 70f, 90f, 110f, 130f, 150f, 170f, 50f };
        Vector3 forward = car.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.right;

        forward.Normalize();

        for (int i = 0; i < distances.Length; i++)
        {
            Vector3 sample = car.position + forward * distances[i];
            Vector3 origin = sample + Vector3.up * 40f;

            if (!Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    120f,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            string name = hit.collider.name;

            if (name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Plane", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Cross", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Sidewalk", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            return hit.point + Vector3.up * 0.1f;
        }

        return car.position + forward * 90f;
    }

    private static void BuildMarker(Transform parent, Vector3 position)
    {
        Transform existing = parent.Find("Destination Marker");

        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        Material material = GetMarkerMaterial();

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Destination Marker";
        ring.transform.SetParent(parent, false);
        ring.transform.position = position + Vector3.up * 0.06f;
        ring.transform.localScale = new Vector3(10f, 0.04f, 10f);

        Collider collider = ring.GetComponent<Collider>();

        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = ring.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "Destination Beam";
        beam.transform.SetParent(parent, false);
        beam.transform.position = position + Vector3.up * 4f;
        beam.transform.localScale = new Vector3(0.6f, 4f, 0.6f);

        Collider beamCollider = beam.GetComponent<Collider>();

        if (beamCollider != null)
            Object.DestroyImmediate(beamCollider);

        Renderer beamRenderer = beam.GetComponent<Renderer>();

        if (beamRenderer != null)
            beamRenderer.sharedMaterial = material;
    }

    private static void BuildSign(
        Transform parent,
        Vector3 position,
        Transform car)
    {
        Transform existing = parent.Find("Destination Sign");

        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject sign = new GameObject("Destination Sign");
        sign.transform.SetParent(parent, false);
        sign.transform.position = position + Vector3.up * 3.2f;

        Vector3 direction = car.position - position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
            sign.transform.rotation = Quaternion.LookRotation(direction.normalized);

        WorldSign.Create(
            "Destination Sign Text",
            sign.transform,
            "GROCERY STORE",
            new Color(0.55f, 1f, 0.7f),
            5f,
            1.2f,
            72,
            Vector3.zero,
            Quaternion.identity
        );
    }

    private static Material GetMarkerMaterial()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Store");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            MarkerMaterialPath
        );

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MarkerMaterialPath);
        }

        material.color = new Color(0.1f, 0.9f, 0.35f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.05f, 0.8f, 0.2f));
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.5f);
        material.SetFloat("_Glossiness", 0.5f);
        EditorUtility.SetDirty(material);

        return material;
    }

    private static void SetString(
        SerializedObject serialized,
        string name,
        string value)
    {
        SerializedProperty property = serialized.FindProperty(name);

        if (property != null)
            property.stringValue = value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string name,
        float value)
    {
        SerializedProperty property = serialized.FindProperty(name);

        if (property != null)
            property.floatValue = value;
    }

    private static void SetVector(
        SerializedObject serialized,
        string name,
        Vector3 value)
    {
        SerializedProperty property = serialized.FindProperty(name);

        if (property != null)
            property.vector3Value = value;
    }
}
#endif

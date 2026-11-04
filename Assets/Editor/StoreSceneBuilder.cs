#if UNITY_EDITOR
// Generator for the grocery store scene. Safe to run repeatedly.
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a small, self-contained grocery store scene from primitives. The
/// scene is intentionally generated so the layout, groceries and cashier are
/// all real scene objects that can be edited in the Inspector afterwards.
///
/// Menu: Paralysis > Setup > Build Store Scene
/// </summary>
[NoAutoStaticsCleanup]
public static class StoreSceneBuilder
{
    private const string StoreScenePath = "Assets/Scenes/Store.unity";
    private const string PlayerPrefabPath =
        "Assets/ModularFirstPersonController/FirstPersonController/FirstPersonController.prefab";
    private const string MaterialFolder = "Assets/Materials/Store";
    private const string StoreSceneName = "Store";

    private const string HomeScenePath = "Assets/Scenes/Home.unity";
    private const string CityScenePath = "Assets/Scenes/City.unity";
    private const string CarDrivingScenePath = "Assets/Scenes/CarDriving.unity";

    private static Material floorMaterial;
    private static Material wallMaterial;
    private static Material ceilingMaterial;
    private static Material shelfMaterial;
    private static Material shelfTrimMaterial;
    private static Material counterMaterial;
    private static Material counterTopMaterial;
    private static Material registerMaterial;
    private static Material breadMaterial;
    private static Material milkMaterial;
    private static Material eggsMaterial;
    private static Material cashierUniformMaterial;
    private static Material cashierSkinMaterial;
    private static Material cashierApronMaterial;
    private static Material transformedMaterial;
    private static Material eyeMaterial;
    private static Material signMaterial;

    [MenuItem("Paralysis/Setup/Build Store Scene")]
    public static void BuildStoreScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "StoreSceneBuilder: exit Play Mode before building the store."
            );

            return;
        }

        Scene previousActive = SceneManager.GetActiveScene();
        bool storeWasOpen = TryGetOpenScene(StoreScenePath, out Scene storeScene);
        bool openedStore = false;

        if (!storeWasOpen)
        {
            if (File.Exists(StoreScenePath))
            {
                storeScene = EditorSceneManager.OpenScene(
                    StoreScenePath,
                    OpenSceneMode.Additive
                );
            }
            else
            {
                storeScene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive
                );
            }

            openedStore = true;
        }

        SceneManager.SetActiveScene(storeScene);
        ClearScene(storeScene);

        EnsureFolder(MaterialFolder);
        CreateMaterials();

        BuildLighting(storeScene);
        BuildRoom(storeScene);
        BuildShelves(storeScene);
        BuildCashierArea(storeScene);
        BuildManagers(storeScene);
        SpawnPlayer(storeScene);

        EditorSceneManager.MarkSceneDirty(storeScene);
        EditorSceneManager.SaveScene(storeScene, StoreScenePath);
        AddStoreToBuildSettings();
        WireStoreSceneIntoStoryDirectors(StoreScenePath);

        if (previousActive.IsValid() && previousActive.isLoaded)
            SceneManager.SetActiveScene(previousActive);

        if (openedStore)
            EditorSceneManager.CloseScene(storeScene, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "StoreSceneBuilder: built '" + StoreScenePath + "' and wired it into " +
            "StoryDirector.storeScene. Open it from the Project window to edit it."
        );
    }

    private static bool TryGetOpenScene(string path, out Scene scene)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene candidate = SceneManager.GetSceneAt(i);

            if (candidate.path == path)
            {
                scene = candidate;
                return true;
            }
        }

        scene = default;
        return false;
    }

    private static void ClearScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                Object.DestroyImmediate(roots[i]);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(parent))
            return;

        parent = parent.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void CreateMaterials()
    {
        floorMaterial = GetOrCreateMaterial(
            "Floor",
            new Color(0.42f, 0.42f, 0.45f),
            0f,
            0.35f
        );

        wallMaterial = GetOrCreateMaterial(
            "Wall",
            new Color(0.72f, 0.72f, 0.69f),
            0f,
            0.1f
        );

        ceilingMaterial = GetOrCreateMaterial(
            "Ceiling",
            new Color(0.55f, 0.55f, 0.58f),
            0f,
            0.05f
        );

        shelfMaterial = GetOrCreateMaterial(
            "Shelf",
            new Color(0.2f, 0.32f, 0.42f),
            0.25f,
            0.45f
        );

        shelfTrimMaterial = GetOrCreateMaterial(
            "ShelfTrim",
            new Color(0.08f, 0.09f, 0.11f),
            0.1f,
            0.5f
        );

        counterMaterial = GetOrCreateMaterial(
            "Counter",
            new Color(0.32f, 0.2f, 0.11f),
            0f,
            0.25f
        );

        counterTopMaterial = GetOrCreateMaterial(
            "CounterTop",
            new Color(0.11f, 0.11f, 0.13f),
            0.1f,
            0.6f
        );

        registerMaterial = GetOrCreateMaterial(
            "Register",
            new Color(0.08f, 0.08f, 0.1f),
            0.2f,
            0.55f
        );

        breadMaterial = GetOrCreateMaterial(
            "Bread",
            new Color(0.66f, 0.44f, 0.18f),
            0f,
            0.2f
        );

        milkMaterial = GetOrCreateMaterial(
            "Milk",
            new Color(0.92f, 0.92f, 0.94f),
            0f,
            0.55f
        );

        eggsMaterial = GetOrCreateMaterial(
            "Eggs",
            new Color(0.92f, 0.86f, 0.64f),
            0f,
            0.3f
        );

        cashierUniformMaterial = GetOrCreateMaterial(
            "CashierUniform",
            new Color(0.13f, 0.42f, 0.22f),
            0f,
            0.2f
        );

        cashierSkinMaterial = GetOrCreateMaterial(
            "CashierSkin",
            new Color(0.76f, 0.6f, 0.5f),
            0f,
            0.15f
        );

        cashierApronMaterial = GetOrCreateMaterial(
            "CashierApron",
            new Color(0.06f, 0.23f, 0.1f),
            0f,
            0.15f
        );

        transformedMaterial = GetOrCreateMaterial(
            "Transformed",
            new Color(0.23f, 0.015f, 0.015f),
            0.05f,
            0.65f
        );

        eyeMaterial = GetOrCreateMaterial(
            "TransformedEyes",
            new Color(1f, 0.08f, 0.03f),
            0f,
            0.8f,
            new Color(2.5f, 0.08f, 0.02f)
        );

        signMaterial = GetOrCreateMaterial(
            "Sign",
            new Color(0.05f, 0.05f, 0.06f),
            0.1f,
            0.4f
        );
    }

    private static Material GetOrCreateMaterial(
        string name,
        Color color,
        float metallic,
        float smoothness,
        Color? emission = null)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Glossiness", smoothness);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    #region Lighting and room

    private static void BuildLighting(Scene scene)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.38f, 0.42f);
        RenderSettings.fog = false;

        GameObject sun = new GameObject("Store Directional Light");
        SceneManager.MoveGameObjectToScene(sun, scene);
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1f, 0.93f, 0.82f);
        sunLight.intensity = 0.35f;
        sunLight.shadows = LightShadows.Soft;

        CreatePointLight(
            scene,
            "Ceiling Light A",
            new Vector3(-6f, 3.75f, -1f),
            new Color(1f, 0.92f, 0.8f),
            2.2f,
            15f
        );

        CreatePointLight(
            scene,
            "Ceiling Light B",
            new Vector3(6f, 3.75f, -1f),
            new Color(1f, 0.92f, 0.8f),
            2.2f,
            15f
        );

        CreatePointLight(
            scene,
            "Ceiling Light C",
            new Vector3(-6f, 3.75f, 8f),
            new Color(1f, 0.92f, 0.8f),
            2.2f,
            15f
        );

        CreatePointLight(
            scene,
            "Ceiling Light D",
            new Vector3(6f, 3.75f, 8f),
            new Color(1f, 0.92f, 0.8f),
            2.2f,
            15f
        );

        CreatePointLight(
            scene,
            "Cashier Spotlight",
            new Vector3(6f, 3.5f, 2.2f),
            new Color(0.85f, 1f, 0.85f),
            1.4f,
            7f
        );
    }

    private static void CreatePointLight(
        Scene scene,
        string name,
        Vector3 position,
        Color color,
        float intensity,
        float range)
    {
        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static void BuildRoom(Scene scene)
    {
        GameObject room = new GameObject("Store Environment");
        SceneManager.MoveGameObjectToScene(room, scene);

        Transform root = room.transform;

        // Floor and ceiling.
        CreateBox(
            root,
            "Floor",
            new Vector3(0f, -0.1f, 3f),
            new Vector3(24f, 0.2f, 20f),
            floorMaterial
        );

        CreateBox(
            root,
            "Ceiling",
            new Vector3(0f, 4.15f, 3f),
            new Vector3(24f, 0.2f, 20f),
            ceilingMaterial
        );

        // Side and back walls.
        CreateBox(
            root,
            "Wall Left",
            new Vector3(-12f, 2f, 3f),
            new Vector3(0.4f, 4f, 20f),
            wallMaterial
        );

        CreateBox(
            root,
            "Wall Right",
            new Vector3(12f, 2f, 3f),
            new Vector3(0.4f, 4f, 20f),
            wallMaterial
        );

        CreateBox(
            root,
            "Wall Back",
            new Vector3(0f, 2f, 13f),
            new Vector3(24f, 4f, 0.4f),
            wallMaterial
        );

        // Front wall with a 4 m wide doorway.
        CreateBox(
            root,
            "Wall Front Left",
            new Vector3(-7f, 2f, -7f),
            new Vector3(10f, 4f, 0.4f),
            wallMaterial
        );

        CreateBox(
            root,
            "Wall Front Right",
            new Vector3(7f, 2f, -7f),
            new Vector3(10f, 4f, 0.4f),
            wallMaterial
        );

        CreateBox(
            root,
            "Door Header",
            new Vector3(0f, 3.5f, -7f),
            new Vector3(4f, 1f, 0.4f),
            wallMaterial
        );

        // Door frame trim.
        CreateBox(
            root,
            "Door Frame Left",
            new Vector3(-2.1f, 1.5f, -6.95f),
            new Vector3(0.2f, 3f, 0.2f),
            shelfTrimMaterial
        );

        CreateBox(
            root,
            "Door Frame Right",
            new Vector3(2.1f, 1.5f, -6.95f),
            new Vector3(0.2f, 3f, 0.2f),
            shelfTrimMaterial
        );

        CreateBox(
            root,
            "Door Frame Top",
            new Vector3(0f, 3.05f, -6.95f),
            new Vector3(4.4f, 0.2f, 0.2f),
            shelfTrimMaterial
        );

        // Entrance mat.
        CreateBox(
            root,
            "Entrance Mat",
            new Vector3(0f, 0.015f, -5.6f),
            new Vector3(4.5f, 0.03f, 2.2f),
            shelfTrimMaterial,
            false
        );

        // Baseboards.
        CreateBox(
            root,
            "Baseboard Left",
            new Vector3(-11.75f, 0.12f, 3f),
            new Vector3(0.1f, 0.24f, 19.6f),
            shelfTrimMaterial,
            false
        );

        CreateBox(
            root,
            "Baseboard Right",
            new Vector3(11.75f, 0.12f, 3f),
            new Vector3(0.1f, 0.24f, 19.6f),
            shelfTrimMaterial,
            false
        );

        CreateBox(
            root,
            "Baseboard Back",
            new Vector3(0f, 0.12f, 12.75f),
            new Vector3(23.6f, 0.24f, 0.1f),
            shelfTrimMaterial,
            false
        );

        // A couple of decorative crates near the entrance.
        CreateBox(
            root,
            "Crate A",
            new Vector3(-9.2f, 0.4f, -4.2f),
            new Vector3(1.2f, 0.8f, 1.2f),
            counterMaterial
        );

        CreateBox(
            root,
            "Crate B",
            new Vector3(-8.2f, 0.3f, -5.1f),
            new Vector3(0.9f, 0.6f, 0.9f),
            counterMaterial
        );

        CreateSign(
            root,
            "Back Wall Sign",
            "GROCERY",
            new Vector3(0f, 3.25f, 12.65f),
            Quaternion.Euler(0f, 180f, 0f),
            90
        );
    }

    #endregion

    #region Shelves and groceries

    private static void BuildShelves(Scene scene)
    {
        GameObject shelves = new GameObject("Shelves");
        SceneManager.MoveGameObjectToScene(shelves, scene);

        Transform leftShelf = CreateShelf(
            shelves.transform,
            "Shelf Left",
            new Vector3(-4f, 0f, 6.5f),
            true
        );

        Transform rightShelf = CreateShelf(
            shelves.transform,
            "Shelf Right",
            new Vector3(4f, 0f, 6.5f),
            false
        );

        // Bread and eggs on the left shelf, milk on the right shelf.
        CreateGrocery(
            leftShelf,
            "Bread",
            "bread",
            new Vector3(0.28f, 1.47f, -1.7f),
            new Vector3(0.5f, 0.26f, 0.28f),
            breadMaterial
        );

        CreateGrocery(
            leftShelf,
            "Eggs",
            "eggs",
            new Vector3(0.28f, 1.42f, 1.4f),
            new Vector3(0.38f, 0.16f, 0.3f),
            eggsMaterial
        );

        CreateGrocery(
            rightShelf,
            "Milk",
            "milk",
            new Vector3(-0.28f, 1.56f, -0.9f),
            new Vector3(0.24f, 0.44f, 0.24f),
            milkMaterial
        );

        // A few non-required products so the shelves do not look empty.
        CreateDecorativeProduct(
            leftShelf,
            new Vector3(0.28f, 1.42f, -3.1f),
            new Vector3(0.3f, 0.2f, 0.24f),
            breadMaterial
        );

        CreateDecorativeProduct(
            rightShelf,
            new Vector3(-0.28f, 1.42f, 1.2f),
            new Vector3(0.26f, 0.2f, 0.26f),
            eggsMaterial
        );

        CreateDecorativeProduct(
            rightShelf,
            new Vector3(-0.28f, 1.47f, 3.0f),
            new Vector3(0.3f, 0.26f, 0.3f),
            counterMaterial
        );

        CreateSign(
            shelves.transform,
            "Aisle Sign",
            "GROCERIES",
            new Vector3(0f, 3.1f, 9.8f),
            Quaternion.Euler(0f, 180f, 0f),
            70
        );
    }

    private static Transform CreateShelf(
        Transform parent,
        string name,
        Vector3 position,
        bool extendsRight)
    {
        GameObject shelf = new GameObject(name);
        shelf.transform.SetParent(parent, false);
        shelf.transform.position = position;

        float backX = extendsRight ? -0.4f : 0.4f;

        CreateBox(
            shelf.transform,
            "Back Panel",
            new Vector3(backX, 1.1f, 0f),
            new Vector3(0.08f, 2.2f, 7f),
            shelfMaterial
        );

        CreateBox(
            shelf.transform,
            "Base",
            new Vector3(0f, 0.15f, 0f),
            new Vector3(0.8f, 0.3f, 7f),
            shelfTrimMaterial
        );

        for (int i = 0; i < 4; i++)
        {
            float y = 0.3f + i * 0.5f;

            CreateBox(
                shelf.transform,
                "Shelf Board " + (i + 1),
                new Vector3(0f, y, 0f),
                new Vector3(0.8f, 0.08f, 7f),
                shelfMaterial
            );

            CreateBox(
                shelf.transform,
                "Shelf Trim " + (i + 1),
                new Vector3(extendsRight ? 0.42f : -0.42f, y, 0f),
                new Vector3(0.04f, 0.12f, 7f),
                shelfTrimMaterial,
                false
            );
        }

        CreateBox(
            shelf.transform,
            "End Panel A",
            new Vector3(0f, 1.1f, -3.46f),
            new Vector3(0.8f, 2.2f, 0.08f),
            shelfMaterial
        );

        CreateBox(
            shelf.transform,
            "End Panel B",
            new Vector3(0f, 1.1f, 3.46f),
            new Vector3(0.8f, 2.2f, 0.08f),
            shelfMaterial
        );

        CreateBox(
            shelf.transform,
            "Top Rail",
            new Vector3(0f, 2.22f, 0f),
            new Vector3(0.84f, 0.08f, 7f),
            shelfTrimMaterial,
            false
        );

        return shelf.transform;
    }

    private static void CreateGrocery(
        Transform parent,
        string name,
        string itemId,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject item = CreateBox(
            parent,
            name,
            localPosition,
            localScale,
            material
        );

        GroceryItem grocery = item.AddComponent<GroceryItem>();

        SerializedObject serialized = new SerializedObject(grocery);
        SetString(serialized, "itemId", itemId);
        SetFloat(serialized, "range", 3.5f);
        SetString(serialized, "prompt", "F - Add to cart");
        SetBool(serialized, "hideAfterPickup", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDecorativeProduct(
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        CreateBox(
            parent,
            "Product",
            localPosition,
            localScale,
            material
        );
    }

    #endregion

    #region Cashier

    private static void BuildCashierArea(Scene scene)
    {
        GameObject area = new GameObject("Checkout");
        SceneManager.MoveGameObjectToScene(area, scene);

        Transform root = area.transform;

        // Counter.
        CreateBox(
            root,
            "Counter Base",
            new Vector3(6f, 0.55f, 2f),
            new Vector3(5f, 1.1f, 1.2f),
            counterMaterial
        );

        CreateBox(
            root,
            "Counter Top",
            new Vector3(6f, 1.14f, 2f),
            new Vector3(5.2f, 0.08f, 1.4f),
            counterTopMaterial
        );

        CreateBox(
            root,
            "Counter Front",
            new Vector3(6f, 0.58f, 1.36f),
            new Vector3(4.6f, 0.9f, 0.08f),
            shelfTrimMaterial,
            false
        );

        // Conveyor belt.
        CreateBox(
            root,
            "Belt",
            new Vector3(6f, 1.19f, 1.9f),
            new Vector3(3.8f, 0.03f, 0.7f),
            registerMaterial,
            false
        );

        // Register.
        CreateBox(
            root,
            "Register Body",
            new Vector3(8.1f, 1.38f, 2.1f),
            new Vector3(0.7f, 0.45f, 0.55f),
            registerMaterial
        );

        CreateBox(
            root,
            "Register Screen",
            new Vector3(8.1f, 1.72f, 2.1f),
            new Vector3(0.55f, 0.32f, 0.06f),
            eyeMaterial,
            false
        );

        CreateBox(
            root,
            "Scanner",
            new Vector3(7.4f, 1.24f, 2.1f),
            new Vector3(0.35f, 0.12f, 0.3f),
            registerMaterial,
            false
        );

        CreateSign(
            root,
            "Checkout Sign",
            "CHECKOUT",
            new Vector3(6f, 3.25f, 2.3f),
            Quaternion.Euler(0f, 180f, 0f),
            80
        );

        // Cashier.
        GameObject cashier = new GameObject("Cashier");
        cashier.transform.SetParent(root, false);
        cashier.transform.position = new Vector3(6f, 0f, 3.35f);

        CashierInteraction interaction = cashier.AddComponent<CashierInteraction>();

        GameObject normalModel = BuildNormalCashier(cashier.transform);
        GameObject transformedModel = BuildTransformedCashier(cashier.transform);

        GameObject interactionPoint = new GameObject("Interaction Point");
        interactionPoint.transform.SetParent(cashier.transform, false);
        interactionPoint.transform.localPosition = new Vector3(0f, 1.35f, -0.35f);

        AudioSource cashierAudio = cashier.AddComponent<AudioSource>();
        cashierAudio.playOnAwake = false;
        cashierAudio.spatialBlend = 0.5f;

        GameObject ambienceObject = new GameObject("Store Ambience");
        ambienceObject.transform.SetParent(root, false);
        ambienceObject.transform.position = new Vector3(6f, 2f, 3f);

        AudioSource ambience = ambienceObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        ambience.volume = 0.18f;
        ambience.spatialBlend = 0f;

        SerializedObject serialized = new SerializedObject(interaction);
        SetObject(serialized, "normalModel", normalModel);
        SetObject(serialized, "transformedModel", transformedModel);
        SetObject(serialized, "interactionPoint", interactionPoint.transform);
        SetObject(serialized, "audioSource", cashierAudio);
        SetObject(serialized, "scannerSource", cashierAudio);
        SetObject(serialized, "ambienceSource", ambience);
        SetBool(serialized, "restoreAfterTransformation", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // A collider on the cashier root gives the line-of-sight check a target
        // even while the visual models are being swapped.
        BoxCollider collider = cashier.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(0.9f, 2f, 0.9f);
        collider.isTrigger = true;
    }

    private static GameObject BuildNormalCashier(Transform parent)
    {
        GameObject model = new GameObject("Normal Model");
        model.transform.SetParent(parent, false);

        CreateCapsule(
            model.transform,
            "Body",
            new Vector3(0f, 0.9f, 0f),
            new Vector3(0.8f, 0.9f, 0.8f),
            cashierUniformMaterial
        );

        CreateSphere(
            model.transform,
            "Head",
            new Vector3(0f, 1.78f, 0f),
            new Vector3(0.46f, 0.5f, 0.46f),
            cashierSkinMaterial
        );

        CreateBox(
            model.transform,
            "Apron",
            new Vector3(0f, 0.92f, 0.38f),
            new Vector3(0.58f, 0.7f, 0.06f),
            cashierApronMaterial,
            false
        );

        CreateBox(
            model.transform,
            "Name Tag",
            new Vector3(0.18f, 1.25f, 0.42f),
            new Vector3(0.14f, 0.08f, 0.02f),
            milkMaterial,
            false
        );

        CreateSphere(
            model.transform,
            "Cap",
            new Vector3(0f, 2.02f, 0f),
            new Vector3(0.48f, 0.14f, 0.48f),
            cashierApronMaterial,
            false
        );

        return model;
    }

    private static GameObject BuildTransformedCashier(Transform parent)
    {
        GameObject model = new GameObject("Transformed Model");
        model.transform.SetParent(parent, false);
        model.SetActive(false);

        CreateCapsule(
            model.transform,
            "Body",
            new Vector3(0f, 1.2f, 0f),
            new Vector3(0.9f, 1.2f, 0.9f),
            transformedMaterial
        );

        CreateSphere(
            model.transform,
            "Head",
            new Vector3(0f, 2.42f, 0f),
            new Vector3(0.54f, 0.62f, 0.54f),
            transformedMaterial
        );

        CreateSphere(
            model.transform,
            "Eye Left",
            new Vector3(-0.16f, 2.48f, 0.24f),
            new Vector3(0.07f, 0.07f, 0.07f),
            eyeMaterial,
            false
        );

        CreateSphere(
            model.transform,
            "Eye Right",
            new Vector3(0.16f, 2.48f, 0.24f),
            new Vector3(0.07f, 0.07f, 0.07f),
            eyeMaterial,
            false
        );

        CreateBox(
            model.transform,
            "Arm Left",
            new Vector3(-0.7f, 1.3f, 0f),
            new Vector3(0.16f, 1.7f, 0.16f),
            transformedMaterial,
            false
        ).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

        CreateBox(
            model.transform,
            "Arm Right",
            new Vector3(0.7f, 1.3f, 0f),
            new Vector3(0.16f, 1.7f, 0.16f),
            transformedMaterial,
            false
        ).transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

        CreateBox(
            model.transform,
            "Horn Left",
            new Vector3(-0.22f, 2.85f, 0f),
            new Vector3(0.08f, 0.5f, 0.08f),
            transformedMaterial,
            false
        ).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        CreateBox(
            model.transform,
            "Horn Right",
            new Vector3(0.22f, 2.85f, 0f),
            new Vector3(0.08f, 0.5f, 0.08f),
            transformedMaterial,
            false
        ).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        GameObject glow = new GameObject("Red Glow");
        glow.transform.SetParent(model.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.8f, 0.6f);

        Light redLight = glow.AddComponent<Light>();
        redLight.type = LightType.Point;
        redLight.color = new Color(1f, 0.05f, 0.02f);
        redLight.intensity = 2.5f;
        redLight.range = 4f;
        redLight.shadows = LightShadows.None;

        return model;
    }

    #endregion

    #region Managers and player

    private static void BuildManagers(Scene scene)
    {
        GameObject managers = new GameObject("StoryManagers");
        SceneManager.MoveGameObjectToScene(managers, scene);

        StoryDirector director = managers.AddComponent<StoryDirector>();
        managers.AddComponent<DialogueSystem>();
        managers.AddComponent<PlayerInteractor>();
        managers.AddComponent<ObjectiveManager>();
        managers.AddComponent<QuestLogUI>();
        ShoppingCart cart = managers.AddComponent<ShoppingCart>();
        managers.AddComponent<ToastUI>();

        SerializedObject serialized = new SerializedObject(director);
        SetString(serialized, "apartmentScene", "Home");
        SetString(serialized, "cityScene", "City");
        SetString(serialized, "storeScene", StoreSceneName);
        SetObject(serialized, "cart", cart);
        SetBool(serialized, "requireCarTripForStoreCompletion", true);
        SetBool(serialized, "enableDebugKeys", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SpawnPlayer(Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PlayerPrefabPath
        );

        if (prefab == null)
        {
            Debug.LogError(
                "StoreSceneBuilder: player prefab not found at '" +
                PlayerPrefabPath + "'."
            );

            return;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
            prefab,
            scene
        );

        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, -5.4f);
        player.transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Scene wiring and build settings

    private static void AddStoreToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == StoreScenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(StoreScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void WireStoreSceneIntoStoryDirectors(string storePath)
    {
        string[] paths =
        {
            HomeScenePath,
            CityScenePath,
            CarDrivingScenePath,
            storePath
        };

        for (int i = 0; i < paths.Length; i++)
        {
            WireScene(paths[i]);
        }
    }

    private static void WireScene(string path)
    {
        if (!File.Exists(path))
            return;

        bool wasOpen = TryGetOpenScene(path, out Scene scene);
        bool opened = false;

        if (!wasOpen)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            opened = true;
        }

        StoryDirector[] directors = Object.FindObjectsByType<StoryDirector>(
            FindObjectsInactive.Include
        );

        bool changed = false;

        for (int i = 0; i < directors.Length; i++)
        {
            if (directors[i] == null ||
                directors[i].gameObject.scene != scene)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(directors[i]);
            SerializedProperty storeScene = serialized.FindProperty("storeScene");

            if (storeScene == null ||
                storeScene.stringValue == StoreSceneName)
            {
                continue;
            }

            storeScene.stringValue = StoreSceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (opened)
            EditorSceneManager.CloseScene(scene, true);
    }

    #endregion

    #region Primitive helpers

    private static GameObject CreateBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool withCollider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();

            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static GameObject CreateSphere(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool withCollider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();

            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static GameObject CreateCapsule(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool withCollider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null)
            renderer.sharedMaterial = material;

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();

            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        return go;
    }

    private static void CreateSign(
        Transform parent,
        string name,
        string message,
        Vector3 localPosition,
        Quaternion localRotation,
        int fontSize)
    {
        GameObject board = CreateBox(
            parent,
            name + " Board",
            localPosition,
            new Vector3(4.8f, 1.1f, 0.12f),
            signMaterial,
            false
        );

        board.transform.localRotation = localRotation;

        WorldSign.Create(
            name + " Text",
            board.transform,
            message,
            new Color(1f, 0.82f, 0.38f),
            4.6f,
            1f,
            fontSize,
            new Vector3(0f, 0f, 0.08f),
            Quaternion.identity
        );
    }

    #endregion

    #region Serialized property helpers

    private static void SetString(
        SerializedObject serialized,
        string propertyName,
        string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property != null)
            property.stringValue = value;
    }

    private static void SetFloat(
        SerializedObject serialized,
        string propertyName,
        float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property != null)
            property.floatValue = value;
    }

    private static void SetBool(
        SerializedObject serialized,
        string propertyName,
        bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property != null)
            property.boolValue = value;
    }

    private static void SetObject(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property != null)
            property.objectReferenceValue = value;
    }

    #endregion
}

/// <summary>
/// One-shot hook used by the automated build. It only runs when the flag file
/// exists, so normal editor reloads never rebuild the store.
/// </summary>
[NoAutoStaticsCleanup]
internal static class StoreSceneBuilderAutoRun
{
    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string projectRoot = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..")
        );

        string flag = Path.Combine(projectRoot, "Temp", "build_store_scene.flag");

        if (!File.Exists(flag))
            return;

        try
        {
            File.Delete(flag);
        }
        catch (IOException)
        {
            // The build can still run; the flag will be removed next time.
        }

        Debug.Log("StoreSceneBuilderAutoRun: flag found, building store scene.");
        StoreSceneBuilder.BuildStoreScene();
    }
}
#endif
// build trigger 2026-10-24T18:05:19.2690804-07:00

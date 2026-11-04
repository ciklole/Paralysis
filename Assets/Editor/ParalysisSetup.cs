using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor conveniences for the story systems. Nothing here runs at runtime.
/// </summary>
public static class ParalysisSetup
{
    [MenuItem("Paralysis/Setup/Create Story Managers In Open Scene")]
    public static void CreateStoryManagers()
    {
        StoryDirector existing = Object.FindAnyObjectByType<StoryDirector>();

        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);

            Debug.Log(
                "Paralysis: a StoryDirector already exists in this scene.",
                existing
            );

            return;
        }

        GameObject managers = new GameObject("StoryManagers");

        Undo.RegisterCreatedObjectUndo(managers, "Create Story Managers");

        managers.AddComponent<StoryDirector>();
        managers.AddComponent<DialogueSystem>();
        managers.AddComponent<ObjectiveManager>();
        managers.AddComponent<QuestLogUI>();
        managers.AddComponent<ShoppingCart>();
        managers.AddComponent<PlayerInteractor>();
        managers.AddComponent<ToastUI>();

        Selection.activeGameObject = managers;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            "Paralysis: created StoryManagers. Save the scene, then add the same " +
            "object to the other story scenes (or copy it). Duplicates delete " +
            "themselves at runtime.",
            managers
        );
    }

    [MenuItem("Paralysis/Setup/Validate Story Setup")]
    public static void ValidateStorySetup()
    {
        int issues = 0;

        issues += RequireManager<StoryDirector>("StoryDirector");
        issues += RequireManager<DialogueSystem>("DialogueSystem");
        issues += RequireManager<ObjectiveManager>("ObjectiveManager");
        issues += RequireManager<QuestLogUI>("QuestLogUI");
        issues += RequireManager<ShoppingCart>("ShoppingCart");
        issues += RequireManager<PlayerInteractor>("PlayerInteractor");
        issues += RequireManager<ToastUI>("ToastUI");

        ShoppingCart cart = Object.FindAnyObjectByType<ShoppingCart>();
        GroceryItem[] groceries = Object.FindObjectsByType<GroceryItem>();

        if (cart != null)
        {
            for (int i = 0; i < groceries.Length; i++)
            {
                GroceryItem grocery = groceries[i];

                if (!cart.IsRequired(grocery.ItemId))
                {
                    Debug.LogWarning(
                        "Paralysis setup: GroceryItem '" + grocery.name +
                        "' uses item id '" + grocery.ItemId +
                        "', which is not on the shopping list.",
                        grocery
                    );

                    issues++;
                }
            }
        }

        StoryDirector director = Object.FindAnyObjectByType<StoryDirector>();

        if (director != null)
        {
            SerializedObject serialized = new SerializedObject(director);
            SerializedProperty store = serialized.FindProperty("storeScene");

            if (store != null)
            {
                string storeScene = store.stringValue;

                if (string.IsNullOrEmpty(storeScene))
                {
                    Debug.LogWarning(
                        "Paralysis setup: StoryDirector has no store scene. " +
                        "The car cannot travel there and the store intro will " +
                        "not run until one is assigned.",
                        director
                    );

                    issues++;
                }
                else if (!Application.CanStreamedLevelBeLoaded(storeScene))
                {
                    Debug.LogError(
                        "Paralysis setup: store scene '" + storeScene +
                        "' is not in Build Settings.",
                        director
                    );

                    issues++;
                }
            }

            issues += CheckId(
                Object.FindAnyObjectByType<LightsObjective>(),
                "objectiveId",
                ReadString(serialized, "lightsObjectiveId"),
                "LightsObjective"
            );

            issues += CheckId(
                Object.FindAnyObjectByType<CarInteraction>(),
                "objectiveId",
                ReadString(serialized, "driveObjectiveId"),
                "CarInteraction"
            );

            issues += CheckId(
                Object.FindAnyObjectByType<CashierInteraction>(),
                "payObjectiveId",
                ReadString(serialized, "payObjectiveId"),
                "CashierInteraction pay objective"
            );
        }

        if (issues == 0)
        {
            Debug.Log(
                "Paralysis setup: no issues found in the open scene."
            );
        }
        else
        {
            Debug.LogWarning(
                "Paralysis setup: " + issues +
                " issue(s) found. See the messages above."
            );
        }
    }

    [MenuItem("Paralysis/Testing/Reset Session (Play Mode)")]
    public static void ResetSession()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Paralysis: enter Play Mode first.");
            return;
        }

        if (StoryDirector.Instance == null)
        {
            Debug.LogWarning("Paralysis: no StoryDirector is running.");
            return;
        }

        StoryDirector.Instance.ResetSession(true);
    }

    [MenuItem("Paralysis/Testing/Skip To Store Stage (Play Mode)")]
    public static void SkipToStore()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Paralysis: enter Play Mode first.");
            return;
        }

        if (StoryDirector.Instance == null)
        {
            Debug.LogWarning("Paralysis: no StoryDirector is running.");
            return;
        }

        StoryDirector.Instance.SkipToStoreStage();
    }

    [MenuItem("Paralysis/Testing/Fill Shopping Cart (Play Mode)")]
    public static void FillCart()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Paralysis: enter Play Mode first.");
            return;
        }

        if (StoryDirector.Instance == null)
        {
            Debug.LogWarning("Paralysis: no StoryDirector is running.");
            return;
        }

        StoryDirector.Instance.FillCartForTesting();
    }

    private static int RequireManager<T>(string label) where T : Component
    {
        T found = Object.FindAnyObjectByType<T>();

        if (found != null)
            return 0;

        Debug.LogWarning(
            "Paralysis setup: no " + label + " found in the open scene. " +
            "Use Paralysis > Setup > Create Story Managers."
        );

        return 1;
    }

    private static string ReadString(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);

        return property != null ? property.stringValue : string.Empty;
    }

    private static int CheckId(
        Object owner,
        string fieldName,
        string expected,
        string label)
    {
        if (owner == null || string.IsNullOrEmpty(expected))
            return 0;

        SerializedObject serialized = new SerializedObject(owner);
        SerializedProperty property = serialized.FindProperty(fieldName);

        if (property == null || property.stringValue == expected)
            return 0;

        Debug.LogWarning(
            "Paralysis setup: " + label + " id '" + property.stringValue +
            "' does not match StoryDirector's '" + expected + "'.",
            owner
        );

        return 1;
    }
}

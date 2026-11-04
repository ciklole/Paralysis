using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class ObjectiveEntry
{
    [Tooltip("Stable id. Never change this when editing the visible text.")]
    public string id;

    public string text;
    public bool done;
}

/// <summary>
/// Keeps the authoritative objective list and shows the current one in the
/// top-left HUD. Other systems call SetObjective / CompleteObjective.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("HUD")]
    [SerializeField] private string headerText = "OBJECTIVE";

    [SerializeField] private TextStyle headerStyle = new TextStyle
    {
        fontSize = 20,
        color = new Color(1f, 0.84f, 0.42f),
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private TextStyle bodyStyle = new TextStyle
    {
        fontSize = 26,
        bold = false,
        alignment = TextAnchor.UpperLeft
    };

    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Vector2 screenPosition = new Vector2(40f, -40f);
    [SerializeField] private Vector2 panelSize = new Vector2(560f, 130f);

    [Tooltip("How long a finished objective stays on screen.")]
    [SerializeField] private float completedDisplayTime = 2f;

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with an ObjectiveView. Leave empty to load " +
        "Resources/Paralysis/ObjectiveCanvas.")]
    [SerializeField] private GameObject canvasPrefab;

    [Tooltip("Turn off to force the runtime-built fallback HUD.")]
    [SerializeField] private bool useAuthoredCanvas = true;

    private const string AuthoredCanvasPath = "Paralysis/ObjectiveCanvas";

    private readonly List<ObjectiveEntry> entries = new List<ObjectiveEntry>();

    public IReadOnlyList<ObjectiveEntry> Entries => entries;
    public event Action Changed;

    private ObjectiveView view;
    private RectTransform panel;
    private Text bodyText;
    private Color authoredBodyColor = Color.white;
    private float hideAt = -1f;

    public static ObjectiveManager Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("ObjectiveManager");
        return go.AddComponent<ObjectiveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy is deferred to the end of the frame. Deactivate first so
            // the duplicate cannot create UI, subscribe, or leave a modal/lock
            // behind during that frame.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Refresh();
    }

    /// <summary>
    /// Re-shows the newest unfinished objective. This keeps the HUD visible
    /// when a scene loads without changing the objective list.
    /// </summary>
    public void Refresh()
    {
        if (panel == null)
            return;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].done)
                continue;

            hideAt = -1f;
            Show(entries[i]);
            return;
        }
    }

    private void CreateUI()
    {
        if (TryCreateAuthoredUI())
            return;

        Canvas canvas = UIHelper.CreateCanvas("ObjectiveCanvas", 200, false);
        canvas.transform.SetParent(transform, false);

        panel = UIHelper.CreateRect(
            "ObjectivePanel",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            screenPosition,
            panelSize
        );

        UIHelper.AddPanel(panel, panelColor);
        UIHelper.AddMask(panel);

        RectTransform headerRect = UIHelper.CreateRect(
            "Header",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -14f),
            new Vector2(panelSize.x - 40f, 26f)
        );

        Text header = UIHelper.AddText(headerRect, headerStyle, headerText);
        header.alignment = TextAnchor.UpperLeft;

        RectTransform bodyRect = UIHelper.CreateRect(
            "Body",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -44f),
            new Vector2(panelSize.x - 40f, panelSize.y - 58f)
        );

        bodyText = UIHelper.AddText(bodyRect, bodyStyle, string.Empty);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Uses the editable ObjectiveCanvas prefab when one is available. The
    /// prefab is parented to this persistent manager so it is cleaned up with
    /// the manager.
    /// </summary>
    private bool TryCreateAuthoredUI()
    {
        UIHelper.EnsureEventSystem();

        GameObject prefab = canvasPrefab;

        if (prefab == null && useAuthoredCanvas)
            prefab = Resources.Load<GameObject>(AuthoredCanvasPath);

        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab, transform, false);
        instance.name = "ObjectiveCanvas";

        view = instance.GetComponentInChildren<ObjectiveView>(true);

        if (view == null || view.panel == null || view.bodyText == null)
        {
            Debug.LogWarning(
                "ObjectiveManager: the authored ObjectiveCanvas is missing " +
                "its panel or body text. Falling back to the runtime HUD.",
                this
            );

            view = null;
            Destroy(instance);
            return false;
        }

        panel = view.panel;
        bodyText = view.bodyText;

        if (view.applyStyles)
        {
            if (view.headerText != null)
                headerStyle.Apply(view.headerText);

            bodyStyle.Apply(bodyText);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        authoredBodyColor = bodyText.color;
        panel.gameObject.SetActive(false);
        return true;
    }

    public void SetObjective(string id, string text)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("ObjectiveManager: SetObjective needs a stable id.");
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("ObjectiveManager: objective '" + id + "' has no text.");
            return;
        }

        ObjectiveEntry entry = Find(id);

        if (entry == null)
        {
            entry = new ObjectiveEntry
            {
                id = id,
                text = text,
                done = false
            };

            entries.Add(entry);
        }
        else
        {
            entry.text = text;
            entry.done = false;
        }

        hideAt = -1f;
        Show(entry);
        Changed?.Invoke();
    }

    public bool CompleteObjective(string id)
    {
        ObjectiveEntry entry = Find(id);

        if (entry == null)
            return false;

        if (entry.done)
            return false;

        entry.done = true;
        Show(entry);

        hideAt = Time.unscaledTime + completedDisplayTime;
        Changed?.Invoke();

        return true;
    }

    public ObjectiveEntry Find(string id)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].id == id)
                return entries[i];
        }

        return null;
    }

    public bool IsDone(string id)
    {
        ObjectiveEntry entry = Find(id);
        return entry != null && entry.done;
    }

    public bool HasActive(string id)
    {
        ObjectiveEntry entry = Find(id);
        return entry != null && !entry.done;
    }

    public void ClearAll()
    {
        entries.Clear();
        hideAt = -1f;

        if (panel != null)
            panel.gameObject.SetActive(false);

        Changed?.Invoke();
    }

    private void Show(ObjectiveEntry entry)
    {
        if (panel == null || bodyText == null)
            return;

        if (view != null)
        {
            if (view.applyStyles)
                bodyStyle.Apply(bodyText);

            bodyText.color = entry.done
                ? view.completedColor
                : (view.applyStyles ? bodyStyle.color : authoredBodyColor);
        }
        else
        {
            bodyStyle.Apply(bodyText);

            bodyText.color = entry.done
                ? new Color(0.72f, 0.72f, 0.72f, 1f)
                : bodyStyle.color;
        }

        bodyText.text = (entry.done ? "[x] " : "- ") + entry.text;

        panel.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (hideAt < 0f)
            return;

        if (Time.unscaledTime < hideAt)
            return;

        hideAt = -1f;

        if (panel != null)
            panel.gameObject.SetActive(false);
    }
}

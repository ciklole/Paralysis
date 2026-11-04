using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One entry on the shopping list.</summary>
[Serializable]
public class ShoppingItem
{
    [Tooltip("Stable id. GroceryItem objects match this, so renaming the display name is safe.")]
    public string id = "item";

    public string displayName = "Item";

    [Min(1)]
    public int requiredQuantity = 1;

    [Tooltip("Runtime progress. Reset when a new session starts.")]
    public int collectedQuantity;

    public Sprite icon;

    [TextArea(1, 3)]
    public string description;

    public AudioClip pickupSound;

    [Tooltip("Lower numbers are listed first.")]
    public int displayOrder;

    public bool IsComplete => collectedQuantity >= requiredQuantity;
}

/// <summary>
/// Authoritative shopping list and collection progress. Item identity is the
/// stable id, never the display name.
/// </summary>
public class ShoppingCart : MonoBehaviour
{
    public static ShoppingCart Instance;

    [Header("Shopping List")]
    [Tooltip("Rename display names freely. Grocery items match these ids.")]
    [SerializeField]
    private List<ShoppingItem> items = new List<ShoppingItem>
    {
        new ShoppingItem
        {
            id = "bread",
            displayName = "Bread",
            requiredQuantity = 1,
            displayOrder = 0
        },
        new ShoppingItem
        {
            id = "milk",
            displayName = "Milk",
            requiredQuantity = 1,
            displayOrder = 1
        },
        new ShoppingItem
        {
            id = "eggs",
            displayName = "Eggs",
            requiredQuantity = 1,
            displayOrder = 2
        }
    };

    private bool completionAnnounced;

    public IReadOnlyList<ShoppingItem> Items => items;

    public event Action Changed;
    public event Action Completed;

    public static ShoppingCart Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("ShoppingCart");
        return go.AddComponent<ShoppingCart>();
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

        ResetProgress(false);
        ValidateIds();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ValidateIds()
    {
        if (items == null)
            return;

        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < items.Count; i++)
        {
            ShoppingItem item = items[i];

            if (item == null || string.IsNullOrEmpty(item.id))
            {
                Debug.LogWarning(
                    "ShoppingCart: a shopping list entry has no stable id.",
                    this
                );
                continue;
            }

            if (!seen.Add(item.id))
            {
                Debug.LogWarning(
                    "ShoppingCart: duplicate item id '" + item.id + "'.",
                    this
                );
            }
        }
    }

    public ShoppingItem Find(string id)
    {
        if (items == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].id == id)
                return items[i];
        }

        return null;
    }

    public bool IsRequired(string id)
    {
        ShoppingItem item = Find(id);

        return item != null && item.requiredQuantity > 0;
    }

    public int TotalRequired
    {
        get
        {
            if (items == null)
                return 0;

            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].requiredQuantity > 0)
                    total += items[i].requiredQuantity;
            }

            return total;
        }
    }

    public int TotalCollected
    {
        get
        {
            if (items == null)
                return 0;

            int total = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].requiredQuantity <= 0)
                    continue;

                total += Mathf.Min(
                    items[i].collectedQuantity,
                    items[i].requiredQuantity
                );
            }

            return total;
        }
    }

    public bool IsComplete
    {
        get
        {
            if (items == null)
                return false;

            bool hasRequirement = false;

            for (int i = 0; i < items.Count; i++)
            {
                ShoppingItem item = items[i];

                if (item == null || item.requiredQuantity <= 0)
                    continue;

                hasRequirement = true;

                if (item.collectedQuantity < item.requiredQuantity)
                    return false;
            }

            return hasRequirement;
        }
    }

    /// <summary>
    /// Records one valid pickup. Returns false when the id is unknown, the
    /// entry is already full, or the object has already been collected.
    /// </summary>
    public bool TryCollect(string id, out ShoppingItem item)
    {
        item = Find(id);

        if (item == null)
        {
            Debug.LogWarning(
                "ShoppingCart: no shopping list entry with id '" + id + "'.",
                this
            );
            return false;
        }

        if (item.collectedQuantity >= item.requiredQuantity)
            return false;

        item.collectedQuantity++;
        Changed?.Invoke();

        if (IsComplete && !completionAnnounced)
        {
            completionAnnounced = true;
            Completed?.Invoke();
        }

        return true;
    }

    public void ResetProgress(bool notify = true)
    {
        completionAnnounced = false;

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    items[i].collectedQuantity = 0;
            }
        }

        if (notify)
            Changed?.Invoke();
    }

    [ContextMenu("Fill With Default Test List")]
    private void FillWithDefaultTestList()
    {
        items = new List<ShoppingItem>
        {
            new ShoppingItem { id = "bread", displayName = "Bread", requiredQuantity = 1, displayOrder = 0 },
            new ShoppingItem { id = "milk", displayName = "Milk", requiredQuantity = 1, displayOrder = 1 },
            new ShoppingItem { id = "eggs", displayName = "Eggs", requiredQuantity = 1, displayOrder = 2 }
        };
    }
}

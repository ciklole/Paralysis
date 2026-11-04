using UnityEngine;

/// <summary>
/// Points DialogueSystem at the hand-authored dialogue canvas. The prefab is
/// optional: without it the original runtime-built dialogue box is used.
/// </summary>
[CreateAssetMenu(
    fileName = "DialogueSystemConfig",
    menuName = "Paralysis/Dialogue System Config")]
public class DialogueSystemConfig : ScriptableObject
{
    public const string ResourcePath = "Paralysis/DialogueSystemConfig";

    [Header("Authored Canvas")]
    [Tooltip(
        "Prefab with a DialogueView. Assign it to edit the dialogue box in " +
        "the Canvas/Prefab editor.")]
    public GameObject canvasPrefab;

    [Tooltip("Turn off to force the runtime-built fallback dialogue box.")]
    public bool useAuthoredCanvas = true;

    public static DialogueSystemConfig Load()
    {
        return Resources.Load<DialogueSystemConfig>(ResourcePath);
    }
}

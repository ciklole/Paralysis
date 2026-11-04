#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Small placement helpers for grocery pickups. The item itself is a normal
/// scene object, so moving it in the Scene view already changes its placement;
/// these buttons just make shelf placement quicker and safer.
/// </summary>
[CustomEditor(typeof(GroceryItem))]
public class GroceryItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GroceryItem item = (GroceryItem)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Placement Helpers", EditorStyles.boldLabel);

        if (string.IsNullOrWhiteSpace(item.ItemId))
        {
            EditorGUILayout.HelpBox(
                "Item Id is empty. It must match an id in the ShoppingCart list.",
                MessageType.Warning
            );
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap to surface below"))
                SnapToSurface(item);

            if (GUILayout.Button("Select interaction point"))
                SelectInteractionPoint(item);
        }

        EditorGUILayout.HelpBox(
            "Move the grocery's Transform to change where it sits. The " +
            "interaction point is optional; when empty the object's own " +
            "Transform is used.",
            MessageType.None
        );
    }

    private static void SnapToSurface(GroceryItem item)
    {
        if (item == null)
            return;

        Transform itemTransform = item.transform;
        Vector3 origin = itemTransform.position + Vector3.up * 0.5f;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            20f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float closest = float.MaxValue;
        Vector3 point = Vector3.zero;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;

            if (collider == null)
                continue;

            if (collider.transform == itemTransform ||
                collider.transform.IsChildOf(itemTransform))
            {
                continue;
            }

            if (hits[i].distance >= closest)
                continue;

            closest = hits[i].distance;
            point = hits[i].point;
            found = true;
        }

        if (!found)
        {
            EditorUtility.DisplayDialog(
                "Snap grocery",
                "No surface was found below '" + item.name + "'.",
                "OK"
            );

            return;
        }

        Undo.RecordObject(itemTransform, "Snap grocery to surface");
        itemTransform.position = point + Vector3.up * 0.02f;
        EditorUtility.SetDirty(itemTransform);
        SceneView.RepaintAll();
    }

    private static void SelectInteractionPoint(GroceryItem item)
    {
        if (item == null)
            return;

        if (item.InteractionPoint == null ||
            item.InteractionPoint == item.transform)
        {
            Selection.activeTransform = item.transform;
            return;
        }

        Selection.activeTransform = item.InteractionPoint;
        SceneView.RepaintAll();
    }
}
#endif

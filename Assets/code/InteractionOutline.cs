using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates a temporary white silhouette outline around the current
/// InteractableObject. It duplicates the target's renderers and draws them
/// with an inverted-hull shader, so it works in the built-in render pipeline
/// without post-processing or a second camera.
///
/// The component is added automatically to the PlayerInteractor object.
/// </summary>
public class InteractionOutline : MonoBehaviour
{
    [Tooltip("Optional material. Only its shader is used; a runtime copy is created.")]
    [SerializeField] private Material outlineMaterial;

    [Tooltip("Optional shader. Falls back to Resources/InteractionOutline.")]
    [SerializeField] private Shader outlineShader;

    private readonly List<GameObject> created = new List<GameObject>();
    private Transform currentRoot;
    private Material runtimeMaterial;

    private void Awake()
    {
        Shader shader = null;

        if (outlineMaterial != null)
            shader = outlineMaterial.shader;

        if (shader == null)
            shader = outlineShader;

        if (shader == null)
            shader = Resources.Load<Shader>("InteractionOutline");

        if (shader == null)
            shader = Shader.Find("Paralysis/InteractionOutline");

        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
            runtimeMaterial.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogWarning(
                "InteractionOutline: no outline shader found. Put " +
                "InteractionOutline.shader in Assets/Resources or assign " +
                "one on the component.",
                this
            );
        }
    }

    /// <summary>Starts outlining the given root. Safe to call every frame.</summary>
    public void Show(Transform root, Color color, float width)
    {
        if (root == null)
        {
            Hide();
            return;
        }

        if (currentRoot == root)
        {
            ApplyStyle(color, width);
            return;
        }

        Hide();

        currentRoot = root;
        ApplyStyle(color, width);

        if (runtimeMaterial == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer source = renderers[i];

            if (source == null ||
                !source.enabled ||
                !source.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (source is MeshRenderer)
                CreateMeshOutline(source);
            else if (source is SkinnedMeshRenderer)
                CreateSkinnedOutline((SkinnedMeshRenderer)source);
        }
    }

    /// <summary>Destroys every temporary outline object.</summary>
    public void Hide()
    {
        for (int i = 0; i < created.Count; i++)
        {
            if (created[i] != null)
                Destroy(created[i]);
        }

        created.Clear();
        currentRoot = null;
    }

    private void ApplyStyle(Color color, float width)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetColor("_Color", color);
        runtimeMaterial.SetFloat("_OutlineWidth", Mathf.Max(0.0001f, width));
    }

    private void CreateMeshOutline(Renderer source)
    {
        MeshFilter filter = source.GetComponent<MeshFilter>();

        if (filter == null || filter.sharedMesh == null)
            return;

        GameObject go = new GameObject(source.gameObject.name + "_Outline");
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);

        MeshFilter outlineFilter = go.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = filter.sharedMesh;

        MeshRenderer outlineRenderer = go.AddComponent<MeshRenderer>();
        outlineRenderer.sharedMaterial = runtimeMaterial;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.allowOcclusionWhenDynamic = false;

        created.Add(go);
    }

    private void CreateSkinnedOutline(SkinnedMeshRenderer source)
    {
        if (source.sharedMesh == null)
            return;

        GameObject go = new GameObject(source.gameObject.name + "_Outline");
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);

        SkinnedMeshRenderer outlineRenderer = go.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = source.sharedMesh;
        outlineRenderer.bones = source.bones;
        outlineRenderer.rootBone = source.rootBone;
        outlineRenderer.sharedMaterial = runtimeMaterial;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.updateWhenOffscreen = source.updateWhenOffscreen;
        outlineRenderer.localBounds = source.localBounds;

        created.Add(go);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Hide();

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}

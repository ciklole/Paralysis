using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated LOADING / LOADING. / LOADING.. / LOADING... label. The text object
/// is created once by the transition manager; this component only changes its
/// string, so the animation does not allocate UI objects every frame.
/// </summary>
public class LoadingIndicator : MonoBehaviour
{
    private static readonly string[] DotStates =
    {
        ".  ",
        ".. ",
        " . ",
        " ..",
        "  .",
        ". ."
    };

    [SerializeField] private Text label;
    [SerializeField] private string baseText = "LOADING";
    [SerializeField] private bool animateDots = true;
    [SerializeField] private float dotStepInterval = 0.25f;

    private float timer;
    private int index;

    public void Configure(
        Text text,
        string value,
        bool animate,
        float interval)
    {
        label = text;
        baseText = value;
        animateDots = animate;
        dotStepInterval = Mathf.Max(0.05f, interval);
        ResetAnimation();
    }

    public void ResetAnimation()
    {
        timer = 0f;
        index = 0;
        Apply();
    }

    public void SetBaseText(string value)
    {
        baseText = value;
        Apply();
    }

    private void Update()
    {
        if (label == null)
            return;

        if (!animateDots)
            return;

        timer += Time.unscaledDeltaTime;

        if (timer < dotStepInterval)
            return;

        timer = 0f;
        index = (index + 1) % DotStates.Length;
        Apply();
    }

    private void Apply()
    {
        if (label == null)
            return;

        label.text = animateDots
            ? baseText + DotStates[index]
            : baseText + "...";
    }

    private void OnDisable()
    {
        timer = 0f;
        index = 0;
    }
}

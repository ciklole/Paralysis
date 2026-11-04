using UnityEngine;

/// <summary>Small shared helpers for the runtime-built menus.</summary>
public static class UiAnimation
{
    /// <summary>
    /// Evaluates an inspector curve, falling back to a smooth ease when the
    /// curve has not been configured.
    /// </summary>
    public static float Evaluate(AnimationCurve curve, float t)
    {
        t = Mathf.Clamp01(t);

        if (curve == null || curve.length == 0)
            return Mathf.SmoothStep(0f, 1f, t);

        return curve.Evaluate(t);
    }

    /// <summary>Moves a value towards a target at a fixed duration.</summary>
    public static float MoveTowards(
        float current,
        float target,
        float duration,
        float deltaTime)
    {
        if (duration <= 0.0001f)
            return target;

        return Mathf.MoveTowards(
            current,
            target,
            deltaTime / duration
        );
    }
}

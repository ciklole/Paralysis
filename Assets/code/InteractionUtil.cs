using System.Collections.Generic;
using UnityEngine;

/// <summary>Small helpers shared by the interaction scripts.</summary>
public static class InteractionUtil
{
    public static Camera FindCamera()
    {
        Camera cam = Camera.main;

        if (cam == null)
            cam = Object.FindAnyObjectByType<Camera>();

        return cam;
    }

    public static FirstPersonController FindController()
    {
        // Prefer the controller that actually owns the active camera. In a
        // scene with more than one player-like object this makes sure we lock
        // the one the player is really using.
        Camera cam = FindCamera();

        if (cam != null)
        {
            FirstPersonController parent =
                cam.GetComponentInParent<FirstPersonController>();

            if (parent != null)
                return parent;
        }

        return Object.FindAnyObjectByType<FirstPersonController>();
    }

    public static Transform FindPlayer()
    {
        FirstPersonController controller = FindController();

        if (controller != null)
            return controller.transform;

        GameObject tagged = null;

        try
        {
            tagged = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            // The tag does not exist in this project; fall through.
        }

        return tagged != null ? tagged.transform : null;
    }

    /// <summary>
    /// True when the target is within range and the crosshair is actually
    /// pointing at it (so you cannot interact through walls).
    /// </summary>
    public static bool IsAimedAt(
        Camera cam,
        Transform target,
        float range,
        LayerMask blockingMask)
    {
        if (cam == null || target == null)
            return false;

        if (Vector3.Distance(cam.transform.position, target.position) > range)
            return false;

        // Objects without a collider cannot be hit by a ray, so range is enough.
        if (target.GetComponentInChildren<Collider>() == null)
            return true;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                range,
                blockingMask,
                QueryTriggerInteraction.Collide))
        {
            return false;
        }

        return hit.collider.transform == target ||
               hit.collider.transform.IsChildOf(target);
    }
}

/// <summary>
/// Locks the first-person controller while a system owns the screen.
/// Multiple systems can lock at once; movement only comes back when the last
/// one unlocks. The controller's original settings are restored, not blindly
/// set to true.
/// </summary>
public static class PlayerControl
{
    private static readonly HashSet<object> locks = new HashSet<object>();

    private static FirstPersonController controller;
    private static bool hasSavedState;
    private static bool savedPlayerCanMove;
    private static bool savedCameraCanMove;
    private static PlayerControlGuard guard;

    public static bool IsLocked => locks.Count > 0;

    public static void Lock(object owner)
    {
        if (owner == null)
            return;

        EnsureGuard();
        locks.Add(owner);
        Apply();
    }

    /// <summary>
    /// Locks a specific controller. Used by camera moves (inspection) so the
    /// lock cannot accidentally land on a different player in the scene.
    /// </summary>
    public static void Lock(object owner, FirstPersonController target)
    {
        if (owner == null)
            return;

        if (target != null && controller != target)
        {
            controller = target;
            hasSavedState = false;
        }

        EnsureGuard();
        Lock(owner);
    }

    public static void Unlock(object owner)
    {
        if (owner == null)
            return;

        locks.Remove(owner);
        Apply();
    }

    public static void ForceUnlockAll()
    {
        locks.Clear();
        Apply();
    }

    /// <summary>
    /// Re-applies the current lock. The guard calls this every frame so a
    /// late-spawned or externally reset controller still stays locked.
    /// </summary>
    public static void Enforce()
    {
        if (locks.Count == 0)
            return;

        Apply();
    }

    private static void EnsureGuard()
    {
        if (guard != null)
            return;

        GameObject go = new GameObject("PlayerControlGuard");
        Object.DontDestroyOnLoad(go);
        guard = go.AddComponent<PlayerControlGuard>();
    }

    private static void Apply()
    {
        // Keep an explicitly targeted controller while it is alive. Only look
        // for a replacement when it has been destroyed (scene change).
        FirstPersonController found = controller;

        if (found == null)
            found = InteractionUtil.FindController();

        // A different scene player took over: capture its own settings.
        if (found != controller)
        {
            controller = found;
            hasSavedState = false;
        }

        if (controller == null)
            return;

        if (!hasSavedState)
        {
            savedPlayerCanMove = controller.playerCanMove;
            savedCameraCanMove = controller.cameraCanMove;
            hasSavedState = true;
        }

        bool free = locks.Count == 0;

        if (free)
        {
            controller.playerCanMove = savedPlayerCanMove;
            controller.cameraCanMove = savedCameraCanMove;
            return;
        }

        controller.playerCanMove = false;
        controller.cameraCanMove = false;

        // Stop residual momentum so the player cannot keep sliding through a
        // conversation. Vertical velocity is left alone so falling/jumping
        // still settles naturally.
        Rigidbody body = controller.GetComponent<Rigidbody>();

        if (body != null && !body.isKinematic)
        {
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, velocity.y, 0f);
            body.angularVelocity = Vector3.zero;
        }
    }
}

/// <summary>
/// Hidden helper that keeps PlayerControl applied while any system owns the
/// screen. This makes the lock survive execution-order races and controllers
/// that are spawned a frame late.
/// </summary>
public class PlayerControlGuard : MonoBehaviour
{
    private void Update()
    {
        PlayerControl.Enforce();
    }
}

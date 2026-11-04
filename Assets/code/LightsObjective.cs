using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Objective 1: every configured light switch must be off.
///
/// StoryDirector activates it after the opening conversation. If no director
/// exists, it can activate and complete itself on Start.
/// </summary>
public class LightsObjective : MonoBehaviour
{
    [Header("Objective")]
    [Tooltip("Stable id. Safe to change the visible text.")]
    [SerializeField] private string objectiveId = "turn_off_lights";

    [SerializeField]
    private string objectiveText = "Turn off all the lights in the house";

    [Tooltip("Shown after completion when there is no StoryDirector.")]
    [SerializeField] private string nextObjectiveId = "go_to_car";

    [SerializeField] private string nextObjectiveText = "Go outside to the car";

    [Header("Required Lights")]
    [Tooltip("Only these switches count. Leave empty to auto-find.")]
    [SerializeField] private List<LightSwitch> requiredSwitches =
        new List<LightSwitch>();

    [SerializeField] private bool autoFindSwitches = true;

    [Header("Activation")]
    [Tooltip("Leave off when StoryDirector sequences the opening.")]
    [SerializeField] private bool activateOnStart;

    [Tooltip("Completes the objective itself when there is no StoryDirector.")]
    [SerializeField] private bool completeObjectiveItself = true;

    [Tooltip("Ignores lights that were already off at scene start until one turns on.")]
    [SerializeField] private bool requireAnyLightWasOn = true;

    [SerializeField] private float checkInterval = 0.25f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCompleted;

    private bool active;
    private bool completed;
    private bool everOn;
    private bool hasMissingReference;
    private float nextCheck;

    // Runtime list: explicit references plus auto-found switches. The
    // serialized list is never mutated at runtime.
    private readonly List<LightSwitch> resolved = new List<LightSwitch>();

    public bool IsActive => active;
    public bool IsComplete => completed;
    public event Action Completed;

    private void Start()
    {
        ResolveSwitches();

        if (!activateOnStart)
            return;

        Activate();

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.SetObjective(objectiveId, objectiveText);
    }

    /// <summary>Called by StoryDirector after the opening conversation.</summary>
    public void Activate()
    {
        if (active || completed)
            return;

        ResolveSwitches();

        if (resolved.Count == 0)
        {
            Debug.LogWarning(
                "LightsObjective: no LightSwitch references. The objective " +
                "will not complete.",
                this
            );
            return;
        }

        active = true;
        nextCheck = 0f;
    }

    private void ResolveSwitches()
    {
        hasMissingReference = false;
        resolved.Clear();

        if (requiredSwitches != null)
        {
            for (int i = 0; i < requiredSwitches.Count; i++)
            {
                LightSwitch lightSwitch = requiredSwitches[i];

                if (lightSwitch == null)
                {
                    hasMissingReference = true;
                    continue;
                }

                if (!resolved.Contains(lightSwitch))
                    resolved.Add(lightSwitch);
            }
        }

        if (autoFindSwitches && resolved.Count == 0)
        {
            LightSwitch[] found = FindObjectsByType<LightSwitch>();

            for (int i = 0; i < found.Length; i++)
                resolved.Add(found[i]);
        }

        if (hasMissingReference)
        {
            Debug.LogWarning(
                "LightsObjective: a required switch reference is missing. " +
                "The objective will not complete until it is assigned.",
                this
            );
        }
    }

    private void Update()
    {
        TrackEverOn();

        if (!active || completed)
            return;

        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + Mathf.Max(0.05f, checkInterval);

        CheckCompletion();
    }

    /// <summary>
    /// Remembers that a light was on at some point, even before the objective
    /// was activated. Prevents the objective from completing just because the
    /// scene happened to load with every light already off.
    /// </summary>
    private void TrackEverOn()
    {
        if (everOn || completed || resolved.Count == 0)
            return;

        for (int i = 0; i < resolved.Count; i++)
        {
            LightSwitch lightSwitch = resolved[i];

            if (lightSwitch != null && lightSwitch.IsOn)
            {
                everOn = true;
                return;
            }
        }
    }

    private void CheckCompletion()
    {
        if (hasMissingReference || resolved.Count == 0)
            return;

        bool anyOn = false;

        for (int i = 0; i < resolved.Count; i++)
        {
            LightSwitch lightSwitch = resolved[i];

            if (lightSwitch == null)
            {
                hasMissingReference = true;
                return;
            }

            if (lightSwitch.IsOn)
            {
                anyOn = true;
                break;
            }
        }

        if (anyOn)
        {
            everOn = true;
            return;
        }

        if (requireAnyLightWasOn && !everOn)
            return;

        Complete();
    }

    private void Complete()
    {
        if (completed)
            return;

        completed = true;
        active = false;

        if (StoryDirector.Instance != null)
        {
            StoryDirector.Instance.NotifyLightsObjectiveCompleted();
        }
        else if (completeObjectiveItself)
        {
            ObjectiveManager.Ensure().CompleteObjective(objectiveId);

            if (!string.IsNullOrEmpty(nextObjectiveText))
            {
                ObjectiveManager.Ensure().SetObjective(
                    nextObjectiveId,
                    nextObjectiveText
                );
            }
        }

        onCompleted?.Invoke();
        Completed?.Invoke();
    }

    public void ResetObjective()
    {
        active = false;
        completed = false;
        everOn = false;
        hasMissingReference = false;
        nextCheck = 0f;
        resolved.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (requiredSwitches == null)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < requiredSwitches.Count; i++)
        {
            if (requiredSwitches[i] != null)
                Gizmos.DrawLine(transform.position, requiredSwitches[i].transform.position);
        }
    }
}

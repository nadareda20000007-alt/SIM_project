using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceKind { Other, Water, Food }

/// <summary>
/// A food or water source in the world. Needs no collider: the player
/// checks distance against every registered source.
/// Attach to any object (barrel, crate, well, etc.).
/// </summary>
public class ResourceSource : MonoBehaviour
{
    /// <summary>All active sources in the scene.</summary>
    public static readonly List<ResourceSource> All = new List<ResourceSource>();

    [Header("Type")]
    [Tooltip("What this counts as for the level goal. 'Other' falls back to the object's Tag ('Water' / 'Food').")]
    [SerializeField] private ResourceKind kind = ResourceKind.Other;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float interactDuration = 5f;

    [Header("Reward")]
    [Tooltip("Seconds added back to the survival timer.")]
    [SerializeField] private float refillSeconds = 60f;

    [Header("Text")]
    [SerializeField] private string promptVerb = "drink";          // "Press [E] to drink"
    [SerializeField] private string progressLabel = "Drinking...";
    [Tooltip("Leave empty to use 'You found water!' / 'You found food!'.")]
    [SerializeField] private string collectedMessage = "";

    [Header("After Use")]
    [SerializeField] private bool singleUse = true;
    [Tooltip("Seconds before it can be used again (ignored if Single Use).")]
    [SerializeField] private float cooldown = 60f;
    [Tooltip("Optional object to hide once a single-use source is consumed.")]
    [SerializeField] private GameObject hideWhenConsumed;

    private float availableAt = 0f;

    public float InteractDuration => Mathf.Max(0.1f, interactDuration);
    public float RefillSeconds => refillSeconds;
    public string PromptVerb => promptVerb;
    public string ProgressLabel => progressLabel;
    public bool IsAvailable => Time.time >= availableAt;

    /// <summary>The resource type. Falls back to the object's tag if left on 'Other'.</summary>
    public ResourceKind Kind
    {
        get
        {
            if (kind != ResourceKind.Other) return kind;

            string t = gameObject.tag;
            if (t.Equals("Water", StringComparison.OrdinalIgnoreCase)) return ResourceKind.Water;
            if (t.Equals("Food", StringComparison.OrdinalIgnoreCase)) return ResourceKind.Food;
            return ResourceKind.Other;
        }
    }

    /// <summary>Message shown when the player finishes collecting this source.</summary>
    public string CollectedMessage
    {
        get
        {
            if (!string.IsNullOrEmpty(collectedMessage)) return collectedMessage;

            switch (Kind)
            {
                case ResourceKind.Water: return "You found water!";
                case ResourceKind.Food: return "You found food!";
                default: return "You found supplies!";
            }
        }
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    public bool IsInRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= interactDistance * interactDistance;
    }

    /// <summary>Called by the player when the interaction completes.</summary>
    public void Consume()
    {
        availableAt = singleUse ? float.PositiveInfinity : Time.time + cooldown;

        if (singleUse)
        {
            // Hide the assigned object, or this object itself if none is assigned
            GameObject toHide = hideWhenConsumed != null ? hideWhenConsumed : gameObject;
            toHide.SetActive(false);
        }
    }

    // Shows the interaction radius in the Scene view when selected
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
using System;
using UnityEngine;

/// <summary>
/// Lets the player use nearby ResourceSources.
/// Instant mode: pressing the interact key collects the item immediately.
/// Timed mode (Instant Interaction off): stand still for the full duration to collect.
/// Attach to the player (same object as 'man').
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Mode")]
    [Tooltip("ON = pressing the key collects immediately. OFF = hold still for the duration below.")]
    [SerializeField] private bool instantInteraction = true;

    [Header("Timed Mode Only")]
    [Tooltip("Seconds the player must stand still. 0 = use each source's own Interact Duration.")]
    [SerializeField] private float durationOverride = 3f;
    [SerializeField] private bool cancelOnMoveInput = true;
    [SerializeField] private float moveTolerance = 0.75f;
    [SerializeField] private bool logCancelReason = true;

    public KeyCode InteractKey => interactKey;

    /// <summary>Nearest usable source (null when none in range).</summary>
    public event Action<ResourceSource> TargetChanged;
    /// <summary>Timed mode: source, elapsed seconds, total seconds.</summary>
    public event Action<ResourceSource, float, float> Progress;
    /// <summary>Arguments: source, true if completed / false if cancelled.</summary>
    public event Action<ResourceSource, bool> InteractionFinished;

    private ResourceSource target;
    private bool interacting;
    private float elapsed;
    private float duration;
    private Vector3 startPosition;

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        // Nothing happens outside of gameplay
        if (gm == null || gm.State != GameState.Playing)
        {
            if (interacting) Cancel("the game is not in the Playing state");
            SetTarget(null);
            return;
        }

        if (interacting)
        {
            Tick(gm);
            return;
        }

        SetTarget(FindNearest());

        if (target != null && Input.GetKeyDown(interactKey))
        {
            if (instantInteraction)
            {
                Complete(gm);   // collected right now, no countdown
                return;
            }

            // Timed mode: start the countdown
            interacting = true;
            elapsed = 0f;
            duration = durationOverride > 0f ? durationOverride : target.InteractDuration;
            startPosition = transform.position;
            Progress?.Invoke(target, 0f, duration);
        }
    }

    // ---------- Timed mode ----------

    private void Tick(GameManager gm)
    {
        if (target == null || !target.IsAvailable)
        {
            Cancel("the source is no longer available");
            return;
        }

        if (!target.IsInRange(transform.position))
        {
            Cancel("you moved out of range of the source");
            return;
        }

        if (cancelOnMoveInput && IsMoveInputPressed())
        {
            Cancel("you pressed a movement key or jumped");
            return;
        }

        Vector3 drift = transform.position - startPosition;
        drift.y = 0f;
        if (drift.magnitude > moveTolerance)
        {
            Cancel("you drifted more than " + moveTolerance + "m from where you started");
            return;
        }

        elapsed += Time.deltaTime;
        Progress?.Invoke(target, elapsed, duration);

        if (elapsed >= duration)
        {
            Complete(gm);
        }
    }

    private void Cancel(string reason)
    {
        if (logCancelReason) Debug.Log("PlayerInteractor: interaction cancelled because " + reason + ".", this);

        ResourceSource cancelled = target;

        interacting = false;
        target = null;   // next frame the nearest source is re-detected and the prompt returns
        InteractionFinished?.Invoke(cancelled, false);
    }

    // ---------- Collecting ----------

    private void Complete(GameManager gm)
    {
        ResourceSource done = target;

        if (gm.Timer != null) gm.Timer.Replenish(done.RefillSeconds);   // does nothing if Allow Refill is off
        done.Consume();

        interacting = false;
        target = null;

        InteractionFinished?.Invoke(done, true);   // shows "You found ..." and counts toward the win
        TargetChanged?.Invoke(null);               // hides the old "Press [E]" prompt
    }

    // ---------- Helpers ----------

    private static bool IsMoveInputPressed()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f
            || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f
            || Input.GetButtonDown("Jump");
    }

    private ResourceSource FindNearest()
    {
        ResourceSource best = null;
        float bestSqr = float.MaxValue;

        foreach (ResourceSource s in ResourceSource.All)
        {
            if (!s.IsAvailable || !s.IsInRange(transform.position)) continue;

            float d = (s.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = s;
            }
        }
        return best;
    }

    private void SetTarget(ResourceSource newTarget)
    {
        if (newTarget == target) return;
        target = newTarget;
        TargetChanged?.Invoke(target);
    }
}
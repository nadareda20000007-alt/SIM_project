using System.Collections;
using UnityEngine;

public enum FallDirection { Forward, Backward, Left, Right }

/// <summary>
/// Plays the death when GameManager enters the Dying state:
/// lands the player if he is in the air, plays a death animation if one is set up,
/// and/or lays the body down on the ground by script. Then reports back for Game Over.
/// Attach to the Survivor (same object as 'man' and the CharacterController).
/// Requires the pivot (origin) of the Survivor to be at his feet.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Death Animation (optional)")]
    [SerializeField] private bool useDeathAnimation = true;
    [Tooltip("Name of the Trigger parameter that starts your death clip.")]
    [SerializeField] private string deathTrigger = "Die";
    [Tooltip("How long to wait for the clip before showing Game Over (roughly the clip length).")]
    [SerializeField] private float deathAnimationSeconds = 3f;

    [Header("Scripted Fall")]
    [Tooltip("Used automatically when there is no death animation. Tick to also use it together with one.")]
    [SerializeField] private bool alsoDoScriptedFall = false;
    [SerializeField] private FallDirection fallDirection = FallDirection.Forward;
    [SerializeField] private float fallDuration = 1.2f;
    [Tooltip("Raises the body slightly so it doesn't sink into the floor when lying down.")]
    [SerializeField] private float lieHeight = 0.15f;

    [Header("Timing")]
    [Tooltip("How long the body lies still before the Game Over panel appears.")]
    [SerializeField] private float holdAfterFall = 1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private bool sequenceRunning;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += OnStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Dying && !sequenceRunning)
        {
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        sequenceRunning = true;
        float startTime = Time.time;

        // 1. If he died in mid-air, let him land first (safety limit of 3 seconds)
        float verticalVelocity = 0f;
        float airTime = 0f;
        while (!controller.isGrounded && airTime < 3f)
        {
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
            airTime += Time.deltaTime;
            yield return null;
        }

        // 2. Death animation, if there is one
        bool animationStarted = TryStartDeathAnimation();

        // 3. Scripted fall: when there is no animation, or when you asked for both
        if (!animationStarted || alsoDoScriptedFall)
        {
            yield return StartCoroutine(ScriptedFall());
        }

        // 4. Wait for the clip to finish if it is still playing
        if (animationStarted)
        {
            float remaining = deathAnimationSeconds - (Time.time - startTime);
            if (remaining > 0f) yield return new WaitForSeconds(remaining);
        }

        // 5. Hold the final pose, then show Game Over
        yield return new WaitForSeconds(holdAfterFall);

        if (GameManager.Instance != null) GameManager.Instance.FinishDeath();
        sequenceRunning = false;
    }

    private bool TryStartDeathAnimation()
    {
        if (!useDeathAnimation || animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == deathTrigger && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(deathTrigger);
                return true;
            }
        }

        Debug.Log("PlayerDeath: no Trigger parameter named '" + deathTrigger + "' found, using the scripted fall.", this);
        return false;
    }

    private IEnumerator ScriptedFall()
    {
        // The capsule stays upright when the transform rotates, so switch it off while the body lies down
        controller.enabled = false;

        Vector3 axis;
        float angle;
        switch (fallDirection)
        {
            case FallDirection.Backward: axis = Vector3.right;   angle = -90f; break;
            case FallDirection.Left:     axis = Vector3.forward; angle = 90f;  break;
            case FallDirection.Right:    axis = Vector3.forward; angle = -90f; break;
            default:                     axis = Vector3.right;   angle = 90f;  break;   // Forward
        }

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.AngleAxis(angle, axis);   // local axis
        Vector3 startPosition = transform.position;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fallDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;   // starts slow, speeds up like a real fall

            transform.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
            transform.position = startPosition + Vector3.up * (lieHeight * eased);
            yield return null;
        }

        transform.rotation = endRotation;
        transform.position = startPosition + Vector3.up * lieHeight;
    }
}
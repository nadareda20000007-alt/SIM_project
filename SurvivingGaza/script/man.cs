using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class man : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    // Every parameter found on the Animator Controller (name -> type)
    private readonly Dictionary<string, AnimatorControllerParameterType> animParams =
        new Dictionary<string, AnimatorControllerParameterType>();

    [Tooltip("Run speed.")]
    public float speed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Walk / Run")]
    public float walkSpeed = 2.5f;
    [Tooltip("ON = run by default, hold Left Shift to walk. OFF = walk by default, hold Left Shift to run.")]
    public bool runByDefault = true;
    [Tooltip("How fast the speed changes between walk and run (units per second).")]
    public float speedChangeRate = 10f;

    [Header("Input Feel")]
    [Tooltip("ON = crisp keyboard input with its own short smoothing. OFF = Unity's default (floatier) axes.")]
    public bool useRawInput = true;
    [Tooltip("How fast movement ramps up and down (units per second). Higher = snappier.")]
    public float acceleration = 12f;

    [Header("Jump Feel")]
    [Tooltip("Can still jump this long after leaving the ground.")]
    public float coyoteTime = 0.15f;
    [Tooltip("A Space press is remembered this long, so early presses still jump.")]
    public float jumpBuffer = 0.15f;

    private Vector2 smoothedInput;
    private float currentTopSpeed;
    private float verticalVelocity;
    private float coyoteCounter;
    private float bufferCounter;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentTopSpeed = runByDefault ? speed : walkSpeed;

        // The Animator is on this object or on the model, a child of it
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("man: no Animator found on this object or its children, so the legs cannot animate.", this);
            return;
        }

        animator.applyRootMotion = false;                          // root motion would fight the CharacterController
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;  // never skip updating the bones
        animator.speed = 1f;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            animParams[p.name] = p.type;
        }

        Diagnose();
    }

    void Update()
    {
        // Frozen until Continue is clicked, and again when the level ends
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            smoothedInput = Vector2.zero;
            SetFloat("Speed", 0f);
            SetFloat("MotionSpeed", 1f);
            return;
        }

        // 1. Input (clamped so diagonals are not faster than straight lines)
        Vector2 rawInput = useRawInput
            ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        rawInput = Vector2.ClampMagnitude(rawInput, 1f);

        smoothedInput = useRawInput
            ? Vector2.MoveTowards(smoothedInput, rawInput, acceleration * Time.deltaTime)
            : rawInput;

        // 2. Walk or run (speed blends smoothly between the two)
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool running = runByDefault ? !shiftHeld : shiftHeld;
        float targetTopSpeed = running ? speed : walkSpeed;
        currentTopSpeed = Mathf.MoveTowards(currentTopSpeed, targetTopSpeed, speedChangeRate * Time.deltaTime);

        // 3. Direction in 3D: X and Z, relative to where the player (and camera) faces
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 move = (forward * smoothedInput.y + right * smoothedInput.x) * currentTopSpeed;
        float animSpeed = smoothedInput.magnitude * currentTopSpeed;

        // 4. Jump timers
        if (controller.isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")) bufferCounter = jumpBuffer;
        else bufferCounter -= Time.deltaTime;

        // 5. Jump / ground / gravity
        bool jumpedThisFrame = false;

        if (coyoteCounter > 0f && bufferCounter > 0f)
        {
            // Jump!
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            bufferCounter = 0f;
            coyoteCounter = 0f;
            jumpedThisFrame = true;
        }
        else if (controller.isGrounded && verticalVelocity < 0f)
        {
            // Keep the character pressed onto the ground
            verticalVelocity = -2f;
        }
        else
        {
            // In the air: apply gravity
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 6. Animator (only parameters that exist are set)
        SetFloat("Speed", animSpeed);
        SetFloat("MotionSpeed", 1f);               // Starter Assets multiplies playback by this; 0 freezes the legs
        SetBool("Grounded", coyoteCounter > 0f);

        if (jumpedThisFrame)
        {
            SetBool("Jump", true);
            SetBool("FreeFall", false);
        }
        else if (controller.isGrounded)
        {
            SetBool("Jump", false);
            SetBool("FreeFall", false);
        }
        else if (coyoteCounter <= 0f && verticalVelocity < 0f)
        {
            SetBool("FreeFall", true);
        }

        // 7. Move
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        // Hitting a ceiling while jumping stops the upward motion
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    // ---------- Animator helpers ----------

    private bool Has(string name, AnimatorControllerParameterType type)
    {
        AnimatorControllerParameterType found;
        return animParams.TryGetValue(name, out found) && found == type;
    }

    private void SetFloat(string name, float value)
    {
        if (animator != null && Has(name, AnimatorControllerParameterType.Float)) animator.SetFloat(name, value);
    }

    private void SetBool(string name, bool value)
    {
        if (animator != null && Has(name, AnimatorControllerParameterType.Bool)) animator.SetBool(name, value);
    }

    /// <summary>Prints what is wrong with the Animator setup (shown once, when Play starts).</summary>
    private void Diagnose()
    {
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("man: the Animator has no Controller assigned, so nothing can play.", this);
        }

        if (animator.avatar == null || !animator.avatar.isValid)
        {
            Debug.LogWarning("man: the Animator has no valid Avatar. Humanoid clips cannot drive the legs.", this);
        }
        else if (!animator.isHuman)
        {
            Debug.LogWarning("man: the Avatar is not Humanoid. Make sure your clips match this rig.", this);
        }

        if (!Has("Speed", AnimatorControllerParameterType.Float))
            Debug.LogWarning("man: the Controller has no FLOAT parameter named 'Speed' (check spelling and capital S).", this);
        if (!Has("Grounded", AnimatorControllerParameterType.Bool))
            Debug.LogWarning("man: the Controller has no BOOL parameter named 'Grounded'.", this);

        string list = "";
        foreach (KeyValuePair<string, AnimatorControllerParameterType> kv in animParams)
        {
            list += kv.Key + " (" + kv.Value + "), ";
        }
        Debug.Log("man: Animator parameters found: " + (list.Length > 0 ? list : "none"), this);
    }
}
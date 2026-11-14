using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class man : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private bool hasSpeedParam;
    private bool hasGroundedParam;

    float inputV;
    float inputH;

    public float speed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Jump Feel")]
    [Tooltip("Can still jump this long after leaving the ground.")]
    public float coyoteTime = 0.15f;
    [Tooltip("A Space press is remembered this long, so early presses still jump.")]
    public float jumpBuffer = 0.15f;

    private float verticalVelocity;
    private float coyoteCounter;
    private float bufferCounter;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // The Animator is usually on the model, a child of this object
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            // Root motion would fight the CharacterController
            animator.applyRootMotion = false;

            // Only set parameters that exist, so the Console doesn't fill with warnings
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "Speed") hasSpeedParam = true;
                if (p.name == "Grounded") hasGroundedParam = true;
            }
        }
    }

    void Update()
    {
        // Freeze player movement and set animator speed to 0 if the game isn't actively playing
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            if (animator != null && hasSpeedParam)
            {
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        // Get movement inputs (W/S, A/D)
        inputV = Input.GetAxis("Vertical");
        inputH = Input.GetAxis("Horizontal");

        // 1. Movement speed for the Animator
        float moveMagnitude = new Vector2(inputH, inputV).magnitude;
        float currentAnimationSpeed = moveMagnitude * speed;

        // 2. Direction for movement
        Vector3 move = (transform.forward * inputV) + (transform.right * inputH);
        move *= speed;

        // 3. Jump timers
        if (controller.isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump")) bufferCounter = jumpBuffer;
        else bufferCounter -= Time.deltaTime;

        // 4. Jump / ground / gravity
        if (coyoteCounter > 0f && bufferCounter > 0f)
        {
            // Jump!
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            bufferCounter = 0f;
            coyoteCounter = 0f;
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

        // Send parameters to the Animator
        if (animator != null)
        {
            if (hasSpeedParam) animator.SetFloat("Speed", currentAnimationSpeed);
            if (hasGroundedParam) animator.SetBool("Grounded", coyoteCounter > 0f);
        }

        // Apply vertical velocity and move
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }
}
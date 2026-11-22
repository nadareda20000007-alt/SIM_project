using UnityEngine;

/// <summary>
/// Third-person camera with mouse-look. Works with a visible, unlocked cursor.
/// Attach to the Main Camera. The cursor is controlled by GameManager.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player. If empty, the script finds the object with the 'man' script.")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private float distance = 5f;       // how far behind the player
    [SerializeField] private float lookHeight = 1.5f;   // point on the player to orbit around
    [SerializeField] private float deathLookHeight = 0.4f;   // focus point once the player is dead
    [SerializeField] private float followSmoothness = 10f;

    [Header("Mouse Look")]
    [SerializeField] private bool turnWithMouse = true;
    [SerializeField] private float mouseSensitivity = 3f;   // left / right (turns the player)
    [SerializeField] private bool allowVerticalLook = true;
    [SerializeField] private float verticalSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float startPitch = 15f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Keyboard Turn (backup when the pointer hits the screen edge)")]
    [Tooltip("Degrees per second using Q (left) and E (right). 0 = off.")]
    [SerializeField] private float keyTurnSpeed = 90f;

    private float pitch;
    private float yaw;
    private float currentLookHeight;

    private void Start()
    {
        if (target == null)
        {
            man m = FindObjectOfType<man>();
            if (m != null) target = m.transform;
        }

        if (target == null)
        {
            Debug.LogError("FollowCamera: no player found. Assign Target in the Inspector.", this);
            enabled = false;
            return;
        }

        if (GetComponent("CinemachineBrain") != null)
        {
            Debug.LogWarning("FollowCamera: a CinemachineBrain is on this camera and will override it. Remove it.", this);
        }

        pitch = startPitch;
        yaw = target.eulerAngles.y;
        currentLookHeight = lookHeight;
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        bool playing = gm == null || gm.State == GameState.Playing;
        bool dead = gm != null && (gm.State == GameState.Dying || gm.State == GameState.GameOver);

        // Look controls only work after Continue has been clicked
        if (playing)
        {
            if (turnWithMouse)
            {
                target.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);

                if (allowVerticalLook)
                {
                    float my = Input.GetAxis("Mouse Y") * verticalSensitivity * (invertY ? -1f : 1f);
                    pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);
                }
            }

            if (keyTurnSpeed > 0f)
            {
                float key = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
                if (key != 0f) target.Rotate(0f, key * keyTurnSpeed * Time.deltaTime, 0f);
            }

            // Only read the player's facing while playing (it is unreliable when he lies down)
            yaw = target.eulerAngles.y;
        }

        // On death the focus point drops from head height down to the body
        float wantedHeight = dead ? deathLookHeight : lookHeight;
        currentLookHeight = Mathf.MoveTowards(currentLookHeight, wantedHeight, 2f * Time.unscaledDeltaTime);

        // Orbit position: behind the player, rotated by yaw and pitch
        Vector3 pivot = target.position + Vector3.up * currentLookHeight;
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = pivot + orbit * new Vector3(0f, 0f, -distance);

        // Smoothly move there (frame-rate independent)
        float t = 1f - Mathf.Exp(-followSmoothness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);

        // Always look at the player
        transform.LookAt(pivot);
    }
}
using UnityEngine;

/// <summary>
/// Third-person camera that follows the player from behind.
/// Attach to the Main Camera.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player. If empty, the script finds the object with the 'man' script.")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private float distance = 5f;      // how far behind the player
    [SerializeField] private float height = 2.5f;      // how high above the player
    [SerializeField] private float lookHeight = 1.5f;  // point on the player to look at
    [SerializeField] private float followSmoothness = 10f;

    [Header("Mouse Turning")]
    [Tooltip("Turn the player (and camera) left/right with the mouse.")]
    [SerializeField] private bool turnWithMouse = true;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private bool lockCursor = true;

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

        if (turnWithMouse && lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        // Rotate the player with the mouse (only while the game is running)
        if (turnWithMouse && Time.timeScale > 0f)
        {
            target.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
        }

        // Desired spot: behind and above the player
        Vector3 desiredPos = target.position
                           - target.forward * distance
                           + Vector3.up * height;

        // Smoothly move there (frame-rate independent)
        float t = 1f - Mathf.Exp(-followSmoothness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);

        // Always look at the player
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
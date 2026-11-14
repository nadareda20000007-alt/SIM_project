using UnityEngine;

/// <summary>
/// Game Over when the player falls off the map.
/// Measures from the height where the player first lands, so it works at any map height.
/// Attach to the player (same object as 'man').
/// </summary>
public class FallDetector : MonoBehaviour
{
    [Tooltip("Game Over when the player is this many meters below their starting ground level.")]
    [SerializeField] private float fallDistance = 15f;

    [Tooltip("Tick to use a fixed world height instead (useful if the player spawns high in the air).")]
    [SerializeField] private bool useAbsoluteHeight = false;
    [SerializeField] private float absoluteFallY = -10f;

    [SerializeField] private string message = "You fell off the map.";

    private CharacterController controller;
    private float limitY;
    private bool limitSet;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (useAbsoluteHeight)
        {
            limitY = absoluteFallY;
            limitSet = true;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        // Wait until the player first touches the ground, then set the limit below that
        if (!limitSet)
        {
            if (controller == null || controller.isGrounded)
            {
                limitY = transform.position.y - fallDistance;
                limitSet = true;
            }
            return;
        }

        if (transform.position.y < limitY)
        {
            gm.GameOver(message);
        }
    }
}
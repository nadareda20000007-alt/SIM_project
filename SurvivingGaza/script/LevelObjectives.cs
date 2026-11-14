using UnityEngine;

/// <summary>
/// Level 1 goal: finish BOTH a water and a food interaction, then the level is won.
/// Attach to the GameManager object.
/// </summary>
public class LevelObjectives : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;   // auto-found if empty
    [SerializeField] private string winMessage = "You have successfully survived level 1!";

    public bool GotWater { get; private set; }
    public bool GotFood { get; private set; }

    private void Start()
    {
        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();

        if (interactor == null)
        {
            Debug.LogError("LevelObjectives: no PlayerInteractor found in the scene.", this);
            enabled = false;
            return;
        }

        interactor.InteractionFinished += OnInteractionFinished;
    }

    private void OnDestroy()
    {
        if (interactor != null) interactor.InteractionFinished -= OnInteractionFinished;
    }

    private void OnInteractionFinished(ResourceSource source, bool completed)
    {
        if (!completed || source == null) return;

        if (source.Kind == ResourceKind.Water) GotWater = true;
        if (source.Kind == ResourceKind.Food) GotFood = true;

        if (GotWater && GotFood && GameManager.Instance != null)
        {
            GameManager.Instance.WinGame(winMessage);   // freezes the game and shows the message
        }
    }
}
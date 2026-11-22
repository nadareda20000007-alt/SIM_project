using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows and hides the Canvas panels based on the GameState and updates the HUD.
/// Contains no game logic and does not touch the cursor (GameManager does that).
/// Keep it on an object that is always active.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private Button continueButton;

    [Header("HUD")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Slider survivalSlider;
    [SerializeField] private TMP_Text timerText;

    [Header("Messages")]
    [SerializeField] private PlayerInteractor interactor;   // auto-found if empty
    [SerializeField] private TMP_Text promptText;           // "Press [E] to eat"
    [SerializeField] private TMP_Text messageText;          // "You found food!"
    [SerializeField] private float messageDisplayTime = 3f;

    [Header("Game Over / Win")]
    [SerializeField] private GameObject gameOverPanel;      // also used for the Win state
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button restartButton;

    private Coroutine messageRoutine;

    private void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("UIManager: no GameManager object in the scene.", this);
            enabled = false;
            return;
        }

        CheckSlots();

        if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();

        if (continueButton != null) continueButton.onClick.AddListener(gm.BeginPlaying);
        if (restartButton != null) restartButton.onClick.AddListener(gm.Restart);

        gm.StateChanged += OnStateChanged;
        if (gm.Timer != null) gm.Timer.Changed += OnTimerChanged;

        if (interactor != null)
        {
            interactor.TargetChanged += OnTargetChanged;
            interactor.InteractionFinished += OnInteractionFinished;
        }

        if (gm.Timer != null) OnTimerChanged(gm.Timer.Remaining, gm.Timer.MaxTime);
        OnStateChanged(gm.State);
    }

    // ---------- Game state ----------

    private void OnStateChanged(GameState state)
    {
        bool ended = state == GameState.GameOver || state == GameState.Win;

        Show(introPanel, state == GameState.Intro);
        Show(hudPanel, state == GameState.Playing || ended);
        Show(gameOverPanel, ended);

        if (ended)
        {
            // Draw the end panel above everything else
            if (gameOverPanel != null) gameOverPanel.transform.SetAsLastSibling();

            // Switch these on in case they were left inactive in the scene
            Show(gameOverText, true);
            Show(restartButton, true);

            if (gameOverText != null) gameOverText.text = GameManager.Instance.GameOverMessage;
        }

        if (state != GameState.Playing)
        {
            Show(promptText, false);
            HideMessage();
        }
    }

    // ---------- Timer ----------

    private void OnTimerChanged(float remaining, float max)
    {
        if (survivalSlider != null) survivalSlider.value = remaining / max;

        if (timerText != null)
        {
            int total = Mathf.CeilToInt(remaining);
            timerText.text = $"{total / 60}:{total % 60:00}";
        }
    }

    // ---------- Prompt and pickup messages ----------

    private void OnTargetChanged(ResourceSource source)
    {
        if (source == null)
        {
            Show(promptText, false);
            return;
        }

        if (promptText != null && interactor != null)
        {
            promptText.text = $"Press [{interactor.InteractKey}] to {source.PromptVerb}";
        }
        Show(promptText, true);
    }

    private void OnInteractionFinished(ResourceSource source, bool completed)
    {
        if (completed && source != null)
        {
            ShowMessage(source.CollectedMessage);
        }
    }

    /// <summary>Shows a message for a few seconds (only while playing).</summary>
    public void ShowMessage(string message)
    {
        if (messageText == null) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(messageDisplayTime);

        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    private void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }
        Show(messageText, false);
    }

    // ---------- Helpers ----------

    private static void Show(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    private static void Show(Component c, bool on)
    {
        if (c != null) c.gameObject.SetActive(on);
    }

    /// <summary>Warns in the Console about any slot left empty.</summary>
    private void CheckSlots()
    {
        Check(introPanel, "Intro Panel");
        Check(continueButton, "Continue Button");
        Check(hudPanel, "Hud Panel");
        Check(survivalSlider, "Survival Slider");
        Check(promptText, "Prompt Text");
        Check(messageText, "Message Text");
        Check(gameOverPanel, "Game Over Panel");
        Check(gameOverText, "Game Over Text");
        Check(restartButton, "Restart Button");
    }

    private void Check(UnityEngine.Object slot, string label)
    {
        if (slot == null) Debug.LogWarning($"UIManager: the '{label}' slot is empty.", this);
    }
}
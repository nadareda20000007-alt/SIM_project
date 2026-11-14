using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows and hides all panels based on the GameState, and updates the HUD.
/// Contains no game logic. Keep it on an object that is always active.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;   // also used for the Win state

    [Header("Start / Intro")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text introText;
    [Tooltip("Leave empty to keep the text you typed in the Intro Text object.")]
    [TextArea(2, 4)]
    [SerializeField] private string introMessage = "";
    [SerializeField] private Button continueButton;
    [Tooltip("Auto-continue after this many seconds. 0 = wait for the Continue button.")]
    [SerializeField] private float introDuration = 0f;
    [SerializeField] private bool dismissOnAnyInput = false;
    [SerializeField] private float introMinTime = 0.5f;

    [Header("Survival HUD")]
    [SerializeField] private Slider survivalSlider;
    [SerializeField] private TMP_Text timerText;           // optional

    [Header("Interaction")]
    [SerializeField] private PlayerInteractor interactor;  // auto-found if empty
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Slider interactionSlider;
    [SerializeField] private TMP_Text interactionText;

    [Header("Pickup Message")]
    [Tooltip("Shows 'You found water!' etc. Create a separate TMP text inside HUDPanel.")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDisplayTime = 3f;

    [Header("Game Over / Win")]
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button restartButton;

    private float introStartTime;
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

        if (startButton != null) startButton.onClick.AddListener(gm.StartIntro);
        if (continueButton != null) continueButton.onClick.AddListener(gm.BeginPlaying);
        if (restartButton != null) restartButton.onClick.AddListener(gm.Restart);

        gm.StateChanged += OnStateChanged;
        if (gm.Timer != null) gm.Timer.Changed += OnTimerChanged;

        if (interactor != null)
        {
            interactor.TargetChanged += OnTargetChanged;
            interactor.Progress += OnProgress;
            interactor.InteractionFinished += OnInteractionFinished;
        }

        if (introText != null && !string.IsNullOrEmpty(introMessage)) introText.text = introMessage;

        if (gm.Timer != null) OnTimerChanged(gm.Timer.Remaining, gm.Timer.MaxTime);
        OnStateChanged(gm.State);
    }

    private void OnDestroy()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.StateChanged -= OnStateChanged;
            if (gm.Timer != null) gm.Timer.Changed -= OnTimerChanged;
        }

        if (interactor != null)
        {
            interactor.TargetChanged -= OnTargetChanged;
            interactor.Progress -= OnProgress;
            interactor.InteractionFinished -= OnInteractionFinished;
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Intro) return;

        float t = Time.unscaledTime - introStartTime;
        bool timeUp = introDuration > 0f && t >= introDuration;
        bool pressed = dismissOnAnyInput && t >= introMinTime &&
                       (Input.anyKeyDown || Input.GetMouseButtonDown(0));

        if (timeUp || pressed) gm.BeginPlaying();
    }

    // ---------- Game state ----------

    private void OnStateChanged(GameState state)
    {
        bool ended = state == GameState.GameOver || state == GameState.Win;

        Show(startPanel, state == GameState.Menu);
        Show(introPanel, state == GameState.Intro);
        Show(hudPanel, state == GameState.Playing || ended);
        Show(gameOverPanel, ended);

        if (state == GameState.Intro) introStartTime = Time.unscaledTime;

        if (ended)
        {
            // Draw the end panel above the HUD
            if (gameOverPanel != null) gameOverPanel.transform.SetAsLastSibling();

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
                gameOverText.text = GameManager.Instance.GameOverMessage;
            }

            if (restartButton != null) restartButton.gameObject.SetActive(true);
        }

        if (state != GameState.Playing)
        {
            Show(promptText, false);
            Show(interactionPanel, false);
            HideMessage();
        }
    }

    // ---------- Survival slider ----------

    private void OnTimerChanged(float remaining, float max)
    {
        if (survivalSlider != null) survivalSlider.value = remaining / max;

        if (timerText != null)
        {
            int total = Mathf.CeilToInt(remaining);
            timerText.text = $"{total / 60}:{total % 60:00}";
        }
    }

    // ---------- Interaction ----------

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

    private void OnProgress(ResourceSource source, float elapsed, float total)
    {
        Show(promptText, false);

        if (interactionPanel != null)
        {
            if (!interactionPanel.activeSelf)
            {
                interactionPanel.SetActive(true);
                interactionPanel.transform.SetAsLastSibling();   // draw above the other HUD items
                Debug.Log("UIManager: showing the interaction panel.", this);
            }
        }

        // Make sure the children are switched on too
        Show(interactionSlider, true);
        Show(interactionText, true);

        if (interactionSlider != null) interactionSlider.value = elapsed / total;

        if (interactionText != null)
        {
            string label = string.IsNullOrEmpty(source.ProgressLabel) ? "Collecting..." : source.ProgressLabel;
            interactionText.text = $"{label} {Mathf.Max(0f, total - elapsed):0.0}s";
        }
    }

    private void OnInteractionFinished(ResourceSource source, bool completed)
    {
        Show(interactionPanel, false);

        if (source == null) return;

        if (completed)
        {
            ShowMessage(source.CollectedMessage);
        }
        else
        {
            ShowMessage($"Stay still to {source.PromptVerb}!");
        }
    }
    // ---------- Pickup message ----------

    public void ShowMessage(string message)
    {
        // Don't show pickup messages over the win / game over screen
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

    public void HideMessage()
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

    private void CheckSlots()
    {
        Check(introPanel, "Intro Panel");
        Check(hudPanel, "Hud Panel");
        Check(gameOverPanel, "Game Over Panel");
        Check(introText, "Intro Text");
        Check(survivalSlider, "Survival Slider");
        Check(promptText, "Prompt Text");
        Check(interactionPanel, "Interaction Panel");
        Check(interactionSlider, "Interaction Slider");
        Check(interactionText, "Interaction Text");
        Check(messageText, "Message Text");
        Check(gameOverText, "Game Over Text");
        Check(restartButton, "Restart Button");
    }

    private void Check(UnityEngine.Object slot, string label)
    {
        if (slot == null) Debug.LogWarning($"UIManager: the '{label}' slot is empty.", this);
    }
}
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Intro, Playing, Win, GameOver }

/// <summary>
/// Single owner of the game state, Time.timeScale and the cursor.
/// Put this on an empty "GameManager" object together with SurvivalTimer.
/// Flow: Intro -> (Continue) -> Playing -> Win / GameOver
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SurvivalTimer timer;
    [SerializeField] private bool lockCursorWhilePlaying = true;
    [Tooltip("Tick this to show the Start panel first (Menu -> Intro -> Playing). Leave off to open on the Intro.")]
    [SerializeField] private bool showStartMenu = false;

    public GameState State { get; private set; } = GameState.Intro;
    public string GameOverMessage { get; private set; }
    public SurvivalTimer Timer => timer;

    /// <summary>Fired whenever the state changes.</summary>
    public event Action<GameState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (timer == null) timer = GetComponent<SurvivalTimer>();

        // Frozen with the cursor visible until the player clicks
        ApplyState(showStartMenu ? GameState.Menu : GameState.Intro);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Public API ----------

    /// <summary>Start button (only used if Show Start Menu is ticked).</summary>
    public void StartIntro()
    {
        if (State == GameState.Menu) SetState(GameState.Intro);
    }

    /// <summary>Continue button: hides the intro and begins gameplay.</summary>
    public void BeginPlaying()
    {
        if (State == GameState.Intro) SetState(GameState.Playing);
    }

    /// <summary>Ends the game with a message. Safe to call more than once.</summary>
    public void GameOver(string reason)
    {
        if (State != GameState.Playing) return;
        GameOverMessage = reason;
        SetState(GameState.GameOver);
    }

    /// <summary>Triggers the win state with a success message.</summary>
    public void WinGame(string reason)
    {
        if (State != GameState.Playing) return;
        GameOverMessage = reason;
        SetState(GameState.Win);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- Internals ----------

    private void SetState(GameState newState)
    {
        if (State == newState) return;
        ApplyState(newState);
        StateChanged?.Invoke(State);
    }

    private void ApplyState(GameState newState)
    {
        State = newState;

        // Only the Playing state runs the world
        Time.timeScale = (newState == GameState.Playing) ? 1f : 0f;

        bool lockCursor = newState == GameState.Playing && lockCursorWhilePlaying;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }
}
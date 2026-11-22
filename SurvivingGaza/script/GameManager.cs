using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Intro, Playing, Dying, Win, GameOver }

/// <summary>How the always-visible cursor behaves.</summary>
public enum CursorBehavior
{
    Free,              // visible and unlocked
    ConfinedToWindow   // visible, but cannot leave the game window
}

/// <summary>
/// Single owner of the game state, Time.timeScale and the cursor.
/// Put this on an empty "GameManager" object together with SurvivalTimer.
/// Flow: Intro -> (Continue) -> Playing -> Win
///                                      -> Dying -> GameOver (death sequence, then Game Over)
///                                      -> GameOver (immediate, e.g. falling off the map)
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SurvivalTimer timer;

    [Tooltip("The cursor is always visible. Confined also stops it leaving the game window.")]
    [SerializeField] private CursorBehavior cursorBehavior = CursorBehavior.Free;

    [Tooltip("Tick this to show the Start panel first (Menu -> Intro -> Playing). Leave off to open on the Intro.")]
    [SerializeField] private bool showStartMenu = false;

    [Tooltip("Safety net: if the death sequence takes longer than this, Game Over is shown anyway.")]
    [SerializeField] private float maxDeathSeconds = 8f;

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

        // Frozen until the player clicks Continue
        ApplyState(showStartMenu ? GameState.Menu : GameState.Intro);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        // Runs after every other script, so nothing can hide or lock the cursor
        ApplyCursor();
    }

    // ---------- Public API ----------

    /// <summary>Start button (only used if Show Start Menu is ticked).</summary>
    public void StartIntro()
    {
        if (State == GameState.Menu) SetState(GameState.Intro);
    }

    /// <summary>Continue button: closes the intro and begins gameplay.</summary>
    public void BeginPlaying()
    {
        if (State == GameState.Intro) SetState(GameState.Playing);
    }

    /// <summary>Ends the game immediately with a message (e.g. falling off the map).</summary>
    public void GameOver(string reason)
    {
        if (State != GameState.Playing) return;
        GameOverMessage = reason;
        SetState(GameState.GameOver);
    }

    /// <summary>
    /// Starts the death sequence. PlayerDeath plays the collapse and then calls FinishDeath().
    /// Without a PlayerDeath in the scene this behaves like GameOver().
    /// </summary>
    public void Die(string reason)
    {
        if (State != GameState.Playing) return;
        GameOverMessage = reason;

        if (FindObjectOfType<PlayerDeath>() == null)
        {
            SetState(GameState.GameOver);
            return;
        }

        SetState(GameState.Dying);
        StartCoroutine(DeathTimeout());
    }

    /// <summary>Called when the death sequence is done: shows Game Over.</summary>
    public void FinishDeath()
    {
        if (State == GameState.Dying) SetState(GameState.GameOver);
    }

    /// <summary>Triggers the win state with a success message. Only works while Playing.</summary>
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

    private IEnumerator DeathTimeout()
    {
        yield return new WaitForSeconds(maxDeathSeconds);
        FinishDeath();   // does nothing if the sequence already finished
    }

    private void SetState(GameState newState)
    {
        if (State == newState) return;
        ApplyState(newState);
        StateChanged?.Invoke(State);
    }

    private void ApplyState(GameState newState)
    {
        State = newState;

        // The world runs while Playing and while the death sequence plays
        bool running = newState == GameState.Playing || newState == GameState.Dying;
        Time.timeScale = running ? 1f : 0f;

        ApplyCursor();
    }

    private void ApplyCursor()
    {
        CursorLockMode mode = cursorBehavior == CursorBehavior.ConfinedToWindow
            ? CursorLockMode.Confined
            : CursorLockMode.None;

        if (Cursor.lockState != mode) Cursor.lockState = mode;
        if (!Cursor.visible) Cursor.visible = true;
    }
}
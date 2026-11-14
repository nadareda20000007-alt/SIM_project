using System;
using UnityEngine;

/// <summary>
/// The global survival countdown. Counts down only while Playing.
/// By default it is one continuous countdown: food and water do NOT add time.
/// Attach to the same object as GameManager.
/// </summary>
public class SurvivalTimer : MonoBehaviour
{
    [SerializeField] private float maxTime = 180f;
    [SerializeField] private string gameOverMessage = "You ran out of time.\nYou never found food or water.";

    [Tooltip("OFF = one continuous countdown. ON = food/water add their Refill Seconds back.")]
    [SerializeField] private bool allowRefill = false;

    public float Remaining { get; private set; }
    public float MaxTime => maxTime;

    /// <summary>Arguments: remaining seconds, max seconds.</summary>
    public event Action<float, float> Changed;

    private void Awake()
    {
        Remaining = maxTime;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
        Changed?.Invoke(Remaining, maxTime);

        if (Remaining <= 0f)
        {
            gm.GameOver(gameOverMessage);
        }
    }

    /// <summary>Adds seconds back to the timer. Does nothing unless Allow Refill is on.</summary>
    public void Replenish(float seconds)
    {
        if (!allowRefill || seconds <= 0f) return;

        Remaining = Mathf.Min(maxTime, Remaining + seconds);
        Changed?.Invoke(Remaining, maxTime);
    }
}
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action            OnGameStarted;
    public event Action            OnGamePaused;
    public event Action            OnGameResumed;
    public event Action<int>       OnGameWon;
    public event Action            OnGameLost;

    [Header("Scene References")]
    [SerializeField] private PlayerController  player;
    [SerializeField] private ScoreManager      scoreManager;
    [SerializeField] private UIManager         uiManager;
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private CheckpointManager  checkpointManager;



    private GameState _currentState = GameState.MainMenu;
    public  GameState CurrentState  => _currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player            == null) player            = FindFirstObjectByType<PlayerController>();
        if (scoreManager      == null) scoreManager      = FindFirstObjectByType<ScoreManager>();
        if (uiManager         == null) uiManager         = FindFirstObjectByType<UIManager>();
        if (leaderboardManager== null) leaderboardManager= FindFirstObjectByType<LeaderboardManager>();
        if (checkpointManager == null) checkpointManager = FindFirstObjectByType<CheckpointManager>();

        // Subscribe to score-zero event → triggers loss
        if (scoreManager != null)
            scoreManager.OnScoreReachedZero += TriggerLose;
        
        ChangeState(GameState.MainMenu);
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.OnScoreReachedZero -= TriggerLose;
    }

    private void ChangeState(GameState newState)
    {
        _currentState = newState;
        OnStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                if (player != null) player.InputEnabled = false;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                if (player != null) player.InputEnabled = true;
                scoreManager?.StartCounting();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                if (player != null) player.InputEnabled = false;
                scoreManager?.StopCounting();
                break;

            case GameState.Win:
                Time.timeScale = 0f;
                if (player != null) player.Stop();
                scoreManager?.StopCounting();
                break;

            case GameState.Lose:
                Time.timeScale = 0f;
                if (player != null) player.Stop();
                scoreManager?.StopCounting();
                break;
        }
    }

    // ── Public API ──
    public void StartGame()
    {
        scoreManager?.ResetScore();
        checkpointManager?.ResetCheckpoints();
        if (player != null)
        {
            player.InputEnabled = true;
            player.RespawnAtLastCheckpoint();
        }
        ChangeState(GameState.Playing);
        OnGameStarted?.Invoke();
    }

    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;
        ChangeState(GameState.Paused);
        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;
        ChangeState(GameState.Playing);
        OnGameResumed?.Invoke();
    }

    public void TriggerWin()
    {
        if (_currentState != GameState.Playing) return;
        int   finalScore  = scoreManager?.CalculateFinalScore() ?? 0;
        int   penalties   = scoreManager?.PenaltyCount          ?? 0;
        float time        = scoreManager?.ElapsedTime            ?? 0f;
        leaderboardManager?.TryAddEntry(finalScore, penalties, time);
        ChangeState(GameState.Win);
        OnGameWon?.Invoke(finalScore);
    }

    public void TriggerLose()
    {
        if (_currentState != GameState.Playing) return;
        ChangeState(GameState.Lose);
        OnGameLost?.Invoke();
    }

    /// <summary>Full restart — resets everything from scratch.</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Restart from last checkpoint — restores score/time/penalties snapshot,
    /// re-enables player input, and resumes Playing state without resetting score.
    /// </summary>
    public void RestartFromCheckpoint()
    {
        if (checkpointManager == null || !checkpointManager.HasActiveCheckpoint())
        {
            RestartGame(); // fallback if no checkpoint reached
            return;
        }
        checkpointManager.RestoreFromCheckpoint();
        if (player != null) player.InputEnabled = true;
        ChangeState(GameState.Playing);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.MainMenu);
    }

    public bool HasCheckpoint() =>
        checkpointManager != null && checkpointManager.HasActiveCheckpoint();
}

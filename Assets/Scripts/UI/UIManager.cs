using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages all UI panels and HUD.
///
/// PANEL VISIBILITY: Uses SetActive(true/false) on each panel GameObject
/// so panels are truly hidden — not just alpha=0. This prevents the
/// "all panels visible on start" issue caused by alpha alone not hiding
/// child elements that have their own CanvasGroup or graphic components.
///
/// SETUP in Inspector:
///   - Drag each Panel GameObject into the 5 panel slots
///   - All panels can be Active in the Editor (for design purposes)
///     this script will hide all of them immediately in Awake()
/// </summary>
public class UIManager : MonoBehaviour
{
    // ────────────────────────────────────────────
    //  Panels
    // ────────────────────────────────────────────
    [Header("Panels — drag Panel GameObjects here")]
    [SerializeField] private GameObject startPanelGO;
    [SerializeField] private GameObject hudPanelGO;
    [SerializeField] private GameObject pausePanelGO;
    [SerializeField] private GameObject winPanelGO;
    [SerializeField] private GameObject losePanelGO;

 //  Instructions Panel
    // ────────────────────────────────────────────
    [Header("Instructions")]
    [SerializeField] private GameObject   instructionsPanelGO;   // outer wrapper panel
    [SerializeField] private GameObject[] instructionPages;       // assign Page1, Page2, Page3 etc.
    [SerializeField] private Button       instrNextButton;
    [SerializeField] private Button       instrPrevButton;
    [SerializeField] private TextMeshProUGUI instrPageIndicator; // "2 / 4"

    // ────────────────────────────────────────────
    //  HUD
    // ────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hudScoreText;
    [SerializeField] private TextMeshProUGUI hudTimerText;
    [SerializeField] private TextMeshProUGUI hudPenaltyFlashText;
    [SerializeField] private TextMeshProUGUI hudCameraLabel;

    // ────────────────────────────────────────────
    //  Win Screen
    // ────────────────────────────────────────────
    [Header("Win Screen")]
    [SerializeField] private TextMeshProUGUI winFinalScoreText;
    [SerializeField] private TextMeshProUGUI winTimeText;
    [SerializeField] private TextMeshProUGUI winPenaltiesText;
    [SerializeField] private TextMeshProUGUI winBestScoreText;

    [Header("Leaderboard Rows")]
    [SerializeField] private LeaderboardRow[] leaderboardRows;

    [System.Serializable]
    public class LeaderboardRow
    {
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI penaltyText;
    }
    // ────────────────────────────────────────────
    //  Lose Screen
    // ────────────────────────────────────────────
    [Header("Lose Screen")]
    [SerializeField] private TextMeshProUGUI loseReasonText;
    [SerializeField] private TextMeshProUGUI loseFinalScoreText;
    [SerializeField] private GameObject      restartFromCheckpointButton;
    [SerializeField] private GameObject      restartButton;

    // ────────────────────────────────────────────
    //  Pause / Settings
    // ────────────────────────────────────────────
    [Header("Pause / Settings")]
    [SerializeField] private Slider          sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;

    // ────────────────────────────────────────────
    //  Private
    // ────────────────────────────────────────────
    private ScoreManager       _score;
    private LeaderboardManager _lb;
    private PlayerController   _player;
    private CameraController   _cam;
    private Coroutine          _penaltyCoroutine;
    private bool               _lostByScoreZero;
    private int                _currentInstrPage;

    // All panels in one array for easy iteration
    private GameObject[] _allPanels;

    // ────────────────────────────────────────────
    //  Awake — hide everything immediately
    // ────────────────────────────────────────────
    private void Awake()
    {
        // Build panel array
        _allPanels = new[]
        {
            startPanelGO,
            instructionsPanelGO,
            hudPanelGO,
            pausePanelGO,
            winPanelGO,
            losePanelGO
        };

        // ── CRITICAL: deactivate ALL panels right away in Awake ──
        // This runs before Start() on any other script, so the player
        // never sees a single frame with all panels visible.
        HideAllPanels();

        // Resolve references
        _score  = FindFirstObjectByType<ScoreManager>();
        _lb     = FindFirstObjectByType<LeaderboardManager>();
        _player = FindFirstObjectByType<PlayerController>();
        _cam    = FindFirstObjectByType<CameraController>();
    }

    // ────────────────────────────────────────────
    //  Start — subscribe to events, show first panel
    // ────────────────────────────────────────────
    private void Start()
    {
        // Game state events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameWon      += HandleWin;
            GameManager.Instance.OnGameLost     += HandleLose;
        }

        // Score events
        if (_score != null)
        {
            _score.OnScoreChanged     += UpdateHudScore;
            _score.OnPenaltyApplied   += _ => TriggerPenaltyFlash();
            _score.OnScoreReachedZero += () => _lostByScoreZero = true;
        }

        // Sensitivity slider
        if (sensitivitySlider != null && _player != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 5f;
            sensitivitySlider.value    = _player.Sensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        // Hide penalty flash text initially
        if (hudPenaltyFlashText != null)
            hudPenaltyFlashText.gameObject.SetActive(false);

        ShowPanel(startPanelGO);
        // Wire instruction buttons (also wirable via Inspector OnClick)
        if (instrNextButton != null) instrNextButton.onClick.AddListener(OnInstrNext);
        if (instrPrevButton != null) instrPrevButton.onClick.AddListener(OnInstrPrev);
    }

    private void Update()
    {
        // Live timer — only while playing
        if (hudTimerText == null || _score == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            hudTimerText.text = _score.ElapsedTimeFormatted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameWon      -= HandleWin;
            GameManager.Instance.OnGameLost     -= HandleLose;
        }
        if (_score != null)
            _score.OnScoreChanged -= UpdateHudScore;
    }

    // ────────────────────────────────────────────
    //  State Handlers
    // ────────────────────────────────────────────
    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu: ShowPanel(startPanelGO); break;
            case GameState.Playing:  ShowPanel(hudPanelGO);   break;
            case GameState.Paused:   ShowPanel(pausePanelGO); break;
            // Win / Lose handled separately — they carry extra display data
        }
    }

    private void HandleWin(int finalScore)
    {
        ShowPanel(winPanelGO);

        float time    = _score?.ElapsedTime  ?? 0f;
        int penalties = _score?.PenaltyCount ?? 0;

        if (winFinalScoreText != null)
            winFinalScoreText.text = $"Score:      {finalScore:N0}";
        if (winTimeText != null)
            winTimeText.text       = $"Time:       {FormatTime(time)}";
        if (winPenaltiesText != null)
            winPenaltiesText.text  = $"Penalties:  {penalties}";
        if (winBestScoreText != null && _lb != null)
            winBestScoreText.text  = $"Best Score: {_lb.GetBestScore():N0}";

        PopulateLeaderboard();
    }

    private void HandleLose()
    {
        ShowPanel(losePanelGO);

        if (loseReasonText != null)
            loseReasonText.text = _lostByScoreZero ? "Score reached zero!" : "You fell off!";

        _lostByScoreZero = false;

        if (loseFinalScoreText != null && _score != null)
            loseFinalScoreText.text = $"Final Score: {_score.CurrentScore:N0}";

        // Show checkpoint restart button only if a checkpoint was reached
        bool hasCP = GameManager.Instance != null && GameManager.Instance.HasCheckpoint();
        if (restartFromCheckpointButton != null)
            restartFromCheckpointButton.SetActive(hasCP);
        if (restartButton != null)
            restartButton.SetActive(true);
    }

    // ────────────────────────────────────────────
    //  HUD Updates
    // ────────────────────────────────────────────
    private void UpdateHudScore(int score)
    {
        if (hudScoreText != null)
            hudScoreText.text = $"Score: {score:N0}";
    }

    private void TriggerPenaltyFlash()
    {
        if (hudPenaltyFlashText == null) return;
        if (_penaltyCoroutine != null) StopCoroutine(_penaltyCoroutine);
        _penaltyCoroutine = StartCoroutine(PenaltyFlashRoutine());
    }

    private IEnumerator PenaltyFlashRoutine()
    {
        int amount = _score?.ObstaclePenaltyAmount ?? 500;
        hudPenaltyFlashText.text  = $"-{amount}  PENALTY!";
        hudPenaltyFlashText.color = Color.red;
        hudPenaltyFlashText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / 1.5f);
            hudPenaltyFlashText.color = new Color(1f, 0.15f, 0.15f, a);
            yield return null;
        }

        hudPenaltyFlashText.gameObject.SetActive(false);
    }

    // ────────────────────────────────────────────
    //  Instructions — multi-page navigation
    // ────────────────────────────────────────────

    /// <summary>Opens instructions from main menu, always starts on page 1.</summary>
    public void OnInstructionsButtonPressed()
    {
        if (instructionPages == null || instructionPages.Length == 0) return;
        _currentInstrPage = 0;
        ShowPanel(instructionsPanelGO);
        RefreshInstructionPage();
    }

    private void OnInstrNext()
    {
        if (instructionPages == null) return;

        // On last page, Next closes instructions and returns to Main Menu
        if (_currentInstrPage >= instructionPages.Length - 1)
        {
            ShowPanel(startPanelGO);
            return;
        }
        _currentInstrPage++;
        RefreshInstructionPage();
    }

    private void OnInstrPrev()
    {
        if (_currentInstrPage <= 0) return;
        _currentInstrPage--;
        RefreshInstructionPage();
    }

    /// <summary>Activates only the current page inside the instructions panel.</summary>
    private void RefreshInstructionPage()
    {
        for (int i = 0; i < instructionPages.Length; i++)
        {
            if (instructionPages[i] != null)
                instructionPages[i].SetActive(i == _currentInstrPage);
        }

        // Prev hidden on first page
        if (instrPrevButton != null)
            instrPrevButton.gameObject.SetActive(_currentInstrPage > 0);

        // Next label changes to "Close" on last page
        if (instrNextButton != null)
        {
            var label = instrNextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = (_currentInstrPage >= instructionPages.Length - 1) ? "X" : ">";
        }

        // Page indicator e.g. "2 / 4"
        if (instrPageIndicator != null)
            instrPageIndicator.text = $"{_currentInstrPage + 1} / {instructionPages.Length}";
    }

    // ────────────────────────────────────────────
    //  Leaderboard — 5 fixed TMP rows
    // ────────────────────────────────────────────
    private void PopulateLeaderboard()
    {
        if (leaderboardRows == null || leaderboardRows.Length == 0)
            return;

        for (int i = 0; i < leaderboardRows.Length; i++)
        {
            leaderboardRows[i].rankText.text = $"#{i + 1}";
            leaderboardRows[i].scoreText.text = "---";
            leaderboardRows[i].timeText.text = "--:--";
            leaderboardRows[i].penaltyText.text = "---";
        }

        if (_lb == null)
            return;

        var entries = _lb.GetEntries();

        for (int i = 0; i < entries.Count && i < leaderboardRows.Length; i++)
        {
            var e = entries[i];

            leaderboardRows[i].rankText.text = $"#{i + 1}";
            leaderboardRows[i].scoreText.text = e.score.ToString("N0");
            leaderboardRows[i].timeText.text = e.FormattedTime;
            leaderboardRows[i].penaltyText.text = e.penaltyCount.ToString();
        }
    }

    // ────────────────────────────────────────────
    //  Settings
    // ────────────────────────────────────────────
    private void OnSensitivityChanged(float v)
    {
        if (_player != null) _player.Sensitivity = v;
        if (sensitivityValueText != null)
            sensitivityValueText.text = v.ToString("F1") + "×";
    }

    // ────────────────────────────────────────────
    //  Camera
    // ────────────────────────────────────────────
    public void ToggleCameraMode()
    {
        _cam?.ToggleMode();
        if (hudCameraLabel != null && _cam != null)
            hudCameraLabel.text = _cam.CurrentMode == CameraController.CameraMode.Follow
                ? "Camera: Follow" : "Camera: Top Down";
    }

    // ────────────────────────────────────────────
    //  Button Callbacks  (wire via OnClick in Inspector)
    // ────────────────────────────────────────────
    public void OnStartButtonPressed()           => GameManager.Instance?.StartGame();
    public void OnPauseButtonPressed()           => GameManager.Instance?.PauseGame();
    public void OnResumeButtonPressed()          => GameManager.Instance?.ResumeGame();
    public void OnRestartButtonPressed()         => GameManager.Instance?.RestartGame();
    public void OnRestartFromCheckpointPressed() => GameManager.Instance?.RestartFromCheckpoint();
    public void OnMainMenuButtonPressed()        => GameManager.Instance?.QuitToMainMenu();

    // ────────────────────────────────────────────
    //  Panel Switching — SetActive based
    // ────────────────────────────────────────────

    /// <summary>
    /// Deactivates all panels, then activates only the target panel.
    /// Using SetActive ensures child graphics, raycasts, and animators
    /// are all properly disabled — not just visually hidden via alpha.
    /// </summary>
    private void ShowPanel(GameObject target)
    {
        foreach (var panel in _allPanels)
        {
            if (panel == null) continue;
            panel.SetActive(panel == target);
        }
    }

    /// <summary>Deactivate every panel. Called in Awake before anything renders.</summary>
    private void HideAllPanels()
    {
        foreach (var panel in _allPanels)
            if (panel != null) panel.SetActive(false);
    }

    // ────────────────────────────────────────────
    //  Utility for time formatting (mm:ss)
    // ────────────────────────────────────────────
    private static string FormatTime(float t) =>
        $"{Mathf.FloorToInt(t / 60f):00}:{Mathf.FloorToInt(t % 60f):00}";
}
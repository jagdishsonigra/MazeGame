using UnityEngine;
using System;

/// <summary>
/// Manages all scoring logic.
/// - Base score decreases over time
/// - Penalties deducted on obstacle collision
/// - Checkpoint bonus awarded
/// - Score hitting 0 triggers a loss
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int baseScore = 10000;
    [SerializeField] private int scorePerSecondDeduction = 10;
    [SerializeField] private int obstaclePenaltyAmount = 500;
    [SerializeField] private int checkpointBonus = 200;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnPenaltyApplied;
    public event Action OnScoreReachedZero;

    // ── Live state ──
    private int   _currentScore;
    private int   _penaltiesTotal;   // total points deducted via penalties
    private int   _penaltyCount;     // number of times hit
    private float _elapsedTime;
    private bool  _isCounting;

    // ── Checkpoint snapshot ──
    private int   _checkpointScore;
    private int   _checkpointPenaltiesTotal;
    private int   _checkpointPenaltyCount;
    private float _checkpointElapsedTime;
    private bool  _checkpointSaved;

    // ── Properties ──
    public int   CurrentScore    => _currentScore;
    public int   PenaltiesTotal  => _penaltiesTotal;
    public int   PenaltyCount    => _penaltyCount;
    public float ElapsedTime     => _elapsedTime;
    public int   ObstaclePenaltyAmount => obstaclePenaltyAmount;
    public string ElapsedTimeFormatted =>
        $"{Mathf.FloorToInt(_elapsedTime / 60f):00}:{Mathf.FloorToInt(_elapsedTime % 60f):00}";

    private void Awake() => ResetScore();

    private void Update()
    {
        if (!_isCounting) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        _elapsedTime += Time.deltaTime;

        int timeDeduction = Mathf.FloorToInt(_elapsedTime) * scorePerSecondDeduction;
        int newScore      = Mathf.Max(0, baseScore - timeDeduction - _penaltiesTotal);

        if (newScore != _currentScore)
        {
            _currentScore = newScore;
            OnScoreChanged?.Invoke(_currentScore);

            if (_currentScore == 0)
                OnScoreReachedZero?.Invoke();
        }
    }

    // ── Public API ──
    public void ResetScore()
    {
        _currentScore         = baseScore;
        _penaltiesTotal       = 0;
        _penaltyCount         = 0;
        _elapsedTime          = 0f;
        _isCounting           = false;
        _checkpointSaved      = false;
        OnScoreChanged?.Invoke(_currentScore);
    }

    public void StartCounting() => _isCounting = true;
    public void StopCounting()  => _isCounting = false;

    public void ApplyObstaclePenalty()
    {
        _penaltiesTotal += obstaclePenaltyAmount;
        _penaltyCount++;
        _currentScore = Mathf.Max(0, _currentScore - obstaclePenaltyAmount);
        OnScoreChanged?.Invoke(_currentScore);
        OnPenaltyApplied?.Invoke(obstaclePenaltyAmount);

        if (_currentScore == 0)
            OnScoreReachedZero?.Invoke();
    }

    public void ApplyCheckpointBonus()
    {
        _currentScore = Mathf.Min(baseScore, _currentScore + checkpointBonus);
        OnScoreChanged?.Invoke(_currentScore);
    }

    /// <summary>Save state snapshot at checkpoint.</summary>
    public void SaveCheckpointSnapshot()
    {
        _checkpointScore          = _currentScore;
        _checkpointPenaltiesTotal = _penaltiesTotal;
        _checkpointPenaltyCount   = _penaltyCount;
        _checkpointElapsedTime    = _elapsedTime;
        _checkpointSaved          = true;
    }

    /// <summary>Restore state to last checkpoint snapshot.</summary>
    public void RestoreCheckpointSnapshot()
    {
        if (!_checkpointSaved) return;
        _currentScore   = _checkpointScore;
        _penaltiesTotal = _checkpointPenaltiesTotal;
        _penaltyCount   = _checkpointPenaltyCount;
        _elapsedTime    = _checkpointElapsedTime;
        OnScoreChanged?.Invoke(_currentScore);
    }

    public bool HasCheckpointSnapshot() => _checkpointSaved;

    public int CalculateFinalScore()
    {
        StopCounting();
        return _currentScore;
    }
}

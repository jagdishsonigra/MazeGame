using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages checkpoints. On activation:
///   - Updates player respawn position
///   - Saves a ScoreManager snapshot (score, time, penalties)
///   - Awards checkpoint bonus
/// On RestoreFromCheckpoint (called when restarting from checkpoint):
///   - Teleports player back
///   - Restores score/time/penalty snapshot exactly
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Serializable]
    public struct CheckpointData
    {
        public int      id;
        public bool     isActivated;
        public Vector3  position;
        public Quaternion rotation;
    }

    private List<CheckpointData> _checkpoints = new();
    private int _lastActivatedId = -1;
    private bool _hasActiveCheckpoint = false;

    public event Action<int> OnCheckpointActivated;

    private PlayerController _player;
    private ScoreManager     _scoreManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _player       = FindFirstObjectByType<PlayerController>();
        _scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    public void RegisterCheckpoint(int id, Vector3 position, Quaternion rotation)
    {
        _checkpoints.Add(new CheckpointData
            { id = id, isActivated = false, position = position, rotation = rotation });
    }

    public bool ActivateCheckpoint(int id)
    {
        for (int i = 0; i < _checkpoints.Count; i++)
        {
            if (_checkpoints[i].id != id || _checkpoints[i].isActivated) continue;

            var updated = _checkpoints[i];
            updated.isActivated = true;
            _checkpoints[i] = updated;

            _lastActivatedId      = id;
            _hasActiveCheckpoint  = true;

            // Update player respawn
            _player?.SetSpawnPoint(updated.position, updated.rotation);

            // Snapshot score state at this checkpoint
            _scoreManager?.SaveCheckpointSnapshot();

            // Award bonus
            _scoreManager?.ApplyCheckpointBonus();

            OnCheckpointActivated?.Invoke(id);
            return true;
        }
        return false;
    }

    public bool HasActiveCheckpoint() => _hasActiveCheckpoint;

    /// <summary>
    /// Restore player and score state to last checkpoint.
    /// Called when player presses "Restart from Checkpoint" on Lose screen.
    /// </summary>
    public void RestoreFromCheckpoint()
    {
        if (!_hasActiveCheckpoint) return;
        _player?.RespawnAtLastCheckpoint();
        _scoreManager?.RestoreCheckpointSnapshot();
    }

    public void ResetCheckpoints()
    {
        for (int i = 0; i < _checkpoints.Count; i++)
        {
            var cp = _checkpoints[i];
            cp.isActivated = false;
            _checkpoints[i] = cp;
        }
        _lastActivatedId     = -1;
        _hasActiveCheckpoint = false;
    }
}

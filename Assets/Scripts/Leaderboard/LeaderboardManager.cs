using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the local leaderboard — top 5 entries.
/// Each entry stores: score, penalty count, completion time, date.
/// Sort priority: highest score first; ties broken by lowest time.
/// Persisted via PlayerPrefs + JSON between sessions.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string SaveKey   = "MazeGame_LB_v2";
    private const int    MaxEntries = 5;

    // ── Entry ──
    [Serializable]
    public class ScoreEntry : IComparable<ScoreEntry>
    {
        public int    score;
        public int    penaltyCount;
        public float  completionTime;
        public string dateLabel;

        public ScoreEntry() { }

        public ScoreEntry(int score, int penaltyCount, float completionTime)
        {
            this.score          = score;
            this.penaltyCount   = penaltyCount;
            this.completionTime = completionTime;
            this.dateLabel      = DateTime.Now.ToString("MM/dd HH:mm");
        }

        /// <summary>Higher score wins. Equal score → lower time wins.</summary>
        public int CompareTo(ScoreEntry other)
        {
            int scoreCmp = other.score.CompareTo(this.score); // descending score
            return scoreCmp != 0 ? scoreCmp : this.completionTime.CompareTo(other.completionTime); // ascending time
        }

        public string FormattedTime =>
            $"{Mathf.FloorToInt(completionTime / 60f):00}:{Mathf.FloorToInt(completionTime % 60f):00}";
    }

    [Serializable]
    private class SaveData { public List<ScoreEntry> entries = new(); }

    private List<ScoreEntry> _entries = new();

    public event Action OnLeaderboardUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    // ── Public API ──
    public bool TryAddEntry(int score, int penaltyCount, float completionTime)
    {
        var entry = new ScoreEntry(score, penaltyCount, completionTime);
        _entries.Add(entry);
        _entries.Sort();
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        Save();
        OnLeaderboardUpdated?.Invoke();
        return true;
    }

    public IReadOnlyList<ScoreEntry> GetEntries() => _entries.AsReadOnly();
    public int GetBestScore() => _entries.Count > 0 ? _entries[0].score : 0;

    // ── Persistence ──
    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SaveData { entries = _entries }));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            _entries = data?.entries ?? new List<ScoreEntry>();
            _entries.Sort();
        }
        catch { _entries = new List<ScoreEntry>(); }
    }
}

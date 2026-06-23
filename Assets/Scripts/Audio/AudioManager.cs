using UnityEngine;

/// <summary>
/// Lightweight audio manager using a simple enum-keyed AudioClip array.
/// Provides static Play methods so any class can trigger sounds without
/// needing a direct reference (reduces coupling).
///
/// For a production game this would be replaced with a proper audio
/// middleware such as FMOD or an addressable-based AudioClip pool.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    //  Sound IDs
    // ─────────────────────────────────────────────
    public enum SoundId
    {
        ButtonClick,
        CheckpointActivated,
        ObstacleHit,
        Win,
        Lose,
        BallRolling
    }

    [System.Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Sound Library")]
    [SerializeField] private SoundEntry[] sounds;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.4f;

    // ─────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────
    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // SFX source
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        // Music source
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;
        _musicSource.playOnAwake = false;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────
    public void PlaySound(SoundId id)
    {
        if (_sfxSource == null) return;
        foreach (var entry in sounds)
        {
            if (entry.id != id || entry.clip == null) continue;
            _sfxSource.PlayOneShot(entry.clip, entry.volume);
            return;
        }
    }

    public static void Play(SoundId id)
    {
        Instance?.PlaySound(id);
    }

    public void SetMusicVolume(float v) => _musicSource.volume = Mathf.Clamp01(v);
    public void SetSFXVolume(float v)   => _sfxSource.volume   = Mathf.Clamp01(v);
}

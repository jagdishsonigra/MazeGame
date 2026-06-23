using UnityEngine;

/// <summary>
/// Place this on any trigger collider in the maze to act as a checkpoint.
/// When the player enters the trigger, it registers with CheckpointManager.
/// 
/// The checkpoint visually changes colour when activated to give clear feedback.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────
    [Header("Checkpoint Settings")]
    [SerializeField] private int checkpointId = 0;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 0f, 0.4f); // yellow semi-transparent
    [SerializeField] private Color activeColor   = new Color(0f, 1f, 0f, 0.6f); // green

    // ─────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────
    private CheckpointManager _checkpointManager;
    private Renderer _renderer;
    private bool _isActivated;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _checkpointManager = FindFirstObjectByType<CheckpointManager>();
    }

    private void Start()
    {
        // Register with manager
        _checkpointManager?.RegisterCheckpoint(checkpointId, transform.position, transform.rotation);
        SetVisualState(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isActivated) return;
        if (!other.CompareTag("Player")) return;

        bool success = _checkpointManager != null && _checkpointManager.ActivateCheckpoint(checkpointId);
        if (success)
        {
            _isActivated = true;
            SetVisualState(true);
        }
    }

    // ─────────────────────────────────────────────
    //  Visuals
    // ─────────────────────────────────────────────
    private void SetVisualState(bool activated)
    {
        if (_renderer == null) return;
        _renderer.material.color = activated ? activeColor : inactiveColor;
    }
}

using UnityEngine;

/// <summary>
/// Abstract base class for all obstacles.
/// Handles the penalty trigger logic; subclasses define movement/behaviour.
/// 
/// Architecture note: using an abstract base class here gives us the
/// Template Method pattern — shared collision logic lives here, unique
/// obstacle behaviour is overridden in subclasses. This keeps each
/// obstacle class focused and makes adding new obstacle types trivial.
/// </summary>
public abstract class ObstacleBase : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] protected bool applyPenaltyOnCollision = true;
    [SerializeField] protected float penaltyCooldown = 1.5f;  // seconds before another penalty can trigger

    private float _lastPenaltyTime = -999f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    protected virtual void Start() { }
    protected virtual void Update() { }

    // ─────────────────────────────────────────────
    //  Collision Handling (shared)
    // ─────────────────────────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        TryApplyPenalty();
        OnPlayerHit(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TryApplyPenalty();
        OnPlayerTriggered(other);
    }

    private void TryApplyPenalty()
    {
        if (!applyPenaltyOnCollision) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
        if (Time.time - _lastPenaltyTime < penaltyCooldown) return;

        _lastPenaltyTime = Time.time;
        // ApplyObstaclePenalty fires ScoreManager.OnPenaltyApplied,
        // which UIManager subscribes to and calls FlashPenalty() internally.
        // No direct UIManager call needed here — keeps coupling low.
        FindFirstObjectByType<ScoreManager>()?.ApplyObstaclePenalty();
    }

    // ─────────────────────────────────────────────
    //  Virtual hooks for subclasses
    // ─────────────────────────────────────────────
    protected virtual void OnPlayerHit(Collision collision) { }
    protected virtual void OnPlayerTriggered(Collider other) { }
}
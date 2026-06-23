using UnityEngine;

/// <summary>
/// Obstacle Type 2: Moving / Patrolling Wall.
/// Slides between two points (PointA and PointB) using a smooth ping-pong motion.
/// Great for blocking corridors intermittently.
/// </summary>
public class MovingObstacle : ObstacleBase
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float pauseAtEndpoint = 0.5f; // seconds to wait at each end

    private float _t = 0f;
    private bool _movingToB = true;
    private float _pauseTimer = 0f;
    private bool _isPausing = false;

    protected override void Update()
    {
        base.Update();
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
        if (pointA == null || pointB == null) return;

        if (_isPausing)
        {
            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer <= 0f)
            {
                _isPausing = false;
                _movingToB = !_movingToB;
            }
            return;
        }

        float speed = moveSpeed / Vector3.Distance(pointA.position, pointB.position);
        _t += Time.deltaTime * speed;

        if (_t >= 1f)
        {
            _t = 1f;
            _isPausing = true;
            _pauseTimer = pauseAtEndpoint;
        }

        transform.position = Vector3.Lerp(
            _movingToB ? pointA.position : pointB.position,
            _movingToB ? pointB.position : pointA.position,
            _t >= 1f ? 1f : SmoothStep(_t));
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    /// <summary>Smoothstep easing: slow at start and end for natural movement.</summary>
    private static float SmoothStep(float t) => t * t * (3f - 2f * t);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawSphere(pointA.position, 0.15f);
        Gizmos.DrawSphere(pointB.position, 0.15f);
    }
#endif
}

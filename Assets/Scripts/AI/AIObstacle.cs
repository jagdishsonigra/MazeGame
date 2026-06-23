using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// AI-controlled obstacle that patrols between waypoints and
/// chases the player when detected.
///
/// Uses a simple finite state machine:
///   • Patrol   - Moves between predefined waypoints.
///   • Chase    - Pursues the player when within detection range.
///   • Cooldown - Temporarily stops chasing before resuming patrol.
///
/// When the player is hit:
///   • A score penalty is applied through the obstacle system.
///   • The AI immediately returns to patrol mode.
///   • A short hit cooldown prevents repeated penalties while
///     the player and AI remain in contact.
///
/// Visual feedback is provided by changing the obstacle color
/// depending on its current state.
/// </summary>
public class AIObstacle : ObstacleBase
{
    // ─────────────────────────────────────────────
    //  AI States
    // ─────────────────────────────────────────────
    private enum AIState
    {
        Patrol,
        Chase,
        Cooldown
    }

    private AIState _currentState = AIState.Patrol;

    // ─────────────────────────────────────────────
    //  Patrol Settings
    // ─────────────────────────────────────────────
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float waypointReachDistance = 0.3f;

    // ─────────────────────────────────────────────
    //  Chase Settings
    // ─────────────────────────────────────────────
    [Header("Chase")]
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float loseInterestRadius = 7f;

    // ─────────────────────────────────────────────
    //  Cooldown Settings
    // ─────────────────────────────────────────────
    [Header("Cooldown")]
    [SerializeField] private float cooldownAfterChase = 2f;

    // ─────────────────────────────────────────────
    //  Visual Feedback
    // ─────────────────────────────────────────────
    [Header("Visual Feedback")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Color patrolColor = Color.blue;
    [SerializeField] private Color chaseColor = Color.red;

    // ─────────────────────────────────────────────
    //  Hit Protection
    // ─────────────────────────────────────────────
    [SerializeField] private float hitCooldown = 2f;
    private bool _canHitPlayer = true;

    // ─────────────────────────────────────────────
    //  Runtime References
    // ─────────────────────────────────────────────
    private int _currentWaypointIndex;
    private Transform _player;
    private float _cooldownTimer;
    private NavMeshAgent _agent;

    // ─────────────────────────────────────────────
    //  Initialization
    // ─────────────────────────────────────────────

    /// <summary>
    /// Finds required references and initializes the NavMeshAgent.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            _player = pc.transform;

        _agent = GetComponent<NavMeshAgent>();

        if (_agent == null)
        {
            Debug.LogError("AIObstacle requires a NavMeshAgent component.");
            enabled = false;
            return;
        }

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();

        _agent.speed = patrolSpeed;
        _agent.stoppingDistance = 0f;

        SetColor(patrolColor);
    }

    // ─────────────────────────────────────────────
    //  Update Loop
    // ─────────────────────────────────────────────

    /// <summary>
    /// Executes state-specific behavior while the game is running.
    /// </summary>
    protected override void Update()
    {
        base.Update();

        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (waypoints == null || waypoints.Length == 0)
            return;

        switch (_currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;

            case AIState.Chase:
                UpdateChase();
                break;

            case AIState.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  State Logic
    // ─────────────────────────────────────────────

    /// <summary>
    /// Patrols through waypoint locations in sequence.
    /// Transitions to Chase when the player is detected.
    /// </summary>
    private void UpdatePatrol()
    {
        Transform target = waypoints[_currentWaypointIndex];

        _agent.speed = patrolSpeed;
        _agent.SetDestination(target.position);

        if (!_agent.pathPending &&
            _agent.remainingDistance <= waypointReachDistance)
        {
            _currentWaypointIndex =
                (_currentWaypointIndex + 1) % waypoints.Length;
        }

        if (_player != null &&
            Vector3.Distance(transform.position, _player.position) <= detectionRadius)
        {
            TransitionTo(AIState.Chase);
        }
    }

    /// <summary>
    /// Continuously follows the player until they escape
    /// beyond the lose-interest distance.
    /// </summary>
    private void UpdateChase()
    {
        if (_player == null)
        {
            TransitionTo(AIState.Cooldown);
            return;
        }

        _agent.speed = chaseSpeed;
        _agent.SetDestination(_player.position);

        if (Vector3.Distance(transform.position, _player.position) > loseInterestRadius)
        {
            TransitionTo(AIState.Cooldown);
        }
    }

    /// <summary>
    /// Temporary waiting state before returning to patrol.
    /// </summary>
    private void UpdateCooldown()
    {
        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer <= 0f)
        {
            TransitionTo(AIState.Patrol);
        }
    }

    // ─────────────────────────────────────────────
    //  State Transitions
    // ─────────────────────────────────────────────

    /// <summary>
    /// Switches the AI state and updates movement
    /// parameters and visual indicators.
    /// </summary>
    private void TransitionTo(AIState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case AIState.Patrol:
                _agent.speed = patrolSpeed;
                SetColor(patrolColor);
                break;

            case AIState.Chase:
                _agent.speed = chaseSpeed;
                SetColor(chaseColor);
                break;

            case AIState.Cooldown:
                _cooldownTimer = cooldownAfterChase;
                _agent.ResetPath();
                SetColor(patrolColor);
                break;
        }
    }

    /// <summary>
    /// Updates obstacle color to reflect current state.
    /// </summary>
    private void SetColor(Color color)
    {
        if (bodyRenderer != null)
            bodyRenderer.material.color = color;
    }

    // ─────────────────────────────────────────────
    //  Player Interaction
    // ─────────────────────────────────────────────

    /// <summary>
    /// Triggered when the obstacle physically collides
    /// with the player.
    /// </summary>
    protected override void OnPlayerHit(Collision collision)
    {
        if (!_canHitPlayer)
            return;

        ReturnToPatrol();
        StartCoroutine(HitCooldownRoutine());
    }

    /// <summary>
    /// Triggered when the player enters a trigger-based
    /// detection collider.
    /// </summary>
    protected override void OnPlayerTriggered(Collider other)
    {
        if (!_canHitPlayer)
            return;

        ReturnToPatrol();
        StartCoroutine(HitCooldownRoutine());
    }

    /// <summary>
    /// Prevents multiple penalties from being applied
    /// while the player remains in contact with the AI.
    /// </summary>
    private IEnumerator HitCooldownRoutine()
    {
        _canHitPlayer = false;

        TransitionTo(AIState.Cooldown);

        yield return new WaitForSeconds(hitCooldown);

        _canHitPlayer = true;
    }

    // ─────────────────────────────────────────────
    //  Patrol Recovery
    // ─────────────────────────────────────────────

    /// <summary>
    /// Finds the nearest waypoint and immediately returns
    /// the AI to its patrol route after hitting the player.
    /// </summary>
    private void ReturnToPatrol()
    {
        if (_agent == null || waypoints == null || waypoints.Length == 0)
            return;

        _agent.ResetPath();

        float closestDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                waypoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        _currentWaypointIndex = closestIndex;

        // Instantly reposition AI at the nearest waypoint
        // to prevent pushing the player into walls.
        _agent.Warp(waypoints[_currentWaypointIndex].position);

        TransitionTo(AIState.Patrol);
    }

#if UNITY_EDITOR

    // ─────────────────────────────────────────────
    //  Debug Visualization
    // ─────────────────────────────────────────────

    /// <summary>
    /// Draws waypoint paths and detection ranges
    /// inside the Unity Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseInterestRadius);

        if (waypoints == null) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    waypoints[i].position,
                    waypoints[i + 1].position);
            }
        }
    }

#endif
}
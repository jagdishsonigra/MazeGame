using UnityEngine;

/// <summary>
/// Controls the player sphere using physics-based movement.
/// Supports accelerometer input on mobile and keyboard input in the Unity Editor.
/// Sensitivity is adjustable at runtime via the Settings UI.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────
    [Header("Movement Settings")]
    [SerializeField] private float movementForce = 15f;
    [SerializeField] private float maxSpeed = 8f;

    [Header("Sensitivity (1 = default, higher = faster)")]
    [SerializeField, Range(0.1f, 5f)] private float sensitivity = 1f;

    [Header("Physics")]
    [SerializeField] private float groundDrag = 2f;
    [SerializeField] private float airDrag = 0.5f;

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────
    private Rigidbody _rb;
    private bool _isGrounded;
    private bool _inputEnabled = true;

    // Checkpoint respawn data
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    // ─────────────────────────────────────────────
    //  Properties
    // ─────────────────────────────────────────────
    public float Sensitivity
    {
        get => sensitivity;
        set => sensitivity = Mathf.Clamp(value, 0.1f, 5f);
    }

    public bool InputEnabled
    {
        get => _inputEnabled;
        set => _inputEnabled = value;
    }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (!_inputEnabled) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        Vector3 inputVector = GetInputVector();
        ApplyMovementForce(inputVector);
        ClampSpeed();
        UpdateDrag();
    }

    // ─────────────────────────────────────────────
    //  Input Handling
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns a normalized input direction based on platform:
    /// Mobile → accelerometer, Editor/PC → keyboard WASD/Arrow.
    /// </summary>
    private Vector3 GetInputVector()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        return new Vector3(h, 0f, v).normalized;
#else
        // Accelerometer: tilt phone to roll ball
        Vector3 accel = Input.acceleration;
        // Map device tilt: accel.x → world X, accel.y → world Z
        Vector3 direction = new Vector3(accel.x, 0f, accel.y);
        // Clamp to magnitude 1 to prevent huge forces
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        return direction;
#endif
    }

    private void ApplyMovementForce(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        _rb.AddForce(direction * (movementForce * sensitivity), ForceMode.Force);
    }

    private void ClampSpeed()
    {
        Vector3 flatVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 clamped = flatVelocity.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(clamped.x, _rb.linearVelocity.y, clamped.z);
        }
    }

    private void UpdateDrag()
    {
        _rb.linearDamping = _isGrounded ? groundDrag : airDrag;
    }

    // ─────────────────────────────────────────────
    //  Ground Detection
    // ─────────────────────────────────────────────
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Wall"))
            _isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Wall"))
            _isGrounded = false;
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Teleport the player to a position and zero out velocity.</summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);
    }

    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        _spawnPosition = position;
        _spawnRotation = rotation;
    }

    public void RespawnAtLastCheckpoint()
    {
        TeleportTo(_spawnPosition, _spawnRotation);
    }

    public void Stop()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _inputEnabled = false;
    }
}

using UnityEngine;

/// <summary>
/// Manages two camera modes:
///   1. FOLLOW CAMERA  – third-person behind-and-above the ball (smooth lerp)
///   2. TOP DOWN CAMERA – orthographic bird's-eye view of the full maze
///
/// The player can switch between modes at runtime via a UI button or the Tab key.
/// Uses a single Camera with parameter blending rather than two separate cameras
/// to avoid audio listener warnings and keep the scene light.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Camera Modes
    // ─────────────────────────────────────────────
    public enum CameraMode { Follow, TopDown }

    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Camera Settings")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 6f, -8f);
    [SerializeField] private float followFieldOfView = 60f;

    [Header("Top Down Camera Settings")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 20f, 0f);
    [SerializeField] private float topDownSmoothSpeed = 5f;
    [SerializeField] private float topDownFieldOfView = 70f;

    [Header("Transition")]
    [SerializeField] private float modeSwitchDuration = 0.4f;  // seconds to blend between modes

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────
    private Camera _camera;
    private CameraMode _currentMode = CameraMode.Follow;
    private float _blendT = 1f;          // 0 = start mode, 1 = target mode fully reached
    private bool _isTransitioning = false;

    // Cached targets for blending
    private Vector3 _blendStartPos;
    private Quaternion _blendStartRot;
    private float _blendStartFOV;

    // ─────────────────────────────────────────────
    //  Properties
    // ─────────────────────────────────────────────
    public CameraMode CurrentMode => _currentMode;
    private Vector3 velocity;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null) _camera = Camera.main;

        // Auto-find player if not assigned
        if (target == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) target = pc.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Toggle camera on Tab key (editor convenience)
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleMode();

        if (_isTransitioning)
            UpdateTransition();
        else
            UpdateCamera();
    }

    // ─────────────────────────────────────────────
    //  Camera Update
    // ─────────────────────────────────────────────
    private void UpdateCamera()
    {
        switch (_currentMode)
        {
            case CameraMode.Follow:   UpdateFollowCamera();  break;
            case CameraMode.TopDown:  UpdateTopDownCamera(); break;
        }
    }

    private void UpdateFollowCamera()
    {
        Vector3 desiredPos = target.position + followOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref velocity,
            0.15f);

        Quaternion desiredRotation = Quaternion.LookRotation(
            (target.position + Vector3.up * 0.5f) - transform.position);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            10f * Time.deltaTime);
    }

    private void UpdateTopDownCamera()
    {
        Vector3 desiredPos = target.position + topDownOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, topDownSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(90f, 0f, 0f), topDownSmoothSpeed * Time.deltaTime);
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, topDownFieldOfView, topDownSmoothSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Mode Transition Blend
    // ─────────────────────────────────────────────
    private void UpdateTransition()
    {
        _blendT += Time.deltaTime / modeSwitchDuration;

        Vector3 targetPos;
        Quaternion targetRot;
        float targetFOV;

        if (_currentMode == CameraMode.Follow)
        {
            targetPos = target.position + followOffset;
            targetRot = Quaternion.LookRotation((target.position + Vector3.up * 0.5f) - targetPos);
            targetFOV = followFieldOfView;
        }
        else
        {
            targetPos = target.position + topDownOffset;
            targetRot = Quaternion.Euler(90f, 0f, 0f);
            targetFOV = topDownFieldOfView;
        }

        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendT));
        transform.position = Vector3.Lerp(_blendStartPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(_blendStartRot, targetRot, t);
        _camera.fieldOfView = Mathf.Lerp(_blendStartFOV, targetFOV, t);

        if (_blendT >= 1f)
            _isTransitioning = false;
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────
    public void ToggleMode()
    {
        SetMode(_currentMode == CameraMode.Follow ? CameraMode.TopDown : CameraMode.Follow);
    }

    public void SetMode(CameraMode mode)
    {
        if (mode == _currentMode && !_isTransitioning) return;

        _currentMode = mode;
        _blendStartPos = transform.position;
        _blendStartRot = transform.rotation;
        _blendStartFOV = _camera.fieldOfView;
        _blendT = 0f;
        _isTransitioning = true;
    }
}

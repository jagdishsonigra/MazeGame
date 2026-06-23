using UnityEngine;

public class SpikeTrapObstacle : ObstacleBase
{
    [Header("References")]
    [SerializeField] private Transform spikeMesh;

    [Header("Positions")]
    [SerializeField] private Transform retractedPoint; // A
    [SerializeField] private Transform extendedPoint;  // B

    [Header("Settings")]
    [SerializeField] private float retractedDuration = 2f;

    private Collider _collider;
    private bool _isRetracted;
    private float _timer;

    protected override void Start()
    {
        base.Start();

        _collider = GetComponent<Collider>();

        if (spikeMesh != null && extendedPoint != null)
        {
            spikeMesh.position = extendedPoint.position;
        }

        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!_isRetracted)
            return;

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            ExtendSpike();
        }
    }

    protected override void OnPlayerTriggered(Collider other)
    {
        base.OnPlayerTriggered(other);

        if (_isRetracted)
            return;

        RetractSpike();
    }

    private void RetractSpike()
    {
        _isRetracted = true;
        _timer = retractedDuration;

        if (spikeMesh != null && retractedPoint != null)
        {
            spikeMesh.position = retractedPoint.position;
        }

        if (_collider != null)
        {
            _collider.enabled = false;
        }
    }

    private void ExtendSpike()
    {
        _isRetracted = false;

        if (spikeMesh != null && extendedPoint != null)
        {
            spikeMesh.position = extendedPoint.position;
        }

        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }
}
using UnityEngine;

/// <summary>
/// Obstacle Type 1: Rotating Blade / Spinner.
/// Rotates around a configurable axis at a set speed.
/// Player collision applies a penalty and launches the ball.
/// </summary>
public class RotatingObstacle : ObstacleBase
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f; // degrees per second
    [SerializeField] private float impactForce = 8f;    // force applied to player on hit

    protected override void Update()
    {
        base.Update();
        // Rotate the obstacle every frame regardless of game pause
        // (Time.timeScale handles pause automatically)
        transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime));
    }

    protected override void OnPlayerHit(Collision collision)
    {
        // Knock the ball away from the obstacle
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        Vector3 knockDir = (collision.transform.position - transform.position).normalized;
        knockDir.y = 0.5f; // slight upward component
        playerRb.AddForce(knockDir * impactForce, ForceMode.Impulse);
    }
}

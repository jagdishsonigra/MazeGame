using UnityEngine;

/// <summary>
/// Placed below the maze floor or around out-of-bounds areas.
/// If the player falls into this zone, the game is lost (one-life system).
/// </summary>
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        GameManager.Instance.TriggerLose();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        UnityEditor.Handles.Label(transform.position + Vector3.up, "DEATH ZONE");
    }
#endif
}

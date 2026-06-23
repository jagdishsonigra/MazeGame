using UnityEngine;

/// <summary>
/// Marks the Start Zone. On game start the player is placed here.
/// This component broadcasts the player spawn position to PlayerController.
/// </summary>
public class StartZone : MonoBehaviour
{
    private void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetSpawnPoint(transform.position, transform.rotation);
            player.TeleportTo(transform.position, transform.rotation);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "START");
    }
#endif
}

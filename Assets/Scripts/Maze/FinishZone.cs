using UnityEngine;

/// <summary>
/// Marks the Finish / End Zone of the maze.
/// When the player enters this trigger, the game is won.
/// Requires a Collider set to "Is Trigger" on this GameObject.
/// </summary>
public class FinishZone : MonoBehaviour
{
    [SerializeField] private ParticleSystem celebrationEffect;  // optional VFX

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        _triggered = true;
        celebrationEffect?.Play();
        GameManager.Instance.TriggerWin();
    }

    private void OnEnable()
    {
        _triggered = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.2f, 0.45f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "FINISH");
    }
#endif
}

using UnityEngine;

/// <summary>
/// Trigger on the goal object. When the player reaches it, notifies the game manager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MazeGoalTrigger : MonoBehaviour
{
    private bool reached;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (reached) return;
        if (!collision.CompareTag("Player")) return;

        reached = true;

        MegaMazeGameManager manager = FindFirstObjectByType<MegaMazeGameManager>();
        if (manager != null)
            manager.OnGoalReached();
    }

    public void Reset()
    {
        reached = false;
    }
}

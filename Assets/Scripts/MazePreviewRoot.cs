using UnityEngine;

/// <summary>
/// Attached to the __MazePreview__ GameObject.
/// Destroys the entire preview (markers, path dots) when the game starts,
/// so nothing from the scene preview appears in the actual game.
/// </summary>
public class MazePreviewRoot : MonoBehaviour
{
    private void Awake()
    {
        Destroy(gameObject);
    }
}

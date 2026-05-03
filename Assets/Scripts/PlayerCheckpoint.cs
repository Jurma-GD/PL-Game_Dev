using UnityEngine;

/// <summary>
/// Allows the player to place a single checkpoint (C key) with a 5-minute cooldown.
/// On reshuffle, the game manager teleports the player to the checkpoint safely.
/// </summary>
public class PlayerCheckpoint : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D playerRb;
    public GameObject checkpointMarkerPrefab;

    [Header("Cooldown")]
    public float placementCooldown = 300f; // 5 minutes

    private bool hasCheckpoint;
    private Vector3 checkpointPosition;
    private GameObject activeMarker;
    private float cooldownTimer;

    // Sector the checkpoint was placed in
    private int checkpointSectorIndex = -1;
    private Vector2Int checkpointSectorCoord;

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (cooldownTimer > 0f)
            {
                int mins = Mathf.FloorToInt(cooldownTimer / 60f);
                int secs = Mathf.FloorToInt(cooldownTimer % 60f);
                Debug.Log($"[Checkpoint] On cooldown. Wait {mins:00}:{secs:00}.");
                return;
            }

            PlaceCheckpoint();
        }
    }

    public void PlaceCheckpoint()
    {
        if (activeMarker != null)
            Destroy(activeMarker);

        checkpointPosition = transform.position;
        hasCheckpoint = true;
        cooldownTimer = placementCooldown;

        // Store which sector this checkpoint is in
        MegaMazeGenerator gen = FindFirstObjectByType<MegaMazeGenerator>();
        if (gen != null)
        {
            checkpointSectorCoord = gen.WorldToSectorCoord(checkpointPosition);
            checkpointSectorIndex = gen.GetSectorIndex(checkpointSectorCoord.x, checkpointSectorCoord.y);
        }

        if (checkpointMarkerPrefab != null)
        {
            activeMarker = Instantiate(checkpointMarkerPrefab, checkpointPosition, Quaternion.identity);
        }
        else
        {
            activeMarker = new GameObject("CheckpointMarker");
            activeMarker.transform.position = checkpointPosition;
            SpriteRenderer sr = activeMarker.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(0f, 0.8f, 1f, 0.7f);
            sr.sortingOrder = 8;
            activeMarker.transform.localScale = Vector3.one * 0.6f;
        }

        Debug.Log("[Checkpoint] Placed at " + checkpointPosition);
    }

    /// <summary>
    /// Called by the game manager when sectors rearrange.
    /// Teleports the player to the checkpoint position.
    /// The game manager should call FindNearestPassage on the generator
    /// to get a safe position before calling this.
    /// </summary>
    public bool TeleportToCheckpointOnReshuffle()
    {
        if (!hasCheckpoint) return false;

        // The game manager will adjust checkpointPosition to a safe spot
        // before calling this, via SafeTeleportPosition.
        if (playerRb != null)
        {
            playerRb.position = checkpointPosition;
            playerRb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = checkpointPosition;
        }

        Debug.Log("[Checkpoint] Teleported to checkpoint after reshuffle.");
        return true;
    }

    /// <summary>
    /// Update the stored checkpoint position to a safe location.
    /// Called by the game manager before teleporting.
    /// </summary>
    public void AdjustCheckpointPosition(Vector3 safePosition)
    {
        checkpointPosition = safePosition;

        // Move the marker too
        if (activeMarker != null)
            activeMarker.transform.position = safePosition;
    }

    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = Vector3.zero;
        cooldownTimer = 0f;

        if (activeMarker != null)
        {
            Destroy(activeMarker);
            activeMarker = null;
        }
    }

    public bool HasCheckpoint => hasCheckpoint;
    public Vector3 CheckpointPosition => checkpointPosition;
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);
    public int CheckpointSectorIndex => checkpointSectorIndex;

    /// <summary>
    /// After a reshuffle, find where the checkpoint's sector ended up
    /// and return the spawn position of that sector.
    /// </summary>
    public Vector3 GetPostReshufflePosition(MegaMazeGenerator gen)
    {
        if (!hasCheckpoint || checkpointSectorIndex < 0) return checkpointPosition;

        // Find where this sector index is now in the grid
        for (int col = 0; col < gen.gridCols; col++)
        {
            for (int row = 0; row < gen.gridRows; row++)
            {
                if (gen.GetSectorIndex(col, row) == checkpointSectorIndex)
                {
                    // Return spawn position of this sector
                    return gen.GetSectorSpawnPosition(col, row);
                }
            }
        }

        // Fallback
        return gen.FindNearestPassage(checkpointPosition);
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

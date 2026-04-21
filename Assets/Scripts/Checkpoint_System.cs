using UnityEngine;
using System.Collections.Generic;

public class Checkpoint_System : MonoBehaviour
{
    // --- Data Model ---
    private struct CheckpointData
    {
        public Vector2 position;
        public int health;      // only meaningful when restoreHealth == true
        public bool isManual;   // true = manual save, false = auto save
    }

    // --- Inspector Fields ---
    [Header("References")]
    public Rigidbody2D playerRb;
    public PlayerHealth playerHealth;
    public GameObject checkpointIndicator;
    public SpriteRenderer playerSprite;

    [Header("Elevation")]
    public Elevation_Entry elevationEntry;
    public Elevation_Exit elevationExit;

    [Header("Input")]
    public string checkpointKey = "q";
    public string teleportKey = "e";

    [Header("Timing")]
    public float autoCheckpointInterval = 10f;
    public float teleportCooldown = 2f;

    [Header("Options")]
    public bool restoreHealth = false;

    // --- Private State ---
    private bool hasCheckpoint;
    private float cooldownRemaining;
    private CheckpointData checkpointData;
    private Coroutine autoCheckpointCoroutine;

    // --- Lifecycle ---
    private void Start()
    {
        // Req 5.3: hide indicator on scene load when no checkpoint exists
        if (checkpointIndicator != null && !hasCheckpoint)
            checkpointIndicator.SetActive(false);

        autoCheckpointCoroutine = StartCoroutine(AutoCheckpointCoroutine());
    }

    private void OnEnable()
    {
        // Resume auto-checkpoint when the player becomes active (Req 2.3)
        if (autoCheckpointCoroutine == null)
            autoCheckpointCoroutine = StartCoroutine(AutoCheckpointCoroutine());
    }

    private void OnDisable()
    {
        // Suspend auto-checkpoint while the player is inactive (Req 2.3)
        if (autoCheckpointCoroutine != null)
        {
            StopCoroutine(autoCheckpointCoroutine);
            autoCheckpointCoroutine = null;
        }
    }

    private void Update()
    {
        // Req 6.1 / 6.3: checkpoint key input with validation
        try
        {
            if (Input.GetKeyDown(checkpointKey))
                SaveCheckpoint(isManual: true);  // Req 1.1, 1.4
        }
        catch (System.ArgumentException)
        {
            Debug.LogError($"[Checkpoint_System] Invalid checkpoint key: '{checkpointKey}'. " +
                           "Please set a valid Unity key name in the Inspector.");
        }

        // Req 6.2 / 6.3: teleport key input with validation
        try
        {
            if (Input.GetKeyDown(teleportKey))
                TryTeleport();  // Req 3.1
        }
        catch (System.ArgumentException)
        {
            Debug.LogError($"[Checkpoint_System] Invalid teleport key: '{teleportKey}'. " +
                           "Please set a valid Unity key name in the Inspector.");
        }

        // Req 3.3: tick down cooldown each frame, clamp to zero
        if (cooldownRemaining > 0f)
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
    }

    // --- Core Logic Stubs ---
    private void SaveCheckpoint(bool isManual)
    {
        checkpointData.position = playerRb.position;

        if (restoreHealth)
            checkpointData.health = playerHealth.health;

        checkpointData.isManual = isManual;
        hasCheckpoint = true;

        if (checkpointIndicator != null)
        {
            checkpointIndicator.transform.position = checkpointData.position;
            checkpointIndicator.SetActive(true);
        }
    }

    private void TryTeleport()
    {
        if (!hasCheckpoint) return;          // Req 3.5: no checkpoint saved yet
        if (cooldownRemaining > 0f) return;  // Req 3.4 / 3.6: cooldown active

        // Req 3.1: move player to saved position
        playerRb.position = checkpointData.position;
        // Req 3.2: zero out velocity
        playerRb.linearVelocity = Vector2.zero;

        // Req 4.2 / 4.3: restore health only when toggle is on
        if (restoreHealth)
            playerHealth.health = checkpointData.health;

        // Req 3.3: start cooldown
        cooldownRemaining = teleportCooldown;

        // Req 5.2: hide indicator after teleport
        if (checkpointIndicator != null)
            checkpointIndicator.SetActive(false);

        // Check if teleport destination is inside elevation and apply correct collider state
        CheckElevationStateAfterTeleport();
    }

    // --- Auto-Checkpoint Coroutine ---

    // Req 2.1: records an auto-checkpoint every autoCheckpointInterval seconds.
    // Req 2.2: skips the save if a manual checkpoint was placed more recently.
    // Req 2.3: coroutine is started/stopped via OnEnable/OnDisable.
    private System.Collections.IEnumerator AutoCheckpointCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoCheckpointInterval);

            // Only auto-save when no manual checkpoint is active (Req 2.2)
            if (!checkpointData.isManual)
                SaveCheckpoint(isManual: false);
        }
    }

    // Check if the teleport destination is inside elevation and apply correct collider/sorting state
    private void CheckElevationStateAfterTeleport()
    {
        if (elevationEntry == null && elevationExit == null) return;

        // Use a small overlap circle to check if player landed inside the elevation trigger
        Collider2D entryCollider = elevationEntry != null ? elevationEntry.GetComponent<Collider2D>() : null;
        
        bool insideElevation = false;
        if (entryCollider != null)
        {
            // Check all colliders at the player's position, including triggers
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;
            List<Collider2D> hits = new List<Collider2D>();
            Physics2D.OverlapPoint(playerRb.position, filter, hits);
            foreach (var hit in hits)
            {
                if (hit == entryCollider)
                {
                    insideElevation = true;
                    break;
                }
            }
        }

        if (insideElevation)
        {
            if (elevationEntry != null)
            {
                foreach (Collider2D mountain in elevationEntry.mountainColliders)
                    mountain.enabled = false;
                foreach (Collider2D boundary in elevationEntry.boundaryColliders)
                    boundary.enabled = true;
            }
            if (playerSprite != null) playerSprite.sortingOrder = 15;
        }
        else
        {
            if (elevationExit != null)
            {
                foreach (Collider2D mountain in elevationExit.mountainColliders)
                    mountain.enabled = true;
                foreach (Collider2D boundary in elevationExit.boundaryColliders)
                    boundary.enabled = false;
            }
            if (playerSprite != null) playerSprite.sortingOrder = 10;
        }
    }
}

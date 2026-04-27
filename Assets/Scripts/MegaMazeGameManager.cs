using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Orchestrates the mega-maze: initialization, 5-minute rearrangement timer,
/// cinematic sleep transition, spawn/exit placement, and UI.
/// </summary>
public class MegaMazeGameManager : MonoBehaviour
{
    [Header("Core References")]
    public MegaMazeGenerator mazeGenerator;
    public Transform playerTransform;
    public Rigidbody2D playerRb;
    public PlayerCheckpoint playerCheckpoint;
    public PlayerMovement playerMovement;
    public CameraFollow cameraFollow;
    public FogOfWar fogOfWar;

    [Header("Goal")]
    public GameObject goalObject;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sectorText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI controlsText;
    public TextMeshProUGUI interactHintText;
    public GameObject winPanel;
    public GameObject reshuffleWarningPanel;

    [Header("Lantern")]
    public GameObject lanternPrefab;

    [Header("Knife")]
    public GameObject knifePrefab;
    public int knifeForceSpawnAfterReshuffles = 5;

    [Header("Timing")]
    public float reshuffleInterval = 300f;
    public float warningTime = 30f;

    [Header("Sleep Transition")]
    public float sleepZoomSize = 2f;
    public float sleepDarkenDuration = 1.5f;
    public float sleepShakeDuration = 2f;
    public float sleepShakeIntensity = 0.3f;
    public float sleepBlackDuration = 1f;
    public float wakeUpDuration = 1.5f;

    private float reshuffleTimer;
    private bool isGameActive;
    private bool goalReached;
    private bool isTransitioning;
    private Vector2Int currentSpawnSector;
    private Vector2Int currentExitSector;
    private int reshuffleCount;
    private bool mazeInitialized;
    private Vector3 originalSpawnPosition;

    // Stuck detection
    private Vector3 lastPositionCheck;
    private float stuckTimer;
    private float stuckCheckInterval = 2f; // check every 2 seconds
    private float stuckThreshold = 30f;    // stuck after 30 seconds of barely moving
    private float moveThreshold = 1.5f;    // must move less than this to count as stuck
    [HideInInspector] public bool isStuck;
    [HideInInspector] public bool hasKnife;

    // Knife tracking
    private GameObject spawnedKnife;
    private bool knifePickedUp;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (reshuffleWarningPanel != null) reshuffleWarningPanel.SetActive(false);

        if (controlsText != null)
            controlsText.text = "WASD: Move | C: Checkpoint | E: Interact | R: Sacrifice";

        if (interactHintText != null)
            interactHintText.gameObject.SetActive(false);

        InitializeGame();
    }

    private void Update()
    {
        if (!isGameActive || isTransitioning) return;

        reshuffleTimer -= Time.deltaTime;

        // Warning
        if (reshuffleWarningPanel != null)
        {
            bool showWarning = reshuffleTimer <= warningTime && reshuffleTimer > 0f;
            if (reshuffleWarningPanel.activeSelf != showWarning)
                reshuffleWarningPanel.SetActive(showWarning);
        }

        if (reshuffleTimer <= 0f)
        {
            StartCoroutine(SleepTransition());
        }

        // Sacrifice — only if has knife
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning)
        {
            if (hasKnife && isStuck)
                StartCoroutine(SacrificeRespawn());
            else if (hasKnife && !isStuck)
                ShowStatus("You can't bring yourself to use the knife.", 3f);
            else if (!hasKnife)
                ShowStatus("You need something sharp to end it...", 2f);
        }

        // Stuck detection
        if (reshuffleCount > 0 && playerTransform != null && !isStuck)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckInterval)
            {
                float moved = Vector3.Distance(playerTransform.position, lastPositionCheck);
                lastPositionCheck = playerTransform.position;

                if (moved < moveThreshold)
                    stuckTimer = stuckCheckInterval; // keep accumulating
                else
                    stuckTimer = 0f; // reset if they moved

                if (stuckTimer >= stuckThreshold)
                {
                    isStuck = true;
                    if (hasKnife)
                        ShowStatus("You feel trapped. Press R to end it.", 4f);
                    else
                        ShowStatus("You feel trapped. Find something sharp...", 4f);
                }
            }
        }

        UpdateUI();
    }

    private void InitializeGame()
    {
        isGameActive = true;
        goalReached = false;
        isTransitioning = false;
        reshuffleCount = 0;

        if (playerMovement != null)
            playerMovement.canMove = true;

        if (!mazeInitialized)
        {
            mazeInitialized = true;

            // Reset tilemap colors in case Preview tinted them
            if (mazeGenerator.wallTilemap != null)
                mazeGenerator.wallTilemap.color = Color.white;
            if (mazeGenerator.floorTilemap != null)
                mazeGenerator.floorTilemap.color = Color.white;

            // Apply settings from main menu
            mazeGenerator.masterSeed = GameSettings.MazeSeed;
            reshuffleInterval = GameSettings.GetReshuffleInterval();
            AudioListener.volume = GameSettings.Volume;

            mazeGenerator.Initialize();

            currentSpawnSector = new Vector2Int(0, 0);
            currentExitSector = new Vector2Int(mazeGenerator.gridCols - 1, mazeGenerator.gridRows - 1);

            mazeGenerator.GenerateFullMaze();

            // Store the original spawn position for sacrifice respawn
            originalSpawnPosition = mazeGenerator.GetSectorSpawnPosition(currentSpawnSector.x, currentSpawnSector.y);
        }

        PlacePlayerAtSpawn();
        PlaceGoalAtExit();

        if (cameraFollow != null)
        {
            cameraFollow.ResetZoom();
            cameraFollow.SnapToTarget();
        }

        if (fogOfWar != null)
            fogOfWar.RestoreNormalFog();

        reshuffleTimer = reshuffleInterval;

        // Start with opening sequence
        StartCoroutine(OpeningSequence());
    }

    private IEnumerator OpeningSequence()
    {
        // Freeze player briefly
        if (playerMovement != null)
            playerMovement.canMove = false;

        ShowStatus("Where... where am I? I can't see anything...", 3f);
        yield return new WaitForSeconds(3.5f);

        ShowStatus("There's something glowing nearby...", 2.5f);

        // Spawn lantern near the player
        SpawnLantern();

        yield return new WaitForSeconds(1f);

        if (playerMovement != null)
            playerMovement.canMove = true;
    }

    private IEnumerator SacrificeRespawn()
    {
        isTransitioning = true;

        if (playerMovement != null)
            playerMovement.canMove = false;

        ShowStatus("You give up... and find your way back to the start.", 3f);

        // Fade to black
        if (fogOfWar != null)
            fogOfWar.SetFullBlack();

        yield return new WaitForSeconds(1.5f);

        // Clear stuck state and knife
        isStuck = false;
        hasKnife = false;
        stuckTimer = 0f;
        if (playerTransform != null)
            lastPositionCheck = playerTransform.position;

        // Clear checkpoint, teleport to original spawn
        if (playerCheckpoint != null)
            playerCheckpoint.ClearCheckpoint();

        if (playerRb != null)
        {
            playerRb.position = originalSpawnPosition;
            playerRb.linearVelocity = Vector2.zero;
        }
        else if (playerTransform != null)
        {
            playerTransform.position = originalSpawnPosition;
        }

        if (cameraFollow != null)
            cameraFollow.SnapToTarget();

        yield return new WaitForSeconds(0.5f);

        // Fade back in
        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            if (fogOfWar != null)
                fogOfWar.AlphaMultiplier = Mathf.Lerp(2f, 1f, elapsed / 1.5f);
            yield return null;
        }

        if (fogOfWar != null)
            fogOfWar.RestoreNormalFog();

        if (playerMovement != null)
            playerMovement.canMove = true;

        isTransitioning = false;
    }

    private void SpawnLantern()    {
        if (lanternPrefab == null) return;

        // Try adjacent tiles in all 4 directions, pick the first valid passage
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, -1f, 0f),
        };

        Vector3 lanternPos = playerTransform.position;
        foreach (var offset in offsets)
        {
            Vector3 candidate = playerTransform.position + offset;
            Vector3 nearest = mazeGenerator.FindNearestPassage(candidate);
            // Check it's actually adjacent (not snapped far away)
            if (Vector3.Distance(nearest, playerTransform.position) < 2.5f &&
                Vector3.Distance(nearest, playerTransform.position) > 0.1f)
            {
                lanternPos = nearest;
                break;
            }
        }

        GameObject lantern = Instantiate(lanternPrefab, lanternPos, Quaternion.identity);
        lantern.SetActive(true);
    }

    public void ShowLanternPickupMessage()
    {
        ShowStatus("You found a lantern! The darkness recedes...", 3f);
    }

    /// <summary>
    /// The cinematic sleep transition:
    /// 1. Freeze player
    /// 2. Camera zooms in on player
    /// 3. Screen slowly darkens (fog alpha increases)
    /// 4. Screen shakes for 2-3 seconds
    /// 5. Full black for a moment (player "falls asleep")
    /// 6. Reshuffle maze, teleport to checkpoint
    /// 7. Fade back in, camera zooms out, player can move
    /// </summary>
    private IEnumerator SleepTransition()
    {
        isTransitioning = true;
        reshuffleCount++;

        if (reshuffleWarningPanel != null)
            reshuffleWarningPanel.SetActive(false);

        // 1. Freeze player
        if (playerMovement != null)
            playerMovement.canMove = false;

        // 2. Zoom camera in
        if (cameraFollow != null)
            cameraFollow.ZoomTo(sleepZoomSize);

        // 3. Slowly darken the fog over sleepDarkenDuration
        float elapsed = 0f;
        while (elapsed < sleepDarkenDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sleepDarkenDuration;
            if (fogOfWar != null)
                fogOfWar.AlphaMultiplier = Mathf.Lerp(1f, 2f, t);
            yield return null;
        }

        // 4. Screen shake
        if (cameraFollow != null)
            cameraFollow.Shake(sleepShakeDuration, sleepShakeIntensity);

        // Show "falling asleep" text
        ShowStatus("Your vision blurs... you can't stay awake...", sleepShakeDuration);

        yield return new WaitForSeconds(sleepShakeDuration);

        // 5. Full black
        if (fogOfWar != null)
            fogOfWar.SetFullBlack();

        yield return new WaitForSeconds(sleepBlackDuration);

        // 6. Reshuffle maze while screen is black
        Vector2Int newSpawn, newExit;
        mazeGenerator.RearrangeSectors(out newSpawn, out newExit);
        currentSpawnSector = newSpawn;
        currentExitSector = newExit;

        // Teleport to checkpoint or spawn
        bool teleported = false;
        if (playerCheckpoint != null && playerCheckpoint.HasCheckpoint)
        {
            // Find the nearest safe passage to the checkpoint position
            Vector3 safePos = mazeGenerator.FindNearestPassage(playerCheckpoint.CheckpointPosition);
            playerCheckpoint.AdjustCheckpointPosition(safePos);
            teleported = playerCheckpoint.TeleportToCheckpointOnReshuffle();
        }
        if (!teleported)
            PlacePlayerAtSpawn();

        PlaceGoalAtExit();

        // Reshuffle knife if not yet picked up
        if (!knifePickedUp)
            RespawnKnife(reshuffleCount >= knifeForceSpawnAfterReshuffles);
        if (cameraFollow != null)
            cameraFollow.SnapToTarget();

        yield return new WaitForSeconds(0.3f);

        // 7. Wake up: fade back in
        elapsed = 0f;
        while (elapsed < wakeUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wakeUpDuration;
            if (fogOfWar != null)
                fogOfWar.AlphaMultiplier = Mathf.Lerp(2f, 1f, t);
            yield return null;
        }

        if (fogOfWar != null)
            fogOfWar.RestoreNormalFog();

        // Zoom camera back out
        if (cameraFollow != null)
            cameraFollow.ResetZoom();

        // Unfreeze player
        if (playerMovement != null)
            playerMovement.canMove = true;

        reshuffleTimer = reshuffleInterval;
        isTransitioning = false;
        isStuck = false;
        stuckTimer = 0f;
        if (playerTransform != null)
            lastPositionCheck = playerTransform.position;

        string cpMsg = teleported
            ? "You wake at your checkpoint."
            : "You wake in an unfamiliar place...";
        ShowStatus($"The maze has shifted. (#{reshuffleCount}) {cpMsg}", 5f);
    }

    private void PlacePlayerAtSpawn()
    {
        Vector3 spawnPos = mazeGenerator.GetSectorSpawnPosition(currentSpawnSector.x, currentSpawnSector.y);
        if (playerRb != null)
        {
            playerRb.position = spawnPos;
            playerRb.linearVelocity = Vector2.zero;
        }
        else if (playerTransform != null)
        {
            playerTransform.position = spawnPos;
        }
    }

    private void PlaceGoalAtExit()
    {
        if (goalObject == null) return;

        Vector3 exitPos = mazeGenerator.GetSectorExitPosition(currentExitSector.x, currentExitSector.y);
        exitPos = mazeGenerator.FindNearestPassage(exitPos);
        goalObject.transform.position = exitPos;
        goalObject.SetActive(true);

        MazeGoalTrigger trigger = goalObject.GetComponent<MazeGoalTrigger>();
        if (trigger != null)
            trigger.Reset();
    }

    public void OnGoalReached()
    {
        if (goalReached || isTransitioning) return;
        goalReached = true;
        isGameActive = false;

        if (winPanel != null) winPanel.SetActive(true);
    }

    public void RestartGame()
    {
        if (winPanel != null) winPanel.SetActive(false);
        MazeInteractable.ClearAllVisited();
        if (playerCheckpoint != null)
            playerCheckpoint.ClearCheckpoint();

        // Reset knife
        if (spawnedKnife != null) Destroy(spawnedKnife);
        spawnedKnife = null;
        knifePickedUp = false;
        hasKnife = false;

        InitializeGame();
    }

    private void RespawnKnife(bool forceNearPlayer)
    {
        if (knifePrefab == null) return;

        // Destroy old knife if still in scene
        if (spawnedKnife != null) Destroy(spawnedKnife);

        Vector3 knifePos;

        if (forceNearPlayer)
        {
            // Spawn 4-8 tiles away from player in a random direction
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(5f, 0f, 0f), new Vector3(-5f, 0f, 0f),
                new Vector3(0f, 5f, 0f), new Vector3(0f, -5f, 0f),
                new Vector3(4f, 4f, 0f), new Vector3(-4f, 4f, 0f),
            };
            Vector3 chosen = offsets[Random.Range(0, offsets.Length)];
            knifePos = mazeGenerator.FindNearestPassage(playerTransform.position + chosen);
        }
        else
        {
            // Random passage somewhere in the maze, not too close to player
            int attempts = 20;
            knifePos = mazeGenerator.GetSectorSpawnPosition(
                Random.Range(0, mazeGenerator.gridCols),
                Random.Range(0, mazeGenerator.gridRows));

            for (int i = 0; i < attempts; i++)
            {
                Vector3 candidate = mazeGenerator.GetSectorSpawnPosition(
                    Random.Range(0, mazeGenerator.gridCols),
                    Random.Range(0, mazeGenerator.gridRows));
                candidate = mazeGenerator.FindNearestPassage(candidate);

                // Must be at least 15 units away from player
                if (Vector3.Distance(candidate, playerTransform.position) > 15f)
                {
                    knifePos = candidate;
                    break;
                }
            }
        }

        spawnedKnife = Instantiate(knifePrefab, knifePos, Quaternion.identity);
        spawnedKnife.SetActive(true);

        // Hook into pickup event to track when player grabs it
        KnifePickup pickup = spawnedKnife.GetComponent<KnifePickup>();
        if (pickup != null)
            pickup.OnPickedUp += () => { knifePickedUp = true; spawnedKnife = null; };
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(reshuffleTimer / 60f);
            int seconds = Mathf.FloorToInt(reshuffleTimer % 60f);
            timerText.text = $"Reshuffle: {minutes:00}:{seconds:00}";

            timerText.color = reshuffleTimer <= warningTime
                ? new Color(1f, 0.3f, 0.3f)
                : Color.white;
        }

        if (sectorText != null && playerTransform != null)
        {
            Vector2Int sector = mazeGenerator.WorldToSectorCoord(playerTransform.position);
            int sectorIdx = mazeGenerator.GetSectorIndex(sector.x, sector.y);
            sectorText.text = $"Sector: {sector.x},{sector.y} (#{sectorIdx})";
        }
    }

    private void ShowStatus(string message, float duration)
    {
        if (statusText != null)
            StartCoroutine(ShowStatusCoroutine(message, duration));
    }

    public void ShowStatusPublic(string message, float duration) => ShowStatus(message, duration);

    private IEnumerator ShowStatusCoroutine(string message, float duration)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }
}

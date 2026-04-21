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

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (reshuffleWarningPanel != null) reshuffleWarningPanel.SetActive(false);

        if (controlsText != null)
            controlsText.text = "WASD: Move | C: Checkpoint | E: Interact";

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

        mazeGenerator.Initialize();

        currentSpawnSector = new Vector2Int(0, 0);
        currentExitSector = new Vector2Int(mazeGenerator.gridCols - 1, mazeGenerator.gridRows - 1);

        mazeGenerator.GenerateFullMaze();

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

    private void SpawnLantern()
    {
        if (lanternPrefab == null) return;

        // Place lantern 2 tiles to the right of the player (find nearest passage)
        Vector3 lanternPos = playerTransform.position + new Vector3(2f, 0f, 0f);
        lanternPos = mazeGenerator.FindNearestPassage(lanternPos);

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

        // Snap camera to new position while still black
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
        InitializeGame();
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

    private IEnumerator ShowStatusCoroutine(string message, float duration)
    {
        statusText.text = message;
        statusText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }
}

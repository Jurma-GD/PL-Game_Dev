using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Editor utility: Menu > Maze > Build Mega Maze Scene
/// Creates the full 16x16 sector labyrinth with fog, interactions, and sleep transition.
/// </summary>
public class MazeSceneBuilder
{
    [MenuItem("Maze/Preview Maze In Scene")]
    public static void PreviewMazeInScene()
    {
        MegaMazeGenerator gen = Object.FindFirstObjectByType<MegaMazeGenerator>();
        if (gen == null)
        {
            Debug.LogError("[MazeSceneBuilder] No MegaMazeGenerator found in scene.");
            return;
        }

        gen.Initialize();
        gen.GenerateFullMaze();

        // --- Remove old preview root ---
        GameObject oldRoot = GameObject.Find("__MazePreview__");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        // --- Create preview root (destroyed on Play via MazePreviewRoot component) ---
        GameObject previewRoot = new GameObject("__MazePreview__");
        previewRoot.AddComponent<MazePreviewRoot>();

        // --- Spawn marker (green) ---
        Vector3 spawnPos = gen.GetSectorSpawnPosition(0, 0);
        GameObject spawnMarker = new GameObject("Spawn");
        spawnMarker.transform.SetParent(previewRoot.transform);
        spawnMarker.transform.position = spawnPos;
        spawnMarker.transform.localScale = Vector3.one * 1.2f;
        var spawnSR = spawnMarker.AddComponent<SpriteRenderer>();
        spawnSR.sprite = CreateSquareSprite();
        spawnSR.color = new Color(0f, 1f, 0.3f, 0.9f);
        spawnSR.sortingOrder = 20;

        // --- Exit marker (yellow diamond) ---
        Vector3 exitPos = gen.GetSectorExitPosition(gen.gridCols - 1, gen.gridRows - 1);
        exitPos = gen.FindNearestPassage(exitPos);
        GameObject exitMarker = new GameObject("Exit");
        exitMarker.transform.SetParent(previewRoot.transform);
        exitMarker.transform.position = exitPos;
        exitMarker.transform.localScale = Vector3.one * 1.4f;
        var exitSR = exitMarker.AddComponent<SpriteRenderer>();
        exitSR.sprite = CreateDiamondSprite();
        exitSR.color = new Color(1f, 0.85f, 0f, 1f);
        exitSR.sortingOrder = 20;

        // --- Interactable pocket previews (cyan dots) ---
        // Re-run pocket generation to get positions
        int pocketCount = 0;
        for (int col = 0; col < gen.gridCols; col++)
        {
            for (int row = 0; row < gen.gridRows; row++)
            {
                int sectorIndex = gen.GetSectorIndex(col, row);
                int seed = sectorIndex; // approximate — pockets use sectorSeed+9999
                UnityEngine.Random.InitState(seed + 9999);
                int count = UnityEngine.Random.Range(1, 4);
                int sectorTilesX = gen.sectorCellsX * 2 + 1;
                int sectorTilesY = gen.sectorCellsY * 2 + 1;
                int baseX = col * sectorTilesX;
                int baseY = row * sectorTilesY;

                for (int p = 0; p < count; p++)
                {
                    int cx = baseX + 3 + UnityEngine.Random.Range(0, sectorTilesX - 6);
                    int cy = baseY + 3 + UnityEngine.Random.Range(0, sectorTilesY - 6);
                    if ((cx - baseX) % 2 == 0) cx++;
                    if ((cy - baseY) % 2 == 0) cy++;
                    cx = Mathf.Clamp(cx, baseX + 2, baseX + sectorTilesX - 3);
                    cy = Mathf.Clamp(cy, baseY + 2, baseY + sectorTilesY - 3);

                    Vector3 pocketWorld = gen.wallTilemap != null
                        ? gen.wallTilemap.CellToWorld(new Vector3Int(cx, cy, 0)) + gen.wallTilemap.cellSize / 2f
                        : new Vector3(cx + 0.5f, cy + 0.5f, 0f);

                    GameObject dot = new GameObject($"Interactable_{pocketCount}");
                    dot.transform.SetParent(previewRoot.transform);
                    dot.transform.position = pocketWorld;
                    dot.transform.localScale = Vector3.one * 0.7f;
                    var dotSR = dot.AddComponent<SpriteRenderer>();
                    dotSR.sprite = CreateSquareSprite();
                    dotSR.color = new Color(0f, 0.9f, 1f, 0.85f);
                    dotSR.sortingOrder = 20;
                    pocketCount++;
                }
            }
        }

        // --- BFS path overlay (white dots along shortest path) ---
        var path = GetBFSPath(gen, spawnPos, exitPos);
        if (path != null)
        {
            for (int i = 0; i < path.Count; i += 3) // every 3rd tile to avoid clutter
            {
                GameObject pathDot = new GameObject($"Path_{i}");
                pathDot.transform.SetParent(previewRoot.transform);
                Vector3 worldPos = gen.wallTilemap != null
                    ? gen.wallTilemap.CellToWorld(new Vector3Int(path[i].x, path[i].y, 0)) + gen.wallTilemap.cellSize / 2f
                    : new Vector3(path[i].x + 0.5f, path[i].y + 0.5f, 0f);
                pathDot.transform.position = worldPos;
                pathDot.transform.localScale = Vector3.one * 0.3f;
                var pathSR = pathDot.AddComponent<SpriteRenderer>();
                pathSR.sprite = CreateSquareSprite();
                float t = (float)i / path.Count;
                pathSR.color = Color.Lerp(new Color(0f, 1f, 0.3f, 0.7f), new Color(1f, 0.85f, 0f, 0.7f), t);
                pathSR.sortingOrder = 19;
            }
            Debug.Log($"[MazePreview] Exit at {exitPos}. Spawn at {spawnPos}. BFS path: ~{path.Count} tiles ({path.Count} world units). Interactables: {pocketCount}");
        }
        else
        {
            Debug.Log($"[MazePreview] Exit at {exitPos}. Spawn at {spawnPos}. No direct BFS path found. Interactables: {pocketCount}");
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static List<Vector2Int> GetBFSPath(MegaMazeGenerator gen, Vector3 spawnWorld, Vector3 exitWorld)
    {
        if (gen.wallTilemap == null || gen.floorTilemap == null) return null;

        Vector3Int startCell = gen.wallTilemap.WorldToCell(spawnWorld);
        Vector3Int endCell = gen.wallTilemap.WorldToCell(exitWorld);

        int w = gen.totalTilesX;
        int h = gen.totalTilesY;

        var visited = new bool[w, h];
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(startCell.x, startCell.y);
        var end = new Vector2Int(endCell.x, endCell.y);

        queue.Enqueue(start);
        visited[start.x, start.y] = true;
        prev[start] = start;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == end)
            {
                // Reconstruct path
                var path = new List<Vector2Int>();
                var node = end;
                while (node != start)
                {
                    path.Add(node);
                    node = prev[node];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + dx[d];
                int ny = cur.y + dy[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited[nx, ny]) continue;
                if (gen.floorTilemap.GetTile(new Vector3Int(nx, ny, 0)) == null) continue;

                var next = new Vector2Int(nx, ny);
                visited[nx, ny] = true;
                prev[next] = cur;
                queue.Enqueue(next);
            }
        }
        return null;
    }

    [MenuItem("Maze/Clear Maze")]
    public static void ClearMaze()
    {
        MegaMazeGenerator gen = Object.FindFirstObjectByType<MegaMazeGenerator>();
        if (gen == null) return;

        if (gen.wallTilemap != null)
        {
            gen.wallTilemap.ClearAllTiles();
            gen.wallTilemap.color = Color.white; // restore color for game
        }
        if (gen.floorTilemap != null)
        {
            gen.floorTilemap.ClearAllTiles();
            gen.floorTilemap.color = Color.white; // restore color for game
        }

        // Remove preview markers
        var preview = GameObject.Find("__MazePreview__");
        if (preview != null) Object.DestroyImmediate(preview);
        // Legacy cleanup
        var exit = GameObject.Find("__PreviewExit__");
        if (exit != null) Object.DestroyImmediate(exit);
        var spawn = GameObject.Find("__PreviewSpawn__");
        if (spawn != null) Object.DestroyImmediate(spawn);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MazeSceneBuilder] Maze cleared, colors restored.");
    }

    [MenuItem("Maze/Build Mega Maze Scene")]
    public static void BuildMegaMazeScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight != null) Object.DestroyImmediate(dirLight);

        // ============================================================
        // 1. CAMERA — zoomed in close to the player
        // ============================================================
        Camera mainCam = Camera.main;
        CameraFollow cameraFollow = null;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 3.5f; // very tight zoom
            mainCam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            mainCam.transform.position = new Vector3(0f, 0f, -10f);

            cameraFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            cameraFollow.smoothSpeed = 12f;
            cameraFollow.defaultOrthoSize = 3.5f;
        }

        // ============================================================
        // 2. GRID + TILEMAPS
        // ============================================================
        GameObject gridGO = new GameObject("Grid");
        Grid grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 1f);

        GameObject floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(gridGO.transform);
        Tilemap floorTilemap = floorGO.AddComponent<Tilemap>();
        TilemapRenderer floorRenderer = floorGO.AddComponent<TilemapRenderer>();
        floorRenderer.sortingOrder = 0;

        GameObject wallGO = new GameObject("Walls");
        wallGO.transform.SetParent(gridGO.transform);
        Tilemap wallTilemap = wallGO.AddComponent<Tilemap>();
        TilemapRenderer wallRenderer = wallGO.AddComponent<TilemapRenderer>();
        wallRenderer.sortingOrder = 1;

        TilemapCollider2D wallCollider = wallGO.AddComponent<TilemapCollider2D>();
        Rigidbody2D wallRb = wallGO.AddComponent<Rigidbody2D>();
        wallRb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D compositeCollider = wallGO.AddComponent<CompositeCollider2D>();
        wallCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        // ============================================================
        // 3. LOAD TILES
        // ============================================================
        TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Tiles/Tilemap_Elevation_0.asset");
        TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Tiles/Tilemap_Flat_0.asset");

        // ============================================================
        // 4. INTERACTABLE TEMPLATE
        // ============================================================
        GameObject interactableTemplate = CreateInteractableTemplate();

        // ============================================================
        // 4b. LANTERN TEMPLATE
        // ============================================================
        GameObject lanternTemplate = CreateLanternTemplate();

        // ============================================================
        // 5. PLAYER
        // ============================================================
        GameObject playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(1.5f, 1.5f, 0f);

        SpriteRenderer playerSR = playerGO.AddComponent<SpriteRenderer>();
        playerSR.sortingOrder = 10;

        Sprite idleSprite = null;
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Sprites/Animations/Warrior_Idle.png");
        if (sprites.Length > 0) idleSprite = sprites[0] as Sprite;
        if (idleSprite == null) idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Animations/Warrior_Idle.png");
        if (idleSprite != null) playerSR.sprite = idleSprite;

        playerGO.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

        Rigidbody2D playerRb = playerGO.AddComponent<Rigidbody2D>();
        playerRb.bodyType = RigidbodyType2D.Dynamic;
        playerRb.gravityScale = 0f;
        playerRb.freezeRotation = true;
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D playerCollider = playerGO.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.55f, 0.55f);

        PlayerMovement movement = playerGO.AddComponent<PlayerMovement>();
        movement.rb = playerRb;
        movement.speed = 6f;

        RuntimeAnimatorController animController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player.controller");
        if (animController != null)
        {
            Animator animator = playerGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = animController;
            movement.animator = animator;
        }

        PlayerCheckpoint checkpoint = playerGO.AddComponent<PlayerCheckpoint>();
        checkpoint.playerRb = playerRb;

        if (cameraFollow != null)
            cameraFollow.target = playerGO.transform;

        // ============================================================
        // 6. GOAL
        // ============================================================
        GameObject goalGO = new GameObject("Goal");
        goalGO.transform.position = Vector3.zero;

        SpriteRenderer goalSR = goalGO.AddComponent<SpriteRenderer>();
        goalSR.color = new Color(1f, 0.85f, 0f, 1f);
        goalSR.sortingOrder = 9;
        goalSR.sprite = CreateDiamondSprite();
        goalGO.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        BoxCollider2D goalCollider = goalGO.AddComponent<BoxCollider2D>();
        goalCollider.isTrigger = true;
        goalCollider.size = new Vector2(1.2f, 1.2f);
        goalGO.AddComponent<MazeGoalTrigger>();

        // ============================================================
        // 7. MEGA MAZE GENERATOR
        // ============================================================
        GameObject genGO = new GameObject("MegaMazeGenerator");
        MegaMazeGenerator mazeGen = genGO.AddComponent<MegaMazeGenerator>();
        mazeGen.gridCols = 5;
        mazeGen.gridRows = 5;
        mazeGen.sectorCellsX = 12;
        mazeGen.sectorCellsY = 12;
        mazeGen.wallTilemap = wallTilemap;
        mazeGen.floorTilemap = floorTilemap;
        mazeGen.wallTile = wallTile;
        mazeGen.floorTile = floorTile;
        mazeGen.interactablePrefab = interactableTemplate;

        // ============================================================
        // 8. UI CANVAS
        // ============================================================
        GameObject canvasGO = new GameObject("UI Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Timer
        GameObject timerGO = CreateTMPText("TimerText", canvasGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f),
            new Vector2(350f, 50f), "Reshuffle: 05:00", 30, TextAlignmentOptions.Center);

        // Sector info
        GameObject sectorGO = CreateTMPText("SectorText", canvasGO.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(15f, -15f),
            new Vector2(350f, 50f), "Sector: 0,0", 24, TextAlignmentOptions.Left);

        // Status message
        GameObject statusGO = CreateTMPText("StatusText", canvasGO.transform,
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero,
            new Vector2(800f, 60f), "", 28, TextAlignmentOptions.Center);
        statusGO.SetActive(false);

        // Controls hint
        GameObject controlsGO = CreateTMPText("ControlsText", canvasGO.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f),
            new Vector2(700f, 40f), "WASD: Move | C: Checkpoint | E: Interact", 20, TextAlignmentOptions.Center);
        controlsGO.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.5f);

        // Reshuffle warning
        GameObject warningPanel = CreatePanel("ReshuffleWarning", canvasGO.transform, new Color(0.8f, 0.2f, 0f, 0.15f));
        CreateTMPText("WarningText", warningPanel.transform,
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero,
            new Vector2(600f, 50f), "You feel drowsy... the maze is about to shift...", 26, TextAlignmentOptions.Center);
        warningPanel.SetActive(false);

        // Win panel
        GameObject winPanel = CreatePanel("WinPanel", canvasGO.transform, new Color(0.05f, 0.15f, 0.05f, 0.95f));
        // manager not created yet, will wire button after

        CreateTMPText("WinTitle", winPanel.transform,
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero,
            new Vector2(700f, 80f), "You Escaped the Labyrinth!", 44, TextAlignmentOptions.Center);
        winPanel.SetActive(false);

        // ============================================================
        // 9. FOG OF WAR OVERLAY
        // ============================================================
        // Create a second canvas for the fog (renders above game, below UI)
        GameObject fogCanvasGO = new GameObject("FogCanvas");
        Canvas fogCanvas = fogCanvasGO.AddComponent<Canvas>();
        fogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fogCanvas.sortingOrder = 50; // between game and UI
        fogCanvasGO.AddComponent<CanvasScaler>();

        GameObject fogImageGO = new GameObject("FogImage");
        fogImageGO.transform.SetParent(fogCanvasGO.transform, false);
        RectTransform fogRT = fogImageGO.AddComponent<RectTransform>();
        fogRT.anchorMin = Vector2.zero;
        fogRT.anchorMax = Vector2.one;
        fogRT.offsetMin = Vector2.zero;
        fogRT.offsetMax = Vector2.zero;
        RawImage fogRawImage = fogImageGO.AddComponent<RawImage>();
        fogRawImage.raycastTarget = false;

        // FogOfWar component on a manager object
        GameObject fogManagerGO = new GameObject("FogOfWar");
        FogOfWar fogOfWar = fogManagerGO.AddComponent<FogOfWar>();
        fogOfWar.playerTransform = playerGO.transform;
        fogOfWar.mainCamera = mainCam;
        fogOfWar.viewRadius = 2f; // very tight, lantern expands to 4
        fogOfWar.fogImage = fogRawImage;

        // ============================================================
        // 10. GAME MANAGER
        // ============================================================
        GameObject managerGO = new GameObject("GameManager");
        MegaMazeGameManager manager = managerGO.AddComponent<MegaMazeGameManager>();
        manager.mazeGenerator = mazeGen;
        manager.playerTransform = playerGO.transform;
        manager.playerRb = playerRb;
        manager.playerCheckpoint = checkpoint;
        manager.playerMovement = movement;
        manager.cameraFollow = cameraFollow;
        manager.fogOfWar = fogOfWar;
        manager.goalObject = goalGO;
        manager.lanternPrefab = lanternTemplate;
        manager.reshuffleInterval = 300f;
        manager.warningTime = 30f;

        // Wire UI
        manager.timerText = timerGO.GetComponent<TextMeshProUGUI>();
        manager.sectorText = sectorGO.GetComponent<TextMeshProUGUI>();
        manager.statusText = statusGO.GetComponent<TextMeshProUGUI>();
        manager.controlsText = controlsGO.GetComponent<TextMeshProUGUI>();
        manager.winPanel = winPanel;
        manager.reshuffleWarningPanel = warningPanel;

        // Now create the win panel button with manager reference
        CreateButton("PlayAgainBtn", winPanel.transform,
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero,
            new Vector2(280f, 60f), "Explore Again", manager, "RestartGame");

        // ============================================================
        // 11. EVENT SYSTEM
        // ============================================================
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ============================================================
        // 12. GLOBAL LIGHT 2D
        // ============================================================
        GameObject lightGO = new GameObject("Global Light 2D");
        var light2DType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (light2DType != null)
        {
            var light2D = lightGO.AddComponent(light2DType);
            var lightTypeProp = light2DType.GetField("m_LightType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lightTypeProp != null)
                lightTypeProp.SetValue(light2D, 4);
        }

        // ============================================================
        // SAVE
        // ============================================================
        string scenePath = "Assets/Scenes/MazeScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes)
            if (s.path == scenePath) { found = true; break; }
        if (!found)
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        Debug.Log("[MazeSceneBuilder] Mega Maze scene saved. Press Play!");
    }

    // ---------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------

    private static GameObject CreateInteractableTemplate()
    {
        GameObject template = new GameObject("InteractableTemplate");

        SpriteRenderer structureSR = template.AddComponent<SpriteRenderer>();
        structureSR.sprite = CreateSquareSprite();
        structureSR.color = new Color(0.6f, 0.4f, 0.8f);
        structureSR.sortingOrder = 6;
        template.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        BoxCollider2D col = template.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.5f, 2.5f);

        // Visited marker (child)
        GameObject markerGO = new GameObject("VisitedMarker");
        markerGO.transform.SetParent(template.transform, false);
        markerGO.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        markerGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer markerSR = markerGO.AddComponent<SpriteRenderer>();
        markerSR.sprite = CreateDiamondSprite();
        markerSR.color = Color.cyan;
        markerSR.sortingOrder = 7;
        markerGO.SetActive(false);

        // Dialogue text (child)
        GameObject textGO = new GameObject("DialogueText");
        textGO.transform.SetParent(template.transform, false);
        textGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        TextMeshPro dialogueTMP = textGO.AddComponent<TextMeshPro>();
        dialogueTMP.fontSize = 3f;
        dialogueTMP.alignment = TextAlignmentOptions.Center;
        dialogueTMP.color = Color.white;
        dialogueTMP.sortingOrder = 20;
        dialogueTMP.rectTransform.sizeDelta = new Vector2(6f, 2f);
        textGO.SetActive(false);

        // Prompt text (child) — "[E] Examine"
        GameObject promptGO = new GameObject("PromptText");
        promptGO.transform.SetParent(template.transform, false);
        promptGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        TextMeshPro promptTMP = promptGO.AddComponent<TextMeshPro>();
        promptTMP.fontSize = 2.5f;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = new Color(1f, 1f, 0.6f, 0.9f);
        promptTMP.sortingOrder = 20;
        promptTMP.rectTransform.sizeDelta = new Vector2(4f, 1f);
        promptGO.SetActive(false);

        // MazeInteractable component
        MazeInteractable interactable = template.AddComponent<MazeInteractable>();
        interactable.structureSprite = structureSR;
        interactable.visitedMarker = markerSR;
        interactable.dialogueText = dialogueTMP;
        interactable.promptText = promptTMP;

        template.SetActive(false);
        return template;
    }

    private static GameObject CreateLanternTemplate()
    {
        GameObject template = new GameObject("LanternTemplate");

        // Lantern sprite — yellow/orange glow placeholder
        SpriteRenderer sr = template.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLanternSprite();
        sr.color = new Color(1f, 0.85f, 0.3f, 1f);
        sr.sortingOrder = 8;
        template.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Trigger collider
        BoxCollider2D col = template.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 2f);

        // Prompt text
        GameObject promptGO = new GameObject("PromptText");
        promptGO.transform.SetParent(template.transform, false);
        promptGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        TextMeshPro promptTMP = promptGO.AddComponent<TextMeshPro>();
        promptTMP.fontSize = 2.5f;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = new Color(1f, 1f, 0.6f, 0.9f);
        promptTMP.sortingOrder = 20;
        promptTMP.rectTransform.sizeDelta = new Vector2(5f, 1f);
        promptGO.SetActive(false);

        // LanternPickup component
        LanternPickup lantern = template.AddComponent<LanternPickup>();
        lantern.lanternSprite = sr;
        lantern.promptText = promptTMP;
        lantern.expandedViewRadius = 4f;

        template.SetActive(false);
        return template;
    }

    private static Sprite CreateLanternSprite()
    {
        // A simple lantern shape: rounded rectangle with a handle
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = size / 2f;
        float cy = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = Mathf.Abs(x - cx);
                float dy = Mathf.Abs(y - cy);

                bool isBody = dx < 12 && dy < 16;
                bool isHandle = dx < 6 && y > cy + 14 && y < cy + 22;
                bool isBase = dx < 14 && dy < 4;
                bool isGlow = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) < 20;

                if (isBody || isHandle || isBase)
                    tex.SetPixel(x, y, Color.white);
                else if (isGlow)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.15f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateSquareSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateDiamondSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                tex.SetPixel(x, y, (dx + dy <= 1f) ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static GameObject CreateTMPText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        Vector2 sizeDelta, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        Vector2 sizeDelta, string label,
        MegaMazeGameManager manager, string methodName)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
        btn.colors = colors;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick,
            (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), manager, methodName));
        CreateTMPText("Label", go.transform,
            Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, label, 24, TextAlignmentOptions.Center);
        return go;
    }
}

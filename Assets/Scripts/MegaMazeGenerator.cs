using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Generates a 16x16 grid of sectors. Each sector is a unique maze.
/// Sectors connect to their neighbors through openings at shared borders.
/// Each sector contains 1-3 "pocket" areas for interactable structures.
/// </summary>
public class MegaMazeGenerator : MonoBehaviour
{
    [Header("Sector Grid")]
    public int gridCols = 5;
    public int gridRows = 5;

    [Header("Per-Sector Maze Size (cells)")]
    public int sectorCellsX = 12;
    public int sectorCellsY = 12;

    [Header("Tilemap References")]
    public Tilemap wallTilemap;
    public TileBase wallTile;
    public Tilemap floorTilemap;
    public TileBase floorTile;

    [Header("Interactable")]
    public GameObject interactablePrefab;

    // Derived: tile dimensions per sector (cells*2+1)
    [HideInInspector] public int sectorTilesX;
    [HideInInspector] public int sectorTilesY;
    [HideInInspector] public int totalTilesX;
    [HideInInspector] public int totalTilesY;

    // The full grid: true = passage, false = wall
    private bool[,] grid;

    // Sector arrangement: sectorMap[col,row] = original sector index (0..255)
    // This gets shuffled on rearrange.
    private int[,] sectorMap;

    // Per-sector seed so each sector is unique and reproducible
    private int[] sectorSeeds;

    // Pocket positions per sector (world positions of interactable spots)
    private List<Vector3>[] sectorPockets;

    // Spawned interactable GameObjects
    private List<GameObject> spawnedInteractables = new List<GameObject>();

    public void Initialize()
    {
        sectorTilesX = sectorCellsX * 2 + 1;
        sectorTilesY = sectorCellsY * 2 + 1;
        totalTilesX = sectorTilesX * gridCols;
        totalTilesY = sectorTilesY * gridRows;

        // Create sector seeds
        sectorSeeds = new int[gridCols * gridRows];
        for (int i = 0; i < sectorSeeds.Length; i++)
            sectorSeeds[i] = Random.Range(1, int.MaxValue);

        // Initialize sector map (identity: sector i is at position i)
        sectorMap = new int[gridCols, gridRows];
        for (int c = 0; c < gridCols; c++)
            for (int r = 0; r < gridRows; r++)
                sectorMap[c, r] = r * gridCols + c;

        sectorPockets = new List<Vector3>[gridCols * gridRows];
        for (int i = 0; i < sectorPockets.Length; i++)
            sectorPockets[i] = new List<Vector3>();
    }

    /// <summary>
    /// Build the entire mega-maze from scratch using current sectorMap arrangement.
    /// </summary>
    public void GenerateFullMaze()
    {
        grid = new bool[totalTilesX, totalTilesY];

        // Generate each sector
        for (int col = 0; col < gridCols; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                int sectorIndex = sectorMap[col, row];
                GenerateSector(col, row, sectorSeeds[sectorIndex]);
            }
        }

        // Carve connections between adjacent sectors
        CarveInterSectorConnections();

        // Paint to tilemaps
        PaintTilemaps();

        // Spawn interactables
        SpawnInteractables();
    }

    /// <summary>
    /// Shuffle sector positions, regenerate, and return new spawn/exit sector coords.
    /// </summary>
    public void RearrangeSectors(out Vector2Int spawnSector, out Vector2Int exitSector)
    {
        // Fisher-Yates shuffle of the sector map
        int[] flat = new int[gridCols * gridRows];
        for (int c = 0; c < gridCols; c++)
            for (int r = 0; r < gridRows; r++)
                flat[r * gridCols + c] = sectorMap[c, r];

        for (int i = flat.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (flat[i], flat[j]) = (flat[j], flat[i]);
        }

        for (int c = 0; c < gridCols; c++)
            for (int r = 0; r < gridRows; r++)
                sectorMap[c, r] = flat[r * gridCols + c];

        // Pick random spawn and exit sectors (must be different)
        spawnSector = new Vector2Int(Random.Range(0, gridCols), Random.Range(0, gridRows));
        do
        {
            exitSector = new Vector2Int(Random.Range(0, gridCols), Random.Range(0, gridRows));
        } while (exitSector == spawnSector);

        // Regenerate everything
        GenerateFullMaze();
    }

    /// <summary>
    /// Get the world-space center of a passage cell in a given sector.
    /// Used for placing player/goal at sector (col, row), cell (1,1).
    /// </summary>
    public Vector3 GetSectorSpawnPosition(int col, int row)
    {
        int baseX = col * sectorTilesX;
        int baseY = row * sectorTilesY;
        // Cell (1,1) is always a passage
        return TileToWorld(baseX + 1, baseY + 1);
    }

    /// <summary>
    /// Get the world-space center of the exit position in a sector.
    /// Uses the bottom-right passage cell.
    /// </summary>
    public Vector3 GetSectorExitPosition(int col, int row)
    {
        int baseX = col * sectorTilesX;
        int baseY = row * sectorTilesY;
        return TileToWorld(baseX + sectorTilesX - 2, baseY + sectorTilesY - 2);
    }

    /// <summary>
    /// Get the original sector index at a given grid position.
    /// </summary>
    public int GetSectorIndex(int col, int row)
    {
        return sectorMap[col, row];
    }

    /// <summary>
    /// Convert a world position to sector grid coordinates.
    /// </summary>
    public Vector2Int WorldToSectorCoord(Vector3 worldPos)
    {
        Vector3Int cell = wallTilemap.WorldToCell(worldPos);
        int col = Mathf.Clamp(cell.x / sectorTilesX, 0, gridCols - 1);
        int row = Mathf.Clamp(cell.y / sectorTilesY, 0, gridRows - 1);
        return new Vector2Int(col, row);
    }

    // ---------------------------------------------------------------
    // INTERNAL
    // ---------------------------------------------------------------

    private void GenerateSector(int col, int row, int seed)
    {
        Random.State prevState = Random.state;
        Random.InitState(seed);

        int baseX = col * sectorTilesX;
        int baseY = row * sectorTilesY;

        // Mark all cells in this sector as walls first (already false by default)
        // Then carve passages using recursive backtracking within sector bounds

        // Starting cell in grid coords
        int startX = baseX + 1;
        int startY = baseY + 1;

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        grid[startX, startY] = true;
        stack.Push(new Vector2Int(startX, startY));

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighborsInSector(current, baseX, baseY);

            if (neighbors.Count > 0)
            {
                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                int wallX = (current.x + chosen.x) / 2;
                int wallY = (current.y + chosen.y) / 2;
                grid[wallX, wallY] = true;
                grid[chosen.x, chosen.y] = true;
                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }

        // Create pockets (small open areas) for interactables
        CreatePockets(col, row, baseX, baseY, seed);

        Random.state = prevState;
    }

    private List<Vector2Int> GetUnvisitedNeighborsInSector(Vector2Int cell, int baseX, int baseY)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(4);
        Vector2Int[] dirs = {
            new Vector2Int(0, 2), new Vector2Int(0, -2),
            new Vector2Int(2, 0), new Vector2Int(-2, 0)
        };

        int minX = baseX + 1;
        int maxX = baseX + sectorTilesX - 2;
        int minY = baseY + 1;
        int maxY = baseY + sectorTilesY - 2;

        foreach (var d in dirs)
        {
            Vector2Int n = cell + d;
            if (n.x >= minX && n.x <= maxX && n.y >= minY && n.y <= maxY && !grid[n.x, n.y])
                neighbors.Add(n);
        }
        return neighbors;
    }

    private void CreatePockets(int col, int row, int baseX, int baseY, int seed)
    {
        int sectorIndex = sectorMap[col, row];
        sectorPockets[sectorIndex].Clear();

        Random.State prevState = Random.state;
        Random.InitState(seed + 9999);

        int pocketCount = Random.Range(1, 4); // 1-3 pockets per sector

        for (int p = 0; p < pocketCount; p++)
        {
            // Pick a random passage cell and clear a 3x3 area around it
            int cx = baseX + 3 + Random.Range(0, sectorTilesX - 6);
            int cy = baseY + 3 + Random.Range(0, sectorTilesY - 6);

            // Make sure center is odd-offset (passage cell)
            if ((cx - baseX) % 2 == 0) cx++;
            if ((cy - baseY) % 2 == 0) cy++;

            // Clamp
            cx = Mathf.Clamp(cx, baseX + 2, baseX + sectorTilesX - 3);
            cy = Mathf.Clamp(cy, baseY + 2, baseY + sectorTilesY - 3);

            // Clear a 3x3 pocket
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px > baseX && px < baseX + sectorTilesX - 1 &&
                        py > baseY && py < baseY + sectorTilesY - 1)
                        grid[px, py] = true;
                }

            sectorPockets[sectorIndex].Add(TileToWorld(cx, cy));
        }

        Random.state = prevState;
    }

    private void CarveInterSectorConnections()
    {
        // For each pair of adjacent sectors, carve 1-2 openings in the shared wall

        // Horizontal connections (between col and col+1)
        for (int col = 0; col < gridCols - 1; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                int wallX = (col + 1) * sectorTilesX; // the shared wall column
                int baseY = row * sectorTilesY;

                // Pick 2 random passage-aligned Y positions to open
                int openings = 2;
                List<int> candidates = new List<int>();
                for (int y = baseY + 1; y < baseY + sectorTilesY - 1; y += 2)
                    candidates.Add(y);

                for (int i = 0; i < openings && candidates.Count > 0; i++)
                {
                    int idx = Random.Range(0, candidates.Count);
                    int oy = candidates[idx];
                    candidates.RemoveAt(idx);

                    // Carve the wall tile and ensure passage on both sides
                    grid[wallX, oy] = true;
                    if (wallX - 1 >= 0) grid[wallX - 1, oy] = true;
                    if (wallX + 1 < totalTilesX) grid[wallX + 1, oy] = true;
                }
            }
        }

        // Vertical connections (between row and row+1)
        for (int row = 0; row < gridRows - 1; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                int wallY = (row + 1) * sectorTilesY;
                int baseX = col * sectorTilesX;

                int openings = 2;
                List<int> candidates = new List<int>();
                for (int x = baseX + 1; x < baseX + sectorTilesX - 1; x += 2)
                    candidates.Add(x);

                for (int i = 0; i < openings && candidates.Count > 0; i++)
                {
                    int idx = Random.Range(0, candidates.Count);
                    int ox = candidates[idx];
                    candidates.RemoveAt(idx);

                    grid[ox, wallY] = true;
                    if (wallY - 1 >= 0) grid[ox, wallY - 1] = true;
                    if (wallY + 1 < totalTilesY) grid[ox, wallY + 1] = true;
                }
            }
        }
    }

    private void PaintTilemaps()
    {
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();

        for (int x = 0; x < totalTilesX; x++)
        {
            for (int y = 0; y < totalTilesY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (grid[x, y])
                {
                    if (floorTile != null)
                        floorTilemap.SetTile(pos, floorTile);
                }
                else
                {
                    if (wallTile != null)
                        wallTilemap.SetTile(pos, wallTile);
                }
            }
        }
    }

    private void SpawnInteractables()
    {
        // Destroy old ones
        foreach (var go in spawnedInteractables)
        {
            if (go != null) Destroy(go);
        }
        spawnedInteractables.Clear();

        if (interactablePrefab == null) return;

        for (int col = 0; col < gridCols; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                int sectorIndex = sectorMap[col, row];
                foreach (var pos in sectorPockets[sectorIndex])
                {
                    GameObject obj = Instantiate(interactablePrefab, pos, Quaternion.identity);
                    obj.SetActive(true); // template is inactive, must activate clones
                    MazeInteractable interactable = obj.GetComponent<MazeInteractable>();
                    if (interactable != null)
                    {
                        interactable.sectorIndex = sectorIndex;
                        interactable.sectorCoord = new Vector2Int(col, row);
                    }
                    spawnedInteractables.Add(obj);
                }
            }
        }
    }

    private Vector3 TileToWorld(int tileX, int tileY)
    {
        if (wallTilemap != null)
            return wallTilemap.CellToWorld(new Vector3Int(tileX, tileY, 0)) + wallTilemap.cellSize / 2f;
        return new Vector3(tileX + 0.5f, tileY + 0.5f, 0f);
    }

    /// <summary>
    /// Given a world position, find the nearest passage (non-wall) tile.
    /// Used to prevent the player from clipping into walls after a reshuffle.
    /// </summary>
    public Vector3 FindNearestPassage(Vector3 worldPos)
    {
        if (grid == null) return worldPos;

        Vector3Int cell = wallTilemap != null
            ? wallTilemap.WorldToCell(worldPos)
            : new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);

        // If current cell is already a passage, return it
        if (cell.x >= 0 && cell.x < totalTilesX && cell.y >= 0 && cell.y < totalTilesY)
        {
            if (grid[cell.x, cell.y])
                return TileToWorld(cell.x, cell.y);
        }

        // BFS outward to find the nearest passage
        int maxSearch = 50;
        for (int radius = 1; radius <= maxSearch; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue; // only check the ring edge

                    int nx = cell.x + dx;
                    int ny = cell.y + dy;

                    if (nx >= 0 && nx < totalTilesX && ny >= 0 && ny < totalTilesY && grid[nx, ny])
                        return TileToWorld(nx, ny);
                }
            }
        }

        // Fallback: return original position
        return worldPos;
    }
}

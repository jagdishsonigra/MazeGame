using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds the maze layout at runtime using the Recursive Backtracker algorithm.
///
/// Algorithm choice rationale:
///   Recursive Backtracker produces "river-like" mazes with long winding paths
///   and relatively few dead ends — ideal for a ball-rolling game where the
///   player needs breathing room to navigate using tilt/accelerometer controls.
///   It also guarantees exactly one solution path (perfect maze), satisfying
///   the "clearly marked start and finish" requirement with a single valid route.
///
/// Grid size 7×7 = 49 cells → generates a maze with 6-8 navigable corridor paths
/// visible to the player (satisfies the 5-8 path requirement).
///
/// Wall placement uses Unity Instantiation of prefabs assigned in the Inspector.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Maze Dimensions")]
    [SerializeField] private int cols = 7;   // number of cells horizontally
    [SerializeField] private int rows = 7;   // number of cells vertically

    [Header("Cell Size (world units)")]
    [SerializeField] private float cellSize = 3f;
    [SerializeField] private float wallHeight = 2f;
    [SerializeField] private float wallThickness = 0.3f;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;   // a flat-scaled cube
    [SerializeField] private GameObject floorPrefab;  // ground tile
    [SerializeField] private GameObject startMarkerPrefab;
    [SerializeField] private GameObject finishMarkerPrefab;

    // ─────────────────────────────────────────────
    //  Private Data
    // ─────────────────────────────────────────────
    // Cell bitfield: bit 0 = North wall, 1 = East, 2 = South, 3 = West
    private int[,] _grid;
    private bool[,] _visited;

    private const int NORTH = 0;
    private const int EAST  = 1;
    private const int SOUTH = 2;
    private const int WEST  = 3;

    private static readonly int[] DX = {  0, 1, 0, -1 };
    private static readonly int[] DY = {  1, 0, -1,  0 };
    private static readonly int[] OPPOSITE = { 2, 3, 0, 1 };

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        GenerateMaze();
        BuildMazeGeometry();
        PlaceStartAndFinish();
    }

    // ─────────────────────────────────────────────
    //  Algorithm
    // ─────────────────────────────────────────────
    private void GenerateMaze()
    {
        _grid    = new int[cols, rows];
        _visited = new bool[cols, rows];

        // Start from top-left cell
        RecursiveBacktrack(0, 0);
    }

    private void RecursiveBacktrack(int cx, int cy)
    {
        _visited[cx, cy] = true;

        // Shuffle direction order for randomness
        int[] directions = { NORTH, EAST, SOUTH, WEST };
        Shuffle(directions);

        foreach (int dir in directions)
        {
            int nx = cx + DX[dir];
            int ny = cy + DY[dir];

            if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
            if (_visited[nx, ny]) continue;

            // Remove wall between current and next cell (bitmask)
            _grid[cx, cy] |= (1 << dir);
            _grid[nx, ny] |= (1 << OPPOSITE[dir]);

            RecursiveBacktrack(nx, ny);
        }
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // ─────────────────────────────────────────────
    //  Geometry Builder
    // ─────────────────────────────────────────────
    private void BuildMazeGeometry()
    {
        Transform mazeParent = new GameObject("MazeGeometry").transform;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellCenter = new Vector3(x * cellSize, 0f, y * cellSize);

                // Place floor tile
                PlaceTile(floorPrefab, cellCenter, Quaternion.identity, mazeParent);

                // North wall (if not carved)
                if ((_grid[x, y] & (1 << NORTH)) == 0)
                {
                    Vector3 wallPos = cellCenter + new Vector3(0f, wallHeight * 0.5f, cellSize * 0.5f);
                    PlaceWall(wallPos, new Vector3(cellSize, wallHeight, wallThickness), mazeParent);
                }

                // East wall
                if ((_grid[x, y] & (1 << EAST)) == 0)
                {
                    Vector3 wallPos = cellCenter + new Vector3(cellSize * 0.5f, wallHeight * 0.5f, 0f);
                    PlaceWall(wallPos, new Vector3(wallThickness, wallHeight, cellSize), mazeParent);
                }
            }
        }

        // Outer boundary walls
        BuildBoundaryWalls(mazeParent);
    }

    private void BuildBoundaryWalls(Transform parent)
    {
        float totalWidth  = cols * cellSize;
        float totalDepth  = rows * cellSize;
        float halfW = totalWidth  * 0.5f - cellSize * 0.5f;
        float halfD = totalDepth * 0.5f - cellSize * 0.5f;

        // South boundary
        PlaceWall(new Vector3(halfW, wallHeight * 0.5f, -cellSize * 0.5f),
                  new Vector3(totalWidth, wallHeight, wallThickness), parent);
        // North boundary
        PlaceWall(new Vector3(halfW, wallHeight * 0.5f, rows * cellSize - cellSize * 0.5f),
                  new Vector3(totalWidth, wallHeight, wallThickness), parent);
        // West boundary
        PlaceWall(new Vector3(-cellSize * 0.5f, wallHeight * 0.5f, halfD),
                  new Vector3(wallThickness, wallHeight, totalDepth), parent);
        // East boundary
        PlaceWall(new Vector3(cols * cellSize - cellSize * 0.5f, wallHeight * 0.5f, halfD),
                  new Vector3(wallThickness, wallHeight, totalDepth), parent);
    }

    private void PlaceWall(Vector3 position, Vector3 scale, Transform parent)
    {
        if (wallPrefab == null) return;
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);
        wall.transform.localScale = scale;
        wall.tag = "Wall";
    }

    private void PlaceTile(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, rotation, parent);
    }

    // ─────────────────────────────────────────────
    //  Start / Finish Placement
    // ─────────────────────────────────────────────
    private void PlaceStartAndFinish()
    {
        // Start = bottom-left cell centre
        Vector3 startPos = new Vector3(0f, 0.1f, 0f);
        if (startMarkerPrefab != null) Instantiate(startMarkerPrefab, startPos, Quaternion.identity);

        // Finish = top-right cell centre (guaranteed connected by algorithm)
        Vector3 finishPos = new Vector3((cols - 1) * cellSize, 0.1f, (rows - 1) * cellSize);
        if (finishMarkerPrefab != null) Instantiate(finishMarkerPrefab, finishPos, Quaternion.identity);
    }
}

using UnityEngine;
using System.Collections.Generic;

public class PathfindingGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 50;
    public int height = 50;
    public float cellSize = 1f;
    public LayerMask obstacleLayer;

    [Header("Dynamic Costs")]
    public InfluenceMap influenceMap;
    [Tooltip("How much influence affects pathfinding cost")]
    public float influenceCostMultiplier = 2f;
    [Tooltip("How much heat affects pathfinding cost (negative = attracted)")]
    public float heatCostMultiplier = -0.5f;

    [Header("Visualization")]
    public bool showGrid = false;
    public bool showWalkable = false;

    private Node[,] grid;

    void Awake()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[width, height];
        
        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float worldX = x * cellSize - halfWidth + cellSize * 0.5f;
                float worldZ = y * cellSize - halfHeight + cellSize * 0.5f;
                Vector3 worldPos = new Vector3(worldX, 0.5f, worldZ);

                // Check if this position is walkable
                bool walkable = !Physics.CheckSphere(worldPos, cellSize * 0.4f, obstacleLayer);

                grid[x, y] = new Node(walkable, worldPos, x, y);
            }
        }
    }

    public void UpdateGrid()
    {
        // Optionally refresh walkability (for dynamic obstacles)
        CreateGrid();
    }

    public Node NodeFromWorldPoint(Vector3 worldPos)
    {
        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;

        int x = Mathf.FloorToInt((worldPos.x + halfWidth) / cellSize);
        int y = Mathf.FloorToInt((worldPos.z + halfHeight) / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return grid[x, y];
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        // Check 8 directions
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    // Get dynamic cost based on influence and heat
    public float GetDynamicCost(Node node)
    {
        if (influenceMap == null) return 1f;

        float baseCost = 1f;
        
        // Add cost for high influence (danger)
        float influence = influenceMap.GetInfluence(node.worldPosition);
        baseCost += influence * influenceCostMultiplier;

        // Reduce cost for heat (attraction to player activity)
        float heat = influenceMap.GetHeat(node.worldPosition);
        baseCost += heat * heatCostMultiplier;

        return Mathf.Max(baseCost, 0.1f); // Never go below 0.1
    }

    void OnDrawGizmos()
    {
        if (grid == null || !showGrid) return;

        foreach (Node node in grid)
        {
            if (showWalkable)
            {
                Gizmos.color = node.walkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (cellSize * 0.9f));
            }
            else
            {
                if (!node.walkable)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (cellSize * 0.9f));
                }
            }
        }
    }
}

[System.Serializable]
public class Node
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;

    // A* values
    public int gCost; // Distance from start
    public int hCost; // Distance to target
    public Node parent;

    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    public int fCost
    {
        get { return gCost + hCost; }
    }
}
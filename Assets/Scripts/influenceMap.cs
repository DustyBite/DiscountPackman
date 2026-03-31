using UnityEngine;

public class InfluenceMap : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    
    public float[,] grid;           // Regular influence (danger)
    public float[,] heatMap;        // Player activity heat map
    
    [Header("Heat Map Settings")]
    [Tooltip("How quickly heat dissipates over time (lower = stays longer)")]
    public float heatDecayRate = 0.5f;
    
    [Tooltip("Minimum heat value before it's removed completely")]
    public float heatThreshold = 0.1f;

    void Awake()
    {
        grid = new float[width, height];
        heatMap = new float[width, height];
    }

    void Update()
    {
        DecayHeat();
    }

    public void ClearMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = 0f;
            }
        }
    }

    public void ClearHeatMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heatMap[x, y] = 0f;
            }
        }
    }

    // Decay heat over time
    void DecayHeat()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (heatMap[x, y] > heatThreshold)
                {
                    heatMap[x, y] -= heatDecayRate * Time.deltaTime;
                }
                else
                {
                    heatMap[x, y] = 0f;
                }
            }
        }
    }

    public void AddInfluence(Vector3 worldPos, float value, float radius)
    {
        Vector2Int center = WorldToGrid(worldPos);
        int r = Mathf.CeilToInt(radius);
        
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                int gx = center.x + x;
                int gy = center.y + y;
                
                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                {
                    float dist = Mathf.Sqrt(x * x + y * y);
                    float falloff = Mathf.Clamp01(1 - dist / radius);
                    grid[gx, gy] += value * falloff;
                }
            }
        }
    }

    // Add heat (player activity)
    public void AddHeat(Vector3 worldPos, float value, float radius)
    {
        Vector2Int center = WorldToGrid(worldPos);
        int r = Mathf.CeilToInt(radius);
        
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                int gx = center.x + x;
                int gy = center.y + y;
                
                if (gx >= 0 && gx < width && gy >= 0 && gy < height)
                {
                    float dist = Mathf.Sqrt(x * x + y * y);
                    float falloff = Mathf.Clamp01(1 - dist / radius);
                    float heatValue = value * falloff;
                    
                    // Add heat, but cap at a maximum
                    heatMap[gx, gy] = Mathf.Min(heatMap[gx, gy] + heatValue, 10f);
                }
            }
        }
    }

    public float GetInfluence(Vector3 worldPos)
    {
        Vector2Int g = WorldToGrid(worldPos);
        return grid[g.x, g.y];
    }

    public float GetHeat(Vector3 worldPos)
    {
        Vector2Int g = WorldToGrid(worldPos);
        return heatMap[g.x, g.y];
    }

    Vector2Int WorldToGrid(Vector3 pos)
    {
        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;
        int x = Mathf.FloorToInt((pos.x + halfWidth) / cellSize);
        int y = Mathf.FloorToInt((pos.z + halfHeight) / cellSize);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return new Vector2Int(x, y);
    }

    [Header("Visualization")]
    public bool showInfluence = true;
    public bool showHeatMap = false;

    void OnDrawGizmos()
    {
        if (grid == null || heatMap == null) return;
        
        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float worldX = x * cellSize - halfWidth + cellSize * 0.5f;
                float worldZ = y * cellSize - halfHeight + cellSize * 0.5f;
                Vector3 pos = new Vector3(worldX, 0.1f, worldZ);
                
                if (showInfluence && grid[x, y] > 0.1f)
                {
                    // Red = danger/influence
                    Gizmos.color = Color.Lerp(Color.clear, Color.red, grid[x, y] / 10f);
                    Gizmos.DrawCube(pos, Vector3.one * (cellSize * 0.9f));
                }
                
                if (showHeatMap && heatMap[x, y] > 0.1f)
                {
                    // Yellow/Orange = player activity heat
                    Gizmos.color = Color.Lerp(Color.yellow, Color.red, heatMap[x, y] / 10f);
                    Gizmos.DrawCube(pos + Vector3.up * 0.2f, Vector3.one * (cellSize * 0.8f));
                }
            }
        }
    }
}
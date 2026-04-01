using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Pathfinding : MonoBehaviour
{
    public PathfindingGrid grid;

    [Header("Settings")]
    [Tooltip("Use diagonal movement")]
    public bool allowDiagonal = true;

    [Tooltip("Recalculate path every X seconds")]
    public float pathUpdateInterval = 0.5f;

    void Awake()
    {
        if (grid == null)
            grid = FindObjectOfType<PathfindingGrid>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable || !targetNode.walkable)
        {
            return new List<Vector3>(); // No path possible
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Path found
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // Check neighbors
            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                // Dynamic cost based on influence/heat
                float dynamicCost = grid.GetDynamicCost(neighbor);
                
                int moveCost = GetDistance(currentNode, neighbor);
                int newGCost = currentNode.gCost + Mathf.RoundToInt(moveCost * dynamicCost);

                if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // No path found
        return new List<Vector3>();
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        // Convert to world positions and simplify
        List<Vector3> waypoints = SimplifyPath(path);
        return waypoints;
    }

    List<Vector3> SimplifyPath(List<Node> path)
    {
        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(
                path[i - 1].gridX - path[i].gridX,
                path[i - 1].gridY - path[i].gridY
            );

            if (directionNew != directionOld)
            {
                waypoints.Add(path[i - 1].worldPosition);
            }

            directionOld = directionNew;
        }

        if (path.Count > 0)
            waypoints.Add(path[path.Count - 1].worldPosition);

        return waypoints;
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (allowDiagonal)
        {
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }
        else
        {
            return 10 * (dstX + dstY);
        }
    }
}
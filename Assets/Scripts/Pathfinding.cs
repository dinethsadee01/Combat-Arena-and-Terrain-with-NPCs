using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public MapGenerator mapGenerator;
    Node[,] grid;

    void Start()
    {
        // Wait to ensure the MapGenerator has finished
        Invoke("CreateGrid", 0.1f);
    }

    // Convert the MapGenerator's 0s and 1s into Nodes
    public void CreateGrid()
    {
        if (mapGenerator == null) return;

        int width = mapGenerator.width;
        int height = mapGenerator.height;
        grid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 1 is Wall (unwalkable), 0 is Floor (walkable)
                bool walkable = (mapGenerator.map[x, y] == 0);

                // Calculate world position based on grid coordinates
                Vector3 worldPoint = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);

                // Penalty: You can add logic here. e.g. if tile is '2', penalty = 10.
                int penalty = 0;

                grid[x, y] = new Node(walkable, worldPoint, x, y, penalty);
            }
        }
    }

    // The A* Algorithm
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = NodeFromWorldPoint(startPos);
        Node targetNode = NodeFromWorldPoint(targetPos);

        // Safety check
        if (startNode == null || targetNode == null || !startNode.walkable || !targetNode.walkable)
        {
            return null;
        }

        List<Node> openSet = new List<Node>();    // Nodes to be evaluated
        HashSet<Node> closedSet = new HashSet<Node>(); // Nodes already evaluated

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            // 1. Find node with lowest F Cost
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

            // 2. Found the target?
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // 3. Check neighbors
            foreach (Node neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Cost to move to neighbor (standard move = 10, diagonal = 14)
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor) + neighbor.movementPenalty;

                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null; // No path found
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    // Distance calculation (Manhattan/Octile)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        // 14 is approx sqrt(2)*10 (Diagonal), 10 is straight move
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    // Get neighbors (including diagonals)
    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < mapGenerator.width && checkY >= 0 && checkY < mapGenerator.height)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    // Convert World Position to Grid Node
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null) return null;

        // Since map is centered at 0,0, we offset
        float percentX = (worldPosition.x + mapGenerator.width / 2f) / mapGenerator.width;
        float percentY = (worldPosition.z + mapGenerator.height / 2f) / mapGenerator.height;

        // Clamp to avoid out of bounds errors
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((mapGenerator.width - 1) * percentX);
        int y = Mathf.RoundToInt((mapGenerator.height - 1) * percentY);

        return grid[x, y];
    }
}
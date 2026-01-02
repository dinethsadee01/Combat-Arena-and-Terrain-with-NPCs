using UnityEngine;

public class Node
{
    public bool walkable;      // Is this a wall or floor?
    public Vector3 worldPosition;
    public int gridX;          // X index in the array
    public int gridY;          // Y index in the array

    // Costs for A* calculation
    public int gCost;          // Cost from start node
    public int hCost;          // Heuristic cost to end node
    public Node parent;        // Parent node (to retrace the path)

    // New requirement: Terrain Cost (e.g., Sand = 2, Grass = 1)
    public int movementPenalty;

    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty;
    }

    // fCost is the total cost (g + h)
    public int fCost
    {
        get { return gCost + hCost; }
    }
}
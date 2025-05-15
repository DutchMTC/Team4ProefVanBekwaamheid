using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementValidator : MonoBehaviour
{
    private class PathNode
    {
        public TileSettings tile;
        public PathNode parent;
        public float gCost;
        public float hCost;

        public PathNode(TileSettings tile, PathNode parent, float gCost, float hCost)
        {
            this.tile = tile;
            this.parent = parent;
            this.gCost = gCost;
            this.hCost = hCost;
        }

        public float GetTotalCost()
        {
            return gCost + hCost;
        }
    }

    public static class PathCost
    {
        public const float NORMAL_TILE_COST = 1f;
        public const float TRAP_TILE_COST = 10f;
    }

    public static List<TileSettings> FindPath(TileSettings startTile, TileSettings endTile, List<TileSettings> allTiles)
    {
        var gridGenerator = startTile.GetComponentInParent<GridGenerator>();
        if (gridGenerator == null) return null;

        var openList = new List<PathNode>();
        var closedList = new HashSet<TileSettings>();
        var pathParents = new Dictionary<TileSettings, PathNode>();
        var tilesByCoord = allTiles.ToDictionary(t => new Vector2Int(t.gridX, t.gridY));

        openList.Add(new PathNode(startTile, null, 0, GetHeuristicCost(startTile, endTile)));

        while (openList.Count > 0)
        {
            // Get the node with lowest total cost
            var currentNode = GetLowestCostNode(openList);
            openList.Remove(currentNode);

            if (currentNode.tile == endTile)
            {
                // Path found, reconstruct and return it
                return ReconstructPath(currentNode);
            }

            closedList.Add(currentNode.tile);            // Get valid neighboring tiles
            var neighbors = GetValidNeighbors(currentNode.tile, tilesByCoord);

            foreach (var neighbor in neighbors)
            {
                if (closedList.Contains(neighbor))
                    continue;

                float movementCost = GetMovementCost(neighbor);
                float totalCost = currentNode.gCost + movementCost;

                PathNode neighborNode = openList.Find(n => n.tile == neighbor);

                if (neighborNode == null)
                {
                    // Add new node to open list
                    neighborNode = new PathNode(
                        neighbor,
                        currentNode,
                        totalCost,
                        GetHeuristicCost(neighbor, endTile)
                    );
                    openList.Add(neighborNode);
                    pathParents[neighbor] = neighborNode;
                }
                else if (totalCost < neighborNode.gCost)
                {
                    // Update existing node with better path
                    neighborNode.parent = currentNode;
                    neighborNode.gCost = totalCost;
                    pathParents[neighbor] = neighborNode;
                }
            }        }

        // No path found
        Debug.LogWarning($"No path found from ({startTile.gridX}, {startTile.gridY}) to ({endTile.gridX}, {endTile.gridY}). " +
                      $"Explored {closedList.Count} tiles, {openList.Count} tiles still in open list.");
        return null;
    }

    private static float GetMovementCost(TileSettings tile)
    {
        // Higher cost for trap tiles to make the pathfinding prefer non-trap tiles
        return tile.occupantType == TileSettings.OccupantType.Trap ? PathCost.TRAP_TILE_COST : PathCost.NORMAL_TILE_COST;
    }

    private static float GetHeuristicCost(TileSettings from, TileSettings to)
    {
        // Manhattan distance heuristic
        return Mathf.Abs(from.gridX - to.gridX) + Mathf.Abs(from.gridY - to.gridY);
    }    private static List<TileSettings> GetValidNeighbors(TileSettings tile, Dictionary<Vector2Int, TileSettings> tilesByCoord)
    {
        var neighbors = new List<TileSettings>();
        var gridGenerator = tile.GetComponentInParent<GridGenerator>();
        if (gridGenerator == null) {
            Debug.LogError("Could not find GridGenerator parent for tile");
            return neighbors;
        }        // Only check orthogonal neighbors (up, down, left, right)
        int[][] directions = new int[][] 
        {
            new int[] { 0, 1 },  // up
            new int[] { 0, -1 }, // down
            new int[] { -1, 0 }, // left
            new int[] { 1, 0 }   // right
        };

        foreach (var dir in directions)
        {
            int newX = tile.gridX + dir[0];
            int newY = tile.gridY + dir[1];

            // Check bounds
            if (newX >= 0 && newX < gridGenerator.width && 
                newY >= 0 && newY < gridGenerator.height)
            {
                var coord = new Vector2Int(newX, newY);
                TileSettings neighborTile;
                if (!tilesByCoord.TryGetValue(coord, out neighborTile))
                {
                    Debug.LogWarning($"Could not find tile at coordinates ({newX}, {newY})");
                }
                else if (IsWalkable(neighborTile))
                {
                    neighbors.Add(neighborTile);
                    Debug.Log($"Added walkable neighbor at ({newX}, {newY})");
                }
            }
        }

        return neighbors;
    }    private static bool IsWalkable(TileSettings tile)
    {
        var isWalkable = tile.occupantType == TileSettings.OccupantType.None ||
                        tile.occupantType == TileSettings.OccupantType.Item ||
                        tile.occupantType == TileSettings.OccupantType.Trap;
        if (!isWalkable) {
            Debug.Log($"Tile at ({tile.gridX}, {tile.gridY}) is not walkable: {tile.occupantType}");
        }
        return isWalkable;
    }

    private static TileSettings FindTileAtCoordinates(int gridX, int gridY, GridGenerator gridGenerator)
    {
        foreach (Transform child in gridGenerator.transform)
        {
            TileSettings tile = child.GetComponent<TileSettings>();
            if (tile != null && tile.gridX == gridX && tile.gridY == gridY)
            {
                return tile;
            }
        }
        return null;
    }

    private static PathNode GetLowestCostNode(List<PathNode> nodes)
    {
        PathNode lowest = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].GetTotalCost() < lowest.GetTotalCost())
            {
                lowest = nodes[i];
            }
        }
        return lowest;
    }

    private static List<TileSettings> ReconstructPath(PathNode endNode)
    {
        var path = new List<TileSettings>();
        var current = endNode;

        while (current != null)
        {
            path.Add(current.tile);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }    
}
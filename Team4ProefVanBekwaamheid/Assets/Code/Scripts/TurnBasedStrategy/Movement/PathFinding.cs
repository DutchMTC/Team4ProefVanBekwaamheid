using System;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding
{
    private const float TRAP_TILE_COST_MULTIPLIER = 10f; // Makes trap tiles more expensive to traverse

    public class PathNode : IComparable<PathNode>
    {
        public TileSettings tile;
        public PathNode parent;
        public float gCost; // Cost from start to current node
        public float hCost; // Estimated cost from current node to end
        public float fCost => gCost + hCost;

        public PathNode(TileSettings tile, PathNode parent, float gCost, float hCost)
        {
            this.tile = tile;
            this.parent = parent;
            this.gCost = gCost;
            this.hCost = hCost;
        }

        public int CompareTo(PathNode other)
        {
            int compare = fCost.CompareTo(other.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(other.hCost);
            }
            return compare;
        }
    }

    public static List<TileSettings> FindPath(TileSettings startTile, TileSettings endTile, List<TileSettings> allTiles)
    {
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<TileSettings>(); 
        var startNode = new PathNode(startTile, null, 0, GetHeuristicCost(startTile, endTile));
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            openSet.Sort();
            var currentNode = openSet[0];
            openSet.RemoveAt(0);

            if (currentNode.tile == endTile)
            {
                return ReconstructPath(currentNode);
            }

            closedSet.Add(currentNode.tile);

            foreach (var neighbor in GetNeighbors(currentNode.tile, allTiles))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float newGCost = currentNode.gCost + GetMovementCost(currentNode.tile, neighbor);
                
                PathNode neighborNode = openSet.Find(n => n.tile == neighbor);
                
                if (neighborNode == null)
                {
                    neighborNode = new PathNode(
                        neighbor,
                        currentNode,
                        newGCost,
                        GetHeuristicCost(neighbor, endTile)
                    );
                    openSet.Add(neighborNode);
                }
                else if (newGCost < neighborNode.gCost)
                {
                    neighborNode.parent = currentNode;
                    neighborNode.gCost = newGCost;
                }
            }
        }

        // No path found
        return null;
    }

    private static float GetMovementCost(TileSettings from, TileSettings to)
    {
        float baseCost = Vector3.Distance(from.transform.position, to.transform.position);
        
        // Apply penalty for trap tiles
        if (to.occupantType == TileSettings.OccupantType.Trap)
        {
            baseCost *= TRAP_TILE_COST_MULTIPLIER;
        }

        return baseCost;
    }

    private static float GetHeuristicCost(TileSettings from, TileSettings to)
    {
        return Vector3.Distance(from.transform.position, to.transform.position);
    }

    private static List<TileSettings> GetNeighbors(TileSettings tile, List<TileSettings> allTiles)
    {
        var neighbors = new List<TileSettings>();
        float maxDistance = 1.5f; // Adjust this value based on your tile spacing

        foreach (var potentialNeighbor in allTiles)
        {
            if (potentialNeighbor == tile) continue;

            float distance = Vector3.Distance(tile.transform.position, potentialNeighbor.transform.position);
            if (distance <= maxDistance)
            {
                // Only add walkable tiles (None, Item, or Trap)
                if (potentialNeighbor.occupantType == TileSettings.OccupantType.None ||
                    potentialNeighbor.occupantType == TileSettings.OccupantType.Item ||
                    potentialNeighbor.occupantType == TileSettings.OccupantType.Trap)
                {
                    neighbors.Add(potentialNeighbor);
                }
            }
        }

        return neighbors;
    }

    private static List<TileSettings> ReconstructPath(PathNode endNode)
    {
        var path = new List<TileSettings>();
        var currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.tile);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    [SerializeField] private LineRenderer pathLineRenderer;
    [SerializeField] private float pathLineHeight = 0.1f;
    [SerializeField] private Color normalPathColor = Color.yellow;
    [SerializeField] private Color trapPathColor = Color.red;

    private GridGenerator gridGenerator;
    private List<TileSettings> currentPath;

    private void Awake()
    {
        gridGenerator = FindObjectOfType<GridGenerator>();
        
        if (pathLineRenderer == null)
        {
            pathLineRenderer = gameObject.AddComponent<LineRenderer>();
            pathLineRenderer.startWidth = 0.1f;
            pathLineRenderer.endWidth = 0.1f;
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        HidePath();
    }    public List<TileSettings> FindPathBetween(TileSettings startTile, TileSettings endTile)
    {
        if (startTile == null || endTile == null)
        {
            Debug.LogWarning("Invalid start or end tile for pathfinding");
            return null;
        }

        var allTiles = gridGenerator.GetAllTiles();
        currentPath = MovementValidator.FindPath(startTile, endTile, allTiles);
        ShowPath(currentPath);
        return currentPath;
    }    public void ShowPath(List<TileSettings> path)
    {
        if (path == null || path.Count == 0)
        {
            HidePath();
            return;
        }

        currentPath = path;
        pathLineRenderer.positionCount = path.Count;
        pathLineRenderer.useWorldSpace = true;
        
        // Count how many segments we'll need for gradient colors
        pathLineRenderer.material.color = Color.white;
        int segments = path.Count - 1;
        pathLineRenderer.colorGradient = new Gradient();

        // Create arrays for our gradient
        GradientColorKey[] colorKeys = new GradientColorKey[path.Count];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[path.Count];

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position = path[i].transform.position;
            position.y += pathLineHeight; // Slightly above the tiles
            pathLineRenderer.SetPosition(i, position);

            // Set color key based on tile type
            Color segmentColor = path[i].occupantType == TileSettings.OccupantType.Trap ? trapPathColor : normalPathColor;
            colorKeys[i] = new GradientColorKey(segmentColor, i / (float)(path.Count - 1));
            alphaKeys[i] = new GradientAlphaKey(1f, i / (float)(path.Count - 1));
        }

        // Apply the gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        pathLineRenderer.colorGradient = gradient;

        pathLineRenderer.enabled = true;
    }

    public void HidePath()
    {
        currentPath = null;
        if (pathLineRenderer != null)
        {
            pathLineRenderer.enabled = false;
        }
    }

    public bool HasPath()
    {
        return currentPath != null && currentPath.Count > 0;
    }

    public List<TileSettings> GetCurrentPath()
    {
        return currentPath;
    }
}

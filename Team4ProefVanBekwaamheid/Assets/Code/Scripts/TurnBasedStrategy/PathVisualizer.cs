using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private Color trapTileColor = Color.red;
    [SerializeField, Range(0.01f, 0.2f), Tooltip("Thickness of the path line")] 
    private float lineWidth = 0.05f;
    
    private LineRenderer lineRenderer;
    private List<TileSettings> currentPath;

    private void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = pathColor;
        HidePath();
    }

    public void ShowPath(List<TileSettings> path)
    {
        if (path == null || path.Count == 0)
        {
            HidePath();
            return;
        }

        currentPath = path;
        lineRenderer.positionCount = path.Count;
        
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position = path[i].transform.position;
            position.y += 0.1f; // Slightly above the tiles
            lineRenderer.SetPosition(i, position);

            // Change color to red for trap tiles
            if (path[i].occupantType == TileSettings.OccupantType.Trap)
            {
                lineRenderer.startColor = trapTileColor;
                lineRenderer.endColor = trapTileColor;
            }
        }

        lineRenderer.enabled = true;
    }

    public void HidePath()
    {
        currentPath = null;
        lineRenderer.enabled = false;
    }

    public List<TileSettings> GetCurrentPath()
    {
        return currentPath;
    }
}

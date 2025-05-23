using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private Color _pathColor = Color.yellow;
    [SerializeField] private Color _trapTileColor = Color.red;
    [SerializeField, Range(0.01f, 0.2f), Tooltip("Thickness of the path line")] 
    private float _lineWidth = 0.05f;
    private LineRenderer _lineRenderer;
    private List<TileSettings> _currentPath;

    private void Awake()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = _pathColor;
        _lineRenderer.endColor = _pathColor;
        HidePath();
    }

    public void ShowPath(List<TileSettings> path)
    {
        if (path == null || path.Count == 0)
        {
            HidePath();
            return;
        }

        _currentPath = path;
        _lineRenderer.positionCount = path.Count;
        
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position = path[i].transform.position;
            position.y += 0.1f; // Slightly above the tiles
            _lineRenderer.SetPosition(i, position);

            // Change color to red for trap tiles
            if (path[i].occupantType == TileSettings.OccupantType.Trap)
            {
                _lineRenderer.startColor = _trapTileColor;
                _lineRenderer.endColor = _trapTileColor;
            }
        }

        _lineRenderer.enabled = true;
    }

    public void HidePath()
    {
        _currentPath = null;
        _lineRenderer.enabled = false;
    }

    public List<TileSettings> GetCurrentPath()
    {
        return _currentPath;
    }
}

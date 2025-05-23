using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    [SerializeField] private LineRenderer _pathLineRenderer;
    [SerializeField] private float _pathLineHeight = 0.1f;
    [SerializeField] private Color _normalPathColor = Color.yellow;
    [SerializeField] private Color _trapPathColor = Color.red;
    private GridGenerator _gridGenerator;
    private List<TileSettings> _currentPath;

    private void Awake()
    {
        _gridGenerator = FindObjectOfType<GridGenerator>();
        
        if (_pathLineRenderer == null)
        {
            _pathLineRenderer = gameObject.AddComponent<LineRenderer>();
            _pathLineRenderer.startWidth = 0.1f;
            _pathLineRenderer.endWidth = 0.1f;
            _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        HidePath();
    }    public List<TileSettings> FindPathBetween(TileSettings startTile, TileSettings endTile)
    {
        if (startTile == null || endTile == null)
        {
            return null;
        }

        var allTiles = _gridGenerator.GetAllTiles();
        _currentPath = MovementValidator.FindPath(startTile, endTile, allTiles);
        ShowPath(_currentPath);
        return _currentPath;
    }    public void ShowPath(List<TileSettings> path)
    {
        if (path == null || path.Count == 0)
        {
            HidePath();
            return;
        }

        _currentPath = path;
        _pathLineRenderer.positionCount = path.Count;
        _pathLineRenderer.useWorldSpace = true;
        
        // Count how many segments we'll need for gradient colors
        _pathLineRenderer.material.color = Color.white;
        int segments = path.Count - 1;
        _pathLineRenderer.colorGradient = new Gradient();

        // Create arrays for our gradient
        GradientColorKey[] colorKeys = new GradientColorKey[path.Count];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[path.Count];

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position = path[i].transform.position;
            position.y += _pathLineHeight; // Slightly above the tiles
            _pathLineRenderer.SetPosition(i, position);

            // Set color key based on tile type
            Color segmentColor = path[i].occupantType == TileSettings.OccupantType.Trap ? _trapPathColor : _normalPathColor;
            colorKeys[i] = new GradientColorKey(segmentColor, i / (float)(path.Count - 1));
            alphaKeys[i] = new GradientAlphaKey(1f, i / (float)(path.Count - 1));
        }

        // Apply the gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        _pathLineRenderer.colorGradient = gradient;

        _pathLineRenderer.enabled = true;
    }

    public void HidePath()
    {
        _currentPath = null;
        if (_pathLineRenderer != null)
        {
            _pathLineRenderer.enabled = false;
        }
    }

    public bool HasPath()
    {
        return _currentPath != null && _currentPath.Count > 0;
    }

    public List<TileSettings> GetCurrentPath()
    {
        return _currentPath;
    }
}

using UnityEngine;
using System.Collections.Generic;

public class TileSelection : MonoBehaviour
{
    [SerializeField] private Camera _topCamera;  // Reference to the top camera
    [SerializeField] private GridGenerator _gridGenerator; // Reference to the grid generator
    private GameObject _selectedTile; // The tile that was selected by the player
    private TileSettings _tileSettings; // Reference to the TileSettings script
    private TileOccupants _tileOccupants; // Reference to the TileOccupants script
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);  
    private HashSet<TileSettings> _tilesInRange = new HashSet<TileSettings>();
    private bool _isSelectionEnabled = false;
    private bool _hasSelectedTile = false;

    void Start()
    {
        _tileOccupants = this.gameObject.GetComponent<TileOccupants>();
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found! Make sure there's a GridGenerator in the scene.");
            }
        }
    }

    void Update()
    {
        // Only process input if selection is enabled and we haven't selected a tile yet
        if (!_isSelectionEnabled || _hasSelectedTile) return;

        // Handle mouse input
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _topCamera.ScreenPointToRay(Input.mousePosition);
            SelectTile(ray);
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = _topCamera.ScreenPointToRay(touch.position);
                SelectTile(ray);
            }
        }
    }

    private void SelectTile(Ray _ray)
    {
        if (!_isSelectionEnabled)
        {
            Debug.Log("Cannot select tiles until FindTilesInRange is called!");
            return;
        }

        if (_hasSelectedTile)
        {
            Debug.Log("Already selected a tile! Call FindTilesInRange again to select another.");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(_ray, out hit, 100f))
        {
            _selectedTile = hit.collider.gameObject;
            TileSettings hitTile = _selectedTile.GetComponent<TileSettings>();
            
            if (hitTile != null && _tilesInRange.Contains(hitTile))
            {
                UpdateCoordinates();
                _hasSelectedTile = true;
                ClearTilesInRange();
            }
            else
            {
                Debug.Log("Selected tile is not in range!");
                _selectedTile = null;
            }
            
            Debug.DrawLine(_ray.origin, hit.point, Color.red, 1f);
        }
        else
        {
            Debug.DrawRay(_ray.origin, _ray.direction * 100f, Color.yellow, 1f);
            Debug.Log("No hit detected. Ray origin: " + _ray.origin + ", direction: " + _ray.direction);
        }
    }

    private void UpdateCoordinates()
    {
        _tileSettings = _selectedTile.GetComponent<TileSettings>();

        if (_tileSettings.occupantType.Equals(TileSettings.OccupantType.None))
        {
            _tileOccupants.row = _tileSettings.row;
            _tileOccupants.column = _tileSettings.column;
            _tileOccupants.MoveToTile();
        }
        else
        {
            Debug.Log($"Selected tile is occupied by: {_tileSettings.occupantType} , cannot move here.");
        }
    }

    private bool IsValidMovement(int startRow, int startCol, int targetRow, int targetCol, int range)
    {
        int rowDiff = Mathf.Abs(targetRow - startRow);
        int colDiff = Mathf.Abs(targetCol - startCol);
        
        // Exclude current position
        if (rowDiff == 0 && colDiff == 0) return false;

        // For range 1, only allow orthogonal movement
        if (range == 1)
        {
            return (rowDiff == 0 && colDiff == 1) || (colDiff == 0 && rowDiff == 1);
        }
        
        // For range >= 2, allow:
        // 1. Orthogonal movement within range
        // 2. Diagonal movement (one step in each direction)
        bool isOrthogonal = (rowDiff == 0 && colDiff <= range) || (colDiff == 0 && rowDiff <= range);
        bool isDiagonal = rowDiff == 1 && colDiff == 1;
        
        return isOrthogonal || isDiagonal;
    }

    public void FindTilesInRange(int range)
    {
        if (_gridGenerator == null || _tileOccupants == null)
        {
            Debug.LogError("Required references missing in TileSelection!");
            return;
        }

        ClearTilesInRange();
        _isSelectionEnabled = true;
        _hasSelectedTile = false;

        // Get the current tile's coordinates
        int currentRow = _tileOccupants.row;
        int currentColumn = _tileOccupants.column;

        // Debug the current position
        Debug.Log($"Current position: Row {currentRow}, Column {currentColumn}");

        // Check all tiles within range
        for (int row = currentRow - range; row <= currentRow + range; row++)
        {
            for (int column = currentColumn - range; column <= currentColumn + range; column++)
            {
                // Check if the tile is:
                // 1. Within grid bounds
                // 2. Valid movement based on range
                if (IsTileInBounds(row, column))
                {
                    TileSettings tile = FindTileAtCoordinates(row, column);
                    if (tile != null && 
                        tile.occupantType.Equals(TileSettings.OccupantType.None) &&
                        IsValidMovement(currentRow, currentColumn, row, column, range))
                    {
                        _tilesInRange.Add(tile);
                        HighlightTile(row, column);
                        Debug.Log($"Added tile at Row {row}, Column {column} to range. Movement type: {(Mathf.Abs(row - currentRow) == 1 && Mathf.Abs(column - currentColumn) == 1 ? "Diagonal" : "Orthogonal")}");
                    }
                }
            }
        }

        if (_tilesInRange.Count == 0)
        {
            Debug.Log("No valid tiles in range!");
            _isSelectionEnabled = false;
        }
        else
        {
            Debug.Log($"Found {_tilesInRange.Count} valid tiles in range");
        }
    }

    private void ClearTilesInRange()
    {
        // Reset all tiles in range to their default color
        foreach (var tile in _tilesInRange)
        {
            if (tile != null)
            {
                tile.SetTileColor(_defaultTileColor);
            }
        }
        
        _tilesInRange.Clear();
        _isSelectionEnabled = false;
    }

    private bool IsTileInBounds(int row, int column)
    {
        if (_gridGenerator == null) return false;
        return row >= 0 && row < _gridGenerator.height && column >= 0 && column < _gridGenerator.width;
    }

    private void HighlightTile(int row, int column)
    {
        if (_gridGenerator == null) return;

        TileSettings tile = FindTileAtCoordinates(row, column);
        if (tile != null)
        {
            if (tile.gameObject.TryGetComponent<Renderer>(out var renderer) && renderer.material != null)
            {
                tile.SetTileColor(_playerTileColor);
                Debug.Log($"Tile at ({row}, {column}) is highlighted.");
            }
            else
            {
                Debug.LogWarning($"Tile at ({row}, {column}) is missing Renderer or Material!");
            }
        }
        else
        {
            Debug.LogWarning($"Could not find tile at ({row}, {column})");
        }
    }

    TileSettings FindTileAtCoordinates(int row, int column)
    {
        if (_gridGenerator == null) return null;

        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.row == row && currentTile.column == column)
            {
                return currentTile;
            }
        }
        return null;
    }
}

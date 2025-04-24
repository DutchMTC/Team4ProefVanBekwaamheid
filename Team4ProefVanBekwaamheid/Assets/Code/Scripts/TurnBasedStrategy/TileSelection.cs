using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class TileSelection : MonoBehaviour
{
    public UnityEvent<TileSettings> OnTileSelected = new UnityEvent<TileSettings>();
    
    [SerializeField] private Camera _topCamera;
    [SerializeField] private GridGenerator _gridGenerator;
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
    private TileOccupants _tileOccupants;
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);  
    private HashSet<TileSettings> _tilesInRange = new HashSet<TileSettings>();
    private bool _isSelectionEnabled = false;
    private bool _hasSelectedTile = false;

    public bool IsSelectingTiles => _isSelectionEnabled;
    public TileSettings CurrentSelectedTile => _tileSettings;
    public HashSet<TileSettings> TilesInRange => new HashSet<TileSettings>(_tilesInRange);

    void Start()
    {
        _tileOccupants = GetComponent<TileOccupants>();
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found!");
            }
        }
    }

    void Update()
    {
        if (!_isSelectionEnabled || _hasSelectedTile) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _topCamera.ScreenPointToRay(Input.mousePosition);
            SelectTile(ray);
        }

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

    public void StartTileSelection(int range, Vector2Int currentPosition)
    {
        ClearTilesInRange();
        _isSelectionEnabled = true;
        _hasSelectedTile = false;
        FindTilesInRange(range, currentPosition.x, currentPosition.y);
    }

    public void CancelTileSelection()
    {
        ClearTilesInRange();
        _isSelectionEnabled = false;
        _hasSelectedTile = false;
    }

    private void FindTilesInRange(int range, int currentRow, int currentColumn)
    {
        for (int row = currentRow - range; row <= currentRow + range; row++)
        {
            for (int column = currentColumn - range; column <= currentColumn + range; column++)
            {
                if (IsTileInBounds(row, column))
                {
                    TileSettings tile = FindTileAtCoordinates(row, column);
                    if (tile != null && 
                        tile.occupantType == TileSettings.OccupantType.None &&
                        IsValidMovement(currentRow, currentColumn, row, column, range))
                    {
                        _tilesInRange.Add(tile);
                        HighlightTile(row, column);
                    }
                }
            }
        }

        if (_tilesInRange.Count == 0)
        {
            Debug.Log("No valid tiles in range!");
            _isSelectionEnabled = false;
        }
    }

    private void SelectTile(Ray ray)
    {
        if (!_isSelectionEnabled)
        {
            Debug.Log("Cannot select tiles until selection is started!");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            _selectedTile = hit.collider.gameObject;
            TileSettings hitTile = _selectedTile.GetComponent<TileSettings>();
            
            if (hitTile != null && _tilesInRange.Contains(hitTile))
            {
                _hasSelectedTile = true;
                _tileSettings = hitTile;
                OnTileSelected.Invoke(hitTile);
                ClearTilesInRange();
            }
            else
            {
                Debug.Log("Selected tile is not in range!");
                _selectedTile = null;
            }
        }
    }

    public void ClearTilesInRange()
    {
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

    private bool IsValidMovement(int startRow, int startCol, int targetRow, int targetCol, int range)
    {
        int rowDiff = Mathf.Abs(targetRow - startRow);
        int colDiff = Mathf.Abs(targetCol - startCol);
        
        if (rowDiff == 0 && colDiff == 0) return false;

        if (range == 1)
        {
            return (rowDiff == 0 && colDiff == 1) || (colDiff == 0 && rowDiff == 1);
        }
        
        bool isOrthogonal = (rowDiff == 0 && colDiff <= range) || (colDiff == 0 && rowDiff <= range);
        bool isDiagonal = rowDiff == 1 && colDiff == 1;
        
        return isOrthogonal || isDiagonal;
    }

    private void HighlightTile(int row, int column)
    {
        TileSettings tile = FindTileAtCoordinates(row, column);
        if (tile != null && tile.gameObject.TryGetComponent<Renderer>(out var renderer))
        {
            tile.SetTileColor(_playerTileColor);
        }
    }

    private bool IsTileInBounds(int row, int column)
    {
        if (_gridGenerator == null) return false;
        return row >= 0 && row < _gridGenerator.height && column >= 0 && column < _gridGenerator.width;
    }

    private TileSettings FindTileAtCoordinates(int row, int column)
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

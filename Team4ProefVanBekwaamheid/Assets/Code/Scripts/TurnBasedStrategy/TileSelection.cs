using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class TileSelection : MonoBehaviour
{
    public enum SelectionType
    {
        Movement,
        Attack
    }

    public enum UserType
    {
        Player,
        Enemy
    }

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
    }    public void StartTileSelection(int range, Vector2Int currentPosition, SelectionType selectionType, UserType userType)
    {
        ClearTilesInRange();
        _isSelectionEnabled = true;
        _hasSelectedTile = false;
        // Vector2Int should be (gridX, gridY) where:
        // currentPosition.x is gridX (column)
        // currentPosition.y is gridY (row)
        FindTilesInRange(range, currentPosition.y, currentPosition.x, selectionType, userType);
    }

    public void CancelTileSelection()
    {
        ClearTilesInRange();
        _isSelectionEnabled = false;
        _hasSelectedTile = false;
    }

    private void FindTilesInRange(int range, int currentGridY, int currentGridX, SelectionType selectionType, UserType userType) // Renamed parameters
    {
        for (int gridY = currentGridY - range; gridY <= currentGridY + range; gridY++) // Renamed loop variable
        {
            for (int gridX = currentGridX - range; gridX <= currentGridX + range; gridX++) // Renamed loop variable
            {
                if (IsTileInBounds(gridY, gridX)) // Used renamed variables
                {
                    TileSettings tile = FindTileAtCoordinates(gridY, gridX); // Used renamed variables
                    if (tile != null && IsValidMovement(currentGridY, currentGridX, gridY, gridX, range)) // Used renamed variables
                    {
                        bool isValidTile = false;
                        
                        switch (selectionType)
                        {
                            case SelectionType.Movement:
                                isValidTile = tile.occupantType == TileSettings.OccupantType.None || tile.occupantType == TileSettings.OccupantType.Item; // Allow moving onto item tiles
                                break;
                            
                            case SelectionType.Attack:
                                if (userType == UserType.Player)
                                    isValidTile = tile.occupantType == TileSettings.OccupantType.Enemy;
                                else
                                    isValidTile = tile.occupantType == TileSettings.OccupantType.Player;
                                break;
                        }

                        if (isValidTile)
                        {
                            _tilesInRange.Add(tile);
                            HighlightTile(gridY, gridX); // Used renamed variables
                        }
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
                _tileSettings.getObjects();
                ClearTilesInRange();
            }
            else
            {
                Debug.Log("Selected tile is not in range!");
                _selectedTile = null;
            }
        }
    }    public void ClearTilesInRange()
    {
        foreach (var tile in _tilesInRange)
        {
            if (tile != null)
            {
                tile.OccupationChangedEvent.Invoke();
            }
        }
        
        _tilesInRange.Clear();
        _isSelectionEnabled = false;
    }

    private bool IsValidMovement(int startGridY, int startGridX, int targetGridY, int targetGridX, int range) // Renamed parameters
    {
        int gridYDiff = Mathf.Abs(targetGridY - startGridY); // Renamed variable
        int gridXDiff = Mathf.Abs(targetGridX - startGridX); // Renamed variable
        
        if (gridYDiff == 0 && gridXDiff == 0) return false;

        // For range 1, it's strictly orthogonal or diagonal adjacent
        if (range == 1)
        {
            // Orthogonal: (0,1) or (1,0)
            // Diagonal: (1,1)
            return (gridYDiff == 0 && gridXDiff == 1) || (gridXDiff == 0 && gridYDiff == 1) || (gridYDiff == 1 && gridXDiff == 1);
        }
        
        // For range > 1, use Manhattan distance (diamond shape)
        return (gridYDiff + gridXDiff) <= range;
    }

    private void HighlightTile(int gridY, int gridX) // Renamed parameters
    {
        TileSettings tile = FindTileAtCoordinates(gridY, gridX); // Used renamed parameters
        if (tile != null && tile.gameObject.TryGetComponent<Renderer>(out var renderer))
        {
            tile.SetTileColor(_playerTileColor);
        }
    }

    private bool IsTileInBounds(int gridY, int gridX) // Renamed parameters
    {
        if (_gridGenerator == null) return false;
        return gridY >= 0 && gridY < _gridGenerator.height && gridX >= 0 && gridX < _gridGenerator.width; // Used renamed parameters
    }

    private TileSettings FindTileAtCoordinates(int gridY, int gridX) // Renamed parameters
    {
        if (_gridGenerator == null) return null;

        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.gridY == gridY && currentTile.gridX == gridX) // Used renamed properties
            {
                return currentTile;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns a list of the currently selectable tiles (those highlighted).
    /// </summary>
    /// <returns>A new List containing the TileSettings of selectable tiles.</returns>
    public List<TileSettings> GetSelectableTiles()
    {
        // Return a copy to prevent external modification of the internal HashSet
        return new List<TileSettings>(_tilesInRange);
    }
}

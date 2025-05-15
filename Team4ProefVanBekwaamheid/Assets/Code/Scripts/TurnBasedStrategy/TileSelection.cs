using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField, Range(1f, 20f), Tooltip("Speed at which the player moves between tiles")] 
    private float moveSpeed = 5f; // Speed for smooth movement
    
    // Allows other scripts to modify the movement speed
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Clamp(value, 1f, 20f);
    }
    private GameObject _selectedTile;    private TileSettings _tileSettings;
    private TileOccupants _tileOccupants;
    private PathVisualizer pathVisualizer;
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);  
    private HashSet<TileSettings> _tilesInRange = new HashSet<TileSettings>();
    private bool _isSelectionEnabled = false;
    private bool _hasSelectedTile = false;
    private List<TileSettings> _currentPath;

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
        }        pathVisualizer = FindObjectOfType<PathVisualizer>();
        if (pathVisualizer == null)
        {
            // Create a new GameObject for the PathVisualizer
            var pathVisualizerObject = new GameObject("Path Visualizer");
            pathVisualizer = pathVisualizerObject.AddComponent<PathVisualizer>();
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

    public void StartTileSelection(int range, Vector2Int currentPosition, SelectionType selectionType, UserType userType)
    {
        ClearTilesInRange();
        _isSelectionEnabled = true;
        _hasSelectedTile = false;
        FindTilesInRange(range, currentPosition.y, currentPosition.x, selectionType, userType);
    }

    private void FindTilesInRange(int range, int currentGridY, int currentGridX, SelectionType selectionType, UserType userType)
    {
        for (int gridY = currentGridY - range; gridY <= currentGridY + range; gridY++)
        {
            for (int gridX = currentGridX - range; gridX <= currentGridX + range; gridX++)
            {
                if (IsTileInBounds(gridY, gridX))
                {
                    TileSettings tile = FindTileAtCoordinates(gridY, gridX);
                    if (tile != null && IsValidMovement(currentGridY, currentGridX, gridY, gridX, range))
                    {
                        bool isValidTile = false;
                        
                        switch (selectionType)
                        {
                            case SelectionType.Movement:
                                isValidTile = tile.occupantType == TileSettings.OccupantType.None || 
                                            tile.occupantType == TileSettings.OccupantType.Item ||
                                            tile.occupantType == TileSettings.OccupantType.Trap;
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
                            HighlightTile(gridY, gridX);
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
                bool selectionComplete = HandlePathSelection(hitTile);
                if (selectionComplete)
                {
                    ClearTilesInRange();
                }
            }
            else
            {
                Debug.Log("Selected tile is not in range!");
                _selectedTile = null;
            }
        }
    }

    public void CancelTileSelection()
    {
        ClearTilesInRange();
        _isSelectionEnabled = false;
        _hasSelectedTile = false;
    }

    private bool IsValidMovement(int startGridY, int startGridX, int targetGridY, int targetGridX, int range)
    {
        int gridYDiff = Mathf.Abs(targetGridY - startGridY);
        int gridXDiff = Mathf.Abs(targetGridX - startGridX);
        
        if (gridYDiff == 0 && gridXDiff == 0) return false;        // For range 1, it's strictly orthogonal movement only (no diagonals)
        if (range == 1)
        {
            // Only allow moving one tile horizontally OR vertically
            return (gridYDiff == 0 && gridXDiff == 1) || (gridXDiff == 0 && gridYDiff == 1);
        }
        
        // For range > 1, use Manhattan distance (diamond shape)
        return (gridYDiff + gridXDiff) <= range;
    }

    private void HighlightTile(int gridY, int gridX)
    {
        TileSettings tile = FindTileAtCoordinates(gridY, gridX);
        if (tile != null && tile.gameObject.TryGetComponent<Renderer>(out var renderer))
        {
            tile.SetTileColor(_playerTileColor);
        }
    }

    private bool IsTileInBounds(int gridY, int gridX)
    {
        if (_gridGenerator == null) return false;
        return gridY >= 0 && gridY < _gridGenerator.height && gridX >= 0 && gridX < _gridGenerator.width;
    }

    private TileSettings FindTileAtCoordinates(int gridY, int gridX)
    {
        var tiles = GetAllTiles();
        return tiles.FirstOrDefault(t => t.gridY == gridY && t.gridX == gridX);
    }

    public List<TileSettings> GetSelectableTiles()
    {
        return new List<TileSettings>(_tilesInRange);
    }

    public void ClearTilesInRange()
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

    private bool HandlePathSelection(TileSettings hitTile)
    {
        // Find player's current tile
        var playerTile = FindTileAtCoordinates(_tileOccupants.gridY, _tileOccupants.gridX);
        if (playerTile == null)
        {
            Debug.LogError("Could not find player's current tile!");
            return false;
        }        // Get all tiles and find path from player to destination
        var allTiles = GetAllTiles();
        _currentPath = MovementValidator.FindPath(playerTile, hitTile, allTiles);

        if (_currentPath != null && _currentPath.Count > 0)
        {
            _hasSelectedTile = true;
            _tileSettings = hitTile;
              // Show and follow the path
            if (pathVisualizer != null)
            {
                pathVisualizer.ShowPath(_currentPath);
                StartCoroutine(MoveAlongPath(_currentPath));
                return true;
            }
            return true;
        }
        else
        {
            Debug.Log("No valid path found to selected tile!");
            return false;
        }
    }

    private System.Collections.IEnumerator MoveAlongPath(List<TileSettings> path)
    {
        var playerObject = _tileOccupants.gameObject;
        
        // Skip the first tile as it's the player's current position
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 startPos = playerObject.transform.position;
            Vector3 targetPos = new Vector3(
                path[i].transform.position.x,
                playerObject.transform.position.y, // Maintain Y position
                path[i].transform.position.z
            );

            float journeyLength = Vector3.Distance(startPos, targetPos);
            float startTime = Time.time;

            while (Time.time - startTime < journeyLength / moveSpeed)
            {
                float distanceCovered = (Time.time - startTime) * moveSpeed;
                float fractionOfJourney = distanceCovered / journeyLength;
                playerObject.transform.position = Vector3.Lerp(startPos, targetPos, fractionOfJourney);
                yield return null;
            }

            playerObject.transform.position = targetPos;
            _tileOccupants.gridX = path[i].gridX;
            _tileOccupants.gridY = path[i].gridY;
            _tileOccupants.MoveToTile();

            // Apply effects or handle interactions based on tile type
            if (path[i].occupantType == TileSettings.OccupantType.Trap)
            {
                // Handle trap tile effects
                // TODO: Implement trap damage or effects
            }
            else if (path[i].occupantType == TileSettings.OccupantType.Item)
            {
                // Handle item pickup
                path[i].getObjects();
            }
        }        // Finish movement
        OnTileSelected.Invoke(_tileSettings);
        _tileSettings.getObjects();
        pathVisualizer.HidePath();
    }

    private List<TileSettings> GetAllTiles()
    {
        var tiles = new List<TileSettings>();
        var coordCheck = new Dictionary<Vector2Int, TileSettings>();

        foreach (Transform child in _gridGenerator.transform)
        {
            var tile = child.GetComponent<TileSettings>();
            if (tile != null)
            {
                var coord = new Vector2Int(tile.gridX, tile.gridY);
                if (coordCheck.ContainsKey(coord))
                {                    Debug.LogWarning($"Duplicate tile found at coordinates ({tile.gridX}, {tile.gridY}). Destroying duplicate.");
                    // Destroy the duplicate tile
                    DestroyImmediate(tile.gameObject);
                }
                else
                {
                    coordCheck[coord] = tile;
                    tiles.Add(tile);
                }
            }
        }
        return tiles;
    }
}

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

public class TileSelection : MonoBehaviour
{
    public enum SelectionType
    {
        Movement,
        Attack,
        Trap
    }

    public enum UserType
    {
        Player,
        Enemy
    }

    public UnityEvent<TileSettings> OnTileSelected = new UnityEvent<TileSettings>();
      [SerializeField] private Camera _topCamera;
      [SerializeField] private GridGenerator _gridGenerator;
      [SerializeField] private CharacterAnimationController characterAnimationController; // Added for dash animations
      [SerializeField] private CharacterRotator characterRotator; // Added for rotation logic
      [SerializeField, Range(1f, 20f), Tooltip("Speed at which the player moves between tiles")]
      private float moveSpeed = 5f; // Speed for smooth movement
    [SerializeField, Tooltip("Time the player pauses on each tile during movement")]
    private float tilePauseDuration = 0.2f;
    [SerializeField, Tooltip("Curve defining the player's movement animation between tiles")]
    private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Default to an ease-in-out curve
    [SerializeField, Tooltip("Duration of the rotation when moving or attacking")]
    private float actionRotationDuration = 0.3f; // Used for rotations

    // Allows other scripts to modify the movement speed
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Clamp(value, 1f, 20f);
    }
    private GameObject _selectedTile;    private TileSettings _tileSettings;
    private TileOccupants _tileOccupants;
    public PathVisualizer pathVisualizer; // Made public
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);  
    private HashSet<TileSettings> _tilesInRange = new HashSet<TileSettings>();
    private bool _isSelectionEnabled = false;
    private bool _hasSelectedTile = false;
    private SelectionType _currentSelectionType; // To store the current selection type
    private UserType _actionUserType; // To store the user type initiating the action
    private List<TileSettings> _currentPath;

    public bool IsSelectingTiles => _isSelectionEnabled;
    public TileSettings CurrentSelectedTile => _tileSettings;
    public HashSet<TileSettings> TilesInRange => new HashSet<TileSettings>(_tilesInRange);

    void Start()
    {
        _tileOccupants = GetComponent<TileOccupants>();
        if (characterRotator == null)
        {
            characterRotator = GetComponent<CharacterRotator>();
            if (characterRotator == null)
            {
                 // Attempt to find it if not assigned, assuming one CharacterRotator on the same GameObject or in children
                characterRotator = GetComponentInChildren<CharacterRotator>();
                if (characterRotator == null)
                {
                    Debug.LogError("CharacterRotator reference not found! Please assign it or add it to the GameObject.");
                }
            }
        }
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found!");
            }
        }
        if (characterAnimationController == null)
        {
            // Attempt to find it if not assigned, assuming one CharacterAnimationController in the scene
            characterAnimationController = FindObjectOfType<CharacterAnimationController>();
            if (characterAnimationController == null)
            {
                Debug.LogWarning("TileSelection: CharacterAnimationController not found in scene and not assigned. Dash animations might not play.");
            }
        }
        pathVisualizer = FindObjectOfType<PathVisualizer>();
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
        _currentSelectionType = selectionType; // Store the selection type
        _actionUserType = userType; // Store the user type initiating the action
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
                                break;                            case SelectionType.Trap:
                                isValidTile = tile.occupantType == TileSettings.OccupantType.None || 
                                            tile.occupantType == TileSettings.OccupantType.Item;
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
                if (_currentSelectionType == SelectionType.Attack)
                {
                    _hasSelectedTile = true;
                    _tileSettings = hitTile;
                    // Rotate towards the target before invoking OnTileSelected
                    // Pass the _actionUserType captured in StartTileSelection
                    StartCoroutine(RotateBeforeAttack(hitTile, _actionUserType, () => {
                        OnTileSelected.Invoke(hitTile);
                        ClearTilesInRange(); // This also sets _isSelectionEnabled = false
                    }));
                }
                else if (_currentSelectionType == SelectionType.Movement)
                {
                    bool selectionComplete = HandlePathSelection(hitTile);
                    if (selectionComplete)
                    {
                        ClearTilesInRange(); // This also sets _isSelectionEnabled = false
                    }
                    // If selection is not complete (e.g. no path), selection remains enabled with highlighted tiles.
                }                else if (_currentSelectionType == SelectionType.Trap)
                {
                    // For trap placement, directly handle the selection since we don't need path finding
                    Debug.Log("Placing trap on selected tile...");
                    _hasSelectedTile = true;
                    _tileSettings = hitTile;
                    OnTileSelected.Invoke(hitTile);
                    ClearTilesInRange(); // This also sets _isSelectionEnabled = false
                }
                else
                {
                    Debug.LogWarning($"Unknown selection type: {_currentSelectionType}");
                    _selectedTile = null;
                }
            }
            else
            {
                Debug.Log("Selected tile is not in range or not a valid tile!");
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

    public TileSettings FindTileAtCoordinates(int gridY, int gridX)
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
                // Pass UserType.Player for player-controlled movement and the animation controller
                StartCoroutine(MoveAlongPath(_currentPath, _tileOccupants.gameObject, _tileOccupants, UserType.Player, characterAnimationController));
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

    public System.Collections.IEnumerator MoveAlongPath(List<TileSettings> path, GameObject entityToMove, TileOccupants entityOccupants, UserType currentUserType, CharacterAnimationController animController)
    {
        if (path == null || path.Count <= 1)
        {
            if (pathVisualizer != null) pathVisualizer.HidePath();
            yield break; // No path or only start tile
        }

        // Skip the first tile as it's the entity's current position
        for (int i = 1; i < path.Count; i++)
        {
            // Rotate towards the next tile first
            Vector3 directionToNextTile = path[i].transform.position - entityToMove.transform.position;
            directionToNextTile.y = 0; // Keep rotation on the Y axis (horizontal plane)
            if (directionToNextTile != Vector3.zero && characterRotator != null)
            {
                // Determine the correct UserType for CharacterRotator
                CharacterRotator.UserType rotatorUserType = (currentUserType == TileSelection.UserType.Player) ? CharacterRotator.UserType.Player : CharacterRotator.UserType.Enemy;
                // Call the modified RotateTowardsAsync without the explicit offset
                yield return StartCoroutine(characterRotator.RotateTowardsAsync(entityToMove.transform, directionToNextTile, rotatorUserType, actionRotationDuration));
            }

            // Trigger Dash animation before moving to *this* specific tile in the path
            if (animController != null)
            {
                if (currentUserType == UserType.Player)
                {
                    animController.PlayerDash();
                }
                else if (currentUserType == UserType.Enemy)
                {
                    animController.EnemyDash();
                }
            }

            Vector3 startPos = entityToMove.transform.position;
            // Use the target tile's X, Y, and Z for the target position.
            // This ensures movement towards the tile's actual transform position.
            // If entity pivot is not at its base, an additional Y offset might be needed here
            // or handled by the entityOccupants.MoveToTile() method.
            // Reverted Y to maintain entity's current height to prevent sinking
            Vector3 targetPos = new Vector3(
                path[i].transform.position.x,
                entityToMove.transform.position.y,
                path[i].transform.position.z
            );

            float journeyLength = Vector3.Distance(startPos, targetPos);
            float startTime = Time.time;

            // Ensure journeyLength is not zero to avoid division by zero
            if (journeyLength == 0)
            {
                entityToMove.transform.position = targetPos; // Snap to target if already there
            }
            else
            {
                while (Time.time - startTime < journeyLength / moveSpeed)
                {
                    float distanceCovered = (Time.time - startTime) * moveSpeed;
                    float fractionOfJourney = distanceCovered / journeyLength;
                    // Apply the curve to the fraction of the journey
                    float curvedFraction = movementCurve.Evaluate(fractionOfJourney);
                    entityToMove.transform.position = Vector3.Lerp(startPos, targetPos, curvedFraction);
                    yield return null;
                }
            }
            
            entityToMove.transform.position = targetPos;
            entityOccupants.gridX = path[i].gridX;
            entityOccupants.gridY = path[i].gridY;
            entityOccupants.MoveToTile();

            // Pause on the tile
            if (tilePauseDuration > 0)
            {
                yield return new WaitForSeconds(tilePauseDuration);
            }
            // Apply effects or handle interactions based on tile type
            if (path[i].occupantType == TileSettings.OccupantType.Trap)
            {
                // Handle trap tile effects
                var trapBehaviour = path[i].tileOccupant?.GetComponent<TrapBehaviour>();
                if (trapBehaviour != null)
                {
                    trapBehaviour.OnCharacterEnterTile(entityOccupants);
                }
            }
            else if (path[i].occupantType == TileSettings.OccupantType.Item)
            {
                // Handle item pickup
                path[i].getObjects();
            }
        }        // Finish movement
        if (path != null && path.Count > 0)
        {
            var finalTile = path[path.Count - 1];
            OnTileSelected.Invoke(finalTile);
            finalTile.getObjects(); // Interact with the final tile
        }
        
        if (pathVisualizer != null)
        {
            pathVisualizer.HidePath();
        }
    }

    private System.Collections.IEnumerator RotateBeforeAttack(TileSettings targetTile, UserType attackerTileSelectionUserType, System.Action onRotationComplete)
    {
        if (characterRotator != null && _tileOccupants != null && targetTile != null && targetTile.tileOccupant != null)
        {
            // Convert TileSelection.UserType to CharacterRotator.UserType
            CharacterRotator.UserType rotatorAttackerUserType =
                (attackerTileSelectionUserType == TileSelection.UserType.Player) ? CharacterRotator.UserType.Player : CharacterRotator.UserType.Enemy;
            
            // Ensure _tileOccupants.gameObject.transform is used if _tileOccupants is the entity doing the attack
            // and targetTile.tileOccupant.transform is the target's transform.
            yield return StartCoroutine(characterRotator.RotateTowardsTargetAsync(_tileOccupants.transform, targetTile.tileOccupant.transform, rotatorAttackerUserType));
        }
        onRotationComplete?.Invoke();
    }

    public List<TileSettings> GetAllTiles()
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

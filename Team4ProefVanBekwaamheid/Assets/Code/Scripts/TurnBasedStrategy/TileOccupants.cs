using UnityEngine;

public class TileOccupants : MonoBehaviour
{
    [SerializeField] private GridGenerator _gridGenerator;
    public TileSettings.OccupantType myOccupantType;
    public int row;
    public int column;
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
    private Renderer _renderer;
    private int _initializationAttempts = 0;
    private const int MAX_INITIALIZATION_ATTEMPTS = 5;
   
    [SerializeField] private int health = 30;       
    
    void Start()
    {
        _renderer = GetComponent<Renderer>();
        // Wait longer for initialization in builds
        float delay = Application.isEditor ? 0.1f : 0.3f;
        Invoke("InitializePosition", delay);
    }

    private void InitializePosition()
    {
        _initializationAttempts++;
        
        // Prevent infinite retry loops
        if (_initializationAttempts > MAX_INITIALIZATION_ATTEMPTS)
        {
            Debug.LogError($"Failed to initialize {gameObject.name} after {MAX_INITIALIZATION_ATTEMPTS} attempts");
            return;
        }

        // Ensure GridGenerator is initialized
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found!");
                if (_initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
                {
                    Invoke("InitializePosition", 0.2f); // Retry initialization
                }
                return;
            }
        }

        // First ensure the transform is properly set
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        // Ensure renderer is initialized
        if (_renderer == null)
        {
            _renderer = GetComponent<Renderer>();
        }

        // Find and initialize the starting tile's occupation state
        FindTileAtCoordinates();
        
        if (_selectedTile != null)
        {
            // Set initial position based on current row/column
            MoveToTile();
            
            // Force rendering update
            transform.position = transform.position;
            if (_renderer != null)
            {
                StartCoroutine(ForceRendererRefresh());
            }
        }
        else
        {
            // If we still don't have a valid tile, retry after a short delay
            Debug.Log($"Retrying initialization - tile not found yet (Attempt {_initializationAttempts}/{MAX_INITIALIZATION_ATTEMPTS})");
            if (_initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
            {
                Invoke("InitializePosition", 0.2f);
            }
        }
    }

    private System.Collections.IEnumerator ForceRendererRefresh()
    {
        _renderer.enabled = false;
        yield return new WaitForEndOfFrame();
        _renderer.enabled = true;
        yield return new WaitForEndOfFrame();
        // Double-check visibility
        if (!_renderer.enabled)
        {
            _renderer.enabled = true;
        }
    }void Update()
    {
        // Only search for a new tile if the row or column values have changed
        if (_selectedTile == null || (_tileSettings != null && (_tileSettings.row != row || _tileSettings.column != column)))
        {      
            MoveToTile();
        }
    }    public void MoveToTile()
    {
        FindTileAtCoordinates(); // Find the tile at the current coordinates

        if (_selectedTile != null && _tileSettings != null)
        {
            // Check if the tile is already occupied by something other than this object
            if (_tileSettings.occupantType != TileSettings.OccupantType.None && 
                _tileSettings.occupantType != myOccupantType)
            {
                Debug.LogWarning($"Cannot move to tile at ({row}, {column}) - tile is occupied by {_tileSettings.occupantType}");
                return;
            }

            // Move this GameObject to the selected tile's position
            Vector3 selectedTilePos = _selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
            
            // Update the tile's occupation state
            _tileSettings.occupantType = myOccupantType;
            _tileSettings.OccupationChangedEvent.Invoke(); // Trigger the occupation change event
        }
        else
        {
            Debug.LogWarning($"Cannot move to tile at ({row}, {column}) - tile not found");
        }
    }

    private void FindTileAtCoordinates()
    {
        // Ensure GridGenerator is initialized
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in FindTileAtCoordinates!");
                return;
            }
        }

        // Clear occupancy of old tile if we're moving
        if (_tileSettings != null)
        {
            _tileSettings.occupantType = TileSettings.OccupantType.None;
            _tileSettings.OccupationChangedEvent.Invoke(); // Trigger the occupation change event
        }

        // Search through all tiles in the grid
        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.row == row && currentTile.column == column)
            {
                _selectedTile = child.gameObject;
                _tileSettings = currentTile;
                // Set the tile's occupant type based on what this GameObject represents
                _tileSettings.occupantType = myOccupantType;
                Debug.Log($"Selected tile at grid position ({row}, {column}). Set occupant type to: {myOccupantType}");
                return;
            }
        }

        // If we didn't find a tile at those coordinates
        _selectedTile = null;
        _tileSettings = null;
        Debug.LogWarning($"No tile found at grid position ({row}, {column})");
    }

    /// <summary>
    /// Returns the TileSettings of the tile this occupant is currently on.
    /// </summary>
    /// <returns>The current TileSettings, or null if not on a valid tile.</returns>
    public TileSettings GetCurrentTile()
    {
        // Ensure the tile reference is up-to-date, although MoveToTile should handle this.
        // FindTileAtCoordinates(); // Optional: Uncomment if you suspect the reference might become stale.
        return _tileSettings;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        Destroy(gameObject);
    }
}

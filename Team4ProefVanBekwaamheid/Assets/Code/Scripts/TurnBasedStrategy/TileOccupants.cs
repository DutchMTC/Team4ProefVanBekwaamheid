using UnityEngine;

public class TileOccupants : MonoBehaviour
{
    [SerializeField] private GridGenerator _gridGenerator; // Reference to the grid generator
    public TileSettings.OccupantType myOccupantType; // What type of occupant this GameObject is
    public int row;
    public int column;
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
   
   [SerializeField] private int health = 30;       
   void Start()
    {
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found!");
                return;
            }
        }
        
        // Find and initialize the starting tile's occupation state
        FindTileAtCoordinates();
        
        // Set initial position based on current row/column if they're set
        MoveToTile();
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

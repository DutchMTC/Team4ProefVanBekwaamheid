using UnityEngine;

public class TileOccupants : MonoBehaviour
{
     [SerializeField] private GridGenerator gridGenerator; // Reference to the grid generator
    [SerializeField] private TileSettings.OccupantType myOccupantType; // What type of occupant this GameObject is
    public int row;
    public int column;
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
   

    void Start()
    {
        // Set initial position based on current row/column if they're set
        MoveToTile();
    }

    void Update()
    {
        // Only search for a new tile if the row or column values have changed
        if (_selectedTile == null || (_tileSettings != null && (_tileSettings.row != row || _tileSettings.column != column)))
        {      
            MoveToTile(); // Here For Testing purposes, will be removed in a later version of this project
        }
    }

    public void MoveToTile()
    {
        FindTileAtCoordinates(); // Find the tile at the current coordinates

        if (_selectedTile != null && _tileSettings != null && _tileSettings.occupantType.Equals(TileSettings.OccupantType.None))
        {
            // If the tile is occupied, we can't move there
            Debug.Log($"Cannot move to tile at ({row}, {column}) because it is occupied by: {_tileSettings.occupantType}");
        }
        else if (_selectedTile != null && _tileSettings != null)
        {
            // Move this GameObject to the selected tile's position
            Vector3 selectedTilePos = _selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
        }
    }

    private void FindTileAtCoordinates()
    {
        // Clear occupancy of old tile if we're moving
        if (_tileSettings != null)
        {
            _tileSettings.occupantType = TileSettings.OccupantType.None;
        }

        // Search through all tiles in the grid
        foreach (Transform child in gridGenerator.transform)
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
}

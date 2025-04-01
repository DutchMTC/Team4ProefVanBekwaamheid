using UnityEngine;

public class TileOccupants : MonoBehaviour
{
    public int row;
    public int column;
    [SerializeField] private GameObject selectedTile;
    [SerializeField] private TileSettings tileSettings;
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private TileSettings.OccupantType myOccupantType; // What type of occupant this GameObject is

    void Start()
    {
        // Set initial position based on current row/column if they're set
        if (selectedTile != null)
        {
            Vector3 selectedTilePos = selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
        }
    }

    void Update()
    {
        // Only search for a new tile if the row or column values have changed
        if (selectedTile == null || 
            (tileSettings != null && (tileSettings.row != row || tileSettings.column != column)))
        {
            // Clear occupancy of old tile if we're moving
            if (tileSettings != null)
            {
                tileSettings.occupantType = TileSettings.OccupantType.None;
            }
            
            FindTileAtCoordinates();
        }

        if (selectedTile != null && tileSettings != null && !tileSettings.occupied)
        {
            // Move this GameObject to the selected tile's position
            Vector3 selectedTilePos = selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
        }
    }

    private void FindTileAtCoordinates()
    {
        // Search through all tiles in the grid
        foreach (Transform child in gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.row == row && currentTile.column == column)
            {
                selectedTile = child.gameObject;
                tileSettings = currentTile;
                // Set the tile's occupant type based on what this GameObject represents
                tileSettings.occupantType = myOccupantType;
                Debug.Log($"Selected tile at grid position ({row}, {column}). Set occupant type to: {myOccupantType}");
                return;
            }
        }
        
        // If we didn't find a tile at those coordinates
        selectedTile = null;
        tileSettings = null;
        Debug.LogWarning($"No tile found at grid position ({row}, {column})");
    }
}

using UnityEngine;

public class TileOccupants : MonoBehaviour
{
    [SerializeField] private GridGenerator _gridGenerator;
    public TileSettings.OccupantType myOccupantType;
    public int gridY; // Renamed from row
    public int gridX; // Renamed from column
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
    private float _damageReduction = 0f;
   
    [SerializeField] private int health = 30;       
    
    void Awake()
    {
        // Ensure we have a reference to the GridGenerator as early as possible
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in Awake!");
            }
        }
    }
    
    void Start()
    {
        // Double check to make sure we have a GridGenerator reference
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in Start!");
                return;
            }
        }
        
        // Force position update with small delay to ensure GridGenerator is fully initialized
        Invoke("InitializePosition", 0.1f);
    }
    
    void InitializePosition()
    {
        FindTileAtCoordinates();
        MoveToTile();
        
        // Log position for debugging
        Debug.Log($"{gameObject.name} initialized at position ({gridY}, {gridX})");
    }

    public void SetDamageReduction(float reduction)
    {
        _damageReduction = Mathf.Clamp(reduction, 0f, 0.8f);
        Debug.Log($"{gameObject.name} defense set to {_damageReduction * 100}%");
    }

    public void TakeDamage(int amount)
    {
        int reducedDamage = Mathf.RoundToInt(amount * (1f - _damageReduction));
        int previousHealth = health;
        health -= reducedDamage;
        
        string defenseMsg = _damageReduction > 0 ? $"[DEFENSE {_damageReduction * 100}%]" : "[NO DEFENSE]";
        Debug.Log($"{defenseMsg} {gameObject.name} Health: {previousHealth} -> {health} " +
                 $"(Took {reducedDamage} damage, reduced from {amount})");
        
        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} has died from {reducedDamage} damage!");
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }

    void Update()
    {
        // Check if the occupant has moved or if the tile reference is lost
        if (_selectedTile == null || _tileSettings == null || _tileSettings.gridY != gridY || _tileSettings.gridX != gridX)
        {
            MoveToTile();
        }
    }

    public void MoveToTile()
    {
        FindTileAtCoordinates();

        if (_selectedTile != null && _tileSettings != null)
        {
            GameObject itemObjectToPickup = null;
            PickupItem pickupItemScript = null;

            // Check if the target tile (_tileSettings from FindTileAtCoordinates) currently holds an item
            if (_tileSettings.occupantType == TileSettings.OccupantType.Item && _tileSettings.tileOccupant != null)
            {
                pickupItemScript = _tileSettings.tileOccupant.GetComponent<PickupItem>();
                if (pickupItemScript != null)
                {
                    itemObjectToPickup = _tileSettings.tileOccupant; // Store reference to the item GameObject
                    Debug.Log($"Tile ({_tileSettings.gridY}, {_tileSettings.gridX}) has item {itemObjectToPickup.name} with PickupItem script.");
                }
                else
                {
                    Debug.LogWarning($"Tile at ({_tileSettings.gridY}, {_tileSettings.gridX}) is marked as Item but occupant {_tileSettings.tileOccupant.name} has no PickupItem script.");
                }
            }
            
            // Validate if the unit can move to the target tile (_tileSettings)
            // This check is already in place and allows moving to 'Item' tiles.
            if (_tileSettings.occupantType != TileSettings.OccupantType.None &&
                _tileSettings.occupantType != TileSettings.OccupantType.Item &&
                _tileSettings.occupantType != myOccupantType)
            {
                Debug.LogWarning($"Cannot move to tile at ({gridY}, {gridX}) - tile is occupied by {_tileSettings.occupantType} and is not an item or self.");
                return;
            }

            // Actual movement and tile occupation
            Vector3 selectedTilePos = _selectedTile.transform.position;
            // Consider using a y-offset for the unit if needed, or ensure tile pivot is at its base.
            // For now, matching x and z, keeping unit's current y.
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
            
            // Set the unit as the occupant of the new tile.
            // FindTileAtCoordinates already cleared this unit from its previous tile.
            _tileSettings.SetOccupant(myOccupantType, this.gameObject);
            // Note: _tileSettings is the new tile the unit is moving to.
            // The internal gridY and gridX are already set to this tile's coordinates by FindTileAtCoordinates
            // if the call originated from Update(). If it originated from a power-up, gridY/gridX were set before calling MoveToTile.

            // If an item was on this tile, activate its pickup AFTER the unit has officially moved and occupied the tile.
            if (itemObjectToPickup != null && pickupItemScript != null)
            {
                Debug.Log($"Unit {this.gameObject.name} moved to item tile. Activating pickup for {itemObjectToPickup.name}.");
                pickupItemScript.ActivatePickup(this.gameObject);
                // ItemManager will handle destroying the item.
                // The tile's occupant is now this unit.
            }
        }
        else
        {
            Debug.LogWarning($"Cannot move to tile at ({gridY}, {gridX}) - tile not found");
        }
    }

    private void FindTileAtCoordinates()
    {
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in FindTileAtCoordinates!");
                return;
            }
        }

        if (_tileSettings != null) // If this occupant was previously on a tile
        {
            // Only clear the occupant if this specific game object was the occupant
            if (_tileSettings.tileOccupant == this.gameObject)
            {
                _tileSettings.SetOccupant(TileSettings.OccupantType.None, null);
            }
        }

        _selectedTile = null; // Reset before searching
        _tileSettings = null; // Reset before searching

        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.gridY == gridY && currentTile.gridX == gridX)
            {
                _selectedTile = child.gameObject;
                _tileSettings = currentTile;
                // Do not set occupant here. MoveToTile will handle it after validation.
                return;
            }
        }
        
        // If loop completes, no tile was found
        Debug.LogWarning($"No tile found at grid position ({gridY}, {gridX})");
    }

    public TileSettings GetCurrentTile()
    {
        return _tileSettings;
    }
}

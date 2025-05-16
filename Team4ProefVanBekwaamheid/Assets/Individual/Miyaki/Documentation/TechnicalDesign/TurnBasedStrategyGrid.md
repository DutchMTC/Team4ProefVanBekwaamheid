---

# Turn Based Strategy Grid System

## 1. Architecture Overview

The Turn Based Strategy Grid system is responsible for generating and managing the grid of tiles, handling the placement and movement of entities (occupants) on these tiles, and managing basic occupant properties like health. It is built using Unity's component-based architecture.

The primary components are:

- [`GridGenerator.cs`](Code/Scripts/TurnBasedStrategy/GridGenerator.cs): Responsible for creating the grid structure.
- [`TileSettings.cs`](Code/Scripts/TurnBasedStrategy/TileSettings.cs): Represents individual tiles and manages their state, including what occupies them.
- [`TileOccupants.cs`](Code/Scripts/TurnBasedStrategy/TileOccupants.cs): Represents entities that can exist on a tile and handles their properties (health, defense) and movement logic.
- [`MovementValidator.cs`](Code/Scripts/TurnBasedStrategy/MovementValidator.cs): (Currently empty) Intended for validating movement rules.

```
+-----------------+     creates     +---------------+
|                 |---------------->|               |
|  GridGenerator  |                 |  TileSettings |
|                 |                 |               |
+--------+--------+                 +-------+-------+
         |                                  ^
         | finds/uses                       |
         |                                  |
+--------+--------+     manages     +-------+-------+
|                 |<---------------->|               |
| TileOccupants   |                  |  TileSettings |
|                 |                  | (via reference)|
+--------+--------+                  +---------------+
         |
         | (intended)
         | validates
         v
+-------------------+
|                   |
| MovementValidator |
|                   |
+-------------------+
```

## 2. Class Structure

### 2.1 GridGenerator Class
```csharp
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;

    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private float _tileWidth;
    [SerializeField] private float _tileHeight;
    [SerializeField] private float _gridHeight;
    [SerializeField] private float _horizontalOffset;

    public int width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public int height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float tileWidth
    {
        get => _tileWidth;
        set
        {
            if (_tileWidth != value)
            {
                _tileWidth = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    public float tileHeight
    {
        get => _tileHeight;
        set
        {
            if (_tileHeight != value)
            {
                _tileHeight = value;
                if (Application.isPlaying) RegenerateGrid();
            }
        }
    }

    void Start()
    {
        GenerateGrid();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            GenerateGrid();
        }
    }

    void GenerateGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                // Convert grid coordinates to isometric space
                float isometricX = (x - y) * _tileWidth * 0.5f + _horizontalOffset;
                float isometricZ = (x + y) * _tileHeight * 0.5f;

                Vector3 tilePosition = new Vector3(isometricX, _gridHeight, isometricZ);
                GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(0, 45, 0));

                tile.GetComponent<TileSettings>().Initzialize(TileSettings.OccupantType.None, x, y, null); // Initialize tile settings with null occupant
                tile.transform.parent = transform;  // Keep hierarchy organized
            }
        }
    }

    private void RegenerateGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        GenerateGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            RegenerateGrid();
        }
    }
#endif
}
```
The `GridGenerator` script is responsible for creating the grid of tiles in the scene. It uses public and serialized fields to define the grid's dimensions (`width`, `height`), the size of each tile (`tileWidth`, `tileHeight`), the vertical position of the grid (`gridHeight`), and a horizontal offset (`horizontalOffset`). The `tilePrefab` GameObject is used as the template for each tile.

The `GenerateGrid` method iterates through the specified width and height, calculates the isometric position for each tile, instantiates the `tilePrefab` at that position, initializes its `TileSettings` component with its grid coordinates and an initial occupant type of `None`, and parents the new tile GameObject under the `GridGenerator`'s transform for organization.

The `RegenerateGrid` method clears all existing tiles (children of the `GridGenerator`'s transform) and then calls `GenerateGrid` to create a new grid. This is triggered in `Start` and also via property setters for width, height, and tile dimensions if the application is playing. A basic input check in `Update` allows regenerating the grid by pressing the 'R' key. The `OnValidate` method in the Unity Editor also triggers regeneration when properties are changed during play mode.

### 2.2 MovementValidator Class
```csharp

```
The `MovementValidator` class is currently empty. Its intended purpose is likely to contain logic for determining if a `TileOccupant` can move to a specific tile based on various rules (e.g., obstacles, enemy presence, movement range).

### 2.3 TileOccupants Class
```csharp
using UnityEngine;
// Potentially add: using UnityEngine.Events; if you want to use UnityEvents for damage.

public class TileOccupants : MonoBehaviour
{
    [Header("Grid & Occupant Info")]
    [SerializeField] private GridGenerator _gridGenerator;
    public TileSettings.OccupantType myOccupantType;
    public int gridY; // Renamed from row
    public int gridX; // Renamed from column
    private GameObject _selectedTile;
    private TileSettings _tileSettings;

    [Header("Health & Defense")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int health = 30; // Current health
    private float _damageReduction = 0f;

    [Header("UI")]
    [SerializeField] private CharacterHealthUI healthBarUI;
    // public UnityAction<float> OnHealthChanged; // Alternative: Use UnityEvent

    void Awake()
    {
        // Ensure we have a reference to the GridGenerator as early as possible
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in Awake!", this);
            }
        }
        health = maxHealth; // Initialize current health to max health
    }

    void Start()
    {
        // Double check to make sure we have a GridGenerator reference
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in Start!", this);
                return;
            }
        }

        // Initialize Health Bar UI
        if (healthBarUI != null)
        {
            // Determine if this is a player character. Adjust this logic if needed.
            bool isPlayer = (myOccupantType == TileSettings.OccupantType.Player);
            healthBarUI.Initialize(this, maxHealth, health, isPlayer);
        }
        else
        {
            Debug.LogWarning($"HealthBarUI not assigned for {gameObject.name}", this);
        }

        // Force position update with small delay to ensure GridGenerator is fully initialized
        Invoke(nameof(InitializePosition), 0.1f);
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
        Debug.Log($"{gameObject.name} defense set to {_damageReduction * 100}%", this);
    }

    public void TakeDamage(int amount)
    {
        int reducedDamage = Mathf.RoundToInt(amount * (1f - _damageReduction));
        int previousHealth = health;
        health -= reducedDamage;
        health = Mathf.Clamp(health, 0, maxHealth); // Ensure health doesn't go below 0 or above max

        string defenseMsg = _damageReduction > 0 ? $"[DEFENSE {_damageReduction * 100}%]" : "[NO DEFENSE]";
        Debug.Log($"{defenseMsg} {gameObject.name} Health: {previousHealth} -> {health} " +
                 $"(Took {reducedDamage} damage, reduced from {amount})", this);

        // Update Health Bar UI
        if (healthBarUI != null)
        {
            healthBarUI.OnHealthChanged(health);
        }
        // OnHealthChanged?.Invoke(health); // Alternative: if using UnityEvent

        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} has died from {reducedDamage} damage!", this);
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.", this);
        // Optional: Notify healthBarUI or other systems about death
        // if (healthBarUI != null) healthBarUI.HandleDeath();
        Destroy(gameObject);
    }

    // Public method to get current health if needed by other systems
    public int GetCurrentHealth()
    {
        return health;
    }

    // Public method to get max health if needed
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    // Example method to heal the character
    public void Heal(int amount)
    {
        int previousHealth = health;
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        Debug.Log($"{gameObject.name} healed. Health: {previousHealth} -> {health}", this);

        if (healthBarUI != null)
        {
            healthBarUI.OnHealthChanged(health);
        }
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
```
The `TileOccupants` script represents any entity that can occupy a tile on the grid, such as players, enemies, or potentially items (though items might have their own specific scripts). It stores the occupant's type (`myOccupantType`), its current grid coordinates (`gridX`, `gridY`), and references to the `GridGenerator` and the `TileSettings` of the tile it currently occupies.

The script includes basic health and defense mechanics (`maxHealth`, `health`, `_damageReduction`) and methods to manage them (`SetDamageReduction`, `TakeDamage`, `Heal`, `Die`). It also integrates with a `CharacterHealthUI` to visualize health.

Movement is handled by the `MoveToTile` method. This method first calls `FindTileAtCoordinates` to locate the `TileSettings` component for the target grid position. `FindTileAtCoordinates` also handles clearing the occupant from the *previous* tile if the occupant was indeed on a tile. `MoveToTile` then validates if the move is possible (currently only checking if the target tile is not occupied by another non-item occupant of a different type) and, if valid, updates the occupant's world position to match the tile's position and calls `TileSettings.SetOccupant` on the new tile to register itself as the occupant. It also includes logic to detect and activate `PickupItem` scripts on the target tile after moving.

The `Update` method includes a check to call `MoveToTile` if the occupant's stored tile reference becomes null or its grid coordinates no longer match the tile's coordinates, potentially as a way to re-sync the occupant's position with the grid state.

### 2.4 TileSettings Class
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TileSettings : MonoBehaviour
{
    public enum OccupantType
    {
        None,
        Player,
        Enemy,
        Obstacle,
        Item // Added Item type
    }
    public GameObject tileOccupant { get; private set; } // Made setter private, managed by SetOccupant

    // Settings
    public OccupantType occupantType { get; private set; } // Made setter private, managed by SetOccupant
    public int gridX; // Renamed from column
    public int gridY; // Renamed from row
    internal UnityEvent OccupationChangedEvent;
    private Material _tileMaterial;
    private Color _defaultTileColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
    private Color _occupiedTileColor = new Color(1.0f, 0f, 0f, 0.5f);
    private Color _playerTileColor = new Color(0f, 0f, 1.0f, 0.5f);
    private Color _itemTileColor = new Color(0f, 1.0f, 0f, 0.5f); // Color for items

    public override bool Equals(object other)
    {
        if (other is TileSettings otherTile)
        {
            return gridY == otherTile.gridY && gridX == otherTile.gridX;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return gridY.GetHashCode() ^ gridX.GetHashCode();
    }

    public void Initzialize(OccupantType initialOccupantType, int initialGridX, int initialGridY, GameObject initialOccupant = null)
    {
        this.gridX = initialGridX;
        this.gridY = initialGridY;
        SetOccupant(initialOccupantType, initialOccupant); // Use SetOccupant for initialization
    }

    public void SetOccupant(OccupantType newOccupantType, GameObject newOccupant)
    {
        if (this.occupantType != newOccupantType || this.tileOccupant != newOccupant)
        {
            this.occupantType = newOccupantType;
            this.tileOccupant = newOccupant;
            OccupationChangedEvent?.Invoke();
        }
    }

    void Start()
    {
        OccupationChangedEvent = new UnityEvent();
        OccupationChangedEvent.AddListener(OnOccupationChange);
        _tileMaterial = GetComponent<Renderer>().material;
        _tileMaterial.color = _defaultTileColor; // Default color for empty tiles
        getObjects();
    }

    public GameObject[] getObjects()
    {
	    TileOccupants[] tileOccupants = FindObjectsOfType<TileOccupants>();
	    GameObject[] objects = new GameObject[tileOccupants.Length];
        for (int i = 0; i < objects.Length; i++)
        {
            if (tileOccupants[i] == null) continue; // Skip if the TileOccupant is null

            // Check if the TileOccupant is on this tile
            if (tileOccupants[i].gridY != gridY || tileOccupants[i].gridX != gridX) continue; // Changed to .gridY and .gridX

            // If it is, set tileOccupant to be that gameobject
            // tileOccupant = tileOccupants[i].gameObject; // This should be handled by SetOccupant
            SetOccupant(tileOccupants[i].myOccupantType, tileOccupants[i].gameObject); // Corrected to use myOccupantType
            //Debug.Log(tileOccupant.name);
        }

        return objects;
    }

    public void OnOccupationChange()
    {
        switch (occupantType)
        {
            case OccupantType.None:
                _tileMaterial.color = _defaultTileColor;
                break;
            case OccupantType.Player:
                //_tileMaterial.color = _playerTileColor; // Commented out
                break;
            case OccupantType.Enemy:
                _tileMaterial.color = _occupiedTileColor;
                break;
            case OccupantType.Obstacle:
                _tileMaterial.color = _occupiedTileColor;
                break;
            case OccupantType.Item:
                _tileMaterial.color = _itemTileColor; // Set color for Item
                break;
        }
    }

    public void SetTileColor(Color color)
    {
        _tileMaterial.color = color; // Set the tile color to the specified color
    }
}
```
The `TileSettings` script is attached to each individual tile GameObject in the grid. It stores the tile's grid coordinates (`gridX`, `gridY`) and information about what is currently occupying it (`occupantType` enum and a reference to the `tileOccupant` GameObject).

The `OccupantType` enum defines the possible types of entities that can occupy a tile (`None`, `Player`, `Enemy`, `Obstacle`, `Item`).

The `SetOccupant` method is the primary way to change the occupant of a tile. It updates the `occupantType` and `tileOccupant` properties and invokes the `OccupationChangedEvent`.

The `OnOccupationChange` method is a listener for the `OccupationChangedEvent`. It updates the visual appearance of the tile (specifically, its material color) based on the new `occupantType`. Different colors are defined for `None`, `Enemy`, `Obstacle`, and `Item` types. The color for `Player` is commented out.

The `Initzialize` method is used by the `GridGenerator` to set the initial coordinates and occupant. The `getObjects` method seems to be an attempt to find existing `TileOccupants` in the scene and assign them to tiles based on their coordinates, although using `FindObjectsOfType` in `Start` for every tile might be inefficient for large grids.

The `Equals` and `GetHashCode` methods are overridden to allow comparing `TileSettings` objects based on their grid coordinates, which can be useful for using tiles in collections like `HashSet` or `Dictionary`.

## 3. Data Structures and Algorithms

### 3.1 Grid Representation
The grid of tiles is represented implicitly by the child GameObjects under the `GridGenerator`'s transform, each having a `TileSettings` component.

Accessing a specific tile by its coordinates currently involves iterating through all child transforms of the `GridGenerator` and checking their `TileSettings.gridX` and `TileSettings.gridY` properties, as seen in `TileOccupants.FindTileAtCoordinates`.

```csharp
private void FindTileAtCoordinates()
{
    // ... (GridGenerator reference finding)

    // ... (Clearing previous occupant)

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
```
**Time Complexity**: O(N) where N is the total number of tiles in the grid, as it may need to iterate through all tiles in the worst case.
**Space Complexity**: O(1) for the search itself, plus the space used by the grid structure.

*Note: This differs from the Match3 system which uses a 2D array for O(1) access. For larger grids, iterating through children could become a performance bottleneck.*

### 3.2 Grid Generation Algorithm
The grid is generated by iterating through the desired width and height and instantiating a tile prefab for each coordinate pair. The world position for each tile is calculated to create an isometric perspective.

```csharp
void GenerateGrid()
{
    for (int x = 0; x < _width; x++)
    {
        for (int y = 0; y < _height; y++)
        {
            // Convert grid coordinates to isometric space
            float isometricX = (x - y) * _tileWidth * 0.5f + _horizontalOffset;
            float isometricZ = (x + y) * _tileHeight * 0.5f;

            Vector3 tilePosition = new Vector3(isometricX, _gridHeight, isometricZ);
            GameObject tile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(0, 45, 0));

            tile.GetComponent<TileSettings>().Initzialize(TileSettings.OccupantType.None, x, y, null); // Initialize tile settings with null occupant
            tile.transform.parent = transform;  // Keep hierarchy organized
        }
    }
}
```
**Time Complexity**: O(W * H) where W is the grid width and H is the grid height, as it instantiates one tile for each cell.
**Space Complexity**: O(W * H) to store the generated tile GameObjects.

### 3.3 Tile Occupation Management
Tile occupation is managed by the `TileSettings` script using the `SetOccupant` method. This method updates the tile's internal state and triggers an event.

```csharp
public void SetOccupant(OccupantType newOccupantType, GameObject newOccupant)
{
    if (this.occupantType != newOccupantType || this.tileOccupant != newOccupant)
    {
        this.occupantType = newOccupantType;
        this.tileOccupant = newOccupant;
        OccupationChangedEvent?.Invoke();
    }
}
```
`TileOccupants` uses this method to claim or vacate a tile during movement.

### 3.4 Movement Logic
Movement for `TileOccupants` involves finding the target tile and updating the occupant's position and the tile's occupant reference.

The `MoveToTile` method in `TileOccupants` orchestrates this:
1.  Calls `FindTileAtCoordinates` to locate the target tile and clear the occupant from its previous tile.
2.  Performs a basic validation check (currently only against non-item occupants of different types).
3.  Updates the occupant's world position to match the target tile's position.
4.  Calls `TileSettings.SetOccupant` on the target tile.
5.  Includes logic to handle picking up items on the target tile.

```csharp
public void MoveToTile()
{
    FindTileAtCoordinates();

    if (_selectedTile != null && _tileSettings != null)
    {
        // ... (Item pickup logic)

        // Validate if the unit can move to the target tile (_tileSettings)
        if (_tileSettings.occupantType != TileSettings.OccupantType.None &&
            _tileSettings.occupantType != TileSettings.OccupantType.Item &&
            _tileSettings.occupantType != myOccupantType)
        {
            Debug.LogWarning($"Cannot move to tile at ({gridY}, {gridX}) - tile is occupied by {_tileSettings.occupantType} and is not an item or self.");
            return;
        }

        // Actual movement and tile occupation
        Vector3 selectedTilePos = _selectedTile.transform.position;
        transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);

        // Set the unit as the occupant of the new tile.
        _tileSettings.SetOccupant(myOccupantType, this.gameObject);

        // ... (Item pickup activation)
    }
    else
    {
        Debug.LogWarning($"Cannot move to tile at ({gridY}, {gridX}) - tile not found");
    }
}
```

### 3.5 Health and Damage Logic
The `TileOccupants` script includes methods for managing health, applying damage (with reduction), healing, and handling death.

```csharp
public void TakeDamage(int amount)
{
    int reducedDamage = Mathf.RoundToInt(amount * (1f - _damageReduction));
    int previousHealth = health;
    health -= reducedDamage;
    health = Mathf.Clamp(health, 0, maxHealth);

    // ... (Logging and UI update)

    if (health <= 0)
    {
        // ... (Logging)
        Die();
    }
}

public void Heal(int amount)
{
    int previousHealth = health;
    health += amount;
    health = Mathf.Clamp(health, 0, maxHealth);
    // ... (Logging and UI update)
}

private void Die()
{
    // ... (Logging)
    Destroy(gameObject);
}
```
These methods provide a basic combat system for grid occupants.

## 4. Input Handling

Basic input is handled in the `GridGenerator` to trigger grid regeneration using the 'R' key.

```csharp
private void Update()
{
    if (Input.GetKeyDown(KeyCode.R))
    {
        RegenerateGrid();
    }
}
```
Input for moving `TileOccupants` is not present in the provided scripts and is likely handled by an external system that calls the `MoveToTile` method on the desired occupant.

## 5. Performance Considerations

### 5.1 Optimization Techniques
- **Tile Lookup:** The current method of finding a tile by iterating through all children of the `GridGenerator` can be inefficient for large grids. Implementing a 2D array or dictionary lookup in `GridGenerator` to quickly access `TileSettings` by coordinates would significantly improve performance.
- **`FindObjectsOfType`:** The `TileSettings.getObjects` method uses `FindObjectsOfType<TileOccupants>()`, which can be slow, especially if called frequently or with many objects in the scene. This method's purpose seems to be initializing tile occupants at the start, but a more efficient approach might be needed if this pattern is used elsewhere.

### 5.2 Memory Management
- Tile and Occupant GameObjects are instantiated and destroyed as needed (e.g., during grid regeneration or when an occupant dies). Standard Unity garbage collection applies.

## 6. Extensibility Points

### 6.1 Adding New Occupant Types
New types of occupants can be added by:
1.  Adding new values to the `TileSettings.OccupantType` enum.
2.  Assigning the new `OccupantType` to `TileOccupants` GameObjects in the Inspector or through code.
3.  Adding cases to the `TileSettings.OnOccupationChange` switch statement if the new type requires a different visual appearance.
4.  Modifying the validation logic in `TileOccupants.MoveToTile` if the new type has specific movement restrictions or interactions.

### 6.2 Adding New Tile Properties
Additional properties or behaviors for tiles can be added by modifying the `TileSettings` script (e.g., adding properties for tile cost, terrain type, events triggered on entry).

### 6.3 Implementing Movement Validation
The empty `MovementValidator.cs` script is an explicit extensibility point for implementing complex movement rules based on grid state, occupant properties, etc.

## 7. Testing Strategy

### 7.1 Unit Tests
- Test `TileOccupants` health and damage mechanics (`TakeDamage`, `Heal`, `SetDamageReduction`, `Die`).
- Test `TileSettings.SetOccupant` and `OnOccupationChange` to ensure tile state and appearance update correctly.

### 7.2 Integration Tests
- Test `GridGenerator.GenerateGrid` and `RegenerateGrid` to ensure the correct number of tiles are created and initialized.
- Test `TileOccupants.MoveToTile` and `FindTileAtCoordinates` to ensure occupants move to the correct tiles and tile occupation is updated accurately, including handling items.
- Test interactions between `TileOccupants` and `TileSettings` during movement and occupation changes.

## 8. Future Improvements

### 8.1 Architectural Considerations
- **Dedicated Grid Manager:** Consider a central grid manager class (potentially enhancing `GridGenerator` or creating a new one) that holds a 2D array or dictionary of `TileSettings` for efficient lookup. This manager could also handle overall grid state and potentially coordinate interactions between occupants and tiles.
- **Event System:** While `TileSettings` uses a UnityEvent, a more centralized event system could be beneficial for decoupling components and handling broader grid events (e.g., "OccupantMoved", "TileOccupied").

### 8.2 Technical Debt
- **Tile Lookup Efficiency:** The current method of finding tiles by iterating through children should be refactored for performance.
- **`TileSettings.getObjects`:** Review the usage and necessity of this method and potentially replace `FindObjectsOfType` with a more targeted approach if needed.
- **Movement Validation:** Implement the logic in `MovementValidator.cs` to centralize and manage movement rules.

---


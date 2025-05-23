using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    public List<ItemData> itemDatas; // Assignable in Inspector (Heal, Armor ScriptableObjects)
    public int numberOfItemsToSpawn = 3; // Configurable number of items
    public float respawnDelay = 60f; // Default 1 minute

    private GridGenerator _gridGenerator;
    private List<GameObject> _activeItems = new List<GameObject>();
    private List<Vector2Int> _occupiedItemTiles = new List<Vector2Int>();

    void Awake() // Changed to Awake for Instance initialization
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if ItemManager should persist across scenes
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        _gridGenerator = FindObjectOfType<GridGenerator>();
        if (_gridGenerator == null)
        {
            return;
        }

        if (itemDatas == null || itemDatas.Count == 0)
        {
            return;
        }
        SpawnInitialItems();
    }

    void SpawnInitialItems()
    {
        for (int i = 0; i < numberOfItemsToSpawn; i++)
        {
            SpawnRandomItem();
        }
    }

    void SpawnRandomItem()
    {
        if (itemDatas.Count == 0) return;

        ItemData selectedItemData = itemDatas[Random.Range(0, itemDatas.Count)];
        GameObject itemPrefab = selectedItemData.itemPrefab;

        if (itemPrefab == null)
        {
            return;
        }
        
        Vector3? spawnPosition = GetRandomUnoccupiedTilePosition();

        if (spawnPosition.HasValue)
        {
            Vector2Int gridCoords = GetGridCoordinates(spawnPosition.Value);
            GameObject newItem = Instantiate(itemPrefab, spawnPosition.Value, Quaternion.identity);
            newItem.transform.parent = this.transform; // Optional: Keep items organized under ItemManager
            
            PickupItem pickupScript = newItem.GetComponent<PickupItem>();
            if (pickupScript != null)
            {
                // pickupScript.itemData = selectedItemData; // This is now done in Initialize
                // pickupScript.OnItemPickedUp += HandleItemPickedUp; // Replaced by direct call
                pickupScript.Initialize(selectedItemData, gridCoords);
            }
            else
            {
                Destroy(newItem); // Clean up if script is missing
                return;
            }
            _activeItems.Add(newItem);
            _occupiedItemTiles.Add(gridCoords);
            
            // Mark the tile as occupied by an item in TileSettings
            TileSettings tileSettings = GetTileSettingsAt(gridCoords);
            if (tileSettings != null)
            {
                tileSettings.SetOccupant(TileSettings.OccupantType.Item, newItem);
            }
        }
    }

    // This method is now public and called by PickupItem.ActivatePickup
    public void HandleItemPickup(GameObject itemObject, Vector2Int gridCoords)
    {
        if (itemObject == null)
        {
            return;
        }

        // Play SFX based on item type
        PickupItem pickupScript = itemObject.GetComponent<PickupItem>();
        if (pickupScript != null && pickupScript.itemData != null && SFXManager.Instance != null)
        {
            switch (pickupScript.itemData.itemType)
            {
                case ItemType.Heal:
                    SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.PickupHeal);
                    break;
                case ItemType.Armor:
                    SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.PickupArmor);
                    break;
                default:
                    break;
            }
        }

        _activeItems.Remove(itemObject);
        _occupiedItemTiles.Remove(gridCoords);
        
        // The PickupItem script no longer has OnItemPickedUp event
        // item.OnItemPickedUp -= HandleItemPickedUp;

        Destroy(itemObject); // Destroy the item GameObject
        StartCoroutine(RespawnItemAfterDelay());
    }

    IEnumerator RespawnItemAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnRandomItem();
    }

    Vector3? GetRandomUnoccupiedTilePosition()
    {
        List<TileSettings> unoccupiedTiles = new List<TileSettings>();
        foreach (Transform tileTransform in _gridGenerator.transform)
        {
            TileSettings tileSettings = tileTransform.GetComponent<TileSettings>();
            if (tileSettings != null && tileSettings.occupantType == TileSettings.OccupantType.None && !IsTileOccupiedByItem(tileSettings.gridX, tileSettings.gridY))
            {
                unoccupiedTiles.Add(tileSettings);
            }
        }

        if (unoccupiedTiles.Count > 0)
        {
            TileSettings randomTile = unoccupiedTiles[Random.Range(0, unoccupiedTiles.Count)];
            // Adjust Y position if necessary, items might need to be slightly above the tile.
            // For now, using the tile's position directly.
            return randomTile.transform.position; 
        }
        return null;
    }
    
    Vector2Int GetGridCoordinates(Vector3 worldPosition)
    {
        // This is a simplified conversion. 
        // A more robust solution would involve inverse transforming from isometric to grid coordinates.
        // For now, we find the closest tile.
        float minDistance = float.MaxValue;
        TileSettings closestTile = null;

        foreach (Transform tileTransform in _gridGenerator.transform)
        {
            float distance = Vector3.Distance(tileTransform.position, worldPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTile = tileTransform.GetComponent<TileSettings>();
            }
        }
        return closestTile != null ? new Vector2Int(closestTile.gridX, closestTile.gridY) : new Vector2Int(-1,-1);
    }

    TileSettings GetTileSettingsAt(Vector2Int gridCoordinates)
    {
        foreach (Transform tileTransform in _gridGenerator.transform)
        {
            TileSettings tileSettings = tileTransform.GetComponent<TileSettings>();
            if (tileSettings != null && tileSettings.gridX == gridCoordinates.x && tileSettings.gridY == gridCoordinates.y)
            {
                return tileSettings;
            }
        }
        return null;
    }
    
    bool IsTileOccupiedByItem(int x, int y)
    {
        return _occupiedItemTiles.Any(tile => tile.x == x && tile.y == y);
    }
}
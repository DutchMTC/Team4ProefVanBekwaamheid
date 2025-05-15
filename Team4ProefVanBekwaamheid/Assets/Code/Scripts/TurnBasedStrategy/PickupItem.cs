using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public ItemData itemData;
    // public System.Action<PickupItem> OnItemPickedUp; // This will be handled by direct call to ItemManager
    private Vector2Int _gridCoords; // To store item's location for ItemManager

    public void Initialize(ItemData data, Vector2Int gridCoords)
    {
        itemData = data;
        _gridCoords = gridCoords;
    }

    // Called by TileOccupants when a unit moves onto this item's tile
    public void ActivatePickup(GameObject collectingUnit)
    {
        if (itemData == null)
        {
            Debug.LogError("ItemData is not assigned on this PickupItem. Make sure Initialize was called.");
            return;
        }

        Debug.Log($"{collectingUnit.name} picked up {itemData.itemName} at {_gridCoords}");

        ApplyBoost(collectingUnit);

        // Notify ItemManager to handle respawn and removal
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.HandleItemPickup(this.gameObject, _gridCoords);
        }
        else
        {
            Debug.LogWarning("ItemManager instance not found. Item will be destroyed locally but not respawned.");
            Destroy(gameObject); // Fallback: destroy if no manager
        }
        // ItemManager.HandleItemPickup will be responsible for Destroy(gameObject) or disabling it.
    }

    void ApplyBoost(GameObject unit)
    {
        // This is a placeholder. You'll need to get the actual unit script.
        // For example, if your unit script is named UnitStats:
        // UnitStats unitStats = unit.GetComponent<UnitStats>();
        // if (unitStats == null) return;

        switch (itemData.itemType)
        {
            case ItemType.Heal:
                Debug.Log(unit.name + " picked up " + itemData.itemName + " and attempting to heal for 10.");
                TileOccupants occupant = unit.GetComponent<TileOccupants>();
                if (occupant != null)
                {
                    occupant.Heal(10);
                }
                else
                {
                    Debug.LogWarning($"{unit.name} does not have a TileOccupants component. Cannot apply heal.");
                }
                break;
            case ItemType.Armor:
                Debug.Log(unit.name + " picked up " + itemData.itemName + " and attempting to apply armor.");
                TileOccupants targetOccupant = unit.GetComponent<TileOccupants>();
                if (targetOccupant != null)
                {
                    targetOccupant.ReceiveArmor();
                }
                else
                {
                    Debug.LogWarning($"{unit.name} does not have a TileOccupants component. Cannot apply armor.");
                }
                break;
        }
    }
}
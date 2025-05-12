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
                // unitStats.ReceiveHeal(itemData.boostAmount);
                Debug.Log(unit.name + " picked up " + itemData.itemName + " and received " + itemData.boostAmount + " heal.");
                // Replace with actual call to unit's ReceiveHeal method
                if (unit.GetComponent<Health>() != null) // Assuming a Health script exists
                {
                    // unit.GetComponent<Health>().ReceiveHeal(itemData.boostAmount); 
                }
                else
                {
                    // Attempt to call a generic ReceiveHeal method if Health component is not found
                    unit.SendMessage("ReceiveHeal", itemData.boostAmount, SendMessageOptions.DontRequireReceiver);
                }
                break;
            case ItemType.Armor:
                // unitStats.ReceiveArmor(itemData.boostAmount);
                Debug.Log(unit.name + " picked up " + itemData.itemName + " and received " + itemData.boostAmount + " armor.");
                // Replace with actual call to unit's ReceiveArmor method
                 unit.SendMessage("ReceiveArmor", itemData.boostAmount, SendMessageOptions.DontRequireReceiver);
                break;
        }
    }
}
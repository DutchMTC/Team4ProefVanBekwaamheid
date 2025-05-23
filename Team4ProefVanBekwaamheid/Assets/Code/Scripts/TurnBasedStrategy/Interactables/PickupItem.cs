using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public ItemData itemData;
    // public System.Action<PickupItem> OnItemPickedUp; // This will be handled by direct call to ItemManager
    private Vector2Int _gridCoords; // To store item's location for ItemManager
    private VignetteController _vignetteController;

    void Awake()
    {
        _vignetteController = FindObjectOfType<VignetteController>();
    }

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
            return;
        }

        ApplyBoost(collectingUnit);

        // Notify ItemManager to handle respawn and removal
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.HandleItemPickup(this.gameObject, _gridCoords);
        }
        else
        {
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
                TileOccupants occupant = unit.GetComponent<TileOccupants>();
                if (occupant != null)
                {
                    occupant.Heal(30);
                    if (_vignetteController != null)
                    {
                        _vignetteController.PlayHealVignette();
                    }
                }
                break;
            case ItemType.Armor:
                TileOccupants targetOccupant = unit.GetComponent<TileOccupants>();
                if (targetOccupant != null)
                {
                    targetOccupant.ReceiveArmor();
                }
                break;
        }
    }
}
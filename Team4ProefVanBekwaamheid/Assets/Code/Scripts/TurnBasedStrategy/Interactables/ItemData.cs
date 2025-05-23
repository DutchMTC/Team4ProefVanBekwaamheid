using UnityEngine;

public enum ItemType
{
    Heal,
    Armor
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public float boostAmount;
    public GameObject itemPrefab; // Visual representation of the item
}
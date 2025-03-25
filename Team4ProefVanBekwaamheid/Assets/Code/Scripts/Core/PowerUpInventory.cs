using UnityEngine;

public class PowerUpInventory : MonoBehaviour
{
    public static PowerUpInventory Instance { get; private set; }

    public enum PowerUpType
    {
        Sword,
        Shield,
        Steps,
        Health
    }

    [SerializeField] private int swordCount = 0;
    [SerializeField] private int shieldCount = 0;
    [SerializeField] private int stepsCount = 0;
    [SerializeField] private int healthCount = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddPowerUps(PowerUpType type, int amount)
    {
        switch (type)
        {
            case PowerUpType.Sword:
                swordCount += amount;
                break;
            case PowerUpType.Shield:
                shieldCount += amount;
                break;
            case PowerUpType.Steps:
                stepsCount += amount;
                break;
            case PowerUpType.Health:
                healthCount += amount;
                break;
        }
        
        LogInventory();
    }

    public int GetPowerUpCount(PowerUpType type)
    {
        return type switch
        {
            PowerUpType.Sword => swordCount,
            PowerUpType.Shield => shieldCount,
            PowerUpType.Steps => stepsCount,
            PowerUpType.Health => healthCount,
            _ => 0
        };
    }

    public void UsePowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Sword when swordCount > 0:
                swordCount--;
                break;
            case PowerUpType.Shield when shieldCount > 0:
                shieldCount--;
                break;
            case PowerUpType.Steps when stepsCount > 0:
                stepsCount--;
                break;
            case PowerUpType.Health when healthCount > 0:
                healthCount--;
                break;
        }
        
        LogInventory();
    }

    private void LogInventory()
    {
        Debug.Log($"Power-Up Inventory:\n" +
                  $"Swords: {swordCount}\n" +
                  $"Shields: {shieldCount}\n" +
                  $"Steps: {stepsCount}\n" +
                  $"Health: {healthCount}");
    }
}
using UnityEngine;

public class PowerUpInventory : MonoBehaviour
{
    public static PowerUpInventory Instance { get; private set; }

    public enum PowerUpType
    {
        Sword,
        Shield,
        Steps,
        Health,
        Wall
    }

    [SerializeField] private int _swordCount = 0;
    [SerializeField] private int _shieldCount = 0;
    [SerializeField] private int _stepsCount = 0;
    [SerializeField] private int _healthCount = 0;
    [SerializeField] private int _wallCount = 0;

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
                _swordCount += amount;
                break;
            case PowerUpType.Shield:
                _shieldCount += amount;
                break;
            case PowerUpType.Steps:
                _stepsCount += amount;
                break;
            case PowerUpType.Health:
                _healthCount += amount;
                break;
            case PowerUpType.Wall:
                _wallCount += amount;
                break;
        }
        
        LogInventory();
    }

    public int GetPowerUpCount(PowerUpType type)
    {
        return type switch
        {
            PowerUpType.Sword => _swordCount,
            PowerUpType.Shield => _shieldCount,
            PowerUpType.Steps => _stepsCount,
            PowerUpType.Health => _healthCount,
            PowerUpType.Wall => _wallCount,
            _ => 0
        };
    }

    public void UsePowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Sword when _swordCount > 0:
                _swordCount--;
                break;
            case PowerUpType.Shield when _shieldCount > 0:
                _shieldCount--;
                break;
            case PowerUpType.Steps when _stepsCount > 0:
                _stepsCount--;
                break;
            case PowerUpType.Health when _healthCount > 0:
                _healthCount--;
                break;
            case PowerUpType.Wall when _wallCount > 0:
                _wallCount--;
                break;
        }
        
        LogInventory();
    }

    private void LogInventory()
    {
        Debug.Log($"Power-Up Inventory:\n" +
                  $"Swords: {_swordCount}\n" +
                  $"Shields: {_shieldCount}\n" +
                  $"Steps: {_stepsCount}\n" +
                  $"Health: {_healthCount}\n" +
                  $"Walls: {_wallCount}");
                  
    }
}
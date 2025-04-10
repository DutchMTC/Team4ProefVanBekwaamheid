using UnityEngine;

public class PowerUpInventory : MonoBehaviour
{
    public static PowerUpInventory Instance { get; private set; }

    public enum PowerUpType
    {
        Sword,
        Shield,
        Steps,
        Wall
    }

    [SerializeField] private int _swordCount = 0;
    [SerializeField] private int _shieldCount = 0;
    [SerializeField] private int _stepsCount = 0;
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
            case PowerUpType.Wall when _wallCount > 0:
                _wallCount--;
                break;
        }
        
        LogInventory();
    }

    /// Sets the count for a specific power-up type.
    /// Used by PowerUpManager for Supercharged cost.
    public void SetPowerUpCount(PowerUpType type, int count)
    {
        switch (type)
        {
            case PowerUpType.Sword: _swordCount = Mathf.Max(0, count); break;
            case PowerUpType.Shield: _shieldCount = Mathf.Max(0, count); break;
            case PowerUpType.Steps: _stepsCount = Mathf.Max(0, count); break;
            case PowerUpType.Wall: _wallCount = Mathf.Max(0, count); break;
        }
        LogInventory();
    }

    /// Decreases the count for a specific power-up type by a given amount.
    /// Used by PowerUpManager for Charged cost.
    public void DecreasePowerUpCount(PowerUpType type, int amount)
    {
        switch (type)
        {
            case PowerUpType.Sword: _swordCount = Mathf.Max(0, _swordCount - amount); break;
            case PowerUpType.Shield: _shieldCount = Mathf.Max(0, _shieldCount - amount); break;
            case PowerUpType.Steps: _stepsCount = Mathf.Max(0, _stepsCount - amount); break;
            case PowerUpType.Wall: _wallCount = Mathf.Max(0, _wallCount - amount); break;
        }
        LogInventory();
    }
 
    private void LogInventory()
    {
        Debug.Log($"Power-Up Inventory:\n" +
                  $"Swords: {_swordCount}\n" +
                  $"Shields: {_shieldCount}\n" +
                  $"Steps: {_stepsCount}\n" +
                  $"Walls: {_wallCount}");
                  
    }
}
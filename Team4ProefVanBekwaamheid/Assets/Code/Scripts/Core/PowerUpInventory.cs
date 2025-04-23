using UnityEngine;
using System; // Added for Action

public class PowerUpInventory : MonoBehaviour
{
    public static PowerUpInventory Instance { get; private set; }

    // Event to notify listeners when a power-up count changes
    // Passes the type and the new count
    public static event Action<PowerUpType, int> OnPowerUpCountChanged;

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
                OnPowerUpCountChanged?.Invoke(type, _swordCount); // Invoke event
                break;
            case PowerUpType.Shield:
                _shieldCount += amount;
                OnPowerUpCountChanged?.Invoke(type, _shieldCount); // Invoke event
                break;
            case PowerUpType.Steps:
                _stepsCount += amount;
                OnPowerUpCountChanged?.Invoke(type, _stepsCount); // Invoke event
                break;
            case PowerUpType.Wall:
                _wallCount += amount;
                OnPowerUpCountChanged?.Invoke(type, _wallCount); // Invoke event
                break;
        }
        // LogInventory(); // Logging can be removed or kept as needed
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
                OnPowerUpCountChanged?.Invoke(type, _swordCount); // Invoke event
                break;
            case PowerUpType.Shield when _shieldCount > 0:
                _shieldCount--;
                OnPowerUpCountChanged?.Invoke(type, _shieldCount); // Invoke event
                break;
            case PowerUpType.Steps when _stepsCount > 0:
                _stepsCount--;
                OnPowerUpCountChanged?.Invoke(type, _stepsCount); // Invoke event
                break;
            case PowerUpType.Wall when _wallCount > 0:
                _wallCount--;
                OnPowerUpCountChanged?.Invoke(type, _wallCount); // Invoke event
                break;
        }
        // LogInventory();
    }

    /// Sets the count for a specific power-up type.
    /// Used by PowerUpManager for Supercharged cost.
    public void SetPowerUpCount(PowerUpType type, int count)
    {
        int newCount = Mathf.Max(0, count);
        switch (type)
        {
            case PowerUpType.Sword: _swordCount = newCount; break;
            case PowerUpType.Shield: _shieldCount = newCount; break;
            case PowerUpType.Steps: _stepsCount = newCount; break;
            case PowerUpType.Wall: _wallCount = newCount; break;
            default: return; // Exit if type is invalid
        }
        OnPowerUpCountChanged?.Invoke(type, newCount); // Invoke event
        // LogInventory();
    }

    /// Decreases the count for a specific power-up type by a given amount.
    /// Used by PowerUpManager for Charged cost.
    public void DecreasePowerUpCount(PowerUpType type, int amount)
    {
        int newCount = 0;
        switch (type)
        {
            case PowerUpType.Sword: _swordCount = Mathf.Max(0, _swordCount - amount); newCount = _swordCount; break;
            case PowerUpType.Shield: _shieldCount = Mathf.Max(0, _shieldCount - amount); newCount = _shieldCount; break;
            case PowerUpType.Steps: _stepsCount = Mathf.Max(0, _stepsCount - amount); newCount = _stepsCount; break;
            case PowerUpType.Wall: _wallCount = Mathf.Max(0, _wallCount - amount); newCount = _wallCount; break;
            default: return; // Exit if type is invalid
        }
        OnPowerUpCountChanged?.Invoke(type, newCount); // Invoke event
        // LogInventory();
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
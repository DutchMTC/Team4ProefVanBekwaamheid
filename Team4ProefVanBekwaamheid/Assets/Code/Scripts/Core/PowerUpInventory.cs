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
        Trap
    }

    [SerializeField] private int _swordCount = 0;
    [SerializeField] private int _shieldCount = 0;
    [SerializeField] private int _stepsCount = 0;
    [SerializeField] private int _trapCount = 0;

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
            case PowerUpType.Trap:
                _trapCount += amount;
                OnPowerUpCountChanged?.Invoke(type, _trapCount); // Invoke event
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
            PowerUpType.Trap => _trapCount,
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
            case PowerUpType.Trap when _trapCount > 0:
                _trapCount--;
                OnPowerUpCountChanged?.Invoke(type, _trapCount); // Invoke event
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
            case PowerUpType.Trap: _trapCount = newCount; break;
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
            case PowerUpType.Trap: _trapCount = Mathf.Max(0, _trapCount - amount); newCount = _trapCount; break;
            default: return; // Exit if type is invalid
        }
        OnPowerUpCountChanged?.Invoke(type, newCount); // Invoke event
        // LogInventory();
    }

   /// <summary>
   /// Resets the count of all power-ups to zero.
   /// </summary>
   public void ClearAllPowerUps()
   {
       _swordCount = 0;
       _shieldCount = 0;
       _stepsCount = 0;
       _trapCount = 0;

       // Invoke events for each type to notify listeners (like PowerUpManager)
       OnPowerUpCountChanged?.Invoke(PowerUpType.Sword, _swordCount);
       OnPowerUpCountChanged?.Invoke(PowerUpType.Shield, _shieldCount);
       OnPowerUpCountChanged?.Invoke(PowerUpType.Steps, _stepsCount);
       OnPowerUpCountChanged?.Invoke(PowerUpType.Trap, _trapCount);

       Debug.Log("Power-up inventory cleared.");
       // LogInventory(); // Optional: Log after clearing
   }

    private void LogInventory()
    {
        Debug.Log($"Power-Up Inventory:\n" +
                  $"Swords: {_swordCount}\n" +
                  $"Shields: {_shieldCount}\n" +
                  $"Steps: {_stepsCount}\n" +
                  $"Traps: {_trapCount}");
                  
    }
}
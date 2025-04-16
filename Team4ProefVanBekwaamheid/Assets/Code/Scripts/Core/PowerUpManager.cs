using UnityEngine;

public enum PowerUpState
{
    Unusable,
    Charged,
    Supercharged
}

public class PowerUpManager : MonoBehaviour
{
    [Tooltip("Reference to the PowerUpInventory script instance.")]
    public PowerUpInventory powerUpInventory;

    /// Gets the current power-up state for a specific power-up type
    /// based on its count in the inventory.current PowerUpState for that type.</returns>
    public PowerUpState GetCurrentState(PowerUpInventory.PowerUpType type)
    {

        int count = powerUpInventory.GetPowerUpCount(type);

        if (count >= 25)
        {
            return PowerUpState.Supercharged;
        }
        else if (count >= 15)
        {
            return PowerUpState.Charged;
        }
        else
        {
            return PowerUpState.Unusable;
        }
    }

    /// Attempts to use a power-up of the specified type.
    /// Checks the state of the specific power-up type and applies costs and effects accordingly.
    public bool TryUsePowerUp(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            Debug.LogError("PowerUpInventory reference is not set!");
            return false;
        }

        PowerUpState currentState = GetCurrentState(type);

        switch (currentState)
        {
            case PowerUpState.Unusable:
                int currentCount = powerUpInventory.GetPowerUpCount(type); 
                Debug.Log($"Power-up {type} unusable: Not enough charge ({currentCount}).");
                return false;

            case PowerUpState.Supercharged:
                ActivateEffect(type, currentState); 
                powerUpInventory.SetPowerUpCount(type, 0);
                Debug.Log($"Used Supercharged {type}. Count for {type} reset to 0.");
                return true;

            case PowerUpState.Charged:
                ActivateEffect(type, currentState);
                powerUpInventory.DecreasePowerUpCount(type, 15);
                Debug.Log($"Used Charged {type}. Count for {type} reduced by 15.");
                return true;

            default:
                Debug.LogWarning($"Unhandled PowerUpState: {currentState} for type {type}");
                return false;
        }
    }

    // Simple test method using key presses
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            TryUsePowerUp(PowerUpInventory.PowerUpType.Sword);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            TryUsePowerUp(PowerUpInventory.PowerUpType.Shield);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            TryUsePowerUp(PowerUpInventory.PowerUpType.Wall);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            TryUsePowerUp(PowerUpInventory.PowerUpType.Steps);
        }
    }

    // Consolidated effect activation logic
    private void ActivateEffect(PowerUpInventory.PowerUpType type, PowerUpState state)
    {
        Debug.Log($"Activating {state} effect for {type}");

        switch (type)
        {
            case PowerUpInventory.PowerUpType.Sword:
                if (state == PowerUpState.Charged) HandleSwordCharged();
                else if (state == PowerUpState.Supercharged) HandleSwordSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Shield:
                if (state == PowerUpState.Charged) HandleShieldCharged();
                else if (state == PowerUpState.Supercharged) HandleShieldSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Wall:
                if (state == PowerUpState.Charged) HandleWallCharged();
                else if (state == PowerUpState.Supercharged) HandleWallSupercharged();
                break;
            case PowerUpInventory.PowerUpType.Steps:
                if (state == PowerUpState.Charged) HandleStepsCharged();
                else if (state == PowerUpState.Supercharged) HandleStepsSupercharged();
                break;
            default:
                 Debug.LogWarning($"Unhandled PowerUpType {type} in ActivateEffect");
                 break;
        }
    }

    // Specific effect handlers remain unchanged for now
    private void HandleSwordCharged()
    {
        Debug.Log("Handling Sword Charged effect!");
    }

    private void HandleSwordSupercharged()
    {
        Debug.Log("Handling Sword Supercharged effect!");
    }

    private void HandleShieldCharged()
    {
        Debug.Log("Handling Shield Charged effect!");
    }

    private void HandleShieldSupercharged()
    {
        Debug.Log("Handling Shield Supercharged effect!");
    }

    private void HandleWallCharged()
    {
        Debug.Log("Handling Wall Charged effect!");
    }

    private void HandleWallSupercharged()
    {
        Debug.Log("Handling Wall Supercharged effect!");
    }

    private void HandleStepsCharged()
    {
        Debug.Log("Handling Steps Charged effect!");
    }

    private void HandleStepsSupercharged()
    {
        Debug.Log("Handling Steps Supercharged effect!");
    }
}
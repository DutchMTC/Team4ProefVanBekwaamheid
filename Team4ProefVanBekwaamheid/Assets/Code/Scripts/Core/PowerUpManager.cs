using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System; // Added for Serializable

[Serializable] // Make it visible in the Inspector
public class PowerUpSprites
{
    public Sprite unusable;
    public Sprite usable;
    public Sprite charged;
    public Sprite supercharged;
}

public enum PowerUpState
{
    Unusable,
    Usable,
    Charged,
    Supercharged
}

public class PowerUpManager : MonoBehaviour
{
    public PowerUpInventory powerUpInventory;

    // --- UI Button References ---
    [Header("UI Button Images")]
    public Image swordButtonImage;
    public Image shieldButtonImage;
    public Image wallButtonImage;
    public Image stepsButtonImage;

    // --- PowerUp Specific Sprites ---
    [Header("PowerUp Specific Sprites")]
    public PowerUpSprites swordSprites;
    public PowerUpSprites shieldSprites;
    public PowerUpSprites wallSprites;
    public PowerUpSprites stepsSprites;

    // Dictionaries to map PowerUpType to UI Image and Sprites
    private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites> powerUpSpriteMap;

    private void Awake()
    {
        // Initialize the map on Awake
        powerUpImageMap = new Dictionary<PowerUpInventory.PowerUpType, Image>
        {
            { PowerUpInventory.PowerUpType.Sword, swordButtonImage },
            { PowerUpInventory.PowerUpType.Shield, shieldButtonImage },
            { PowerUpInventory.PowerUpType.Wall, wallButtonImage },
            { PowerUpInventory.PowerUpType.Steps, stepsButtonImage }
        };

        // Initialize the sprite map
        powerUpSpriteMap = new Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites>
        {
            { PowerUpInventory.PowerUpType.Sword, swordSprites },
            { PowerUpInventory.PowerUpType.Shield, shieldSprites },
            { PowerUpInventory.PowerUpType.Wall, wallSprites },
            { PowerUpInventory.PowerUpType.Steps, stepsSprites }
        };


        // Initial visual update
        UpdateAllPowerUpVisuals(); // Set initial state

        // Subscribe to inventory changes
        PowerUpInventory.OnPowerUpCountChanged += HandlePowerUpCountChanged;
    }

    private void OnDestroy() // Or OnDisable if the object might be disabled/enabled
    {
        // Unsubscribe to prevent errors and memory leaks
        PowerUpInventory.OnPowerUpCountChanged -= HandlePowerUpCountChanged;
    }

    // Method called by the event from PowerUpInventory
    private void HandlePowerUpCountChanged(PowerUpInventory.PowerUpType type, int newCount)
    {
        UpdatePowerUpVisual(type); // Update the visual for the specific power-up that changed
    }


    // Helper method to determine state based on count
    private PowerUpState DetermineStateFromCount(int count)
    {
        if (count >= 25) return PowerUpState.Supercharged;
        if (count >= 15) return PowerUpState.Charged;
        if (count >= 1) return PowerUpState.Usable; // Added Usable check
        return PowerUpState.Unusable;
    }

    public PowerUpState GetCurrentUsageState(PowerUpInventory.PowerUpType type)
    {
        int count = powerUpInventory.GetPowerUpCount(type);
        // Usage logic might differ slightly (e.g., maybe 'Usable' isn't a trigger state)
        // For now, let's keep the original logic for TryUsePowerUp
        if (count >= 25) return PowerUpState.Supercharged;
        if (count >= 15) return PowerUpState.Charged;
        // Original logic didn't have a distinct 'Usable' trigger state, only Charged/Supercharged
        return PowerUpState.Unusable; // Default to Unusable if not Charged/Supercharged
    }


    /// Updates the visual representation (sprite) of a specific power-up button.
    public void UpdatePowerUpVisual(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            Debug.LogError("PowerUpInventory reference is not set!");
            return;
        }

        int count = powerUpInventory.GetPowerUpCount(type);
        PowerUpState visualState = DetermineStateFromCount(count); // Use helper for visual state

        bool imageFound = powerUpImageMap.TryGetValue(type, out Image targetImage);
        bool spritesFound = powerUpSpriteMap.TryGetValue(type, out PowerUpSprites sprites);

        if (imageFound && targetImage != null && spritesFound && sprites != null)
        {
            targetImage.sprite = GetSpriteForState(sprites, visualState);
        }
        else
        {
            // Log warnings based on what was missing
            if (!imageFound || targetImage == null)
            {
                Debug.LogWarning($"UI Image for PowerUpType {type} is not assigned or found in the map.");
            }
            if (!spritesFound || sprites == null)
            {
                 Debug.LogWarning($"Sprite set for PowerUpType {type} is not assigned or found in the map.");
            }
        }
    }

    /// Updates the visuals for all power-up buttons.
    public void UpdateAllPowerUpVisuals()
    {
        foreach (PowerUpInventory.PowerUpType type in System.Enum.GetValues(typeof(PowerUpInventory.PowerUpType)))
        {
             // Check if the type is valid before updating (optional, depends on PowerUpType enum definition)
            if (System.Enum.IsDefined(typeof(PowerUpInventory.PowerUpType), type))
            {
                 UpdatePowerUpVisual(type);
            }
        }
    }


    // Helper to get the correct sprite based on type and state
    private Sprite GetSpriteForState(PowerUpSprites sprites, PowerUpState state)
    {
        switch (state)
        {
            case PowerUpState.Unusable: return sprites.unusable;
            case PowerUpState.Usable: return sprites.usable;
            case PowerUpState.Charged: return sprites.charged;
            case PowerUpState.Supercharged: return sprites.supercharged;
            default:
                Debug.LogWarning($"Unhandled PowerUpState for sprite selection: {state}");
                return sprites.unusable; // Default to unusable sprite for this type
        }
    }


    public bool TryUsePowerUp(PowerUpInventory.PowerUpType type)
    {
        if (powerUpInventory == null)
        {
            Debug.LogError("PowerUpInventory reference is not set!");
            return false;
        }

        // Use the usage-specific state check
        PowerUpState usageState = GetCurrentUsageState(type);

        switch (usageState)
        {
            case PowerUpState.Unusable:
                int currentCount = powerUpInventory.GetPowerUpCount(type);
                Debug.Log($"Power-up {type} unusable: Not enough charge ({currentCount}). Needs 15 for Charged, 25 for Supercharged.");
                return false;

            case PowerUpState.Supercharged:
                ActivateEffect(type, usageState);
                powerUpInventory.SetPowerUpCount(type, 0); // Reset count - This will trigger the event now
                Debug.Log($"Used Supercharged {type}. Count for {type} reset to 0.");
                // UpdatePowerUpVisual(type); // No longer needed here, event handles it
                return true;

            case PowerUpState.Charged:
                ActivateEffect(type, usageState);
                powerUpInventory.DecreasePowerUpCount(type, 15); // Decrease count - This will trigger the event now
                Debug.Log($"Used Charged {type}. Count for {type} reduced by 15.");
                // UpdatePowerUpVisual(type); // No longer needed here, event handles it
                return true;

            // Note: PowerUpState.Usable is not a trigger state for using the power-up here. It's only a visual state.
            default:
                Debug.LogWarning($"Unhandled PowerUpState in TryUsePowerUp: {usageState} for type {type}");
                return false;
        }
    }

    // --- Public Methods for UI Button OnClick Events ---

    public void UseSwordPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Sword);
    }

    public void UseShieldPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Shield);
    }

    public void UseWallPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Wall);
    }

    public void UseStepsPowerUp()
    {
        TryUsePowerUp(PowerUpInventory.PowerUpType.Steps);
    }

    // --- Private Helper Methods ---

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
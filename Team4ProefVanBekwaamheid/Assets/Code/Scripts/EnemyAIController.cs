using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Needed for List<>
using System;                   // Needed for Serializable and Action
using System.Linq;              // Needed for Enum.GetValues().Cast<>()

public class EnemyAIController : MonoBehaviour
{
    // Helper struct to map PowerUpType to its icon in the Inspector
    [Serializable]
    public struct PowerupIconMapping
    {
        public PowerUpInventory.PowerUpType type;
        public Sprite icon;
    }

    [Header("References")]
    [Tooltip("Assign the GameManager instance here.")]
    [SerializeField] private GameManager gameManager; // Reference to the GameManager
    [Tooltip("Assign UI Image elements here to display chosen powerups.")]
    public Image[] powerupDisplayIcons; // Assign e.g., 3 Image components in Inspector
    [Tooltip("Assign sprites for each powerup type.")]
    public List<PowerupIconMapping> powerupIconMappings; // Assign in Inspector

    [Header("AI Probabilities")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) of selecting 3 powerups instead of 2.")]
    public float probabilityToSelectThreePowerups = 0.2f; // 20% chance for 3

    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) of one selected powerup being supercharged.")]
    public float probabilityToSupercharge = 0.15f; // 15% chance

    // Internal struct to hold selected powerup info
    private struct SelectedPowerup
    {
        public PowerUpInventory.PowerUpType Type;
        public bool IsSupercharged;
    }

    private List<SelectedPowerup> chosenPowerups = new List<SelectedPowerup>();
    private Color defaultIconColor = Color.white; // Store default color

    void OnEnable()
    {
        // Subscribe to GameManager state changes using the assigned reference
        if (gameManager != null)
        {
            // The event itself is static, but we check the reference to ensure
            // we have a valid GameManager context for other operations (like UpdateGameState).
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        else
        {
            Debug.LogError("EnemyAIController: GameManager reference not assigned in Inspector!");
        }

        // Store default color from the first icon if available
        if (powerupDisplayIcons != null && powerupDisplayIcons.Length > 0 && powerupDisplayIcons[0] != null)
        {
            defaultIconColor = powerupDisplayIcons[0].color;
        }
        HidePowerups(); // Ensure icons are hidden initially
    }

    void OnDisable()
    {
        // Unsubscribe from the static GameManager state change event
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    // --- Event Handler for Game State Changes ---

    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Enemy:
                // Enemy's turn begins: Execute previously selected powerups
                Debug.Log("Enemy AI: Enemy state entered. Executing powerups...");
                // SelectPowerups(); // Moved to Matching state
                ExecutePowerups();
                break;

            case GameState.Matching:
                // Player's matching phase begins: Select powerups for the *next* enemy turn.
                // GameManager will call DisplayPowerups immediately after this state change.
                Debug.Log("Enemy AI: Matching state entered. Selecting powerups for next turn.");
                SelectPowerups();
                // DisplayPowerups(); // GameManager calls this now
                break;

            case GameState.Player:
                 // Player's turn/RPG phase ends: Hide powerups
                Debug.Log("Enemy AI: Player state entered. Hiding powerups...");
                HidePowerups();
                break;

             // Add cases for other states if needed (e.g., reset on Win/GameOver)
            case GameState.Win:
            case GameState.GameOver:
                chosenPowerups.Clear();
                HidePowerups();
                break;
        }
    }


    // --- Core Logic Methods ---

    private void SelectPowerups()
    {
        chosenPowerups.Clear();

        // Get all available powerup types from the enum
        List<PowerUpInventory.PowerUpType> availableTypes =
            Enum.GetValues(typeof(PowerUpInventory.PowerUpType))
                .Cast<PowerUpInventory.PowerUpType>()
                .ToList();

        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("EnemyAIController: No powerup types defined in PowerUpInventory.PowerUpType enum.");
            return;
        }

        // 1. Determine Quantity
        int count = (UnityEngine.Random.value < probabilityToSelectThreePowerups) ? 3 : 2;
        count = Mathf.Min(count, availableTypes.Count); // Can't select more than available types

        Debug.Log($"Enemy AI: Selecting {count} powerups.");

        // Create a temporary list to pick from without duplicates
        List<PowerUpInventory.PowerUpType> pool = new List<PowerUpInventory.PowerUpType>(availableTypes);

        // 2. Select Powerups (Types)
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break; // Safety check

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            chosenPowerups.Add(new SelectedPowerup { Type = pool[randomIndex], IsSupercharged = false });
            pool.RemoveAt(randomIndex); // Remove selected type from pool
        }

        // 3. Determine Supercharge (AI's random chance)
        if (chosenPowerups.Count > 0 && UnityEngine.Random.value < probabilityToSupercharge)
        {
            int superchargeIndex = UnityEngine.Random.Range(0, chosenPowerups.Count);
            SelectedPowerup charged = chosenPowerups[superchargeIndex];
            charged.IsSupercharged = true;
            chosenPowerups[superchargeIndex] = charged; // Update struct in list
            Debug.Log($"Enemy AI: Powerup type '{charged.Type}' is designated SUPERCHARGED by AI!");
        }
    }

    // Made public so GameManager can call it directly
    public void DisplayPowerups()
    {
        if (powerupDisplayIcons == null || powerupIconMappings == null)
        {
             Debug.LogError("EnemyAIController: UI Icons or Icon Mappings not assigned!");
             return;
        }

        HidePowerups(); // Clear previous state

        for (int i = 0; i < chosenPowerups.Count && i < powerupDisplayIcons.Length; i++)
        {
            if (powerupDisplayIcons[i] != null)
            {
                // Find the corresponding icon from the mappings
                Sprite iconToShow = GetIconForType(chosenPowerups[i].Type);

                if (iconToShow != null)
                {
                    powerupDisplayIcons[i].sprite = iconToShow;
                    powerupDisplayIcons[i].color = chosenPowerups[i].IsSupercharged ? Color.yellow : defaultIconColor; // Indicate AI supercharge
                    powerupDisplayIcons[i].enabled = true;
                }
                else
                {
                    Debug.LogWarning($"EnemyAIController: No icon mapping found for type {chosenPowerups[i].Type}. Hiding slot {i}.");
                    powerupDisplayIcons[i].enabled = false; // Hide if no icon found
                }
            }
        }
         // Disable remaining unused slots
        for (int i = chosenPowerups.Count; i < powerupDisplayIcons.Length; i++)
        {
             if (powerupDisplayIcons[i] != null)
             {
                 powerupDisplayIcons[i].enabled = false;
             }
        }
    }

     // Helper to get icon from mappings
    private Sprite GetIconForType(PowerUpInventory.PowerUpType type)
    {
        foreach (var mapping in powerupIconMappings)
        {
            if (mapping.type == type)
            {
                return mapping.icon;
            }
        }
        return null; // Return null if no mapping found
    }


    private void HidePowerups()
    {
        if (powerupDisplayIcons == null) return;

        foreach (Image iconImage in powerupDisplayIcons)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null; // Clear sprite
                iconImage.color = defaultIconColor; // Reset color
            }
        }
    }

    private void ExecutePowerups()
    {
        if (chosenPowerups.Count == 0)
        {
            Debug.Log("Enemy AI: No powerups were selected for this turn.");
        }
        else
        {
            Debug.Log("Enemy AI: Starting powerup execution sequence...");
            // Iterate through a copy in case we modify the list or icons during execution
            List<SelectedPowerup> powerupsToExecute = new List<SelectedPowerup>(chosenPowerups);

            for(int i = 0; i < powerupsToExecute.Count; i++)
            {
                 SelectedPowerup powerup = powerupsToExecute[i];
                 Debug.Log($"- Executing Powerup Type: {powerup.Type} (AI Supercharged: {powerup.IsSupercharged})");

                 // --- Add actual enemy-specific powerup effect logic here ---
                 // Example: ApplyDamage(powerup.IsSupercharged ? 10 : 5);

                 // Visually hide the executed powerup's icon
                 if (i < powerupDisplayIcons.Length && powerupDisplayIcons[i] != null)
                 {
                     // Optional: Add a slight delay here if needed using a coroutine
                     powerupDisplayIcons[i].enabled = false;
                     Debug.Log($"  > Hiding icon at index {i}");
                 }
                 else
                 {
                     Debug.LogWarning($"  > Could not find icon at index {i} to hide.");
                 }

                 // Optional: Add delay between powerup executions if desired
                 // yield return new WaitForSeconds(0.5f); // Requires making ExecutePowerups a Coroutine
            }
            Debug.Log("Enemy AI: Finished executing powerups.");
        }

        // Notify GameManager to transition to the next state (e.g., back to Matching)
        if (gameManager != null)
        {
             Debug.Log("Enemy AI: Notifying GameManager to transition state back to Matching.");
             gameManager.UpdateGameState(GameState.Matching);
        }
        else
        {
            Debug.LogError("EnemyAIController: Cannot notify GameManager, reference not assigned!");
        }

        // Clear the list *after* execution and state transition notification
        // chosenPowerups.Clear(); // SelectPowerups already clears at the start of Matching
    }
}
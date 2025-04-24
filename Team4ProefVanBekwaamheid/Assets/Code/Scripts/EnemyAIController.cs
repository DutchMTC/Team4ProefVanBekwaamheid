using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Needed for List<>
using System;                   // Needed for Serializable and Action
using System.Linq;              // Needed for Enum.GetValues().Cast<>()
using static PowerUpManager;    // Import PowerUpState and PowerUpSprites

public class EnemyAIController : MonoBehaviour
{
    // Helper struct to map PowerUpType to its state-specific sprites in the Inspector
    [Serializable]
    public struct PowerupSpriteMapping
    {
        public PowerUpInventory.PowerUpType type;
        public PowerUpSprites sprites; // Uses the struct from PowerUpManager
    }

    [Header("References")]
    [Tooltip("Assign the GameManager instance here.")]
    [SerializeField] private GameManager gameManager; // Reference to the GameManager
    [Tooltip("Assign UI Image elements here to display chosen powerups.")]
    public Image[] powerupDisplayIcons; // Assign e.g., 3 Image components in Inspector
    [Tooltip("Assign state sprites for each powerup type.")]
    public List<PowerupSpriteMapping> powerupSpriteMappings; // Assign in Inspector
    // No longer need enemyTransform if powerupDisplayParent is a child
    // [SerializeField] private Transform enemyTransform;
    [Tooltip("Assign the parent RectTransform of the powerupDisplayIcons. This should be a child of the Enemy, likely on a World Space Canvas.")]
    [SerializeField] private RectTransform powerupDisplayParent;

    [Header("UI Positioning")]
    [Tooltip("Local vertical offset from this GameObject's origin (or the parent's origin if not a direct child).")]
    [SerializeField] private float verticalOffset = 2.0f; // This is now a local offset

    [Header("AI Probabilities")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) of selecting 3 powerups instead of 2.")]
    public float probabilityToSelectThreePowerups = 0.2f; // 20% chance for 3

    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) of selecting a 'Charged' powerup (vs 'Usable').")]
    public float probabilityCharged = 0.3f; // 30% chance for Charged

    [Tooltip("Probability (0-1) of selecting a 'Supercharged' powerup (vs 'Usable' or 'Charged'). Checked after Charged.")]
    [Range(0f, 1f)] // This was the duplicate, removing the one above this line
    public float probabilitySupercharged = 0.1f; // 10% chance for Supercharged

    // Internal struct to hold selected powerup info
    private struct SelectedPowerup
    {
        public PowerUpInventory.PowerUpType Type;
        public PowerUpState State; // Use the enum from PowerUpManager
    }

    private List<SelectedPowerup> chosenPowerups = new List<SelectedPowerup>();
    private Color defaultIconColor = Color.white; // Store default color
    private Coroutine _executionCoroutine = null; // To manage the execution coroutine
    // No longer need camera/canvas caching for World Space approach
    // private Camera mainCamera;
    // private Canvas parentCanvas;
    // private RectTransform canvasRectTransform;

    // --- Unity Lifecycle Methods ---

    // Awake is no longer needed for caching camera/canvas
    // void Awake() { ... }

    void Start()
    {
        // Set initial position once
        PositionPowerupDisplay();
    }

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

    // LateUpdate is no longer needed if the UI is parented correctly
    // void LateUpdate()
    // {
    //     PositionPowerupDisplay();
    // }

    // --- Event Handler for Game State Changes ---

    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Enemy:
                // Enemy's turn begins: Execute previously selected powerups
                Debug.Log("Enemy AI: Enemy state entered. Starting powerup execution...");
                // Stop any previous execution coroutine if it's somehow still running
                if (_executionCoroutine != null)
                {
                    StopCoroutine(_executionCoroutine);
                }
                _executionCoroutine = StartCoroutine(ExecutePowerups()); // Start as coroutine
                break;

            case GameState.Matching:
                // Player's matching phase begins: Select powerups for the *next* enemy turn.
                Debug.Log("Enemy AI: Matching state entered. Selecting powerups for next turn.");
                SelectPowerups();
                DisplayPowerups(); // Call DisplayPowerups immediately after selecting them.
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
        count = Mathf.Min(count, availableTypes.Count, powerupDisplayIcons.Length); // Also limited by available UI slots

        Debug.Log($"Enemy AI: Selecting {count} powerups.");

        // Create a temporary list to pick from without duplicates
        List<PowerUpInventory.PowerUpType> pool = new List<PowerUpInventory.PowerUpType>(availableTypes);

        // 2. Select Powerups (Types and States)
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break; // Safety check

            // Select Type
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            PowerUpInventory.PowerUpType selectedType = pool[randomIndex];
            pool.RemoveAt(randomIndex); // Remove selected type from pool

            // Determine State (Usable, Charged, Supercharged) - Excluding Unusable
            PowerUpState selectedState = PowerUpState.Usable; // Default to Usable
            float randomValue = UnityEngine.Random.value;

            if (randomValue < probabilitySupercharged)
            {
                selectedState = PowerUpState.Supercharged;
            }
            else if (randomValue < probabilitySupercharged + probabilityCharged) // Check Charged only if not Supercharged
            {
                selectedState = PowerUpState.Charged;
            }
            // Else remains Usable

            chosenPowerups.Add(new SelectedPowerup { Type = selectedType, State = selectedState });
            Debug.Log($"Enemy AI: Selected Powerup Type '{selectedType}' with State '{selectedState}'.");
        }
    }

    // Made public so GameManager can call it directly
    public void DisplayPowerups()
    {
        Debug.Log($"--- DisplayPowerups Called. Chosen Count: {chosenPowerups.Count} ---"); // DEBUG: Confirm method call
        if (powerupDisplayIcons == null || powerupSpriteMappings == null)
        {
             Debug.LogError("EnemyAIController: UI Icons or Sprite Mappings not assigned!");
             return;
        }

        HidePowerups(); // Clear previous state

        for (int i = 0; i < chosenPowerups.Count && i < powerupDisplayIcons.Length; i++)
        {
            if (powerupDisplayIcons[i] != null)
            {
                SelectedPowerup currentPowerup = chosenPowerups[i];
                // Find the corresponding sprite based on Type and State
                Sprite iconToShow = GetSpriteForState(currentPowerup.Type, currentPowerup.State);

                 // --- DEBUGGING: Log info specifically for the third icon ---
                 if (i == 2)
                 {
                     Debug.Log($"DisplayPowerups: Processing third icon (i=2). Chosen Count: {chosenPowerups.Count}, Icon Array Length: {powerupDisplayIcons.Length}");
                     Debug.Log($"DisplayPowerups (i=2): Powerup Type: {currentPowerup.Type}, State: {currentPowerup.State}");
                     Debug.Log($"DisplayPowerups (i=2): Found Sprite: {(iconToShow != null ? iconToShow.name : "NULL")}");
                 }
                 // --- END DEBUGGING ---
 
                 if (iconToShow != null)
                 {
                     powerupDisplayIcons[i].sprite = iconToShow;
                     powerupDisplayIcons[i].color = defaultIconColor; // Reset to default color, sprite indicates state
                     powerupDisplayIcons[i].enabled = true;
                     if (i == 2) Debug.Log("DisplayPowerups (i=2): Enabling icon."); // DEBUG
                 }
                 else
                 {
                     Debug.LogWarning($"EnemyAIController: No sprite mapping found for type {currentPowerup.Type} and state {currentPowerup.State}. Hiding slot {i}.");
                     powerupDisplayIcons[i].enabled = false; // Hide if no icon found
                     if (i == 2) Debug.Log("DisplayPowerups (i=2): Disabling icon (sprite not found)."); // DEBUG
                 }
             }
             else if (i == 2) // DEBUG
             {
                  Debug.LogWarning("DisplayPowerups (i=2): powerupDisplayIcons[2] is NULL!");
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

    // Helper to get the correct sprite based on type and state
    private Sprite GetSpriteForState(PowerUpInventory.PowerUpType type, PowerUpState state)
    {
        // Find the mapping for the given type
        foreach (var mapping in powerupSpriteMappings)
        {
            if (mapping.type == type)
            {
                // Get the sprite for the specific state from the found mapping
                switch (state)
                {
                    case PowerUpState.Usable:       return mapping.sprites.usable;
                    case PowerUpState.Charged:      return mapping.sprites.charged;
                    case PowerUpState.Supercharged: return mapping.sprites.supercharged;
                    // case PowerUpState.Unusable: // We don't select Unusable for the enemy AI
                    default:
                        Debug.LogWarning($"EnemyAIController: Unhandled or unexpected PowerUpState '{state}' requested for type '{type}'.");
                        return null; // Or return a default 'error' sprite
                }
            }
        }
        Debug.LogWarning($"EnemyAIController: No PowerupSpriteMapping found for type '{type}'.");
        return null; // Return null if no mapping found for the type
    }

    // Simplified for World Space Canvas approach
    private void PositionPowerupDisplay()
    {
        if (powerupDisplayParent != null)
        {
            // Set the local position relative to the parent (Enemy or World Space Canvas)
            // Assuming the parent's forward is aligned appropriately or rotation is handled by the parent
            powerupDisplayParent.localPosition = new Vector3(0, verticalOffset, 0);

            // Optional: Make the UI always face the camera (Billboard effect)
            // if (mainCamera != null) // Need mainCamera reference back if using billboard
            // {
            //      powerupDisplayParent.LookAt(powerupDisplayParent.position + mainCamera.transform.rotation * Vector3.forward,
            //                                  mainCamera.transform.rotation * Vector3.up);
            // }
        }
        else
        {
            Debug.LogWarning("EnemyAIController: Powerup Display Parent RectTransform not assigned!");
        }
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

    private System.Collections.IEnumerator ExecutePowerups() // Changed to IEnumerator
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
                 Debug.Log($"- Executing Powerup Type: {powerup.Type} (State: {powerup.State})");

                 // --- Add actual enemy-specific powerup effect logic here ---
                 // Example: ApplyEffectBasedOnState(powerup.Type, powerup.State);

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

                 // Add delay between powerup executions
                 yield return new WaitForSeconds(2.0f); // Wait for 2 seconds
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

        _executionCoroutine = null; // Clear the coroutine reference when done
    }
}
using System;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for LINQ
using UnityEngine;

[System.Serializable]
public struct TileSpriteSet
{
    public Block.BlockType type; // Use Block.BlockType
    public Sprite normalSprite;
    public Sprite disabledSprite;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState State { get; private set; }
    public static event Action<GameState> OnGameStateChanged;

    [Header("Component References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private EnemyAIController _enemyAIController;
    [SerializeField] private Timer _playerTimer; // Reference to the Timer script
    [SerializeField] private GameObject _matchCounterUI;
    [SerializeField] private GameObject _timerUI;
    [SerializeField] private PowerUpManager _powerUpManager; // Add reference

    [Header("Tile Sprites")]
    public List<TileSpriteSet> tileSprites; // List to hold sprite sets for each block type

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ensure timer is subscribed correctly when starting
        if (_playerTimer != null)
        {
            _playerTimer.onTimerEnd.RemoveListener(OnPlayerTimerEnd); // Remove just in case
            _playerTimer.onTimerEnd.AddListener(OnPlayerTimerEnd);
        }
        else
        {
            Debug.LogError("GameManager: Player Timer reference not set in Inspector!");
        }
        UpdateGameState(GameState.Matching);
    }

    void OnDestroy()
    {
        // Unsubscribe from the timer event when the GameManager is destroyed
        if (_playerTimer != null)
        {
            _playerTimer.onTimerEnd.RemoveListener(OnPlayerTimerEnd);
        }
    }

    /// <summary>
    /// Updates the game state and handles the logic associated with each state.
    /// </summary>
    /// <param name="newState"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void UpdateGameState(GameState newState)
    {
        State = newState;
        // Handle game state changes here
        switch (newState)
        {
            case GameState.Matching:
                // Handle matching logic
                HandleMatching();
                break;
            case GameState.Player:
                // Handle player turn logic
                HandlePlayerTurn();
                break;
            case GameState.Enemy:
                // Handle enemy turn logic
                HandleEnemyTurn();
                break;
            case GameState.Win:
                // Handle win logic
                HandleWin();
                break;
            case GameState.GameOver:
                // Handle game over logic
                HandleGameOver();
                break;
            case GameState.Pause:
                // Handle pause logic
                HandlePause();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleMatching()
    {
        Debug.Log("Matching phase!");

        // Restore power-up visuals from inventory
        if (_powerUpManager != null)
        {
            _powerUpManager.RestoreVisualsFromInventory();
        }

        // Set UI visibility
        if (_matchCounterUI != null) _matchCounterUI.SetActive(true);
        if (_timerUI != null) _timerUI.SetActive(false);

        if (_powerUpManager != null) _powerUpManager.SetButtonsInteractable(false);

        if (_gridManager != null) _gridManager.currentSwaps = 0;

        // Enable match 3 interaction
        if (_gridManager != null) _gridManager.gridActive = true;

        // Update swap counter text using new variable names
        if (_gridManager != null && _gridManager.matchCounterText != null)
        {
            _gridManager.matchCounterText.text = (_gridManager.swapLimit - _gridManager.currentSwaps).ToString();
        }

        EnableMatch3Tiles();

        // Tell the Enemy AI to display its chosen powerups
        if (_enemyAIController != null)
        {
            _enemyAIController.DisplayPowerups();
        }
        else
        {
            Debug.LogWarning("GameManager: EnemyAIController reference not set in Inspector!");
        }
    }

    public void HandlePlayerTurn()
    {
        Debug.Log("Player's turn!");

        // Set UI visibility
        if (_matchCounterUI != null) _matchCounterUI.SetActive(false);
        if (_timerUI != null) _timerUI.SetActive(true);

        if (_powerUpManager != null) _powerUpManager.SetButtonsInteractable(true); // Enable buttons

        // The _playerTimer.StartTimer() call was here, but it's removed as the timer is now manually ended.
        // The _playerTimer reference is still needed for the onTimerEnd event.
        // Ensure _playerTimer is assigned in the Inspector.

        DisableMatch3Tiles();
        if (_gridManager != null) _gridManager.gridActive = false; // Disable match 3 grid interaction;
    }

    private void HandleEnemyTurn()
    {
        Debug.Log("Enemy's turn!");

        // UI visibility for _matchCounterUI and _timerUI is now set in OnPlayerTimerEnd
        // and should persist through the enemy turn.
        // if (_matchCounterUI != null) _matchCounterUI.SetActive(false); // Removed
        // if (_timerUI != null) _timerUI.SetActive(true); // Removed

        if (_powerUpManager != null) _powerUpManager.SetButtonsInteractable(false); // Disable buttons

        // Ensure Match-3 grid remains disabled/greyed out
        DisableMatch3Tiles();
        if (_gridManager != null) _gridManager.gridActive = false;

        // Set power-up visuals to unusable for enemy turn
        if (_powerUpManager != null)
        {
            _powerUpManager.SetVisualsToUnusable();
        }
        else
        {
             Debug.LogWarning("GameManager: PowerUpManager reference not set. Cannot set visuals to unusable.");
        }

        // Stop the player timer visually if needed, though the event handles state change
        // if (_playerTimer != null) _playerTimer.StopTimer(); // Optional: Stop visual rotation

        // Note: EnemyAIController now handles its own logic via OnGameStateChanged
    }

    private void HandleWin()
    {
        // Handle win logic here
        Debug.Log("You win!");
    }

    private void HandleGameOver()
    {
        // Handle game over logic here
        Debug.Log("Game Over!");
    }

    private void HandlePause()
    {
        // Handle pause logic here
        Debug.Log("Game Paused!");
    }

    // This method is now redundant if Enable/Disable methods handle sprites directly.
    // Keep it if transparency is still needed for other effects.
    private void SetBlockTrasperancy(float alpha)
    {
        // Validate the alpha value
        alpha = Mathf.Clamp01(alpha);

        // Add null checks to prevent errors
        if (_gridManager == null || _gridManager.Blocks == null)
        {
            Debug.LogWarning("GridManager or its Blocks collection is not initialized yet.");
            return; // Exit the method if references are null
        }

        // Set the transparency of the blocks in the match 3 grid
        foreach (var blockGO in _gridManager.Blocks) // Renamed variable to avoid conflict
        {
            if (blockGO == null) continue; // Skip if block GameObject is null
            var renderer = blockGO.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                var blockColor = renderer.color;
                renderer.color = new Color(blockColor.r, blockColor.g, blockColor.b, alpha);
            }
        }
    }

    private void DisableMatch3Tiles()
    {
        UpdateTileSprites(false);
    }

    private void EnableMatch3Tiles()
    {
        UpdateTileSprites(true);
    }

    private void UpdateTileSprites(bool enabled)
    {
        if (_gridManager == null || _gridManager.Blocks == null)
        {
            Debug.LogWarning("GridManager or its Blocks collection is not initialized.");
            return;
        }

        if (tileSprites == null || tileSprites.Count == 0)
        {
             Debug.LogWarning("Tile Sprites list is not configured in GameManager.");
             return;
        }

        // Optional: Convert list to Dictionary for faster lookups if performance becomes an issue
        // var spriteDict = tileSprites.ToDictionary(s => s.type, s => enabled ? s.normalSprite : s.disabledSprite);

        foreach (var blockGO in _gridManager.Blocks)
        {
            if (blockGO == null) continue;

            var blockComponent = blockGO.GetComponent<Block>(); // Assuming Block script exists
            var renderer = blockGO.GetComponent<SpriteRenderer>();

            if (blockComponent == null || renderer == null)
            {
                Debug.LogWarning($"Block GameObject {blockGO.name} is missing Block component or SpriteRenderer.", blockGO);
                continue;
            }

            // Find the corresponding sprite set for the block's type
            // Using FirstOrDefault to handle cases where a type might not be in the list
            // Ensure comparison uses Block.BlockType
            TileSpriteSet spriteSet = tileSprites.FirstOrDefault(s => s.type == blockComponent.type);

            // Check if a valid sprite set was found (type exists in the list)
            // Ensure comparison uses Block.BlockType
            if (spriteSet.Equals(default(TileSpriteSet)) && !tileSprites.Any(s => s.type == blockComponent.type)) // Check if it's default AND the type isn't actually in the list
            {
                 Debug.LogWarning($"No sprite set found for BlockType {blockComponent.type} in GameManager's tileSprites list.", blockGO);
                 continue;
            }


            Sprite targetSprite = enabled ? spriteSet.normalSprite : spriteSet.disabledSprite;

            if (targetSprite != null)
            {
                renderer.sprite = targetSprite;
                // Optional: Reset transparency if SetBlockTrasperancy is removed/not used
                // var color = renderer.color;
                // renderer.color = new Color(color.r, color.g, color.b, 1f);
            }
            else
            {
                Debug.LogWarning($"{(enabled ? "Normal" : "Disabled")} sprite is null for BlockType {blockComponent.type} in GameManager's tileSprites list.", blockGO);
            }
        }
    }

    // Method called when the player timer ends
    private void OnPlayerTimerEnd()
    {
        Debug.Log("Player timer ended. Switching to Enemy phase.");

        // Show Match Counter UI and hide Timer UI
        if (_matchCounterUI != null) _matchCounterUI.SetActive(true);
        if (_timerUI != null) _timerUI.SetActive(false);

        UpdateGameState(GameState.Enemy);
    }
}

public enum GameState
{
    Start,
    Matching,
    Player,
    Enemy,
    Win,
    GameOver,
    Pause
}

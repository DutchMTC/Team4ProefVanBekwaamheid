using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState State { get; private set; }
    public static event Action<GameState> OnGameStateChanged;

    [Header("Component References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private EnemyAIController _enemyAIController; // Add reference to the Enemy AI

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateGameState(GameState.Matching);
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
        // Handle matching logic here
        Debug.Log("Matching phase!");

        // Reset current matches
        _gridManager.currentMatches = 0; 

        // Enable match 3 interaction
        _gridManager.gridActive = true;

        _gridManager.matchCounterText.text = (_gridManager.matchLimit - _gridManager.currentMatches).ToString(); // Update match counter text

        SetBlockTrasperancy(1f); // Set transparency

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
        // Handle player turn logic here
        Debug.Log("Player's turn!");

        // Example: enable player PowerUps

        SetBlockTrasperancy(0.1f); // Set transparency

        _gridManager.gridActive = false; // Disable match 3 grid prefab;
    }

    private void HandleEnemyTurn()
    {
        // Handle enemy turn logic here
        Debug.Log("Enemy's turn!");

        // Example: start enemy AI logic
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
        foreach (var block in _gridManager.Blocks)
        {
            var blockColor = block.GetComponent<SpriteRenderer>().color;
            block.GetComponent<SpriteRenderer>().color = new Color(blockColor.r, blockColor.g, blockColor.b, alpha); // Gray out the block
        }
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

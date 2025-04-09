using System;
using UnityEngine;
using UnityEngine.XR;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject gridPrefab;
    public GameState State { get; private set; }

    public static event Action<GameState> OnGameStateChanged;
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
        // Example: ungray out the grid
    }
    public void HandlePlayerTurn()
    {
        // Handle player turn logic here
        Debug.Log("Player's turn!");
        // Example: gray out the grid
        // Example: enable player PowerUps
    }
    private void HandleEnemyTurn()
    {
        // Handle enemy turn logic here
        Debug.Log("Enemy's turn!");
        // Example: start enemy AI logic
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

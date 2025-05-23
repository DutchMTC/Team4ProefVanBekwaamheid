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
    private GameState _previousGameState; // To track the previous state
    public static event Action<GameState> OnGameStateChanged;
 
    [Header("Component References")]
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private EnemyAIController _enemyAIController;
    [SerializeField] private Timer _playerTimer; // Reference to the Timer script
    [SerializeField] private GameObject _matchCounterUI;
    [SerializeField] private GameObject _timerUI;
    [SerializeField] private PowerUpManager _powerUpManager; // Add reference
    [SerializeField] private UnityEngine.UI.Image _phaseTransitionImage; // UI Image for phase animation
    [SerializeField] private Animator _phaseTransitionAnimator; // Animator for phase transition
    [SerializeField] private Animator _matchGridCoverAnimator; // Animator for the Match3 grid cover
    [SerializeField] private Animator _powerUpInfoAnimator; // Animator for the PowerUp information UI
    [SerializeField] private EndScreen _endScreen; // Reference to the EndScreen script
 
    [Header("Phase Animation Sprites")]
    [SerializeField] private Sprite _matchingPhaseSprite;
    [SerializeField] private Sprite _playerPhaseSprite;
    [SerializeField] private Sprite _enemyPhaseSprite;

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
        // Initialize previous state to something different from Matching to avoid issues on first run
        _previousGameState = GameState.Start;
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
        // Prevent state changes if the game is already over or won
        if (State == GameState.Win || State == GameState.GameOver)
        {
            // Optionally log or handle this attempt to change state after game end
          return;
        }

        _previousGameState = State; // Store current state as previous before updating
        State = newState;
        // Handle game state changes here
        switch (newState)
        {
            case GameState.Matching:
                // Handle matching logic
                if (_previousGameState != GameState.Start) PlayPhaseAnimation(_matchingPhaseSprite);
                HandleMatching();
                break;
            case GameState.Player:
                // Handle player turn logic
                if (_previousGameState != GameState.Start) PlayPhaseAnimation(_playerPhaseSprite);
                HandlePlayerTurn();
                break;
            case GameState.Enemy:
                // Handle enemy turn logic
                if (_previousGameState != GameState.Start) PlayPhaseAnimation(_enemyPhaseSprite);
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
                break;
            default: throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleMatching()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayMusic(SFXManager.Instance.matchPhaseMusic);
        }

        // Hide cover if not the initial game start
        if (_previousGameState != GameState.Start)
        {
            if (_matchGridCoverAnimator != null)
            {
                //_matchGridCoverAnimator.SetTrigger("CoverHide");
            }
        }

        // Hide powerup info only if coming from Player phase
        if (_previousGameState == GameState.Enemy)
        {
            if (_powerUpInfoAnimator != null)
            {
                _powerUpInfoAnimator.SetTrigger("PowerUpHide");
            }
        }
 
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
    }

    public void HandlePlayerTurn()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayMusic(SFXManager.Instance.battlePhaseMusic);
        }

        if (_matchGridCoverAnimator != null)
        {
            //_matchGridCoverAnimator.SetTrigger("CoverShow");
        }

        if (_powerUpInfoAnimator != null)
        {
            _powerUpInfoAnimator.SetTrigger("PowerUpShow");
        }
 
        // Set UI visibility
        if (_matchCounterUI != null) _matchCounterUI.SetActive(false);
        if (_timerUI != null) _timerUI.SetActive(true);
 
        if (_powerUpManager != null)
        {
            _powerUpManager.SetButtonsInteractable(true); // Enable buttons
            _powerUpManager.AnimateFillsToDisappearForPlayerPhase(); // Animate fills to disappear
        }
 
        // The _playerTimer.StartTimer() call was here, but it's removed as the timer is now manually ended.
        // The _playerTimer reference is still needed for the onTimerEnd event.
        // Ensure _playerTimer is assigned in the Inspector.

        DisableMatch3Tiles();
        if (_gridManager != null) _gridManager.gridActive = false; // Disable match 3 grid interaction;
    }

    private void HandleEnemyTurn()
    {
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
    }

    private void HandleWin()
    {
        // Handle win logic here
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.EnemyDeath);
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.Win);
        }

        if (_phaseTransitionAnimator != null)
        {
            _phaseTransitionAnimator.SetTrigger("GameEnd");
        }

        if (_endScreen != null)
        {
            _endScreen.ShowVictoryScreen();
        }
    }

    private void HandleGameOver()
    {
        // Handle game over logic here
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.PlayerDeath);
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.GameOver);
        }

        if (_phaseTransitionAnimator != null)
        {
            _phaseTransitionAnimator.SetTrigger("GameEnd");
        }

        if (_endScreen != null)
        {
            _endScreen.ShowGameOverScreen();
        }
    }

    // This method is now redundant if Enable/Disable methods handle sprites directly.
    // Keep it if transparency is still needed for other effects.
    private void SetBlockTransparency(float alpha)
    {
        // Validate the alpha value
        alpha = Mathf.Clamp01(alpha);

        // Add null checks to prevent errors
        if (_gridManager == null || _gridManager.Blocks == null)
        {
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
            return;
        }

        if (tileSprites == null || tileSprites.Count == 0)
        {
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
                 continue;
            }


            Sprite targetSprite = enabled ? spriteSet.normalSprite : spriteSet.disabledSprite;

            if (targetSprite != null)
            {
                renderer.sprite = targetSprite;
            }
        }
    }

    // Method called when the player timer ends
    private void OnPlayerTimerEnd()
    {
        // Show Match Counter UI and hide Timer UI
        if (_matchCounterUI != null) _matchCounterUI.SetActive(true);
        if (_timerUI != null) _timerUI.SetActive(false);

        UpdateGameState(GameState.Enemy);
    }
 
    private void PlayPhaseAnimation(Sprite phaseSprite)
    {
        if (_phaseTransitionImage != null && _phaseTransitionAnimator != null && phaseSprite != null)
        {
            _phaseTransitionImage.sprite = phaseSprite;
            _phaseTransitionImage.gameObject.SetActive(true); // Ensure it's active
            _phaseTransitionAnimator.SetTrigger("PhaseSwitch");
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

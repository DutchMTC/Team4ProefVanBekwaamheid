using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using static PowerUpManager;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;
using System.Collections;

public class EnemyAIController : MonoBehaviour
{
    [Serializable]
    public struct PowerupSpriteMapping
    {
        public PowerUpInventory.PowerUpType type;
        public PowerUpSprites sprites;
    }

    [Header("References")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private TileOccupants _playerOccupants;  
    [SerializeField] private CharacterAnimationController _characterAnimationController; 
    [SerializeField] private TileSelection _tileSelection;
    [SerializeField] private CharacterRotator _characterRotator; 
    public Image[] powerupDisplayIcons;
    public List<PowerupSpriteMapping> powerupSpriteMappings;
 
    private AttackPowerUp _attackPowerUp;
    private TrapPowerUp _trapPowerUp;
    private DefensePowerUp _defensePowerUp;
    private TileOccupants _enemyOccupants;
    private GridGenerator _gridGenerator; 

    [Header("AI Probabilities")]
    [Range(0f, 1f)]
    public float probabilityToSelectThreePowerups = 0.2f;
    [Range(0f, 1f)]
    public float probabilityCharged = 0.3f;
    [Range(0f, 1f)]
    public float probabilitySupercharged = 0.1f;

    private bool _movementWasChosen = false;
    private bool _hasMovedThisTurn = false;
    private bool _trapTriggered = false;

    private struct SelectedPowerup
    {
        public PowerUpInventory.PowerUpType Type;
        public PowerUpState State;
    }

    private List<SelectedPowerup> _chosenPowerups = new List<SelectedPowerup>();
    private Color _defaultIconColor = Color.white;
    private Coroutine _executionCoroutine = null;

    void Start()
    {
        _attackPowerUp = GetComponent<AttackPowerUp>();
        _trapPowerUp = GetComponent<TrapPowerUp>();
        _defensePowerUp = GetComponent<DefensePowerUp>();
        _enemyOccupants = GetComponent<TileOccupants>();

        // Attempt to find TileSelection if not assigned
        if (_tileSelection == null)
        {
            _tileSelection = FindObjectOfType<TileSelection>();
        }

        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
        }

        if (_characterAnimationController != null)
        {
            _characterAnimationController.EnemyEntrance();
        }
    }

    void OnEnable()
    {
        if (_gameManager != null)
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        if (powerupDisplayIcons != null && powerupDisplayIcons.Length > 0 && powerupDisplayIcons[0] != null)
        {
            _defaultIconColor = powerupDisplayIcons[0].color;
        }
        HidePowerups();
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Enemy:
                if (_executionCoroutine != null)
                {
                    StopCoroutine(_executionCoroutine);
                }
                _executionCoroutine = StartCoroutine(ExecutePowerups());
                break;

            case GameState.Matching:
                SelectPowerups();
                DisplayPowerups();
                break;

            case GameState.Player:
                break;

            case GameState.Win:
            case GameState.GameOver:
                _chosenPowerups.Clear();
                HidePowerups();
                break;
        }
    }

    private void SelectPowerups()
    {
        _chosenPowerups.Clear();

        List<PowerUpInventory.PowerUpType> availableTypes =
            Enum.GetValues(typeof(PowerUpInventory.PowerUpType))
                .Cast<PowerUpInventory.PowerUpType>()
                .ToList();

        if (availableTypes.Count == 0)
        {
            return;
        }

        int count = (UnityEngine.Random.value < probabilityToSelectThreePowerups) ? 3 : 2;
        count = Mathf.Min(count, availableTypes.Count, powerupDisplayIcons.Length);

        List<PowerUpInventory.PowerUpType> pool = new List<PowerUpInventory.PowerUpType>(availableTypes);

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            PowerUpInventory.PowerUpType selectedType = pool[randomIndex];
            pool.RemoveAt(randomIndex);

            PowerUpState selectedState = PowerUpState.Usable;
            float randomValue = UnityEngine.Random.value;

            if (randomValue < probabilitySupercharged)
            {
                selectedState = PowerUpState.Supercharged;
            }
            else if (randomValue < probabilitySupercharged + probabilityCharged)
            {
                selectedState = PowerUpState.Charged;
            }

            _chosenPowerups.Add(new SelectedPowerup { Type = selectedType, State = selectedState });
        }
    }

    public void DisplayPowerups()
    {
        Debug.Log($"--- DisplayPowerups Called. Chosen Count: {_chosenPowerups.Count} ---");
        if (powerupDisplayIcons == null || powerupSpriteMappings == null)
        {
            return;
        }

        HidePowerups();

        for (int i = 0; i < _chosenPowerups.Count && i < powerupDisplayIcons.Length; i++)
        {
            if (powerupDisplayIcons[i] != null)
            {
                SelectedPowerup currentPowerup = _chosenPowerups[i];
                Sprite iconToShow = GetSpriteForState(currentPowerup.Type, currentPowerup.State);

                if (iconToShow != null)
                {
                    powerupDisplayIcons[i].sprite = iconToShow;
                    powerupDisplayIcons[i].color = _defaultIconColor;
                    powerupDisplayIcons[i].enabled = true;
                }
                else
                {
                    powerupDisplayIcons[i].enabled = false;
                }
            }
        }

        for (int i = _chosenPowerups.Count; i < powerupDisplayIcons.Length; i++)
        {
            if (powerupDisplayIcons[i] != null)
            {
                powerupDisplayIcons[i].enabled = false;
            }
        }
    }

    private Sprite GetSpriteForState(PowerUpInventory.PowerUpType type, PowerUpState state)
    {
        foreach (var mapping in powerupSpriteMappings)
        {
            if (mapping.type == type)
            {
                switch (state)
                {
                    case PowerUpState.Usable:       return mapping.sprites.usable;
                    case PowerUpState.Charged:      return mapping.sprites.charged;
                    case PowerUpState.Supercharged: return mapping.sprites.supercharged;
                    default:
                        return null;
                }
            }
        }
        return null;
    }

    private void HidePowerups()
    {
        if (powerupDisplayIcons == null) return;

        foreach (Image iconImage in powerupDisplayIcons)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
                iconImage.color = _defaultIconColor;
            }
        }
    }

    public void OnTrapTriggered()
    {
        _trapTriggered = true;
        if (_executionCoroutine != null)
        {
            StopCoroutine(_executionCoroutine);
            _executionCoroutine = null;
        }
        // Clear any remaining powerups
        _chosenPowerups.Clear();
        
        // Immediately transition to Matching state when enemy triggers a trap
        if (_gameManager != null)
        {
            _gameManager.UpdateGameState(GameState.Matching);
        }
    }

    private System.Collections.IEnumerator ExecutePowerups()
    {
        _trapTriggered = false; // Reset trap trigger state at start of execution
        _movementWasChosen = false; // Reset movement selection state
        _hasMovedThisTurn = false; // Reset movement completion state
        
        // Reset any trap interruption flag at the start of enemy turn
        TrapBehaviour.ResetTrapInterrupt();
        
        if (_chosenPowerups.Count == 0)
        {
            if (_gameManager != null)
            {
                _gameManager.UpdateGameState(GameState.Matching);
            }
            yield break;
        }

        yield return new WaitForSeconds(2.0f);

        List<PowerUpInventory.PowerUpType> priorityOrder = new List<PowerUpInventory.PowerUpType>
        {
            PowerUpInventory.PowerUpType.Steps,
            PowerUpInventory.PowerUpType.Shield,
            PowerUpInventory.PowerUpType.Sword,
            PowerUpInventory.PowerUpType.Trap
        };

        List<int> executedIconIndices = new List<int>();
        foreach (var priorityType in priorityOrder)
        {
            // If enemy hit a trap, stop all remaining actions
            if (TrapBehaviour.EnemyTurnInterruptedByTrap)
            {
                yield break;
            }

            int chosenIndex = _chosenPowerups.FindIndex(p => p.Type == priorityType);

            if (chosenIndex != -1)
            {
                SelectedPowerup powerupToExecute = _chosenPowerups[chosenIndex];
                bool executedThisPowerup = false;

                switch (powerupToExecute.Type)
                {
                    case PowerUpInventory.PowerUpType.Steps:
                        _movementWasChosen = true;
                        if (_tileSelection != null && _enemyOccupants != null && _playerOccupants != null && _gridGenerator != null)
                        {
                            int moveRange = GetMoveRangeFromState(powerupToExecute.State);
                            TileSettings targetTile = FindBestMovementTile(moveRange);

                            if (targetTile != null)
                            {
                                TileSettings currentEnemyTile = _tileSelection.FindTileAtCoordinates(_enemyOccupants.gridY, _enemyOccupants.gridX);
                                if (currentEnemyTile != null)
                                {
                                    List<TileSettings> path = MovementValidator.FindPath(currentEnemyTile, targetTile, _tileSelection.GetAllTiles());
                                    if (path != null && path.Count > 0)
                                    {
                                        if (_tileSelection.pathVisualizer != null)
                                        {
                                            _tileSelection.pathVisualizer.ShowPath(path);
                                        }
                                        yield return StartCoroutine(_tileSelection.MoveAlongPath(path, _enemyOccupants.gameObject,
                                            _enemyOccupants, TileSelection.UserType.Enemy, _characterAnimationController));

                                        // Check if we hit a trap
                                        if (TrapBehaviour.EnemyTurnInterruptedByTrap)
                                        {           
                                            yield break; // Stop movement and end turn
                                        }
                                        _hasMovedThisTurn = true;
                                        executedThisPowerup = true;
                                    }
                                }
                            }
                        }
                        break;
                    case PowerUpInventory.PowerUpType.Shield:
                        if (_defensePowerUp != null)
                        {
                            _defensePowerUp.DefensePowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy);
                            if (_characterAnimationController != null) _characterAnimationController.EnemyDefense(); // Defense Animation
                            executedThisPowerup = true;
                        }
                        break;
                    case PowerUpInventory.PowerUpType.Sword:
                        if (_attackPowerUp != null && _characterRotator != null && _playerOccupants != null)
                        {
                            // Rotate towards player before attacking
                            yield return StartCoroutine(_characterRotator.RotateTowardsTargetAsync(transform, _playerOccupants.transform, CharacterRotator.UserType.Enemy));

                            _attackPowerUp.AttackPowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy, _playerOccupants);
                            if (_characterAnimationController != null)
                            {
                                switch (powerupToExecute.State)
                                {
                                    case PowerUpState.Usable:
                                        _characterAnimationController.EnemyAttackUsable();
                                        break;
                                    case PowerUpState.Charged:
                                        _characterAnimationController.EnemyAttackCharged();
                                        break;
                                    case PowerUpState.Supercharged:
                                        _characterAnimationController.EnemyAttackSupercharged();
                                        break;
                                }
                            }
                            executedThisPowerup = true;
                        }
                        break;
                    case PowerUpInventory.PowerUpType.Trap:
                        bool canUseTrap = !_movementWasChosen || _hasMovedThisTurn;
                        if (_trapPowerUp != null && canUseTrap)
                        {
                            _trapPowerUp.TrapPowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy, _playerOccupants);
                            executedThisPowerup = true;
                        }
                        break;
                }

                if (executedThisPowerup)
                {
                    executedIconIndices.Add(chosenIndex);
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        if (executedIconIndices.Count > 0)
        {
            foreach (int indexToHide in executedIconIndices)
            {
                if (indexToHide >= 0 && indexToHide < powerupDisplayIcons.Length && powerupDisplayIcons[indexToHide] != null)
                {
                    powerupDisplayIcons[indexToHide].enabled = false;
                }
            }
        }
        // (trap trigger handles its own state transition)
        if (!_trapTriggered && _gameManager != null)
        {
            _gameManager.UpdateGameState(GameState.Matching);
        }

        _executionCoroutine = null;
    }

    public void PlayDeathAnimation()
    {
        if (_characterAnimationController != null)
        {
            _characterAnimationController.EnemyDeath();
        }
    }

    public void PlayDamageAnimation()
    {
        if (_characterAnimationController != null)
        {
            _characterAnimationController.EnemyDamage();
        }
    }    private int GetMoveRangeFromState(PowerUpState state)
    {
        switch (state)
        {
            case PowerUpState.Usable: return 1; // Can only move 1 tile
            case PowerUpState.Charged: return 2; // Can move 2 tiles
            case PowerUpState.Supercharged: return 3; // Can move 3 tiles
            default: return 1;
        }
    }

    private TileSettings FindBestMovementTile(int moveRange)
    {
        if (_tileSelection == null || _enemyOccupants == null || _playerOccupants == null || _gridGenerator == null)
        {
            return null;
        }

        List<TileSettings> reachableTiles = new List<TileSettings>();
        TileSettings currentEnemyTile = _tileSelection.FindTileAtCoordinates(_enemyOccupants.gridY, _enemyOccupants.gridX);

        if (currentEnemyTile == null)
        {
            return null;
        }
        
        // Simplified: Get all tiles and filter by range and occupancy
        // A more robust solution would use pathfinding to check actual reachability within range.
        var allTiles = _tileSelection.GetAllTiles();
        foreach (var tile in allTiles)
        {
            // Skip tiles that are occupied by something other than items or traps
            if (tile.occupantType != TileSettings.OccupantType.None && 
                tile.occupantType != TileSettings.OccupantType.Item && 
                tile.occupantType != TileSettings.OccupantType.Trap)
            {
                continue;
            }

            // Calculate Manhattan distance
            int distY = Mathf.Abs(tile.gridY - currentEnemyTile.gridY);
            int distX = Mathf.Abs(tile.gridX - currentEnemyTile.gridX);
            int totalDist = distX + distY;

            // Strict range check - must be exactly within the range, not more
            if (totalDist > 0 && totalDist <= moveRange) 
            {
                var path = MovementValidator.FindPath(currentEnemyTile, tile, allTiles);
                // Only add if path exists and its length is within range
                if (path != null && path.Count - 1 <= moveRange) // -1 because path includes starting tile
                {
                    reachableTiles.Add(tile);
                }
            }
        }
        
        if (reachableTiles.Count == 0) return null;

        // Try to move closer to the player
        TileSettings playerTile = _tileSelection.FindTileAtCoordinates(_playerOccupants.gridY, _playerOccupants.gridX);
        if (playerTile != null)
        {
            reachableTiles = reachableTiles.OrderBy(t =>
                Mathf.Abs(t.gridX - playerTile.gridX) + Mathf.Abs(t.gridY - playerTile.gridY) // Manhattan distance to player
            ).ToList();
        }
        
        // Potentially add randomness or other heuristics here
        return reachableTiles.FirstOrDefault();
    }
}
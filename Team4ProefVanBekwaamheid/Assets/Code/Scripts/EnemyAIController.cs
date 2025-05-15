using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using static PowerUpManager;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

public class EnemyAIController : MonoBehaviour
{
    [Serializable]
    public struct PowerupSpriteMapping
    {
        public PowerUpInventory.PowerUpType type;
        public PowerUpSprites sprites;
    }

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TileOccupants playerOccupants;
    public Image[] powerupDisplayIcons;
    public List<PowerupSpriteMapping> powerupSpriteMappings;
    [SerializeField] private RectTransform powerupDisplayParent;
    [SerializeField] private CharacterAnimationController characterAnimationController; // Added for animations

    [SerializeField] private MovementPowerUp _movementPowerUp;
    private AttackPowerUp _attackPowerUp;
    private TrapPowerUp _trapPowerUp;
    private DefensePowerUp _defensePowerUp;
    private TileOccupants _enemyOccupants;

    [Header("UI Positioning")]
    [SerializeField] private float verticalOffset = 2.0f;

    [Header("AI Probabilities")]
    [Range(0f, 1f)]
    public float probabilityToSelectThreePowerups = 0.2f;
    [Range(0f, 1f)]
    public float probabilityCharged = 0.3f;
    [Range(0f, 1f)]
    public float probabilitySupercharged = 0.1f;

    private struct SelectedPowerup
    {
        public PowerUpInventory.PowerUpType Type;
        public PowerUpState State;
    }

    private List<SelectedPowerup> chosenPowerups = new List<SelectedPowerup>();
    private Color defaultIconColor = Color.white;
    private Coroutine _executionCoroutine = null;

    void Start()
    {
        PositionPowerupDisplay();
        _movementPowerUp = GetComponent<MovementPowerUp>();
        _attackPowerUp = GetComponent<AttackPowerUp>();
        _trapPowerUp = GetComponent<TrapPowerUp>();
        _defensePowerUp = GetComponent<DefensePowerUp>();
        _enemyOccupants = GetComponent<TileOccupants>();

        if (characterAnimationController != null)
        {
            characterAnimationController.EnemyEntrance();
        }
        else
        {
            Debug.LogWarning("EnemyAIController: CharacterAnimationController not assigned. Enemy entrance animation will not play.");
        }

        if (_movementPowerUp == null || _attackPowerUp == null || _trapPowerUp == null || _defensePowerUp == null)
        {
            Debug.LogError("EnemyAIController: One or more PowerUp script references are missing on this GameObject!");
        }
        if (_enemyOccupants == null)
        {
            Debug.LogError("EnemyAIController: TileOccupants component missing on this GameObject!");
        }
        if (playerOccupants == null)
        {
            Debug.LogError("EnemyAIController: Player TileOccupants reference not assigned in Inspector!");
        }
    }

    void OnEnable()
    {
        if (gameManager != null)
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }
        else
        {
            Debug.LogError("EnemyAIController: GameManager reference not assigned in Inspector!");
        }

        if (powerupDisplayIcons != null && powerupDisplayIcons.Length > 0 && powerupDisplayIcons[0] != null)
        {
            defaultIconColor = powerupDisplayIcons[0].color;
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
                Debug.Log("Enemy AI: Enemy state entered. Starting powerup execution...");
                if (_executionCoroutine != null)
                {
                    StopCoroutine(_executionCoroutine);
                }
                _executionCoroutine = StartCoroutine(ExecutePowerups());
                break;

            case GameState.Matching:
                Debug.Log("Enemy AI: Matching state entered. Selecting powerups for next turn.");
                SelectPowerups();
                DisplayPowerups();
                break;

            case GameState.Player:
                Debug.Log("Enemy AI: Player state entered. Enemy powerups remain visible.");
                break;

            case GameState.Win:
            case GameState.GameOver:
                chosenPowerups.Clear();
                HidePowerups();
                break;
        }
    }

    private void SelectPowerups()
    {
        chosenPowerups.Clear();

        List<PowerUpInventory.PowerUpType> availableTypes =
            Enum.GetValues(typeof(PowerUpInventory.PowerUpType))
                .Cast<PowerUpInventory.PowerUpType>()
                .ToList();

        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("EnemyAIController: No powerup types defined in PowerUpInventory.PowerUpType enum.");
            return;
        }

        int count = (UnityEngine.Random.value < probabilityToSelectThreePowerups) ? 3 : 2;
        count = Mathf.Min(count, availableTypes.Count, powerupDisplayIcons.Length);

        Debug.Log($"Enemy AI: Selecting {count} powerups.");

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

            chosenPowerups.Add(new SelectedPowerup { Type = selectedType, State = selectedState });
            Debug.Log($"Enemy AI: Selected Powerup Type '{selectedType}' with State '{selectedState}'.");
        }
    }

    public void DisplayPowerups()
    {
        Debug.Log($"--- DisplayPowerups Called. Chosen Count: {chosenPowerups.Count} ---");
        if (powerupDisplayIcons == null || powerupSpriteMappings == null)
        {
            Debug.LogError("EnemyAIController: UI Icons or Sprite Mappings not assigned!");
            return;
        }

        HidePowerups();

        for (int i = 0; i < chosenPowerups.Count && i < powerupDisplayIcons.Length; i++)
        {
            if (powerupDisplayIcons[i] != null)
            {
                SelectedPowerup currentPowerup = chosenPowerups[i];
                Sprite iconToShow = GetSpriteForState(currentPowerup.Type, currentPowerup.State);

                if (iconToShow != null)
                {
                    powerupDisplayIcons[i].sprite = iconToShow;
                    powerupDisplayIcons[i].color = defaultIconColor;
                    powerupDisplayIcons[i].enabled = true;
                }
                else
                {
                    Debug.LogWarning($"EnemyAIController: No sprite mapping found for type {currentPowerup.Type} and state {currentPowerup.State}. Hiding slot {i}.");
                    powerupDisplayIcons[i].enabled = false;
                }
            }
        }

        for (int i = chosenPowerups.Count; i < powerupDisplayIcons.Length; i++)
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
                        Debug.LogWarning($"EnemyAIController: Unhandled or unexpected PowerUpState '{state}' requested for type '{type}'.");
                        return null;
                }
            }
        }
        Debug.LogWarning($"EnemyAIController: No PowerupSpriteMapping found for type '{type}'.");
        return null;
    }

    private void PositionPowerupDisplay()
    {
        if (powerupDisplayParent != null)
        {
            powerupDisplayParent.localPosition = new Vector3(0, verticalOffset, 0);
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
                iconImage.sprite = null;
                iconImage.color = defaultIconColor;
            }
        }
    }

    private System.Collections.IEnumerator ExecutePowerups()
    {
        bool _hasMovedThisTurn = false;
        bool _movementWasChosen = chosenPowerups.Any(p => p.Type == PowerUpInventory.PowerUpType.Steps);

        if (chosenPowerups.Count == 0)
        {
            Debug.Log("Enemy AI: No powerups were selected for this turn.");
        }
        else
        {
            Debug.Log("Enemy AI: Starting powerup execution sequence based on priority...");
            Debug.Log("Enemy AI: Waiting for 2 seconds before executing first power-up...");
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
                int chosenIndex = chosenPowerups.FindIndex(p => p.Type == priorityType);

                if (chosenIndex != -1)
                {
                    SelectedPowerup powerupToExecute = chosenPowerups[chosenIndex];
                    bool executedThisPowerup = false;

                    Debug.Log($"Enemy AI: Considering {powerupToExecute.Type} (State: {powerupToExecute.State}) at index {chosenIndex}.");

                    switch (powerupToExecute.Type)
                    {
                        case PowerUpInventory.PowerUpType.Steps:
                            if (_movementPowerUp != null)
                            {
                                Debug.Log($"Enemy AI: Executing Movement ({powerupToExecute.State})");
                                _movementPowerUp.MovementPowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy, playerOccupants);
                                if (characterAnimationController != null) characterAnimationController.EnemyDash(); // Dash Animation
                                _hasMovedThisTurn = true;
                                executedThisPowerup = true;
                            }
                            break;
                        case PowerUpInventory.PowerUpType.Shield:
                            if (_defensePowerUp != null)
                            {
                                Debug.Log($"Enemy AI: Executing Defense ({powerupToExecute.State})");
                                _defensePowerUp.DefensePowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy);
                                if (characterAnimationController != null) characterAnimationController.EnemyDefense(); // Defense Animation
                                executedThisPowerup = true;
                            }
                            break;
                        case PowerUpInventory.PowerUpType.Sword:
                            if (_attackPowerUp != null)
                            {
                                Debug.Log($"Enemy AI: Executing Attack ({powerupToExecute.State})");
                                _attackPowerUp.AttackPowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy, playerOccupants);
                                if (characterAnimationController != null)
                                {
                                    switch (powerupToExecute.State)
                                    {
                                        case PowerUpState.Usable:
                                            characterAnimationController.EnemyAttackUsable();
                                            break;
                                        case PowerUpState.Charged:
                                            characterAnimationController.EnemyAttackCharged();
                                            break;
                                        case PowerUpState.Supercharged:
                                            characterAnimationController.EnemyAttackSupercharged();
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
                                Debug.Log($"Enemy AI: Executing Trap ({powerupToExecute.State})");
                                _trapPowerUp.TrapPowerUpSelected(powerupToExecute.State, TileSelection.UserType.Enemy, playerOccupants);
                                executedThisPowerup = true;
                            }
                            else if (!canUseTrap)
                            {
                                Debug.Log($"Enemy AI: Skipping Trap ({powerupToExecute.State}) because Movement was chosen but not executed yet.");
                            }
                            break;
                    }

                    if (executedThisPowerup)
                    {
                        Debug.Log($"Enemy AI: Initiated {powerupToExecute.Type}.");
                        executedIconIndices.Add(chosenIndex);
                        yield return new WaitForSeconds(1.0f);
                    }
                    else
                    {
                        Debug.LogWarning($"Enemy AI: Failed to initiate {powerupToExecute.Type} (script missing, condition not met, or error).");
                    }
                }
            }

            if (executedIconIndices.Count > 0)
            {
                Debug.Log($"Enemy AI: Hiding icons for {executedIconIndices.Count} executed powerups.");
                foreach (int indexToHide in executedIconIndices)
                {
                    if (indexToHide >= 0 && indexToHide < powerupDisplayIcons.Length && powerupDisplayIcons[indexToHide] != null)
                    {
                        powerupDisplayIcons[indexToHide].enabled = false;
                        Debug.Log($"Enemy AI: Hiding icon at index {indexToHide}");
                    }
                    else
                    {
                        Debug.LogWarning($"Enemy AI: Could not find icon at index {indexToHide} to hide.");
                    }
                }
            }
            else
            {
                Debug.Log("Enemy AI: No powerups were executed this turn.");
            }
            Debug.Log("Enemy AI: Finished powerup execution sequence.");
        }

        if (gameManager != null)
        {
            Debug.Log("Enemy AI: Notifying GameManager to transition state back to Matching.");
            gameManager.UpdateGameState(GameState.Matching);
        }
        else
        {
            Debug.LogError("EnemyAIController: Cannot notify GameManager, reference not assigned!");
        }

        _executionCoroutine = null;
    }

    public void PlayDeathAnimation()
    {
        if (characterAnimationController != null)
        {
            characterAnimationController.EnemyDeath();
        }
        else
        {
            Debug.LogWarning("EnemyAIController: CharacterAnimationController not assigned. Enemy death animation will not play.");
        }
    }

    public void PlayDamageAnimation()
    {
        if (characterAnimationController != null)
        {
            characterAnimationController.EnemyDamage();
        }
        else
        {
            Debug.LogWarning("EnemyAIController: CharacterAnimationController not assigned. Enemy damage animation will not play.");
        }
    }
}
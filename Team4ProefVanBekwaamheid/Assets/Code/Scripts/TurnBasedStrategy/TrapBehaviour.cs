using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

public class TrapBehaviour : MonoBehaviour
{
    [SerializeField] private Animator _trapAnimatorLevel1;
    [SerializeField] private Animator _trapAnimatorLevel2;
    [SerializeField] private GameObject _trapLevel1;
    [SerializeField] private GameObject _trapLevel2;
    private TileSettings _parentTile;
    private int _trapDamage;
    private PowerUpState _currentPowerUpState;

    // Add static flag for enemy turn interruption
    public static bool EnemyTurnInterruptedByTrap { get; private set; } = false;
    public static void ResetTrapInterrupt() => EnemyTurnInterruptedByTrap = false;

    public void Initialize(int damage, PowerUpState powerUpState)
    {
        _trapDamage = damage;
        _currentPowerUpState = powerUpState;
        
        // Set visibility based on power-up state
        if (_trapLevel1 != null && _trapLevel2 != null)
        {
            _trapLevel1.SetActive(powerUpState == PowerUpState.Usable);
            _trapLevel2.SetActive(powerUpState == PowerUpState.Charged || powerUpState == PowerUpState.Supercharged);
            
            // Ensure the correct animator is enabled
            if (_trapAnimatorLevel1 != null) _trapAnimatorLevel1.enabled = powerUpState == PowerUpState.Usable;
            if (_trapAnimatorLevel2 != null) _trapAnimatorLevel2.enabled = powerUpState != PowerUpState.Usable;
        }
    }

    void Start()
    {
        _parentTile = GetComponentInParent<TileSettings>();
    }

    public void Animationlevel1()
    {
        // Trigger the appropriate animator based on power-up state
        if (_currentPowerUpState == PowerUpState.Usable && _trapAnimatorLevel1 != null)
        {
            _trapAnimatorLevel1.SetTrigger("TrapTrigger");
        }
        else if (_currentPowerUpState != PowerUpState.Usable && _trapAnimatorLevel2 != null)
        {
            _trapAnimatorLevel2.SetTrigger("TrapTrigger");
        }
    }

    public void OnCharacterEnterTile(TileOccupants character)
    {
        if (character == null)
        {
            return;
        }

        if (_parentTile == null)
        {
            _parentTile = GetComponentInParent<TileSettings>();
            if (_parentTile == null)
            {
                return;
            }
        }

        // Set the interruption flag immediately for enemy
        if (character.myOccupantType == TileSettings.OccupantType.Enemy)
        {
            EnemyTurnInterruptedByTrap = true;
            var enemyAI = FindObjectOfType<EnemyAIController>();
            if (enemyAI != null)
            {
                enemyAI.OnTrapTriggered();
            }
        }

        // Get child LeafBehaviour if it exists
        LeafBehaviour leafBehaviour = GetComponentInChildren<LeafBehaviour>();
        if (leafBehaviour != null)
        {
            // Start with leaf fade-out, trap effects will follow after completion
            StartCoroutine(TrapActivationSequence(character, leafBehaviour));
        }
        else
        {
            // No leaf found, process trap immediately
            ProcessTrapEffect(character);
        }
    }    private IEnumerator TrapActivationSequence(TileOccupants character, LeafBehaviour leafBehaviour)
    {
        // First, apply damage immediately
        character.TakeDamage(_trapDamage);
        
        // Then start animations
        leafBehaviour.StartFadeOut(1f);
        yield return new WaitForSeconds(1.1f);
        
        Animationlevel1();
        yield return new WaitForSeconds(1.0f);
        
        // Process the remaining trap effects (game state changes, etc.)
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (character.myOccupantType == TileSettings.OccupantType.Player)
            {
                gameManager.UpdateGameState(GameState.Enemy);
            }
            else if (character.myOccupantType == TileSettings.OccupantType.Enemy)
            {
                gameManager.UpdateGameState(GameState.Matching);
            }
        }
        
        // Clean up the trap
        _parentTile.SetOccupant(TileSettings.OccupantType.None, null);
        TrapPowerUp.DecrementTrapCount();
        Destroy(gameObject);
    }

    private void ProcessTrapEffect(TileOccupants character)
    {
        // Start coroutine to handle the trap effect sequence
        StartCoroutine(ProcessTrapEffectSequence(character));
    }

    private IEnumerator ProcessTrapEffectSequence(TileOccupants character)
    {
        // Apply damage immediately
        character.TakeDamage(_trapDamage);
        
        // Then play animations
        Animationlevel1();
        yield return new WaitForSeconds(1.0f);

        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (character.myOccupantType == TileSettings.OccupantType.Player)
            {
                gameManager.UpdateGameState(GameState.Enemy);
            }
            else if (character.myOccupantType == TileSettings.OccupantType.Enemy)
            {
                gameManager.UpdateGameState(GameState.Matching);
            }
        }

        // Clean up the trap
        _parentTile.SetOccupant(TileSettings.OccupantType.None, null);
        TrapPowerUp.DecrementTrapCount();
        Destroy(gameObject);
    }

    // Helper method for decoy leaves
    public void TriggerDecoyFadeOut()
    {
        LeafBehaviour leafBehaviour = GetComponent<LeafBehaviour>();
        if (leafBehaviour != null)
        {
            leafBehaviour.StartFadeOut(1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

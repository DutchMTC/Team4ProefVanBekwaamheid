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

    public void Initialize(int damage, PowerUpState powerUpState)
    {
        _trapDamage = damage;
        _currentPowerUpState = powerUpState;
        Debug.Log($"TrapBehaviour: Initialized with damage {_trapDamage} and state {powerUpState}");
        
        // Set visibility based on power-up state
        if (_trapLevel1 != null && _trapLevel2 != null)
        {
            _trapLevel1.SetActive(powerUpState == PowerUpState.Usable);
            _trapLevel2.SetActive(powerUpState == PowerUpState.Charged || powerUpState == PowerUpState.Supercharged);
            
            // Ensure the correct animator is enabled
            if (_trapAnimatorLevel1 != null) _trapAnimatorLevel1.enabled = powerUpState == PowerUpState.Usable;
            if (_trapAnimatorLevel2 != null) _trapAnimatorLevel2.enabled = powerUpState != PowerUpState.Usable;
        }
        else
        {
            Debug.LogWarning("TrapBehaviour: One or both trap level GameObjects are not assigned!");
        }
    }

    void Start()
    {
        _parentTile = GetComponentInParent<TileSettings>();
        if (_parentTile == null)
        {
            Debug.LogError("TrapBehaviour: No parent TileSettings found!");
        }
    }

    public void Animationlevel1()
    {
        // Trigger the appropriate animator based on power-up state
        if (_currentPowerUpState == PowerUpState.Usable && _trapAnimatorLevel1 != null)
        {
            _trapAnimatorLevel1.SetTrigger("TrapTrigger");
            Debug.Log("TrapBehaviour: Playing Level 1 trap animation");
        }
        else if (_currentPowerUpState != PowerUpState.Usable && _trapAnimatorLevel2 != null)
        {
            _trapAnimatorLevel2.SetTrigger("TrapTrigger");
            Debug.Log("TrapBehaviour: Playing Level 2 trap animation");
        }
        else
        {
            Debug.LogWarning($"TrapBehaviour: Cannot play animation for power-up state {_currentPowerUpState} - animator missing");
        }
    }

    public void OnCharacterEnterTile(TileOccupants character)
    {
        Debug.Log("TrapBehaviour: OnCharacterEnterTile called");

        if (character == null)
        {
            Debug.LogError("TrapBehaviour: Null character entered trap tile!");
            return;
        }

        if (_parentTile == null)
        {
            _parentTile = GetComponentInParent<TileSettings>();
            if (_parentTile == null)
            {
                Debug.LogError("TrapBehaviour: Parent tile is null when trying to trigger trap!");
                return;
            }
        }

        string characterType = character.myOccupantType == TileSettings.OccupantType.Player ? "Player" : "Enemy";
        Debug.Log($"TrapBehaviour: {characterType} is standing on trap at position ({_parentTile.gridY}, {_parentTile.gridX})");

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
        // First, start the leaf fade-out animation
        leafBehaviour.StartFadeOut(1f);
        
        // Wait for the leaf fade-out animation to complete (1 second + small buffer)
        yield return new WaitForSeconds(1.1f);
        
        // Play the trap animation
        Animationlevel1();
        
        // Wait for the trap animation to complete (assuming animation is 1 second, adjust as needed)
        yield return new WaitForSeconds(1.0f);
        
        // Now that both animations are complete, apply trap effects
        character.TakeDamage(_trapDamage);
        
        // Process the remaining trap effects (game state changes, etc.)
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (character.myOccupantType == TileSettings.OccupantType.Player)
            {
                Debug.Log("TrapBehaviour: Player triggered trap, switching to Enemy phase");
                gameManager.UpdateGameState(GameState.Enemy);
            }
            else if (character.myOccupantType == TileSettings.OccupantType.Enemy)
            {
                Debug.Log("TrapBehaviour: Enemy triggered trap, switching to Matching phase");
                gameManager.UpdateGameState(GameState.Matching);
            }
        }
        
        // Clean up the trap
        _parentTile.SetOccupant(TileSettings.OccupantType.None, null);
        TrapPowerUp.DecrementTrapCount();
        Destroy(gameObject);
    }    private void ProcessTrapEffect(TileOccupants character)
    {
        // Start coroutine to handle the trap effect sequence
        StartCoroutine(ProcessTrapEffectSequence(character));
    }

    private IEnumerator ProcessTrapEffectSequence(TileOccupants character)
    {
        // Play the trap animation first
        Animationlevel1();
        
        // Wait for the trap animation to complete (assuming animation is 1 second, adjust as needed)
        yield return new WaitForSeconds(1.0f);
        
        // Apply trap effects (damage, etc.)
        character.TakeDamage(_trapDamage);

        // Get reference to GameManager
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            // Switch game state based on who triggered the trap
            if (character.myOccupantType == TileSettings.OccupantType.Player)
            {
                Debug.Log("TrapBehaviour: Player triggered trap, switching to Enemy phase");
                gameManager.UpdateGameState(GameState.Enemy);
            }
            else if (character.myOccupantType == TileSettings.OccupantType.Enemy)
            {
                Debug.Log("TrapBehaviour: Enemy triggered trap, switching to Matching phase");
                gameManager.UpdateGameState(GameState.Matching);
            }
        }
        else
        {
            Debug.LogError("TrapBehaviour: Could not find GameManager instance!");
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
            Debug.LogWarning("TrapBehaviour: No LeafBehaviour found on decoy");
            Destroy(gameObject);
        }
    }
}

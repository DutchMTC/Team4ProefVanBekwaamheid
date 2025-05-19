using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

public class TrapBehaviour : MonoBehaviour
{
    private TileSettings _parentTile;
    private int _trapDamage;

    public void Initialize(int damage)
    {
        _trapDamage = damage;
        Debug.Log($"TrapBehaviour: Initialized with damage {_trapDamage}");
    }

    void Start()
    {
        _parentTile = GetComponentInParent<TileSettings>();
        if (_parentTile == null)
        {
            Debug.LogError("TrapBehaviour: No parent TileSettings found!");
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
        }        // Clean up the trap
        _parentTile.SetOccupant(TileSettings.OccupantType.None, null);
        TrapPowerUp.DecrementTrapCount();
        Destroy(gameObject);
    }
}

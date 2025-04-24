using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using static PowerUpManager;
using UnityEngine.Tilemaps;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class AttackPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range = 1;
        [SerializeField] private int _baseDamage = 10;
        private int _currentDamage;
        private TileSelection _tileSelection;
        private TileOccupants _tileOccupants; // The user of the powerup
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();
            
            if (_tileSelection == null)
            {
                Debug.LogError("TileSelection script not found!");
            }
        }

        // Removed Update method with debug key press

        // Added optional targetOccupant parameter for AI
        public void AttackPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    _currentDamage = _baseDamage;
                    break;
                case PowerUpState.Charged:
                    _range = 1;
                    _currentDamage = _baseDamage * 2;
                    break;
                case PowerUpState.Supercharged:
                    _range = 2;
                    _currentDamage = _baseDamage * 3;
                    break;
            }

            if (_isWaitingForSelection)
            {
                // Cancel current selection
                _tileSelection.CancelTileSelection();
                _isWaitingForSelection = false;
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
                return;
            }

            // Start tile selection process to find valid tiles
            Vector2Int currentPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Attack, userType);

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                if (playerTile != null && IsTileInRange(playerTile))
                {
                     Debug.Log($"Enemy AI (Attack): Attacking player at ({playerTile.row}, {playerTile.column})");
                     Attack(playerTile);
                }
                else
                {
                    Debug.LogWarning("Enemy AI (Attack): Player is not in range or player tile not found.");
                    // Optionally, do nothing or attack nearest valid target if any
                }
                _tileSelection.CancelTileSelection(); // Clean up selection state
            }
            else // Player waits for input
            {
                _isWaitingForSelection = true;
                _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            }
        }

        // This is now only called for Player input
        private void HandleTileSelected(TileSettings selectedTile)
        {
            if (!_isWaitingForSelection) return; // Only proceed if waiting for player input

            _isWaitingForSelection = false;
            _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);

            // Player Logic: Attack the selected tile
            Attack(selectedTile);

            // AI logic is handled directly in AttackPowerUpSelected, so this part is no longer needed here.
            /*
            if (_currentUserType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
            {
                 // This block is now handled directly in AttackPowerUpSelected for AI
            }
            else // Player Logic
            {
                 // This is handled above
            }
            */
        }

        // Helper to check if a tile is within the current attack range
        private bool IsTileInRange(TileSettings targetTile)
        {
            if (targetTile == null || _tileOccupants == null) return false;

            Vector2Int currentPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
            Vector2Int targetPos = new Vector2Int(targetTile.row, targetTile.column);

            // Simple Manhattan distance check for grid movement
            int distance = Mathf.Abs(currentPos.x - targetPos.x) + Mathf.Abs(currentPos.y - targetPos.y);
            return distance <= _range;
        }

        private void Attack(TileSettings targetTile)
        {
            // Determine the correct target type based on the user
            TileSettings.OccupantType expectedTargetType = (_currentUserType == TileSelection.UserType.Player)
                ? TileSettings.OccupantType.Enemy
                : TileSettings.OccupantType.Player;

            if (targetTile != null && targetTile.occupantType == expectedTargetType)
            {
                // Get the target's health component and apply damage
                var targetHealth = targetTile.tileOccupant.GetComponent<TileOccupants>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(_currentDamage);
                    Debug.Log($"{_currentUserType} attacked {expectedTargetType} for {_currentDamage} damage!");
                }
                else
                {
                    Debug.LogWarning($"{expectedTargetType} tile found but no TileOccupants component present!");
                }
            }
            else
            {
                if (targetTile == null)
                {
                     Debug.Log("Invalid target tile for attack.");
                }
                else
                {
                    Debug.Log($"Selected tile has no {expectedTargetType} to attack!");
                }
            }
        }

        void OnDestroy()
        {
            if (_tileSelection != null)
            {
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            }
        }
    }
}

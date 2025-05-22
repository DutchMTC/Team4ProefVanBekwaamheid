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
        private CharacterAnimationController _animationController;
        private PowerUpState _currentPowerUpState; // Store the current power-up state

        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();
            _animationController = FindObjectOfType<CharacterAnimationController>();
        }

        // Removed Update method with debug key press

        // Added optional targetOccupant parameter for AI
        public void AttackPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI
            _currentPowerUpState = _state; // Store the current power-up state
 
            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    _currentDamage = _baseDamage;
                    // Animation moved to Attack method
                    break;
                case PowerUpState.Charged:
                    _range = 1;
                    _currentDamage = _baseDamage * 2;
                    // Animation moved to Attack method
                    break;
                case PowerUpState.Supercharged:
                    _range = 2;
                    _currentDamage = _baseDamage * 3;
                    // Animation moved to Attack method
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
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY); // Standardized: (gridX, gridY) -> (column, row)
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Attack, userType);

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                if (playerTile != null && IsTileInRange(playerTile))
                {
                     Attack(playerTile);
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
        }

        // Helper to check if a tile is within the current attack range
        private bool IsTileInRange(TileSettings targetTile)
        {
            if (targetTile == null || _tileOccupants == null) return false;

            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridY, _tileOccupants.gridX); // Changed to gridY and gridX
            Vector2Int targetPos = new Vector2Int(targetTile.gridY, targetTile.gridX); // Changed to gridY and gridX

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
                // Trigger animation before dealing damage
                if (_currentUserType == TileSelection.UserType.Player && _animationController != null)
                {
                    switch (_currentPowerUpState)
                    {
                        case PowerUpState.Usable:
                            _animationController.PlayerAttackUsable();
                            break;
                        case PowerUpState.Charged:
                            _animationController.PlayerAttackCharged();
                            break;
                        case PowerUpState.Supercharged:
                            _animationController.PlayerAttackSupercharged();
                            break;
                    }
                }

                // Get the target's health component and apply damage
                var targetHealth = targetTile.tileOccupant.GetComponent<TileOccupants>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(_currentDamage);
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

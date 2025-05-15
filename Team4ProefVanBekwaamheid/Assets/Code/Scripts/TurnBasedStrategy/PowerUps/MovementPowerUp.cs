using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using System.Collections.Generic; // Added for List<>

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class MovementPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // Reference to the TileOccupants script (the user of the powerup)
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI
        private CharacterAnimationController _animationController;
 
        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();
            _animationController = FindObjectOfType<CharacterAnimationController>();
            
            if (_tileSelection == null)
            {
                Debug.LogError("TileSelection script not found!");
            }
        }      

        // Removed Update method with debug key press
        // Added optional targetOccupant parameter for AI
        public void MovementPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            // Movement moet hierin aangeroepen worden en range moet hierin bepaald worden
            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1; // Set range for usable state
                    break;
                case PowerUpState.Charged:
                    _range = 2; // Set range for charged state
                    break;
                case PowerUpState.Supercharged:
                    _range = 3; // Set range for supercharged state
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
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement, userType);

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                TileSettings bestTile = FindBestMoveTileTowardsTarget(selectableTiles, _targetOccupantForAI);

                if (bestTile != null)
                {
                    Debug.Log($"Enemy AI (Movement): Moving towards player at ({_targetOccupantForAI.gridY}, {_targetOccupantForAI.gridX}). Best tile: ({bestTile.gridY}, {bestTile.gridX})"); // Changed to gridY and gridX
                    Move(bestTile);
                }
                else
                {
                    Debug.LogWarning("Enemy AI (Movement): Could not find a valid tile to move towards the player.");
                    // Optionally, pick a random valid tile or do nothing
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

            // Player Logic: Move to the selected tile
            Move(selectedTile);

            // AI logic is handled directly in MovementPowerUpSelected, so this part is no longer needed here.
            /*
            if (_currentUserType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                 List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                 TileSettings bestTile = FindBestMoveTileTowardsTarget(selectableTiles, _targetOccupantForAI);
                 // Debug.LogWarning("Enemy AI (Movement): AI targeting needs TileSelection.GetSelectableTiles() or equivalent."); // Removed warning

                 if (bestTile != null)
                 {
                     Debug.Log($"Enemy AI (Movement): Moving towards player at ({_targetOccupantForAI.row}, {_targetOccupantForAI.column}). Best tile: ({bestTile.row}, {bestTile.column})");
                     Move(bestTile);
                 }
                 else
                 {
                     Debug.LogWarning("Enemy AI (Movement): Could not find a valid tile to move towards the player.");
                     // Optionally, pick a random valid tile or do nothing
                     Move(selectedTile); // Fallback to originally selected (might be null or invalid for AI)
                 }
                 _tileSelection.CancelTileSelection(); // Ensure selection mode is exited
            }
            else // Player Logic
            {
                 // This is handled above
            }
            */
        }

        private void Move(TileSettings targetTile)
        {
            // Allow moving if the tile is None OR an Item
            if (targetTile != null &&
                (targetTile.occupantType == TileSettings.OccupantType.None || targetTile.occupantType == TileSettings.OccupantType.Item))
            {
                _tileOccupants.gridY = targetTile.gridY; // Changed to gridY
                _tileOccupants.gridX = targetTile.gridX; // Changed to gridX
                _tileOccupants.MoveToTile();
                if (_currentUserType == TileSelection.UserType.Player && _animationController != null)
                {
                    _animationController.PlayerDash();
                }
            }
            else
            {
                if (targetTile == null) {
                    Debug.Log("Target tile is null, cannot move.");
                } else {
                    // Provide more specific feedback if the tile is occupied by something other than None or Item
                    Debug.Log($"Selected tile is occupied by {targetTile.occupantType} or invalid, cannot move here.");
                }
            }
        }

        // AI Helper: Find the best tile to move towards the target
        private TileSettings FindBestMoveTileTowardsTarget(List<TileSettings> selectableTiles, TileOccupants target)
        {
            TileSettings bestTile = null;
            float minDistanceSq = float.MaxValue;
            Vector2Int targetPos = new Vector2Int(target.gridX, target.gridY); // Standardized: (gridX, gridY) -> (column, row)

            foreach (var tile in selectableTiles)
            {
                if (tile.occupantType == TileSettings.OccupantType.None) // Ensure tile is empty
                {
                    Vector2Int tilePos = new Vector2Int(tile.gridX, tile.gridY); // Standardized: (gridX, gridY) -> (column, row)
                    float distanceSq = Vector2Int.Distance(tilePos, targetPos); // Using squared distance for efficiency

                    if (distanceSq < minDistanceSq)
                    {
                        minDistanceSq = distanceSq;
                        bestTile = tile;
                    }
                }
            }
            return bestTile;
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

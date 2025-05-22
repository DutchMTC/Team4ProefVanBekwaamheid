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
        }    private void Move(TileSettings targetTile)
    {
        if (targetTile == null)
        {
            Debug.Log("Target tile is null, cannot move.");
            return;
        }

        if (targetTile.occupantType == TileSettings.OccupantType.None || 
            targetTile.occupantType == TileSettings.OccupantType.Item ||
            targetTile.occupantType == TileSettings.OccupantType.Trap ||
            targetTile.occupantType == TileSettings.OccupantType.Decoy)
        {
            // Check for trap before moving
            if (targetTile.occupantType == TileSettings.OccupantType.Trap)
            {
                Debug.Log($"MovementPowerUp: Found trap on tile ({targetTile.gridX}, {targetTile.gridY})");
                
                var trapBehaviour = targetTile.GetComponentInChildren<TrapBehaviour>(true);
                if (trapBehaviour != null)
                {
                    Debug.Log("MovementPowerUp: Found TrapBehaviour, triggering OnCharacterEnterTile");
                    trapBehaviour.OnCharacterEnterTile(_tileOccupants);
                    // Don't proceed with movement if this is an enemy - trap will handle state change
                    if (_currentUserType == TileSelection.UserType.Enemy)
                    {
                        return;
                    }
                }
            }

            _tileOccupants.gridY = targetTile.gridY;
            _tileOccupants.gridX = targetTile.gridX;
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
        }        // AI Helper: Find the best tile to move towards the target
        private TileSettings FindBestMoveTileTowardsTarget(List<TileSettings> selectableTiles, TileOccupants target)
        {
            if (selectableTiles == null || target == null || _tileOccupants == null)
            {
                Debug.LogError("MovementPowerUp: Missing required references for finding best move tile");
                return null;
            }

            TileSettings bestTile = null;
            float minDistanceToTarget = float.MaxValue;
            Vector2Int targetPos = new Vector2Int(target.gridX, target.gridY);
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY);

            foreach (TileSettings tile in selectableTiles)
            {
                if (tile == null) continue;

                Vector2Int tilePos = new Vector2Int(tile.gridX, tile.gridY);
                
                // Verify the tile is within movement range
                int distance = Mathf.Abs(tilePos.x - currentPos.x) + Mathf.Abs(tilePos.y - currentPos.y);
                if (distance > _range)
                {
                    continue;
                }

                // Only consider empty tiles
                if (tile.occupantType == TileSettings.OccupantType.None)
                {
                    float distanceToTarget = Vector2.Distance(tilePos, targetPos);
                    if (distanceToTarget < minDistanceToTarget)
                    {
                        minDistanceToTarget = distanceToTarget;
                        bestTile = tile;
                    }
                }
            }

            return bestTile;        }

        private void OnDestroy()
        {
            if (_tileSelection != null)
            {
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            }
        }
    }
}

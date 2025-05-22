using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using System.Collections.Generic; // Added for List<>

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class WallPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        [SerializeField] private GameObject _wallPrefab; // The prefab to place as a wall
        [SerializeField] private Vector3 _positionOffset = Vector3.zero; // Offset for the wall's spawn position
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // The user of the powerup
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();
        }

        // Added optional targetOccupant parameter for AI
        public void WallPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
             _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    break;
                case PowerUpState.Charged:
                    _range = 1;
                    break;
                case PowerUpState.Supercharged:
                    _range = 1; 
                    break;
            }

            if (_isWaitingForSelection)
            {
                _tileSelection.CancelTileSelection();
                _isWaitingForSelection = false;
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
                return;
            }

            // Start tile selection process to find valid empty tiles within range
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY); // Standardized: (gridX, gridY) -> (column, row)
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement, userType); // Movement type finds empty tiles

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                 TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                 List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();

                 TileSettings bestTile = FindBestWallPlacementTile(selectableTiles, playerTile);

                 if (bestTile != null)
                 {               
                     PlaceWall(bestTile);
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

            // Player Logic: Place wall on the selected tile
            PlaceWall(selectedTile);
        }

        private void PlaceWall(TileSettings targetTile)
        {
            if (targetTile == null)
            {

                return; // Exit early if tile is null
            }
            
            // Original condition check
            if (targetTile.occupantType == TileSettings.OccupantType.None && _wallPrefab != null)
            {
                Vector2Int userPos = new Vector2Int(_tileOccupants.gridY, _tileOccupants.gridX); // Changed to gridY and gridX
                Vector2Int targetPos = new Vector2Int(targetTile.gridY, targetTile.gridX); // Changed to gridY and gridX
                Vector2Int direction = targetPos - userPos; // Corrected variable name

                float angle = 0f;

                if (direction == Vector2Int.left)
                {
                    angle = 45f;
                }
                else if (direction == Vector2Int.up)
                {
                    angle = 315f;
                }
                else if (direction == Vector2Int.right)
                {
                    angle = 225f;
                }
                else if (direction == Vector2Int.down)
                {
                    angle = 135f;
                }

                Vector3 spawnPosition = targetTile.transform.position + _positionOffset;
                Quaternion spawnRotation = Quaternion.Euler(0, angle, 0);

                GameObject wallInstance = Instantiate(_wallPrefab, spawnPosition, spawnRotation);

                targetTile.SetOccupant(TileSettings.OccupantType.Trap, wallInstance); // Used SetOccupant  
            }
        }

        // AI Helper: Find the best empty, selectable tile adjacent to the enemy, prioritizing the one closest to the player.
        private TileSettings FindBestWallPlacementTile(List<TileSettings> selectableTiles, TileSettings targetPlayerTile)
        {
             TileSettings bestTile = null;
             float minDistanceToPlayerSq = float.MaxValue;

             if (selectableTiles == null || selectableTiles.Count == 0)
             {
                 return null; // No tiles to choose from
             }

             if (targetPlayerTile == null)
             {
                 return selectableTiles[0]; // Fallback: just pick the first one
             }

             Vector2Int playerPos = new Vector2Int(targetPlayerTile.gridY, targetPlayerTile.gridX); // Changed to gridY and gridX

             foreach (var tile in selectableTiles)
             {
                 // Selectable tiles are adjacent to the enemy and guaranteed empty
                 Vector2Int tilePos = new Vector2Int(tile.gridY, tile.gridX); // Changed to gridY and gridX
                 float distanceSq = Vector2.Distance(tilePos, playerPos); // Use squared distance for efficiency

                 // Debug.Log($"Enemy AI (Wall Finder): Checking tile ({tilePos.x}, {tilePos.y}). DistSq to player: {distanceSq}"); // Optional: Verbose log

                 if (distanceSq < minDistanceToPlayerSq)
                 {
                     minDistanceToPlayerSq = distanceSq;
                     bestTile = tile;
                 }
             }

             if (bestTile != null)
             {
                 Debug.Log($"Enemy AI (Wall Finder): Selected tile ({bestTile.gridY}, {bestTile.gridX}) as closest to player (DistSq: {minDistanceToPlayerSq})."); // Changed to gridY and gridX
             }
             else
             {
                 // This should technically not happen if selectableTiles is not empty, but added as safety.
                 bestTile = selectableTiles[0];
             }

             return bestTile;
        }

        void OnDestroy()
        {
            // Ensure listener is removed if the object is destroyed while waiting
            if (_tileSelection != null && _isWaitingForSelection)
            {
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            }
        }
    }
}
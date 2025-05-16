using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using System.Collections.Generic;
using Unity.VisualScripting; // Added for List<>

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class TrapPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        [SerializeField] private int _baseDamage = 10; // Base damage of the trap
        private int _currentDamage; // Current damage of the trap, modified by power-up state
        [SerializeField] private GameObject _trapPrefab; // The prefab to place as a trap
        private TileSelection _tileSelection; // Reference to the TileSelection script
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
            if (_tileOccupants == null)
            {
                Debug.LogError("TileOccupants script not found on the GameObject!");
            }
            if (_trapPrefab == null)
            {
                Debug.LogError("Trap Prefab is not assigned in the TrapPowerUp script!");
            }
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.T))
            {
                // Debug key press for testing
                Debug.Log("T key pressed - Triggering TrapPowerUpSelected with default parameters.");
                TrapPowerUpSelected(PowerUpState.Usable, TileSelection.UserType.Player);
            }
        }

        // Removed Update method with debug key press

        // Added optional targetOccupant parameter for AI
        public void TrapPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    _currentDamage = _baseDamage; // Reset damage for usable state
                    break;
                case PowerUpState.Charged:
                    _range = 2;
                    _currentDamage = _baseDamage + 5;
                    break;
                case PowerUpState.Supercharged:
                    _range = 3;
                    _currentDamage = _baseDamage + 10;
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
                Debug.Log($"Enemy AI (Trap): Attempting trap placement with range {_range}.");
                TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                Debug.Log($"Enemy AI (Trap): Found {selectableTiles.Count} selectable empty tiles in range.");

                if (playerTile == null)
                {
                    Debug.LogWarning("Enemy AI (Trap): Cannot target trap placement, player tile is null.");
                }

                TileSettings bestTile = FindBestTrapPlacementTile(selectableTiles, playerTile);

                if (bestTile != null)
                {
                    // *** DETAILED DEBUG: Log the chosen tile before attempting placement ***
                    Debug.Log($"Enemy AI (Trap): Found best tile at ({bestTile.gridY}, {bestTile.gridX}). Attempting placement..."); // Changed to gridY and gridX
                    PlaceTrap(bestTile);
                }
                else
                {
                    // This warning now correctly reflects that no valid tile (adjacent or fallback) was found within range.
                    Debug.LogWarning("Enemy AI (Trap): Could not find any valid tile to place trap.");
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

            // Player Logic: Place trap on the selected tile
            PlaceTrap(selectedTile);

            // AI logic is handled directly in TrapPowerUpSelected, so this part is no longer needed here.
            /* AI logic moved to TrapPowerUpSelected */
        }

        private void PlaceTrap(TileSettings targetTile)
        {
            // *** DETAILED DEBUG: Log received tile and condition checks ***
            if (targetTile == null)
            {
                Debug.LogError("Enemy AI (PlaceTrap): Received NULL targetTile!");
                return; // Exit early if tile is null
            }

            Debug.Log($"Enemy AI (PlaceTrap): Attempting to place trap on tile at ({targetTile.gridY}, {targetTile.gridX})."); // Changed to gridY and gridX
            Debug.Log($"Enemy AI (PlaceTrap): Checking conditions - Is Tile Null? {targetTile == null}, Occupant Type: {targetTile.occupantType}, Is Prefab Null? {_trapPrefab == null}");

            // Original condition check
            if (targetTile.occupantType == TileSettings.OccupantType.None && _trapPrefab != null)
            {
                Debug.Log("Enemy AI (PlaceTrap): Conditions met. Proceeding with instantiation.");

                Vector3 spawnPosition = targetTile.transform.position;

                GameObject trapInstance = Instantiate(_trapPrefab, spawnPosition, Quaternion.identity);

                targetTile.SetOccupant(TileSettings.OccupantType.Trap, trapInstance); // Used SetOccupant
                // targetTile.OccupationChangedEvent.Invoke(); // Invoke is handled by SetOccupant

                Debug.Log($"Enemy AI (PlaceTrap): Successfully placed trap at {targetTile.gridY}, {targetTile.gridX}."); // Changed to gridY and gridX
            }
            else // Log specific reason for failure
            {
                 if (targetTile.occupantType != TileSettings.OccupantType.None)
                 {
                      Debug.LogError($"Enemy AI (PlaceTrap): Failed - Tile ({targetTile.gridY}, {targetTile.gridX}) is occupied by {targetTile.occupantType}."); // Changed to gridY and gridX
                 }
                 else if (_trapPrefab == null)
                 {
                      Debug.LogError("Enemy AI (PlaceTrap): Failed - Trap Prefab is not assigned!");
                 }
                 else
                 {
                      // This case should ideally not be reached with the checks above, but included for safety.
                      Debug.LogError("Enemy AI (PlaceTrap): Failed - Unknown reason (Tile might be null despite initial check, or other logic error).");
                 }
            }
        }

        // AI Helper: Find the best empty, selectable tile adjacent to the enemy, prioritizing the one closest to the player.
        private TileSettings FindBestTrapPlacementTile(List<TileSettings> selectableTiles, TileSettings targetPlayerTile)
        {
             TileSettings bestTile = null;
             float minDistanceToPlayerSq = float.MaxValue;

             if (selectableTiles == null || selectableTiles.Count == 0)
             {
                 Debug.Log("Enemy AI (Trap Finder): No selectable tiles provided.");
                 return null; // No tiles to choose from
             }

             if (targetPlayerTile == null)
             {
                 Debug.LogWarning("Enemy AI (Trap Finder): Player tile is null. Selecting the first available tile.");
                 return selectableTiles[0]; // Fallback: just pick the first one
             }

             Vector2Int playerPos = new Vector2Int(targetPlayerTile.gridY, targetPlayerTile.gridX); // Changed to gridY and gridX
             Debug.Log($"Enemy AI (Trap Finder): Target player at ({playerPos.x}, {playerPos.y}). Checking {selectableTiles.Count} adjacent tiles for closest one.");

             foreach (var tile in selectableTiles)
             {
                 // Selectable tiles are adjacent to the enemy and guaranteed empty
                 Vector2Int tilePos = new Vector2Int(tile.gridY, tile.gridX); // Changed to gridY and gridX
                 float distanceSq = Vector2.Distance(tilePos, playerPos); // Use squared distance for efficiency

                 // Debug.Log($"Enemy AI (Trap Finder): Checking tile ({tilePos.x}, {tilePos.y}). DistSq to player: {distanceSq}"); // Optional: Verbose log

                 if (distanceSq < minDistanceToPlayerSq)
                 {
                     minDistanceToPlayerSq = distanceSq;
                     bestTile = tile;
                 }
             }

             if (bestTile != null)
             {
                 Debug.Log($"Enemy AI (Trap Finder): Selected tile ({bestTile.gridY}, {bestTile.gridX}) as closest to player (DistSq: {minDistanceToPlayerSq})."); // Changed to gridY and gridX
             }
             else
             {
                 // This should technically not happen if selectableTiles is not empty, but added as safety.
                 Debug.LogWarning("Enemy AI (Trap Finder): Could not determine closest tile, selecting first available.");
                 bestTile = selectableTiles[0];
             }

             return bestTile;
        }

        // Helper for distance calculation (optional refinement)
        private int CalculateManhattanDistance(Vector2Int posA, Vector2Int posB)
        {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
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
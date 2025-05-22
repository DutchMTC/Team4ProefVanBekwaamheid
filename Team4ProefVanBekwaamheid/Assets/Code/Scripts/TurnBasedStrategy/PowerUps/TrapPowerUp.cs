using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using System.Collections.Generic;
using Unity.VisualScripting; // Added for List<>
using System.Linq; // Added for LINQ operations

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class TrapPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        [SerializeField] private int _baseDamage = 8; // Base damage of the trap
        [SerializeField] private int _decoyAmount; // Base damage of the trap
        [SerializeField] private GameObject _trapPrefab; // The prefab to place as a trap
        [SerializeField] private GameObject _leafPrefab; // The prefab to place as a trap
        [SerializeField] private GameObject _trapLimitReachedIndicator; // Indicator for max traps reached
        private PowerUpState _currentPowerUpState;
        private int _currentDamage; // Current damage of the trap, modified by power-up state
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // The user of the powerup
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        // Add trap limit tracking
        private const int MAX_TRAPS = 3;
        private static int _activeTrapCount = 0;
        public static bool CanPlaceTrap => _activeTrapCount < MAX_TRAPS;

        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();

            if (_trapLimitReachedIndicator == null)
            {
                Debug.LogWarning("Trap Limit Reached Indicator is not assigned in the TrapPowerUp script. UI updates for trap limit will not be shown.");
            }
            else
            {
                _trapLimitReachedIndicator.SetActive(false); // Initially hide the indicator
            }
        }

        // Removed Update method with debug key press

        // Added optional targetOccupant parameter for AI
        public void TrapPowerUpSelected(PowerUpState state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentPowerUpState = state; // Store the state
            
            // First check if we can place more traps
            if (!CanPlaceTrap)
            {                
                return;
            }

            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    _decoyAmount = 0; // Reset decoy amount for usable state
                    _currentDamage = _baseDamage; // Reset damage for usable state
                    break;
                case PowerUpState.Charged:
                    _range = 2;
                    _decoyAmount = 1; // Set decoy amount for charged state
                    _currentDamage = _baseDamage + 4;
                    break;
                case PowerUpState.Supercharged:
                    _range = 3;
                    _decoyAmount = 1; // Set decoy amount for supercharged state
                    _currentDamage = _baseDamage + 14;
                    break;
            }

            if (_isWaitingForSelection)
            {
                _tileSelection.CancelTileSelection();
                _isWaitingForSelection = false;
                _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
                return;
            }            // Start tile selection process to find valid empty tiles within range
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY); // Standardized: (gridX, gridY) -> (column, row)
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Trap, userType); // Use Trap type for proper handling

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                TileSettings bestTile = FindBestTrapPlacementTile(selectableTiles, playerTile);

                if (bestTile != null)
                { 
                    PlaceTrap(bestTile);
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
        }

        private void PlaceTrap(TileSettings targetTile)
        {
            if (targetTile == null)
            {
                return; // Exit early if tile is null
            }

            if (!CanPlaceTrap)
            {
                return; // Exit early if trap limit is reached
            }

            if ((targetTile.occupantType == TileSettings.OccupantType.None || targetTile.occupantType == TileSettings.OccupantType.Decoy) && targetTile.occupantType != TileSettings.OccupantType.Item && _trapPrefab != null)
            {
                Vector3 spawnPosition = targetTile.transform.position;
                
                // Instantiate trap with preserved scale
                GameObject trapInstance = Instantiate(_trapPrefab, spawnPosition, _trapPrefab.transform.rotation);
                trapInstance.transform.SetParent(targetTile.transform, true);

                // Initialize the trap with current damage value and power-up state
                var trapBehaviour = trapInstance.GetComponent<TrapBehaviour>();
                if (trapBehaviour != null)
                {
                    trapBehaviour.Initialize(_currentDamage, _currentPowerUpState);
                    IncrementTrapCount();
                    
                    // If we're in charged or supercharged state, add leaf on top of trap and a decoy
                    if (_decoyAmount > 0 && _leafPrefab != null)
                    {
                        // Add leaf on top of trap
                        GameObject leafOnTrap = Instantiate(_leafPrefab, spawnPosition, _leafPrefab.transform.rotation);
                        leafOnTrap.transform.SetParent(trapInstance.transform, true);
                        // Adjust the local position after parenting
                        leafOnTrap.transform.localPosition = new Vector3(leafOnTrap.transform.localPosition.x, -1f, leafOnTrap.transform.localPosition.z);
                        
                        // First try to find adjacent empty tiles
                        var allTiles = _tileSelection.GetSelectableTiles();
                        var adjacentTiles = allTiles.Where(t => 
                            t != targetTile && 
                            t.occupantType == TileSettings.OccupantType.None &&
                            Mathf.Abs(t.gridX - targetTile.gridX) + Mathf.Abs(t.gridY - targetTile.gridY) == 1
                        ).ToList();

                        // If no adjacent tiles available, look for tiles two steps away
                        if (adjacentTiles.Count == 0)
                        {
                            adjacentTiles = allTiles.Where(t =>
                                t != targetTile &&
                                t.occupantType == TileSettings.OccupantType.None &&
                                Mathf.Abs(t.gridX - targetTile.gridX) + Mathf.Abs(t.gridY - targetTile.gridY) == 2
                            ).ToList();
                        }                        // If we found any valid tiles, place the decoy                        // Filter out any item tiles from adjacent tiles
                        adjacentTiles.RemoveAll(tile => tile.occupantType == TileSettings.OccupantType.Item);
                        
                        if (adjacentTiles.Count > 0)
                        {TileSettings decoyTile = adjacentTiles[Random.Range(0, adjacentTiles.Count)];
                            Vector3 decoyPosition = decoyTile.transform.position + new Vector3(0, -1f, 0);
                            GameObject decoyLeaf = Instantiate(_leafPrefab, decoyPosition, _leafPrefab.transform.rotation);
                            decoyLeaf.transform.SetParent(decoyTile.transform, true);
                            decoyTile.SetOccupant(TileSettings.OccupantType.Decoy, decoyLeaf);
                        }
                    }
                }
                else
                {
                    Destroy(trapInstance);
                    return;
                }
                
                targetTile.SetOccupant(TileSettings.OccupantType.Trap, trapInstance);

                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.TrapThrow);
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
                 bestTile = selectableTiles[0];
             }

             return bestTile;
        }

        // Helper for distance calculation (optional refinement)
        private int CalculateManhattanDistance(Vector2Int posA, Vector2Int posB)
        {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        }

        // Method to increment trap count when a trap is placed
        public static void IncrementTrapCount()
        {
            _activeTrapCount++;
            UpdateTrapLimitIndicator();
        }

        // Method to decrement trap count when a trap is destroyed
        public static void DecrementTrapCount()
        {
            _activeTrapCount = Mathf.Max(0, _activeTrapCount - 1);
            UpdateTrapLimitIndicator();
        }

        // Method to update the trap limit indicator
        private static void UpdateTrapLimitIndicator()
        {
            TrapPowerUp instance = FindObjectOfType<TrapPowerUp>();
            if (instance != null && instance._trapLimitReachedIndicator != null)
            {
                instance._trapLimitReachedIndicator.SetActive(_activeTrapCount >= MAX_TRAPS);
            }
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
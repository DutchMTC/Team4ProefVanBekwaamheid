using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class WallPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        [SerializeField] private GameObject _wallPrefab; // The prefab to place as a wall
        [SerializeField] private Vector3 _positionOffset = Vector3.zero; // Offset for the wall's spawn position
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // Reference to the TileOccupants script (of the player)
        private bool _isWaitingForSelection = false;

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
            if (_wallPrefab == null)
            {
                Debug.LogError("Wall Prefab is not assigned in the WallPowerUp script!");
            }
        }

        void Update()
        {
            // Debug: Press J to activate the power-up (Usable state)
            if (Input.GetKeyDown(KeyCode.J))
            {
                Debug.Log("J key pressed - Activating Wall PowerUp (Usable)");
                WallPowerUpSelected(PowerUpState.Usable);
            }
        }

        public void WallPowerUpSelected(PowerUpState _state)
        {
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

            _isWaitingForSelection = true;
            _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            Vector2Int currentPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement , TileSelection.UserType.Player);
        }

        private void HandleTileSelected(TileSettings selectedTile)
        {
            if (!_isWaitingForSelection) return;

            _isWaitingForSelection = false;
            _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            PlaceWall(selectedTile);
        }

        private void PlaceWall(TileSettings targetTile)
        {
            if (targetTile != null && targetTile.occupantType == TileSettings.OccupantType.None && _wallPrefab != null)
            {
                Vector2Int playerPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
                Vector2Int targetPos = new Vector2Int(targetTile.row, targetTile.column);
                Vector2Int direction = targetPos - playerPos;

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
                else
                {
                    Debug.LogWarning("Could not calculate direction, using default rotation.");
                }

                Vector3 spawnPosition = targetTile.transform.position + _positionOffset;
                Quaternion spawnRotation = Quaternion.Euler(0, angle, 0);

                GameObject wallInstance = Instantiate(_wallPrefab, spawnPosition, spawnRotation);

                targetTile.occupantType = TileSettings.OccupantType.Obstacle; 

                Debug.Log($"Placed wall at {targetPos} with rotation {angle} degrees.");
            }
            else
            {
                if (targetTile == null)
                {
                    Debug.Log("Invalid target tile.");
                }
                else if (_wallPrefab == null)
                {
                     Debug.LogError("Wall Prefab is not assigned!");
                }
                else
                {
                    Debug.Log("Selected tile is occupied or invalid, cannot place wall here.");
                }
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
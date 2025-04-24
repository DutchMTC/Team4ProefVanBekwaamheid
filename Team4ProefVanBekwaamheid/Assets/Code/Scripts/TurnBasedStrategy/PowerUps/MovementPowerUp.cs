using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class MovementPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // Reference to the TileOccupants script
        private bool _isWaitingForSelection = false;        
        
        void Start()
        {
            _tileSelection = FindObjectOfType<TileSelection>();
            _tileOccupants = GetComponent<TileOccupants>();
            
            if (_tileSelection == null)
            {
                Debug.LogError("TileSelection script not found!");
            }
        }      

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) // 'M' for Movement
            {
                MovementPowerUpSelected(PowerUpState.Usable); // Example usage, replace with actual state
            }
        } 
        public void MovementPowerUpSelected(PowerUpState _state)
        {
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
            }            // Start new selection
            _isWaitingForSelection = true;
            _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            Vector2Int currentPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement, TileSelection.UserType.Player);
        }          
        private void HandleTileSelected(TileSettings selectedTile)
        {
            if (!_isWaitingForSelection) return;

            _isWaitingForSelection = false;
            _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            Move(selectedTile);
        }

        private void Move(TileSettings targetTile)
        {
            if (targetTile != null && targetTile.occupantType == TileSettings.OccupantType.None)
            {
                _tileOccupants.row = targetTile.row;
                _tileOccupants.column = targetTile.column;
                _tileOccupants.MoveToTile();
            }
            else
            {
                Debug.Log("Selected tile is occupied or invalid, cannot move here.");
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

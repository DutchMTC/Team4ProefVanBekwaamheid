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
        private TileOccupants _tileOccupants;
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
            if (Input.GetKeyDown(KeyCode.A))
            {
                AttackPowerUpSelected(PowerUpState.Usable);
            }
        }

        public void AttackPowerUpSelected(PowerUpState _state)
        {
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

            // Start new selection
            _isWaitingForSelection = true;
            _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            Vector2Int currentPos = new Vector2Int(_tileOccupants.row, _tileOccupants.column);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Attack, TileSelection.UserType.Player);
        }

        private void HandleTileSelected(TileSettings selectedTile)
        {
            if (!_isWaitingForSelection) return;

            _isWaitingForSelection = false;
            _tileSelection.OnTileSelected.RemoveListener(HandleTileSelected);
            Attack(selectedTile);
        }

        private void Attack(TileSettings targetTile)
        {
            if (targetTile != null && targetTile.occupantType == TileSettings.OccupantType.Enemy)
            {
                // Get the enemy's health component and apply damage
                var enemyHealth = targetTile.tileOccupant.GetComponent<TileOccupants>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(_currentDamage);
                    Debug.Log($"Attacked enemy for {_currentDamage} damage!");
                }
                else
                {
                    Debug.LogWarning("Enemy tile found but no Health component present!");
                }
            }
            else
            {
                Debug.Log("Selected tile has no enemy to attack!");
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

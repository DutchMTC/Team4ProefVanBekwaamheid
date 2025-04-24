using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class AttackPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range = 1; // Attack range
        [SerializeField] private int _damage = 10; // Attack damage
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
            if (Input.GetKeyDown(KeyCode.A)) // 'A' for Attack
            {
                AttackPowerUpSelected();
            }
        }

        private void AttackPowerUpSelected()
        {
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
            _tileSelection.StartTileSelection(_range, currentPos);
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
                // Here you would implement the actual attack logic
                Debug.Log($"Attacking enemy at position ({targetTile.row}, {targetTile.column}) for {_damage} damage!");
                
                // Example of how you might damage an enemy
                // var enemy = targetTile.GetComponent<EnemyHealth>();
                // if (enemy != null)
                // {
                //     enemy.TakeDamage(_damage);
                // }
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

using UnityEngine;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class TrapBehavior : MonoBehaviour
    {
        private TileSettings _parentTile;
        private int _trapDamage = 10;

        void Start()
        {
            _parentTile = GetComponentInParent<TileSettings>();
            if (_parentTile == null)
            {
                Debug.LogError("TrapBehavior: No parent TileSettings found!");
            }
        }

        public void OnCharacterEnterTile(TileOccupants character)
        {
            if (character == null)
            {
                Debug.LogError("TrapBehavior: Null character entered trap tile!");
                return;
            }

            string characterType = character.myOccupantType == TileSettings.OccupantType.Player ? "Player" : "Enemy";
            Debug.Log($"{characterType} is standing on trap at position ({_parentTile.gridY}, {_parentTile.gridX})");

            // Apply trap effects (damage, etc.)
            character.TakeDamage(_trapDamage);

            // Clean up the trap
            _parentTile.SetOccupant(TileSettings.OccupantType.None, null);
            Destroy(gameObject);
        }
    }
}
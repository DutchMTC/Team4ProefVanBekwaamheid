using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using static PowerUpManager;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class DefensePowerUp : MonoBehaviour
    {
        [SerializeField] private float _baseReduction = 0.3f; // 30% damage reduction base
        private float _currentReduction;
        private TileOccupants _tileOccupants;
        private bool _isActive = false;
        private TileSelection.UserType _currentUser;

        void Start()
        {
            _tileOccupants = GetComponent<TileOccupants>();
        }

        public void DefensePowerUpSelected(PowerUpState state, TileSelection.UserType userType)
        {
            _currentUser = userType;
            _isActive = true;

            // Set damage reduction based on power up state
            switch (state)
            {
                case PowerUpState.Usable:
                    _currentReduction = _baseReduction; // 30% reduction
                    break;
                case PowerUpState.Charged:
                    _currentReduction = _baseReduction * 1.5f; // 45% reduction
                    break;
                case PowerUpState.Supercharged:
                    _currentReduction = _baseReduction * 2f; // 60% reduction
                    break;
            }

            // Apply the defense buff
            if (_tileOccupants != null)
            {
                _tileOccupants.SetDamageReduction(_currentReduction);           
            }
        }

        void OnDestroy()
        {
            // Remove defense buff when destroyed
            if (_tileOccupants != null && _isActive)
            {
                _tileOccupants.SetDamageReduction(0);
            }
        }
    }
}

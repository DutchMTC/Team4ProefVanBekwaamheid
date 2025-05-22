using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;
using System.Collections.Generic; // Added for List<T>
// Potentially add: using UnityEngine.Events; if you want to use UnityEvents for damage.

public class TileOccupants : MonoBehaviour
{
    private GameManager _gameManager; // Added for game state management

    [Header("Grid & Occupant Info")]
    [SerializeField] private GridGenerator _gridGenerator;
    public TileSettings.OccupantType myOccupantType;
    public int gridY; // Renamed from row
    public int gridX; // Renamed from column
    private GameObject _selectedTile;
    private TileSettings _tileSettings;

    [Header("Health & Defense")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int health = 30; // Current health
    private float _damageReduction = 0f;
    private bool hasArmor = false; // Added for armor mechanic

    [Header("Armor Visuals (Player Only)")]
    [SerializeField] private List<GameObject> armorPlayerVisualsToEnable;
    [SerializeField] private List<GameObject> armorPlayerVisualsToDisable;

    [Header("Damage Delays")]
    [SerializeField] private float usableAttackDamageDelay = 0f;
    [SerializeField] private float chargedAttackDamageDelay = 0f;
    [SerializeField] private float superchargedAttackDamageDelay = 0f;
    [SerializeField] private float trapDamageDelay = 0f; // Added for trap damage delay
 
    [Header("UI")]
    [SerializeField] private CharacterHealthUI healthBarUI;
    // public UnityAction<float> OnHealthChanged; // Alternative: Use UnityEvent
    private CharacterAnimationController _animationController; // Reference to the main character animation controller
    private EnemyAIController _enemyAIController; // Used for Enemy animations
 
    void Awake()
    {
        // Ensure we have a reference to the GridGenerator as early as possible
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
        }
        health = maxHealth; // Initialize current health to max health

        _gameManager = FindObjectOfType<GameManager>();
        _animationController = FindObjectOfType<CharacterAnimationController>(); // Get the global animation controller

        if (myOccupantType == TileSettings.OccupantType.Enemy)
        {
            _enemyAIController = GetComponent<EnemyAIController>();
        }
    }
 
    void Start()
    {
        // Double check to make sure we have a GridGenerator reference
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                return;
            }
        }

        // Initialize Health Bar UI
        if (healthBarUI != null)
        {
            // Determine if this is a player character. Adjust this logic if needed.
            bool isPlayer = (myOccupantType == TileSettings.OccupantType.Player);
            healthBarUI.Initialize(this, maxHealth, health, isPlayer);
        }

        // Force position update with small delay to ensure GridGenerator is fully initialized
        Invoke(nameof(InitializePosition), 0.1f);
    }

    void InitializePosition()
    {
        FindTileAtCoordinates();
        MoveToTile();
    }

    public void SetDamageReduction(float reduction)
    {
        _damageReduction = Mathf.Clamp(reduction, 0f, 0.8f);
    }

    public void TakeDamage(int amount)
    {
        StartCoroutine(ApplyDamageAfterDelay(amount));
    }

    private System.Collections.IEnumerator ApplyDamageAfterDelay(int amount)
    {
        // 1. ARMOR CHECK
        if (hasArmor)
        {
            hasArmor = false;
            if (healthBarUI != null)
            {
                healthBarUI.UpdateArmorStatus(false);
            }
            // If Player, toggle armor visuals off
            if (myOccupantType == TileSettings.OccupantType.Player)
            {
                ToggleArmorVisuals(false);
            }
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.ArmorBreak);
            }
            yield break; // No health damage, no animation, no delay.
        }

        // 2. CALCULATE REDUCED DAMAGE
        int reducedDamage = Mathf.RoundToInt(amount * (1f - _damageReduction));
 
        // 3. IF ACTUAL DAMAGE WILL BE DEALT
        if (reducedDamage > 0)
        {
            // 3a. CALCULATE DELAY
            float delay = 0f;
            if (amount >= 25) { delay = superchargedAttackDamageDelay; }
            else if (amount >= 15) { delay = chargedAttackDamageDelay; }
            else if (amount >= 10) { delay = usableAttackDamageDelay; }
            
            // 3b. APPLY DELAY
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            // 3c. Ensure object still exists and is active after delay
            if (this == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            // 3d. APPLY HEALTH CHANGE
            health -= reducedDamage;
            health = Mathf.Clamp(health, 0, maxHealth);
 
            // 3e. UPDATE UI
            if (healthBarUI != null)
            {
                healthBarUI.OnHealthChanged(health);
            }
 
            // 3f. PLAY ANIMATION (if still alive and damage was dealt)
            if (health > 0) // Check health *after* damage is applied
            {
                if (myOccupantType == TileSettings.OccupantType.Enemy && _enemyAIController != null)
                {
                    _enemyAIController.PlayDamageAnimation();
                }
                else if (myOccupantType == TileSettings.OccupantType.Player && _animationController != null)
                {
                    _animationController.PlayerDamage();
                }
            }
 
            // 3g. CHECK FOR DEATH
            if (health <= 0)
            {
                // Death animation is handled in Die(), so no specific animation call here unless needed before Die()
                Die();
            }
        }
    }

    public void ReceiveArmor()
    {
        hasArmor = true;
        // Optionally, notify UI to show armor icon here
        if (healthBarUI != null)
        {
            healthBarUI.UpdateArmorStatus(true);
        }
        // If Player, toggle armor visuals on
        if (myOccupantType == TileSettings.OccupantType.Player)
        {
            ToggleArmorVisuals(true);
        }
    }

    private void ToggleArmorVisuals(bool armorActive)
    {
        if (armorPlayerVisualsToEnable != null)
        {
            foreach (GameObject go in armorPlayerVisualsToEnable)
            {
                if (go != null) go.SetActive(armorActive);
            }
        }

        if (armorPlayerVisualsToDisable != null)
        {
            foreach (GameObject go in armorPlayerVisualsToDisable)
            {
                if (go != null) go.SetActive(!armorActive);
            }
        }
    }

    // Helper method for debugging armor status
    public bool GetHasArmorStatus()
    {
        return hasArmor;
    }

    private void Die()
    {
        float destructionDelay = 0f;
        if (myOccupantType == TileSettings.OccupantType.Player)
        {
            if (_animationController != null)
            {
                _animationController.PlayerDeath();
                destructionDelay = 2f;
            }
            if (_gameManager != null)
            {
                _gameManager.UpdateGameState(GameState.GameOver);
            }
        }
        else if (myOccupantType == TileSettings.OccupantType.Enemy)
        {
            if (_enemyAIController != null)
            {
                _enemyAIController.PlayDeathAnimation();
                destructionDelay = 2f; // Assuming enemy death animation also takes time
            }
            if (_gameManager != null)
            {
                // This assumes any enemy death leads to a win.
                // If multiple enemies exist, GameManager would need to check if all are defeated.
                _gameManager.UpdateGameState(GameState.Win);
            }
        }
        // Optional: Notify healthBarUI or other systems about death
        // if (healthBarUI != null) healthBarUI.HandleDeath();
        Destroy(gameObject, destructionDelay); // Delay destruction if animation is playing
    }
 
    // Public method to get current health if needed by other systems
    public int GetCurrentHealth()
    {
        return health;
    }

    // Public method to get max health if needed
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    // Example method to heal the character
    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (healthBarUI != null)
        {
            healthBarUI.OnHealthChanged(health);
        }
    }

    void Update()
    {
        // Check if the occupant has moved or if the tile reference is lost
        if (_selectedTile == null || _tileSettings == null || _tileSettings.gridY != gridY || _tileSettings.gridX != gridX)
        {
            MoveToTile();
        }
    }

    public void MoveToTile()
    {
        FindTileAtCoordinates();

        if (_selectedTile != null && _tileSettings != null)
        {
            GameObject itemObjectToPickup = null;
            PickupItem pickupItemScript = null;

            // Store trap information before moving
            bool hasTrap = _tileSettings.occupantType == TileSettings.OccupantType.Trap;
            GameObject trapObject = hasTrap ? _tileSettings.tileOccupant : null;

            // Check if the target tile currently holds an item
            if (_tileSettings.occupantType == TileSettings.OccupantType.Item && _tileSettings.tileOccupant != null)
            {
                pickupItemScript = _tileSettings.tileOccupant.GetComponent<PickupItem>();
                if (pickupItemScript != null)
                {
                    itemObjectToPickup = _tileSettings.tileOccupant;
                }
            }

            // Check for decoy and trigger fade-out
            if (_tileSettings.occupantType == TileSettings.OccupantType.Decoy && _tileSettings.tileOccupant != null)
            {
                LeafBehaviour leafBehaviour = _tileSettings.tileOccupant.GetComponent<LeafBehaviour>();
                if (leafBehaviour != null)
                {
                    leafBehaviour.StartFadeOut(1f);
                    _tileSettings.SetOccupant(TileSettings.OccupantType.None, null);
                }
            }

            // Validate if the unit can move to the target tile            
            if (_tileSettings.occupantType != TileSettings.OccupantType.None &&
            _tileSettings.occupantType != TileSettings.OccupantType.Item &&
            _tileSettings.occupantType != TileSettings.OccupantType.Trap &&
            _tileSettings.occupantType != TileSettings.OccupantType.Decoy &&
            _tileSettings.occupantType != myOccupantType)
            {
                return;
            }

            // Move to the new position
            Vector3 selectedTilePos = _selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.Dash);
            }
            
            // Handle trap if present
            if (hasTrap && trapObject != null)
            {
                var trapBehaviour = trapObject.GetComponent<TrapBehaviour>();
                if (trapBehaviour != null)
                {
                    TileSettings tileThatHadTrap = _tileSettings;
                    // Immediately mark the tile as not having an active trap to prevent re-triggering.
                    // The TrapBehaviour itself will handle the trap object's lifecycle.
                    tileThatHadTrap.SetOccupant(TileSettings.OccupantType.None, null);

                    StartCoroutine(HandleTrapDamage(trapBehaviour, tileThatHadTrap));
                    StartCoroutine(PlayStuckAnimationAfterDelay(0.25f)); // Moved to a new coroutine with delay
                }
                else // Trap object exists, but no TrapBehaviour script. Occupy as normal.
                {
                    _tileSettings.SetOccupant(myOccupantType, this.gameObject);
                }
            }
            else // No trap on the tile. Occupy as normal.
            {
                _tileSettings.SetOccupant(myOccupantType, this.gameObject);
            }

            // Handle item pickup after movement
            if (itemObjectToPickup != null && pickupItemScript != null)
            {
                pickupItemScript.ActivatePickup(gameObject);
            }
        }
    }

    private void FindTileAtCoordinates()
    {
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                return;
            }
        }

        if (_tileSettings != null) // If this occupant was previously on a tile
        {
            // Only clear the occupant if this specific game object was the occupant
            if (_tileSettings.tileOccupant == this.gameObject)
            {
                _tileSettings.SetOccupant(TileSettings.OccupantType.None, null);
            }
        }

        _selectedTile = null; // Reset before searching
        _tileSettings = null; // Reset before searching

        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.gridY == gridY && currentTile.gridX == gridX)
            {
                _selectedTile = child.gameObject;
                _tileSettings = currentTile;
                // Do not set occupant here. MoveToTile will handle it after validation.
                return;
            }
        }
    }

    public TileSettings GetCurrentTile()
    {
        return _tileSettings;
    }

    private System.Collections.IEnumerator HandleTrapDamage(TrapBehaviour trapBehaviour, TileSettings tileWhereTrapWas)
    {
        if (trapDamageDelay > 0)
        {
            yield return new WaitForSeconds(trapDamageDelay);
        }

        // Ensure objects still exist and are active after delay
        if (this == null || !gameObject.activeInHierarchy || trapBehaviour == null)
        {
            yield break;
        }

        trapBehaviour.OnCharacterEnterTile(this); // Trap effect occurs here

        // After trap effect, if the character is still on this tile and it's clear, occupy it.
        if (this != null && gameObject.activeInHierarchy && _tileSettings == tileWhereTrapWas)
        {
            // We set tileWhereTrapWas to None before starting the coroutine.
            // If it's still None, and the player hasn't moved, they occupy it.
            if (tileWhereTrapWas.occupantType == TileSettings.OccupantType.None)
            {
                tileWhereTrapWas.SetOccupant(myOccupantType, this.gameObject);
            }
        }
    }

    private System.Collections.IEnumerator PlayStuckAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.Stuck);
        }

        if (this != null && gameObject.activeInHierarchy && _animationController != null)
        {
            if (myOccupantType == TileSettings.OccupantType.Player)
            {
                _animationController.PlayerStuck();
            }
            else if (myOccupantType == TileSettings.OccupantType.Enemy)
            {
                // Assuming Enemy stuck animation is also handled by the global _animationController
                // If enemies have their own animation controllers that handle 'Stuck',
                // you might need to adjust this part, e.g., using _enemyAIController.PlayStuckAnimation()
                // For now, using the global controller as per existing PlayerStuck pattern.
                _animationController.EnemyStuck();
            }
        }
    }
}

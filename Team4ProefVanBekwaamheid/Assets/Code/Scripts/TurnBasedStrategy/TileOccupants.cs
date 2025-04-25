using UnityEngine;

public class TileOccupants : MonoBehaviour
{
    [SerializeField] private GridGenerator _gridGenerator;
    public TileSettings.OccupantType myOccupantType;
    public int row;
    public int column;
    private GameObject _selectedTile;
    private TileSettings _tileSettings;
    private float _damageReduction = 0f;
   
    [SerializeField] private int health = 30;       
    
    void Awake()
    {
        // Ensure we have a reference to the GridGenerator as early as possible
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in Awake!");
            }
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
                Debug.LogError("GridGenerator reference not found in Start!");
                return;
            }
        }
        
        // Force position update with small delay to ensure GridGenerator is fully initialized
        Invoke("InitializePosition", 0.1f);
    }
    
    void InitializePosition()
    {
        FindTileAtCoordinates();
        MoveToTile();
        
        // Log position for debugging
        Debug.Log($"{gameObject.name} initialized at position ({row}, {column})");
    }

    public void SetDamageReduction(float reduction)
    {
        _damageReduction = Mathf.Clamp(reduction, 0f, 0.8f);
        Debug.Log($"{gameObject.name} defense set to {_damageReduction * 100}%");
    }

    public void TakeDamage(int amount)
    {
        int reducedDamage = Mathf.RoundToInt(amount * (1f - _damageReduction));
        int previousHealth = health;
        health -= reducedDamage;
        
        string defenseMsg = _damageReduction > 0 ? $"[DEFENSE {_damageReduction * 100}%]" : "[NO DEFENSE]";
        Debug.Log($"{defenseMsg} {gameObject.name} Health: {previousHealth} -> {health} " +
                 $"(Took {reducedDamage} damage, reduced from {amount})");
        
        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} has died from {reducedDamage} damage!");
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject);
    }

    void Update()
    {
        if (_selectedTile == null || (_tileSettings != null && (_tileSettings.row != row || _tileSettings.column != column)))
        {      
            MoveToTile();
        }
    }

    public void MoveToTile()
    {
        FindTileAtCoordinates();

        if (_selectedTile != null && _tileSettings != null)
        {
            if (_tileSettings.occupantType != TileSettings.OccupantType.None && 
                _tileSettings.occupantType != myOccupantType)
            {
                Debug.LogWarning($"Cannot move to tile at ({row}, {column}) - tile is occupied by {_tileSettings.occupantType}");
                return;
            }            Vector3 selectedTilePos = _selectedTile.transform.position;
            transform.position = new Vector3(selectedTilePos.x, transform.position.y, selectedTilePos.z);
            
            _tileSettings.tileOccupant = this.gameObject;  // Set the reference to this occupant
            _tileSettings.occupantType = myOccupantType;
            _tileSettings.OccupationChangedEvent.Invoke();
        }
        else
        {
            Debug.LogWarning($"Cannot move to tile at ({row}, {column}) - tile not found");
        }
    }

    private void FindTileAtCoordinates()
    {
        if (_gridGenerator == null)
        {
            _gridGenerator = FindObjectOfType<GridGenerator>();
            if (_gridGenerator == null)
            {
                Debug.LogError("GridGenerator reference not found in FindTileAtCoordinates!");
                return;
            }
        }

        if (_tileSettings != null)
        {
            _tileSettings.occupantType = TileSettings.OccupantType.None;
            _tileSettings.OccupationChangedEvent.Invoke();
        }

        foreach (Transform child in _gridGenerator.transform)
        {
            TileSettings currentTile = child.GetComponent<TileSettings>();
            if (currentTile != null && currentTile.row == row && currentTile.column == column)
            {
                _selectedTile = child.gameObject;
                _tileSettings = currentTile;
                _tileSettings.occupantType = myOccupantType;
                return;
            }
        }

        _selectedTile = null;
        _tileSettings = null;
        Debug.LogWarning($"No tile found at grid position ({row}, {column})");
    }

    public TileSettings GetCurrentTile()
    {
        return _tileSettings;
    }
}

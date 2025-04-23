using UnityEngine;

public class Block : MonoBehaviour
{
    [System.Serializable]
    public class BlockTypeData
    {
        public BlockType type;
        [Range(1, 100)] // 1 is very common, 100 is extremely rare
        public int rarity = 1;
        [Tooltip("Higher rarity means less frequent spawning")]
        public Sprite sprite;
        [Tooltip("If true, this block type can match with any other blocks")]
        public bool isJoker;
        [Tooltip("What type of power-up this joker provides")]
        public PowerUpInventory.PowerUpType powerUpType;
    }

    public enum BlockType
    {
        Blue,
        Red,
        Green,
        Yellow,
        Purple,
        JokerSword,
        JokerShield,
        JokerSteps,
        JokerHealth
    }

    public BlockTypeData[] blockTypes;
    public BlockType type;
    public int column;
    public int row;
    private Vector2 _targetPosition;
    private float _moveSpeed = 10f;
    private SpriteRenderer _spriteRenderer;
    public bool IsFalling { get; set; } = false; // Flag to indicate if GridManager is controlling fall animation

    // Helper property to check if current block type is a joker
    public bool IsJoker
    {
        get 
        {
            foreach (var blockType in blockTypes)
            {
                if (blockType.type == type)
                {
                    return blockType.isJoker;
                }
            }
            return false;
        }
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void Initialize(BlockType type, int column, int row)
    {
        this.type = type;
        this.column = column;
        this.row = row;
        UpdatePosition();
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        foreach (var blockType in blockTypes)
        {
            if (blockType.type == type)
            {
                _spriteRenderer.sprite = blockType.sprite;
                break;
            }
        }
    }

    /// <summary>
    /// Gets the rarity value of the current block type.
    /// Higher values (1-100) mean the block is more rare.
    /// </summary>
    /// <returns>
    /// A value between 1 and 100 representing block rarity,
    /// where 1 is very common and 100 is extremely rare.
    /// </returns>
    public int GetRarity()
    {
        foreach (var blockType in blockTypes)
        {
            if (blockType.type == type)
            {
                return blockType.rarity;
            }
        }
        return 1; // Default rarity if not found
    }

    public PowerUpInventory.PowerUpType GetPowerUpType()
    {
        foreach (var blockType in blockTypes)
        {
            if (blockType.type == type)
            {
                return blockType.powerUpType;
            }
        }
        return PowerUpInventory.PowerUpType.Sword; // Default fallback
    }

    public void UpdatePosition()
    {
        _targetPosition = new Vector2(column, row);
        transform.position = new Vector3(column, row, 0);
    }

    public void SetTargetPosition(Vector2 position)
    {
        _targetPosition = position;
    }

    /// <summary>
    /// Syncs the internal target position with the current transform position.
    /// Used after GridManager finishes controlling movement (e.g., falling).
    /// </summary>
    public void SyncTargetPosition()
    {
        _targetPosition = transform.position;
    }

    private void Update()
    {
        // Only allow Block's own movement logic (for swapping) if it's not currently falling
        if (!IsFalling && (Vector2)transform.position != _targetPosition)
        {
            transform.position = Vector2.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
        }
    }
}
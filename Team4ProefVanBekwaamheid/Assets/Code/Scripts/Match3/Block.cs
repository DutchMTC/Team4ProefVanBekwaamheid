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
    }

    public enum BlockType
    {
        Blue,
        Red,
        Green,
        Yellow,
        Purple
    }

    public BlockTypeData[] blockTypes;
    public BlockType type;
    public int column;
    public int row;
    private Vector2 targetPosition;
    private float moveSpeed = 10f;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
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
                spriteRenderer.sprite = blockType.sprite;
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

    public void UpdatePosition()
    {
        targetPosition = new Vector2(column, row);
        transform.position = new Vector3(column, row, 0);
    }

    public void SetTargetPosition(Vector2 position)
    {
        targetPosition = position;
    }

    private void Update()
    {
        if ((Vector2)transform.position != targetPosition)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }
}
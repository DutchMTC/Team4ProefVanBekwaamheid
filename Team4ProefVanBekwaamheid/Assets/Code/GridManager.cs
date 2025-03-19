using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public GameObject blockPrefab;
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float swapSpeed = 0.3f;

    private Block[,] blocks;
    private bool isSwapping = false;
    private Vector2 touchStart;
    private Block selectedBlock;

    private void Start()
    {
        blocks = new Block[gridWidth, gridHeight];
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CreateBlock(x, y);
            }
        }
    }

    private bool WouldCauseMatch(int x, int y, Block.BlockType type)
    {
        // Check horizontal matches (need at least 2 blocks of same type to left or right)
        if (x >= 2 &&
            blocks[x - 1, y]?.type == type &&
            blocks[x - 2, y]?.type == type)
        {
            return true;
        }
        if (x >= 1 && x < gridWidth - 1 &&
            blocks[x - 1, y]?.type == type &&
            blocks[x + 1, y]?.type == type)
        {
            return true;
        }
        if (x < gridWidth - 2 &&
            blocks[x + 1, y]?.type == type &&
            blocks[x + 2, y]?.type == type)
        {
            return true;
        }

        // Check vertical matches (need at least 2 blocks of same type above or below)
        if (y >= 2 &&
            blocks[x, y - 1]?.type == type &&
            blocks[x, y - 2]?.type == type)
        {
            return true;
        }
        if (y >= 1 && y < gridHeight - 1 &&
            blocks[x, y - 1]?.type == type &&
            blocks[x, y + 1]?.type == type)
        {
            return true;
        }
        if (y < gridHeight - 2 &&
            blocks[x, y + 1]?.type == type &&
            blocks[x, y + 2]?.type == type)
        {
            return true;
        }

        return false;
    }

    private Block.BlockType GetRandomBlockType(int x, int y)
    {
        Block blockComponent = blockPrefab.GetComponent<Block>();
        List<Block.BlockType> validTypes = new List<Block.BlockType>();
        Dictionary<Block.BlockType, int> typeWeights = new Dictionary<Block.BlockType, int>();
        int totalWeight = 0;

        // Calculate weights for each valid block type
        foreach (var blockType in blockComponent.blockTypes)
        {
            if (!WouldCauseMatch(x, y, blockType.type))
            {
                validTypes.Add(blockType.type);
                int validRarity = Mathf.Clamp(blockType.rarity, 1, 100);
                int weight = 101 - validRarity;
                typeWeights[blockType.type] = weight;
                totalWeight += weight;
            }
        }

        // If no valid types (shouldn't happen with 5 colors), return any type
        if (validTypes.Count == 0)
        {
            return Block.BlockType.Blue;
        }

        // Select random type from valid ones based on weight
        int randomWeight = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var type in validTypes)
        {
            currentWeight += typeWeights[type];
            if (randomWeight < currentWeight)
            {
                return type;
            }
        }

        return validTypes[0]; // Fallback to first valid type
    }

    private void CreateBlock(int x, int y)
    {
        GameObject blockObject = Instantiate(blockPrefab, new Vector3(x, y, 0), Quaternion.identity);
        blockObject.transform.parent = transform;

        Block block = blockObject.GetComponent<Block>();
        Block.BlockType randomType = GetRandomBlockType(x, y);
        block.Initialize(randomType, x, y);
        blocks[x, y] = block;
    }

    private void Update()
    {
        if (isSwapping) return;

        // Handle mouse input
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            selectedBlock = GetBlockAtPosition(touchStart);
        }
        else if (Input.GetMouseButtonUp(0) && selectedBlock != null)
        {
            Vector2 touchEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = touchEnd - touchStart;

            if (direction.magnitude > 0.5f)
            {
                // Determine direction of swipe
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Vector2Int swapDirection = angle switch
                {
                    > -45 and <= 45 => new Vector2Int(1, 0),   // Right
                    > 45 and <= 135 => new Vector2Int(0, 1),   // Up
                    > 135 or <= -135 => new Vector2Int(-1, 0), // Left
                    _ => new Vector2Int(0, -1)                 // Down
                };

                TrySwapBlocks(selectedBlock, swapDirection);
            }
            selectedBlock = null;
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                touchStart = Camera.main.ScreenToWorldPoint(touch.position);
                selectedBlock = GetBlockAtPosition(touchStart);
            }
            else if (touch.phase == TouchPhase.Ended && selectedBlock != null)
            {
                Vector2 touchEnd = Camera.main.ScreenToWorldPoint(touch.position);
                Vector2 direction = touchEnd - touchStart;

                if (direction.magnitude > 0.5f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    Vector2Int swapDirection = angle switch
                    {
                        > -45 and <= 45 => new Vector2Int(1, 0),   // Right
                        > 45 and <= 135 => new Vector2Int(0, 1),   // Up
                        > 135 or <= -135 => new Vector2Int(-1, 0), // Left
                        _ => new Vector2Int(0, -1)                 // Down
                    };

                    TrySwapBlocks(selectedBlock, swapDirection);
                }
                selectedBlock = null;
            }
        }
    }

    private Block GetBlockAtPosition(Vector2 position)
    {
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return blocks[x, y];
        }
        return null;
    }

    private void TrySwapBlocks(Block block, Vector2Int direction)
    {
        int newX = block.column + direction.x;
        int newY = block.row + direction.y;

        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            Block otherBlock = blocks[newX, newY];
            SwapBlocks(block, otherBlock);
        }
    }

    private void SwapBlocks(Block block1, Block block2)
    {
        isSwapping = true;

        // Swap array positions
        blocks[block1.column, block1.row] = block2;
        blocks[block2.column, block2.row] = block1;

        // Swap grid positions
        int tempColumn = block1.column;
        int tempRow = block1.row;
        block1.column = block2.column;
        block1.row = block2.row;
        block2.column = tempColumn;
        block2.row = tempRow;

        // Animate movement
        block1.SetTargetPosition(new Vector2(block1.column, block1.row));
        block2.SetTargetPosition(new Vector2(block2.column, block2.row));

        // Check for matches after swapping
        Invoke(nameof(CheckMatches), swapSpeed);
    }

    private void CheckMatches()
    {
        isSwapping = false;
        List<Block> matchingBlocks = FindMatches();
        if (matchingBlocks.Count >= 3)
        {
            // Remove matched blocks and log their destruction
            foreach (Block block in matchingBlocks)
            {
                Vector2Int pos = new Vector2Int(block.column, block.row);
                Debug.Log($"Destroyed {block.type} block at position ({pos.x}, {pos.y}) with rarity {block.GetRarity()}");
                blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }

            // Apply gravity and fill empty spaces
            StartFalling();
        }
    }

    private void StartFalling()
    {
        bool needsMoreFalling;
        do
        {
            needsMoreFalling = false;

            // Move blocks down if there's empty space below
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    if (blocks[x, y] == null)
                    {
                        // Look for the next non-null block above
                        for (int above = y + 1; above < gridHeight; above++)
                        {
                            if (blocks[x, above] != null)
                            {
                                // Move block down
                                Block block = blocks[x, above];
                                blocks[x, y] = block;
                                blocks[x, above] = null;
                                block.row = y;
                                block.SetTargetPosition(new Vector2(block.column, block.row));
                                needsMoreFalling = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Fill empty spaces at the top with new blocks
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (blocks[x, y] == null)
                    {
                        CreateBlock(x, y);
                        blocks[x, y].transform.position = new Vector3(x, gridHeight, 0);
                        blocks[x, y].SetTargetPosition(new Vector2(x, y));
                        needsMoreFalling = true;
                    }
                }
            }

        } while (needsMoreFalling);

        // Check for new matches after falling
        Invoke(nameof(CheckForNewMatches), swapSpeed);
    }

    private void CheckForNewMatches()
    {
        List<Block> newMatches = FindMatches();
        if (newMatches.Count >= 3)
        {
            foreach (Block block in newMatches)
            {
                Vector2Int pos = new Vector2Int(block.column, block.row);
                blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }
            StartFalling();
        }
    }

    private List<Block> FindMatches()
    {
        HashSet<Block> matchingBlocks = new HashSet<Block>();

        // Check horizontal matches
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 2; x++)
            {
                Block block1 = blocks[x, y];
                Block block2 = blocks[x + 1, y];
                Block block3 = blocks[x + 2, y];

                if (block1 != null && block2 != null && block3 != null &&
                    block1.type == block2.type && block2.type == block3.type)
                {
                    matchingBlocks.Add(block1);
                    matchingBlocks.Add(block2);
                    matchingBlocks.Add(block3);

                    // Check for longer matches
                    for (int i = x + 3; i < gridWidth; i++)
                    {
                        Block nextBlock = blocks[i, y];
                        if (nextBlock != null && nextBlock.type == block1.type)
                        {
                            matchingBlocks.Add(nextBlock);
                        }
                        else break;
                    }
                }
            }
        }

        // Check vertical matches
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight - 2; y++)
            {
                Block block1 = blocks[x, y];
                Block block2 = blocks[x, y + 1];
                Block block3 = blocks[x, y + 2];

                if (block1 != null && block2 != null && block3 != null &&
                    block1.type == block2.type && block2.type == block3.type)
                {
                    matchingBlocks.Add(block1);
                    matchingBlocks.Add(block2);
                    matchingBlocks.Add(block3);

                    // Check for longer matches
                    for (int i = y + 3; i < gridHeight; i++)
                    {
                        Block nextBlock = blocks[x, i];
                        if (nextBlock != null && nextBlock.type == block1.type)
                        {
                            matchingBlocks.Add(nextBlock);
                        }
                        else break;
                    }
                }
            }
        }

        return matchingBlocks.ToList();
    }
}
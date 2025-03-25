using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public GameObject blockPrefab;
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float swapSpeed = 0.3f;

    [Range(0f, 1f)]
    public float rarityInfluence = 0.7f; // How much rarity affects spawn chances (0 = no effect, 1 = maximum effect)
    
    private Block[,] blocks;
    private bool isSwapping = false;
    private Vector2 touchStart;
    private Block selectedBlock;
    private Block block1SwappedWith;
    private Block block2SwappedWith;

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
        Block blockPrefabComponent = blockPrefab.GetComponent<Block>();
        bool isJokerType = false;
        foreach (var blockTypeData in blockPrefabComponent.blockTypes)
        {
            if (blockTypeData.type == type && blockTypeData.isJoker)
            {
                isJokerType = true;
                break;
            }
        }

        // If this is a joker being placed, check if it would connect two same-colored blocks
        if (isJokerType)
        {
            // Check horizontal matches
            if (x >= 1 && x < gridWidth - 1 && 
                blocks[x - 1, y] != null && blocks[x + 1, y] != null &&
                blocks[x - 1, y].type == blocks[x + 1, y].type)
            {
                return true;
            }
            if (x >= 2 && 
                blocks[x - 1, y] != null && blocks[x - 2, y] != null &&
                blocks[x - 1, y].type == blocks[x - 2, y].type)
            {
                return true;
            }
            if (x < gridWidth - 2 && 
                blocks[x + 1, y] != null && blocks[x + 2, y] != null &&
                blocks[x + 1, y].type == blocks[x + 2, y].type)
            {
                return true;
            }

            // Check vertical matches
            if (y >= 1 && y < gridHeight - 1 && 
                blocks[x, y - 1] != null && blocks[x, y + 1] != null &&
                blocks[x, y - 1].type == blocks[x, y + 1].type)
            {
                return true;
            }
            if (y >= 2 && 
                blocks[x, y - 1] != null && blocks[x, y - 2] != null &&
                blocks[x, y - 1].type == blocks[x, y - 2].type)
            {
                return true;
            }
            if (y < gridHeight - 2 && 
                blocks[x, y + 1] != null && blocks[x, y + 2] != null &&
                blocks[x, y + 1].type == blocks[x, y + 2].type)
            {
                return true;
            }
        }
        else
        {
            // For non-joker blocks, check if they would match with existing blocks including jokers
            // Horizontal checks
            if (x >= 2)
            {
                Block b1 = blocks[x - 1, y];
                Block b2 = blocks[x - 2, y];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }
            if (x >= 1 && x < gridWidth - 1)
            {
                Block b1 = blocks[x - 1, y];
                Block b2 = blocks[x + 1, y];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }
            if (x < gridWidth - 2)
            {
                Block b1 = blocks[x + 1, y];
                Block b2 = blocks[x + 2, y];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }

            // Vertical checks
            if (y >= 2)
            {
                Block b1 = blocks[x, y - 1];
                Block b2 = blocks[x, y - 2];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }
            if (y >= 1 && y < gridHeight - 1)
            {
                Block b1 = blocks[x, y - 1];
                Block b2 = blocks[x, y + 1];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }
            if (y < gridHeight - 2)
            {
                Block b1 = blocks[x, y + 1];
                Block b2 = blocks[x, y + 2];
                if (b1 != null && b2 != null)
                {
                    if ((b1.IsJoker && b2.type == type) || 
                        (b2.IsJoker && b1.type == type) ||
                        (b1.type == type && b2.type == type))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Block.BlockType GetRandomBlockType(int x, int y)
    {
        Block blockComponent = blockPrefab.GetComponent<Block>();
        
        // Step 1: Separate valid and invalid types (those that would cause matches)
        List<Block.BlockTypeData> validTypes = new List<Block.BlockTypeData>();
        List<Block.BlockTypeData> invalidTypes = new List<Block.BlockTypeData>();
        
        foreach (var blockTypeData in blockComponent.blockTypes)
        {
            if (!WouldCauseMatch(x, y, blockTypeData.type))
            {
                validTypes.Add(blockTypeData);
            }
            else
            {
                invalidTypes.Add(blockTypeData);
            }
        }
        
        // If there are no valid types, we have no choice but to create a match
        if (validTypes.Count == 0)
        {
            Debug.LogWarning($"No valid block types at position ({x}, {y}), will create a match");
            validTypes = invalidTypes;
        }
        
        // Step 2: Calculate total rarity values
        int totalRarity = 0;
        foreach (var blockTypeData in validTypes)
        {
            totalRarity += blockTypeData.rarity;
        }
        
        // Step 3: Use a two-tier selection system
        // First, give all types a base chance (1-rarityInfluence)
        // Then, distribute the remaining probability (rarityInfluence) based on rarity
        float randomValue = Random.value;
        
        // If we're using rarity influence
        if (rarityInfluence > 0 && randomValue <= rarityInfluence)
        {
            // Using rarity-based probabilities
            int randomRarity = Random.Range(1, totalRarity + 1);
            int currentRarity = 0;
            
            foreach (var blockTypeData in validTypes)
            {
                currentRarity += blockTypeData.rarity;
                if (randomRarity <= currentRarity)
                {
                    return blockTypeData.type;
                }
            }
        }
        else 
        {
            // Using equal probabilities
            int randomIndex = Random.Range(0, validTypes.Count);
            return validTypes[randomIndex].type;
        }
        
        // Fallback
        return validTypes[0].type;
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

        // Store the blocks being swapped
        block1SwappedWith = block1;
        block2SwappedWith = block2;

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

    private void SwapBlocksBack(Block block1, Block block2)
    {
        // Swap array positions back
        blocks[block1.column, block1.row] = block2;
        blocks[block2.column, block2.row] = block1;

        // Swap grid positions back
        int tempColumn = block1.column;
        int tempRow = block1.row;
        block1.column = block2.column;
        block1.row = block2.row;
        block2.column = tempColumn;
        block2.row = tempRow;

        // Animate movement back
        block1.SetTargetPosition(new Vector2(block1.column, block1.row));
        block2.SetTargetPosition(new Vector2(block2.column, block2.row));

        // Reset state after animation completes
        Invoke(nameof(ResetSwapState), swapSpeed);
    }

    private void ResetSwapState()
    {
        isSwapping = false;
        block1SwappedWith = null;
        block2SwappedWith = null;
    }

    private void CheckMatches()
    {
        List<Block> matchingBlocks = FindMatches();
        if (matchingBlocks.Count >= 3)
        {
            // Find and notify about any joker matches before destroying blocks
            foreach (Block block in matchingBlocks)
            {
                if (block.IsJoker)
                {
                    // Find the type the joker matched with
                    Block.BlockType matchedType = Block.BlockType.Blue; // default
                    foreach (Block otherBlock in matchingBlocks)
                    {
                        if (otherBlock != block && !otherBlock.IsJoker)
                        {
                            matchedType = otherBlock.type;
                            break;
                        }
                    }
                    OnJokerMatched(block, matchedType);
                }
            }

            // Continue with regular match handling
            isSwapping = false;
            block1SwappedWith = null;
            block2SwappedWith = null;

            foreach (Block block in matchingBlocks)
            {
                Vector2Int pos = new Vector2Int(block.column, block.row);
                Debug.Log($"Destroyed {block.type} block at position ({pos.x}, {pos.y}) with rarity {block.GetRarity()}");
                blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }

            StartFalling();
        }
        else if (block1SwappedWith != null && block2SwappedWith != null)
        {
            SwapBlocksBack(block1SwappedWith, block2SwappedWith);
        }
        else
        {
            isSwapping = false;
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

                if (block1 != null && block2 != null && block3 != null)
                {
                    // For jokers, we need to ensure they're actually connecting same-colored blocks
                    bool isMatch = false;
                    
                    if (block1.IsJoker)
                    {
                        isMatch = block2.type == block3.type; // Joker must connect two same blocks
                    }
                    else if (block2.IsJoker)
                    {
                        isMatch = block1.type == block3.type; // Joker in middle must connect same blocks
                    }
                    else if (block3.IsJoker)
                    {
                        isMatch = block1.type == block2.type; // Joker must connect two same blocks
                    }
                    else
                    {
                        // No jokers - all blocks must match
                        isMatch = block1.type == block2.type && block2.type == block3.type;
                    }

                    if (isMatch)
                    {
                        matchingBlocks.Add(block1);
                        matchingBlocks.Add(block2);
                        matchingBlocks.Add(block3);

                        // Check for longer matches
                        for (int i = x + 3; i < gridWidth; i++)
                        {
                            Block nextBlock = blocks[i, y];
                            if (nextBlock == null) break;
                            
                            // For longer matches, if we have a joker in the sequence,
                            // it must connect to blocks of the same type
                            bool canExtendMatch = false;
                            if (nextBlock.IsJoker)
                            {
                                // Get the last non-joker block type from the match
                                Block.BlockType matchType = Block.BlockType.Blue; // default
                                bool foundType = false;
                                for (int j = i - 1; j >= x && !foundType; j--)
                                {
                                    if (!blocks[j, y].IsJoker)
                                    {
                                        matchType = blocks[j, y].type;
                                        foundType = true;
                                    }
                                }
                                canExtendMatch = foundType; // only extend if we found a type to match
                            }
                            else
                            {
                                // Get the type we're matching against (first non-joker block)
                                Block.BlockType matchType = Block.BlockType.Blue;
                                for (int j = i - 1; j >= x; j--)
                                {
                                    if (!blocks[j, y].IsJoker)
                                    {
                                        matchType = blocks[j, y].type;
                                        break;
                                    }
                                }
                                canExtendMatch = nextBlock.type == matchType;
                            }

                            if (canExtendMatch)
                            {
                                matchingBlocks.Add(nextBlock);
                            }
                            else break;
                        }
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

                if (block1 != null && block2 != null && block3 != null)
                {
                    // For jokers, we need to ensure they're actually connecting same-colored blocks
                    bool isMatch = false;
                    
                    if (block1.IsJoker)
                    {
                        isMatch = block2.type == block3.type; // Joker must connect two same blocks
                    }
                    else if (block2.IsJoker)
                    {
                        isMatch = block1.type == block3.type; // Joker in middle must connect same blocks
                    }
                    else if (block3.IsJoker)
                    {
                        isMatch = block1.type == block2.type; // Joker must connect two same blocks
                    }
                    else
                    {
                        // No jokers - all blocks must match
                        isMatch = block1.type == block2.type && block2.type == block3.type;
                    }

                    if (isMatch)
                    {
                        matchingBlocks.Add(block1);
                        matchingBlocks.Add(block2);
                        matchingBlocks.Add(block3);

                        // Check for longer matches
                        for (int i = y + 3; i < gridHeight; i++)
                        {
                            Block nextBlock = blocks[x, i];
                            if (nextBlock == null) break;
                            
                            // For longer matches, if we have a joker in the sequence,
                            // it must connect to blocks of the same type
                            bool canExtendMatch = false;
                            if (nextBlock.IsJoker)
                            {
                                // Get the last non-joker block type from the match
                                Block.BlockType matchType = Block.BlockType.Blue; // default
                                bool foundType = false;
                                for (int j = i - 1; j >= y && !foundType; j--)
                                {
                                    if (!blocks[x, j].IsJoker)
                                    {
                                        matchType = blocks[x, j].type;
                                        foundType = true;
                                    }
                                }
                                canExtendMatch = foundType; // only extend if we found a type to match
                            }
                            else
                            {
                                // Get the type we're matching against (first non-joker block)
                                Block.BlockType matchType = Block.BlockType.Blue;
                                for (int j = i - 1; j >= y; j--)
                                {
                                    if (!blocks[x, j].IsJoker)
                                    {
                                        matchType = blocks[x, j].type;
                                        break;
                                    }
                                }
                                canExtendMatch = nextBlock.type == matchType;
                            }

                            if (canExtendMatch)
                            {
                                matchingBlocks.Add(nextBlock);
                            }
                            else break;
                        }
                    }
                }
            }
        }

        return matchingBlocks.ToList();
    }

    private bool AreBlocksMatching(Block block1, Block block2)
    {
        // If neither block is a joker, just check if they're the same type
        if (!block1.IsJoker && !block2.IsJoker)
        {
            return block1.type == block2.type;
        }

        // We have at least one joker - but we'll check during the match-3 evaluation
        // if it's actually connecting same-colored blocks
        return true;
    }

    private void OnJokerMatched(Block jokerBlock, Block.BlockType matchedWithType)
    {
        if (jokerBlock == null) return;
        
        // Only trigger if this is actually a joker block
        if (!jokerBlock.IsJoker) return;
        
        Debug.Log($"Joker at ({jokerBlock.column}, {jokerBlock.row}) matched with {matchedWithType} blocks");
    }
}
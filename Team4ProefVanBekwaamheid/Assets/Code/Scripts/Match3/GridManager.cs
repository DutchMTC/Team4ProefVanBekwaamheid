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
    
    private Block[,] _blocks;
    private bool _isSwapping = false;
    private bool _isFalling = false;    
    private Vector2 _touchStart;
    private Block _selectedBlock;
    private Block _block1SwappedWith;
    private Block _block2SwappedWith;

    private void Start()
    {
        _blocks = new Block[gridWidth, gridHeight];
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
                _blocks[x - 1, y] != null && _blocks[x + 1, y] != null &&
                _blocks[x - 1, y].type == _blocks[x + 1, y].type)
            {
                return true;
            }
            if (x >= 2 && 
                _blocks[x - 1, y] != null && _blocks[x - 2, y] != null &&
                _blocks[x - 1, y].type == _blocks[x - 2, y].type)
            {
                return true;
            }
            if (x < gridWidth - 2 && 
                _blocks[x + 1, y] != null && _blocks[x + 2, y] != null &&
                _blocks[x + 1, y].type == _blocks[x + 2, y].type)
            {
                return true;
            }

            // Check vertical matches
            if (y >= 1 && y < gridHeight - 1 && 
                _blocks[x, y - 1] != null && _blocks[x, y + 1] != null &&
                _blocks[x, y - 1].type == _blocks[x, y + 1].type)
            {
                return true;
            }
            if (y >= 2 && 
                _blocks[x, y - 1] != null && _blocks[x, y - 2] != null &&
                _blocks[x, y - 1].type == _blocks[x, y - 2].type)
            {
                return true;
            }
            if (y < gridHeight - 2 && 
                _blocks[x, y + 1] != null && _blocks[x, y + 2] != null &&
                _blocks[x, y + 1].type == _blocks[x, y + 2].type)
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
                Block b1 = _blocks[x - 1, y];
                Block b2 = _blocks[x - 2, y];
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
                Block b1 = _blocks[x - 1, y];
                Block b2 = _blocks[x + 1, y];
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
                Block b1 = _blocks[x + 1, y];
                Block b2 = _blocks[x + 2, y];
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
                Block b1 = _blocks[x, y - 1];
                Block b2 = _blocks[x, y - 2];
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
                Block b1 = _blocks[x, y - 1];
                Block b2 = _blocks[x, y + 1];
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
                Block b1 = _blocks[x, y + 1];
                Block b2 = _blocks[x, y + 2];
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
        _blocks[x, y] = block;
        
        // If creating a block above the grid (for falling animation), set the falling state
        if (block.transform.position.y > y)
        {
            _isFalling = true;
        }
    }

    private void Update()
    {
        // If blocks are falling or being swapped, don't allow new moves
        if (_isSwapping || _isFalling) return;

        // Handle mouse input
        if (Input.GetMouseButtonDown(0))
        {
            _touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _selectedBlock = GetBlockAtPosition(_touchStart);
        }
        else if (Input.GetMouseButtonUp(0) && _selectedBlock != null)
        {
            Vector2 touchEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = touchEnd - _touchStart;

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

                TrySwapBlocks(_selectedBlock, swapDirection);
            }
            _selectedBlock = null;
        }

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                _touchStart = Camera.main.ScreenToWorldPoint(touch.position);
                _selectedBlock = GetBlockAtPosition(_touchStart);
            }
            else if (touch.phase == TouchPhase.Ended && _selectedBlock != null)
            {
                Vector2 touchEnd = Camera.main.ScreenToWorldPoint(touch.position);
                Vector2 direction = touchEnd - _touchStart;

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

                    TrySwapBlocks(_selectedBlock, swapDirection);
                }
                _selectedBlock = null;
            }
        }
    }

    private Block GetBlockAtPosition(Vector2 position)
    {
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y);

        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return _blocks[x, y];
        }
        return null;
    }

    private void TrySwapBlocks(Block block, Vector2Int direction)
    {
        int newX = block.column + direction.x;
        int newY = block.row + direction.y;

        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            Block otherBlock = _blocks[newX, newY];
            SwapBlocks(block, otherBlock);
        }
    }

    private void SwapBlocks(Block block1, Block block2)
    {
        _isSwapping = true;

        // Store the blocks being swapped
        _block1SwappedWith = block1;
        _block2SwappedWith = block2;

        // Swap array positions
        _blocks[block1.column, block1.row] = block2;
        _blocks[block2.column, block2.row] = block1;

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
        _blocks[block1.column, block1.row] = block2;
        _blocks[block2.column, block2.row] = block1;

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
        _isSwapping = false;
        _block1SwappedWith = null;
        _block2SwappedWith = null;
    }

    private void CheckMatches()
    {
        List<Block> matchingBlocks = FindMatches();
        if (matchingBlocks.Count >= 3)
        {
            // --- Award Power-ups for any match ---
            Block representativeBlock = null;
            foreach (Block block in matchingBlocks)
            {
                if (!block.IsJoker)
                {
                    representativeBlock = block;
                    break;
                }
            }
            // If match was all jokers (edge case), use the first one
            if (representativeBlock == null && matchingBlocks.Count > 0) {
                 representativeBlock = matchingBlocks[0];
            }

            if (representativeBlock != null) {
                int powerUpAmount = matchingBlocks.Count;
                PowerUpInventory.Instance?.AddPowerUps(representativeBlock.GetPowerUpType(), powerUpAmount);
                Debug.Log($"Match of {matchingBlocks.Count} blocks (type: {representativeBlock.type}) - Awarded {powerUpAmount} {representativeBlock.GetPowerUpType()} power-ups");
            }
            // --- End Power-up Award ---

            // Continue with regular match handling
            _isSwapping = false; // Reset swap state since a match occurred
            _block1SwappedWith = null;
            _block2SwappedWith = null;

            foreach (Block block in matchingBlocks)
            {
                Vector2Int pos = new Vector2Int(block.column, block.row);
                Debug.Log($"Destroyed {block.type} block at position ({pos.x}, {pos.y}) with rarity {block.GetRarity()}");
                _blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }

            StartFalling();
        }
        else if (_block1SwappedWith != null && _block2SwappedWith != null)
        {
            SwapBlocksBack(_block1SwappedWith, _block2SwappedWith);
            _isFalling = false; // Reset falling state when swapping back
        }
        else
        {
            _isSwapping = false;
            _isFalling = false; // Reset falling state when no matches found
        }
    }

    private void StartFalling()
    {
        _isFalling = true; // Set falling state when blocks start falling
        bool needsMoreFalling;
        do
        {
            needsMoreFalling = false;

            // Move blocks down if there's empty space below
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight - 1; y++)
                {
                    if (_blocks[x, y] == null)
                    {
                        // Look for the next non-null block above
                        for (int above = y + 1; above < gridHeight; above++)
                        {
                            if (_blocks[x, above] != null)
                            {
                                // Move block down
                                Block block = _blocks[x, above];
                                _blocks[x, y] = block;
                                _blocks[x, above] = null;
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
                    if (_blocks[x, y] == null)
                    {
                        CreateBlock(x, y);
                        _blocks[x, y].transform.position = new Vector3(x, gridHeight, 0);
                        _blocks[x, y].SetTargetPosition(new Vector2(x, y));
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
                _blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }
            StartFalling();
        }
        else
        {
            _isFalling = false;
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
                Block block1 = _blocks[x, y];
                Block block2 = _blocks[x + 1, y];
                Block block3 = _blocks[x + 2, y];

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
                            Block nextBlock = _blocks[i, y];
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
                                    if (!_blocks[j, y].IsJoker)
                                    {
                                        matchType = _blocks[j, y].type;
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
                                    if (!_blocks[j, y].IsJoker)
                                    {
                                        matchType = _blocks[j, y].type;
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
                Block block1 = _blocks[x, y];
                Block block2 = _blocks[x, y + 1];
                Block block3 = _blocks[x, y + 2];

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
                            Block nextBlock = _blocks[x, i];
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
                                    if (!_blocks[x, j].IsJoker)
                                    {
                                        matchType = _blocks[x, j].type;
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
                                    if (!_blocks[x, j].IsJoker)
                                    {
                                        matchType = _blocks[x, j].type;
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

}
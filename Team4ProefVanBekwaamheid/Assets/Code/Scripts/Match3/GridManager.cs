using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GridManager : MonoBehaviour
{
    
    public Block[,] Blocks { get; private set; }
    public GameObject blockPrefab;
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float swapSpeed = 0.3f;
    public float fallSpeed = 0.5f; 
    public AnimationCurve fallEaseCurve; 
    [Range(0f, 1f)]    public float rarityInfluence = 0.7f; 

    [Header("Swap Limit Settings")] 
    public int swapLimit = 10;      
    public int currentSwaps;        
    public TMP_Text matchCounterText; 
    public bool gridActive; 


    private bool _isSwapping = false;
    private bool _isFalling = false;
    private int _activeFallingAnimations = 0; 
    private Vector2 _touchStart;
    private Block _selectedBlock;
    private Block _block1SwappedWith;
    private Block _block2SwappedWith;

    private void Start()
    {
        Blocks = new Block[gridWidth, gridHeight];
        InitializeGrid();
        // Initialize text based on swaps
        if (matchCounterText != null) matchCounterText.text = (swapLimit - currentSwaps).ToString();
        else Debug.LogError("MatchCounterText is not assigned in the Inspector!");
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
                Blocks[x - 1, y] != null && Blocks[x + 1, y] != null &&
                Blocks[x - 1, y].type == Blocks[x + 1, y].type)
            {
                return true;
            }
            if (x >= 2 && 
                Blocks[x - 1, y] != null && Blocks[x - 2, y] != null &&
                Blocks[x - 1, y].type == Blocks[x - 2, y].type)
            {
                return true;
            }
            if (x < gridWidth - 2 && 
                Blocks[x + 1, y] != null && Blocks[x + 2, y] != null &&
                Blocks[x + 1, y].type == Blocks[x + 2, y].type)
            {
                return true;
            }

            // Check vertical matches
            if (y >= 1 && y < gridHeight - 1 && 
                Blocks[x, y - 1] != null && Blocks[x, y + 1] != null &&
                Blocks[x, y - 1].type == Blocks[x, y + 1].type)
            {
                return true;
            }
            if (y >= 2 && 
                Blocks[x, y - 1] != null && Blocks[x, y - 2] != null &&
                Blocks[x, y - 1].type == Blocks[x, y - 2].type)
            {
                return true;
            }
            if (y < gridHeight - 2 && 
                Blocks[x, y + 1] != null && Blocks[x, y + 2] != null &&
                Blocks[x, y + 1].type == Blocks[x, y + 2].type)
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
                Block b1 = Blocks[x - 1, y];
                Block b2 = Blocks[x - 2, y];
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
                Block b1 = Blocks[x - 1, y];
                Block b2 = Blocks[x + 1, y];
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
                Block b1 = Blocks[x + 1, y];
                Block b2 = Blocks[x + 2, y];
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
                Block b1 = Blocks[x, y - 1];
                Block b2 = Blocks[x, y - 2];
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
                Block b1 = Blocks[x, y - 1];
                Block b2 = Blocks[x, y + 1];
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
                Block b1 = Blocks[x, y + 1];
                Block b2 = Blocks[x, y + 2];
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
        
        // Separate valid and invalid types (those that would cause matches)
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
        
        // If there are no valid types, create a match
        if (validTypes.Count == 0)
        {
            Debug.LogWarning($"No valid block types at position ({x}, {y}), will create a match");
            validTypes = invalidTypes;
        }
        
        // Calculate total rarity values
        int totalRarity = 0;
        foreach (var blockTypeData in validTypes)
        {
            totalRarity += blockTypeData.rarity;
        }
        
        // Use a two-tier selection system
        // Give all types a base chance (1-rarityInfluence)
        // Distribute the remaining probability (rarityInfluence) based on rarity
        float randomValue = Random.value;
        
        if (rarityInfluence > 0 && randomValue <= rarityInfluence)
        {
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
        Blocks[x, y] = block;
        
        // Initial position for newly spawned blocks will be set in StartFalling
    }

    private void Update()
    {
        // If blocks are falling or being swapped, don't allow new moves
        if (_isSwapping || _isFalling) return;

        // Handle mouse input
        if (Input.GetMouseButtonDown(0) && gridActive == true)
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
        if (Input.touchCount > 0 && gridActive == true)
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
            return Blocks[x, y];
        }
        return null;
    }

    private void TrySwapBlocks(Block block, Vector2Int direction)
    {
        int newX = block.column + direction.x;
        int newY = block.row + direction.y;

        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            Block otherBlock = Blocks[newX, newY];
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
        Blocks[block1.column, block1.row] = block2;
        Blocks[block2.column, block2.row] = block1;

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

        Invoke(nameof(CheckMatches), swapSpeed);
    }

    private void SwapBlocksBack(Block block1, Block block2)
    {
        // Swap array positions back
        Blocks[block1.column, block1.row] = block2;
        Blocks[block2.column, block2.row] = block1;

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
            currentSwaps++;
            if (matchCounterText != null)
            {
                matchCounterText.text = (swapLimit - currentSwaps).ToString();
            }
            Debug.Log($"Valid swap performed (Match Found). Current Swaps: {currentSwaps}/{swapLimit}");

            Block representativeBlock = null;
            foreach (Block block in matchingBlocks)
            {
                if (!block.IsJoker)
                {
                    representativeBlock = block;
                    break;
                }
            }
            if (representativeBlock == null && matchingBlocks.Count > 0) {
                 representativeBlock = matchingBlocks[0];
            }

            if (representativeBlock != null) {
                int powerUpAmount = matchingBlocks.Count;
                PowerUpInventory.Instance?.AddPowerUps(representativeBlock.GetPowerUpType(), powerUpAmount);
                Debug.Log($"Match of {matchingBlocks.Count} blocks (type: {representativeBlock.type}) - Awarded {powerUpAmount} {representativeBlock.GetPowerUpType()} power-ups");
            }

            _isSwapping = false; 
            _block1SwappedWith = null;
            _block2SwappedWith = null;

            foreach (Block block in matchingBlocks)
            {
                Vector2Int pos = new Vector2Int(block.column, block.row);
                Debug.Log($"Destroyed {block.type} block at position ({pos.x}, {pos.y}) with rarity {block.GetRarity()}");
                Blocks[pos.x, pos.y] = null;
                Destroy(block.gameObject);
            }
            StartCoroutine(ProcessFallingAndCheckMatches());
        }
        else if (_block1SwappedWith != null && _block2SwappedWith != null)
        {
            SwapBlocksBack(_block1SwappedWith, _block2SwappedWith);
            // _isFalling is reset in ResetSwapState after animation
        }
        else
        {
             ResetSwapState(); 
        }
    }

    private System.Collections.IEnumerator ProcessFallingAndCheckMatches()
    {
        if (_isFalling) yield break; 
        _isFalling = true;
        _activeFallingAnimations = 0;

        bool blocksMoved;
        do
        {
            blocksMoved = false;
            List<System.Collections.IEnumerator> fallAnimations = new List<System.Collections.IEnumerator>();

            // Handle existing blocks falling down
            for (int x = 0; x < gridWidth; x++)
            {
                int fallTargetY = -1; 
                for (int y = 0; y < gridHeight; y++)
                {
                    if (Blocks[x, y] == null && fallTargetY == -1)
                    {
                        fallTargetY = y; // 
                    }
                    else if (Blocks[x, y] != null && fallTargetY != -1)
                    {
                        // Found a block above an empty spot, make it fall
                        Block block = Blocks[x, y];
                        Blocks[x, fallTargetY] = block;
                        Blocks[x, y] = null;
                        block.row = fallTargetY;

                        // Start animation
                        Vector3 startPos = block.transform.position;
                        Vector3 endPos = new Vector3(block.column, block.row, 0);
                        fallAnimations.Add(AnimateBlockMovement(block, startPos, endPos, fallSpeed));
                        _activeFallingAnimations++;

                        blocksMoved = true;
                        fallTargetY++;
                    }
                }
            }

            // Spawn new blocks at the top
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = gridHeight - 1; y >= 0; y--)
                {
                    if (Blocks[x, y] == null)
                    {
                        // Create new block at a consistent height above the grid
                        Vector3 spawnPos = new Vector3(x, gridHeight, 0); // Consistent spawn height above grid
                        GameObject blockObject = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
                        blockObject.transform.parent = transform;

                        Block block = blockObject.GetComponent<Block>();
                        Block.BlockType randomType = GetRandomBlockType(x, y);
                        block.Initialize(randomType, x, y); // Initialize with final grid position
                        Blocks[x, y] = block; // Place in array

                        // Start animation
                        Vector3 endPos = new Vector3(x, y, 0);
                        fallAnimations.Add(AnimateBlockMovement(block, spawnPos, endPos, fallSpeed));
                        _activeFallingAnimations++;

                        blocksMoved = true;
                    }
                    else
                    {
                        // Found a block, no need to check lower in this column for spawning
                        break;
                    }
                }
            }

            // Wait for all animations in this pass to complete
            if (fallAnimations.Count > 0)
            {
                // Start all coroutines for this pass
                foreach (var animCoroutine in fallAnimations)
                {
                    StartCoroutine(animCoroutine);
                }

                // Wait until all animations are done
                yield return new WaitUntil(() => _activeFallingAnimations == 0);
            }


            // After blocks fall/spawn, check for new matches immediately
            List<Block> newMatches = FindMatches();
            if (newMatches.Count >= 3)
            {
                Block representativeBlock = null;
                foreach (Block block in newMatches)
                {
                    if (!block.IsJoker)
                    {
                        representativeBlock = block;
                        break;
                    }
                }
                if (representativeBlock == null && newMatches.Count > 0) {
                     representativeBlock = newMatches[0];
                }

                if (representativeBlock != null) {
                    int powerUpAmount = newMatches.Count;
                    PowerUpInventory.Instance?.AddPowerUps(representativeBlock.GetPowerUpType(), powerUpAmount);
                    Debug.Log($"Cascade Match of {newMatches.Count} blocks (type: {representativeBlock.type}) - Awarded {powerUpAmount} {representativeBlock.GetPowerUpType()} power-ups");
                }

                foreach (Block block in newMatches)
                {
                    if (Blocks[block.column, block.row] == block) 
                    {
                        Blocks[block.column, block.row] = null;
                        Destroy(block.gameObject);
                    }
                }
                blocksMoved = true;
            }
            else
            {
                blocksMoved = false;
            }

        } while (blocksMoved); // Loop if blocks moved (either fell or new matches were cleared)

        _isFalling = false; // All falling and cascading is complete

        // Final check for game state transition based on swaps
        if (GameManager.Instance.State == GameState.Matching && currentSwaps >= swapLimit)
        {
            GameManager.Instance.UpdateGameState(GameState.Player);
        }
    }


    // Animates a block's movement from startPos to endPos over a given duration.
    private System.Collections.IEnumerator AnimateBlockMovement(Block block, Vector3 startPos, Vector3 endPos, float duration)
    {
        if (block != null) block.IsFalling = true; 
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            if (block == null) yield break;

            float t = timeElapsed / duration;
            float easedT = fallEaseCurve != null && fallEaseCurve.keys.Length > 0 ? fallEaseCurve.Evaluate(t) : t; // Apply easing

            block.transform.position = Vector3.LerpUnclamped(startPos, endPos, easedT); // Use LerpUnclamped for potential overshoot

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final position is exact
        if (block != null)
        {
             block.transform.position = endPos;
             block.SyncTargetPosition();
             block.IsFalling = false; 
        }

        // Decrement counter when animation finishes
        _activeFallingAnimations--;
         if (_activeFallingAnimations < 0) _activeFallingAnimations = 0;
    }


    private List<Block> FindMatches()
    {
        HashSet<Block> matchingBlocks = new HashSet<Block>();

        // Check horizontal matches
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 2; x++)
            {
                Block block1 = Blocks[x, y];
                Block block2 = Blocks[x + 1, y];
                Block block3 = Blocks[x + 2, y];

                if (block1 != null && block2 != null && block3 != null)
                {

                    bool isMatch = false;
                    
                    if (block1.IsJoker)
                    {
                        isMatch = block2.type == block3.type;
                    }
                    else if (block2.IsJoker)
                    {
                        isMatch = block1.type == block3.type; 
                    }
                    else if (block3.IsJoker)
                    {
                        isMatch = block1.type == block2.type;
                    }
                    else
                    {
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
                            Block nextBlock = Blocks[i, y];
                            if (nextBlock == null) break;
                            
                            // For longer matches, if we have a joker in the sequence,
                            // it must connect to blocks of the same type
                            bool canExtendMatch = false;
                            if (nextBlock.IsJoker)
                            {
                                // Get the last non-joker block type from the match
                                Block.BlockType matchType = Block.BlockType.Blue; 
                                bool foundType = false;
                                for (int j = i - 1; j >= x && !foundType; j--)
                                {
                                    if (!Blocks[j, y].IsJoker)
                                    {
                                        matchType = Blocks[j, y].type;
                                        foundType = true;
                                    }
                                }
                                canExtendMatch = foundType;
                            }
                            else
                            {
                                // Get the type we're matching against (first non-joker block)
                                Block.BlockType matchType = Block.BlockType.Blue;
                                for (int j = i - 1; j >= x; j--)
                                {
                                    if (!Blocks[j, y].IsJoker)
                                    {
                                        matchType = Blocks[j, y].type;
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
                Block block1 = Blocks[x, y];
                Block block2 = Blocks[x, y + 1];
                Block block3 = Blocks[x, y + 2];

                if (block1 != null && block2 != null && block3 != null)
                {
                    // For jokers, we need to ensure they're actually connecting same-colored blocks
                    bool isMatch = false;
                    
                    if (block1.IsJoker)
                    {
                        isMatch = block2.type == block3.type; 
                    }
                    else if (block2.IsJoker)
                    {
                        isMatch = block1.type == block3.type; 
                    }
                    else if (block3.IsJoker)
                    {
                        isMatch = block1.type == block2.type; 
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
                            Block nextBlock = Blocks[x, i];
                            if (nextBlock == null) break;
                            
                            bool canExtendMatch = false;
                            if (nextBlock.IsJoker)
                            {
                                Block.BlockType matchType = Block.BlockType.Blue;
                                bool foundType = false;
                                for (int j = i - 1; j >= y && !foundType; j--)
                                {
                                    if (!Blocks[x, j].IsJoker)
                                    {
                                        matchType = Blocks[x, j].type;
                                        foundType = true;
                                    }
                                }
                                canExtendMatch = foundType;
                            }
                            else
                            {
                                // Get the type we're matching against (first non-joker block)
                                Block.BlockType matchType = Block.BlockType.Blue;
                                for (int j = i - 1; j >= y; j--)
                                {
                                    if (!Blocks[x, j].IsJoker)
                                    {
                                        matchType = Blocks[x, j].type;
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
        return true;
    }

}
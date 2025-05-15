using UnityEngine;
using System.Collections; // Added for Coroutines
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI; // Added for UI elements like Buttons

public class GridManager : MonoBehaviour
{

    public Block[,] Blocks { get; private set; }
    public GameObject blockPrefab;
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float swapSpeed = 0.3f;
    public float fallSpeed = 0.5f;
    public AnimationCurve fallEaseCurve;
    [Range(0f, 1f)] public float rarityInfluence = 0.7f;

    [Header("Swap Limit Settings")]
    public int swapLimit = 10;
    public int currentSwaps;
    public TMP_Text matchCounterText;
    public bool gridActive;

    [Header("PowerUp Animation Settings")]
    public float liftHeight = 0.5f; // How high blocks lift before moving
    public float liftDuration = 0.2f; // Duration of the lift phase
    // public float moveToGroupDuration = 0.15f; // Duration to move to the group center (Removed for curved path)
    public float moveToPowerUpDuration = 0.5f; // Duration of the move towards powerup (Adjusted back)
    public float powerUpAnimationDelay = 0.05f; // Delay between each block starting its animation
    public AnimationCurve moveToPowerUpCurve; // Easing for the move
    public Transform movePowerUpTarget; // Assign the Move PowerUp Button Transform
    public Transform attackPowerUpTarget; // Assign the Attack PowerUp Button Transform
    public Transform defensePowerUpTarget; // Assign the Defense PowerUp Button Transform
    public Transform trapPowerUpTarget; // Assign the Trap PowerUp Button Transform
    // Add more targets as needed

    [Header("Animation Parent")]
    [SerializeField] private RectTransform animationPanelParent; // Assign a UI Panel (RectTransform) to parent blocks during animation


    private bool _isSwapping = false;
    private bool _isFalling = false;
    private int _activeFallingAnimations = 0;
    private int _activePowerUpAnimations = 0; // Counter for flying animations
    private Vector2 _touchStart;
    private Block _selectedBlock;
    private Block _block1SwappedWith;
    private Block _block2SwappedWith;

    // Event to signal when a block finishes animating to a power-up
    public static event System.Action<PowerUpInventory.PowerUpType> OnBlockAnimationFinished;


    private void Start()
    {
        Blocks = new Block[gridWidth, gridHeight];
        InitializeGrid();
        if (matchCounterText != null) matchCounterText.text = (swapLimit - currentSwaps).ToString();
        else Debug.LogError("MatchCounterText is not assigned in the Inspector!");

        // Basic check for power-up targets
        if (movePowerUpTarget == null || attackPowerUpTarget == null || defensePowerUpTarget == null || trapPowerUpTarget == null)
        {
            Debug.LogWarning("One or more PowerUp Target Transforms are not assigned in the GridManager Inspector. Animations might not target correctly.");
        }
        if (animationPanelParent == null)
        {
            Debug.LogError("Animation Panel Parent is not assigned in the GridManager Inspector! Power-up animations might not render correctly.");
        }
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
        // After initial creation, check for and clear any starting matches without animation
        ClearInitialMatches();
    }

    // New method to clear matches at the start without animation/powerups
    private void ClearInitialMatches()
    {
        bool matchesFound;
        do
        {
            matchesFound = false;
            List<Block> initialMatches = FindMatches();
            if (initialMatches.Count >= 3)
            {
                matchesFound = true;
                foreach (Block block in initialMatches)
                {
                    if (Blocks[block.column, block.row] == block)
                    {
                        Blocks[block.column, block.row] = null;
                        Destroy(block.gameObject);
                    }
                }
                // Simple immediate fill without animation for startup
                FillGridAfterInitialClear();
            }
        } while (matchesFound);
    }

    // Simplified fill logic for startup
    private void FillGridAfterInitialClear()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            int emptySlots = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                if (Blocks[x, y] == null)
                {
                    emptySlots++;
                }
                else if (emptySlots > 0)
                {
                    Block block = Blocks[x, y];
                    Blocks[x, y - emptySlots] = block;
                    Blocks[x, y] = null;
                    block.row = y - emptySlots;
                    block.transform.position = new Vector3(block.column, block.row, 0); // Set position directly
                    block.SyncTargetPosition();
                }
            }

            // Fill top slots
            for (int i = 0; i < emptySlots; i++)
            {
                int y = gridHeight - 1 - i;
                CreateBlock(x, y); // Creates block at correct position
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

        if (isJokerType)
        {
            if (x >= 1 && x < gridWidth - 1 &&
                Blocks[x - 1, y] != null && Blocks[x + 1, y] != null &&
                !Blocks[x-1, y].IsJoker && !Blocks[x+1, y].IsJoker && // Jokers don't match jokers via another joker
                Blocks[x - 1, y].type == Blocks[x + 1, y].type)
            {
                return true;
            }
            if (x >= 2 &&
                Blocks[x - 1, y] != null && Blocks[x - 2, y] != null &&
                !Blocks[x-1, y].IsJoker && !Blocks[x-2, y].IsJoker &&
                Blocks[x - 1, y].type == Blocks[x - 2, y].type)
            {
                return true;
            }
            if (x < gridWidth - 2 &&
                Blocks[x + 1, y] != null && Blocks[x + 2, y] != null &&
                 !Blocks[x+1, y].IsJoker && !Blocks[x+2, y].IsJoker &&
                Blocks[x + 1, y].type == Blocks[x + 2, y].type)
            {
                return true;
            }

            if (y >= 1 && y < gridHeight - 1 &&
                Blocks[x, y - 1] != null && Blocks[x, y + 1] != null &&
                 !Blocks[x, y-1].IsJoker && !Blocks[x, y+1].IsJoker &&
                Blocks[x, y - 1].type == Blocks[x, y + 1].type)
            {
                return true;
            }
            if (y >= 2 &&
                Blocks[x, y - 1] != null && Blocks[x, y - 2] != null &&
                 !Blocks[x, y-1].IsJoker && !Blocks[x, y-2].IsJoker &&
                Blocks[x, y - 1].type == Blocks[x, y - 2].type)
            {
                return true;
            }
            if (y < gridHeight - 2 &&
                Blocks[x, y + 1] != null && Blocks[x, y + 2] != null &&
                 !Blocks[x, y+1].IsJoker && !Blocks[x, y+2].IsJoker &&
                Blocks[x, y + 1].type == Blocks[x, y + 2].type)
            {
                return true;
            }
        }
        else // Placing a non-joker
        {
             // Horizontal checks
            if (x >= 2)
            {
                Block b1 = Blocks[x - 1, y];
                Block b2 = Blocks[x - 2, y];
                if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }
            if (x >= 1 && x < gridWidth - 1)
            {
                Block b1 = Blocks[x - 1, y];
                Block b2 = Blocks[x + 1, y];
                 if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }
            if (x < gridWidth - 2)
            {
                Block b1 = Blocks[x + 1, y];
                Block b2 = Blocks[x + 2, y];
                 if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }

            // Vertical checks
            if (y >= 2)
            {
                Block b1 = Blocks[x, y - 1];
                Block b2 = Blocks[x, y - 2];
                 if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }
            if (y >= 1 && y < gridHeight - 1)
            {
                Block b1 = Blocks[x, y - 1];
                Block b2 = Blocks[x, y + 1];
                 if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }
            if (y < gridHeight - 2)
            {
                Block b1 = Blocks[x, y + 1];
                Block b2 = Blocks[x, y + 2];
                 if (b1 != null && b2 != null && AreBlocksMatchingWithPotentialJoker(b1, b2, type)) return true;
            }
        }

        return false;
    }

    // Helper for WouldCauseMatch to check if two existing blocks match the new type, considering jokers
    private bool AreBlocksMatchingWithPotentialJoker(Block b1, Block b2, Block.BlockType newType)
    {
        // Case 1: Both existing are non-jokers
        if (!b1.IsJoker && !b2.IsJoker)
        {
            return b1.type == newType && b2.type == newType;
        }
        // Case 2: b1 is joker, b2 is non-joker
        else if (b1.IsJoker && !b2.IsJoker)
        {
            return b2.type == newType;
        }
        // Case 3: b1 is non-joker, b2 is joker
        else if (!b1.IsJoker && b2.IsJoker)
        {
            return b1.type == newType;
        }
        // Case 4: Both existing are jokers - they don't form a match with the new block alone
        else
        {
            return false;
        }
    }


    private Block.BlockType GetRandomBlockType(int x, int y)
    {
        Block blockComponent = blockPrefab.GetComponent<Block>();
        if (blockComponent.blockTypes == null || blockComponent.blockTypes.Length == 0)
        {
            Debug.LogError("Block prefab has no block types defined!");
            return default; // Or handle error appropriately
        }

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

        List<Block.BlockTypeData> typesToChooseFrom = validTypes.Count > 0 ? validTypes : invalidTypes;

        if (typesToChooseFrom.Count == 0)
        {
             Debug.LogError($"No block types (valid or invalid) available for position ({x}, {y})! Falling back to first defined type.");
             // Fallback to the first type defined in the prefab if absolutely necessary
             return blockComponent.blockTypes[0].type;
        }


        int totalRarity = 0;
        foreach (var blockTypeData in typesToChooseFrom)
        {
            totalRarity += blockTypeData.rarity;
        }

        float randomValue = Random.value;

        // Rarity-based selection only if totalRarity > 0
        if (rarityInfluence > 0 && randomValue <= rarityInfluence && totalRarity > 0)
        {
            int randomRarity = Random.Range(1, totalRarity + 1);
            int currentRarity = 0;

            foreach (var blockTypeData in typesToChooseFrom)
            {
                currentRarity += blockTypeData.rarity;
                if (randomRarity <= currentRarity)
                {
                    return blockTypeData.type;
                }
            }
             // Fallback within rarity selection if something goes wrong (shouldn't happen)
            return typesToChooseFrom[typesToChooseFrom.Count - 1].type;
        }
        else // Equal chance selection
        {
            int randomIndex = Random.Range(0, typesToChooseFrom.Count);
            return typesToChooseFrom[randomIndex].type;
        }
    }

    private void CreateBlock(int x, int y)
    {
        Vector3 position = new Vector3(x, y, 0);
        GameObject blockObject = Instantiate(blockPrefab, position, Quaternion.identity);
        blockObject.transform.parent = transform;

        Block block = blockObject.GetComponent<Block>();
        Block.BlockType randomType = GetRandomBlockType(x, y);
        block.Initialize(randomType, x, y);
        Blocks[x, y] = block;
        block.SyncTargetPosition(); // Ensure target position is set correctly initially
    }

    private void Update()
    {
        // Prevent interaction if swapping, falling, or animating to powerup
        if (_isSwapping || _isFalling || _activePowerUpAnimations > 0 || !gridActive) return;

        // --- Mouse Input ---
        if (Input.GetMouseButtonDown(0))
        {
            _touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _selectedBlock = GetBlockAtPosition(_touchStart);
        }
        else if (Input.GetMouseButtonUp(0) && _selectedBlock != null)
        {
            Vector2 touchEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ProcessSwipeInput(_touchStart, touchEnd);
            _selectedBlock = null; // Deselect after processing
        }

        // --- Touch Input ---
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
                ProcessSwipeInput(_touchStart, touchEnd);
                _selectedBlock = null; // Deselect after processing
            }
        }
    }

    // Helper to process swipe input from both mouse and touch
    private void ProcessSwipeInput(Vector2 startPos, Vector2 endPos)
    {
        Vector2 direction = endPos - startPos;

        if (direction.magnitude > 0.5f) // Threshold for swipe detection
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
    }


    private Block GetBlockAtPosition(Vector2 position)
    {
        // Adjust position based on the grid's transform if it's not at origin
        Vector3 localPos = transform.InverseTransformPoint(position);
        int x = Mathf.RoundToInt(localPos.x);
        int y = Mathf.RoundToInt(localPos.y);


        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return Blocks[x, y];
        }
        return null;
    }

    private void TrySwapBlocks(Block block, Vector2Int direction)
    {
        if (block == null) return;

        int newX = block.column + direction.x;
        int newY = block.row + direction.y;

        if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
        {
            Block otherBlock = Blocks[newX, newY];
            if (otherBlock != null) // Ensure there is a block to swap with
            {
                 SwapBlocks(block, otherBlock);
            }
        }
    }

    private void SwapBlocks(Block block1, Block block2)
    {
        if (_isSwapping || _isFalling || _activePowerUpAnimations > 0) return; // Prevent swapping during animations

        _isSwapping = true;

        _block1SwappedWith = block1;
        _block2SwappedWith = block2;

        // Swap array positions
        Blocks[block1.column, block1.row] = block2;
        Blocks[block2.column, block2.row] = block1;

        // Swap grid positions (column/row properties)
        int tempColumn = block1.column;
        int tempRow = block1.row;
        block1.column = block2.column;
        block1.row = block2.row;
        block2.column = tempColumn;
        block2.row = tempRow;

        // Animate movement using the existing coroutine
        StartCoroutine(AnimateBlockMovement(block1, block1.transform.position, new Vector3(block1.column, block1.row, 0), swapSpeed, false)); // Don't decrement falling counter
        StartCoroutine(AnimateBlockMovement(block2, block2.transform.position, new Vector3(block2.column, block2.row, 0), swapSpeed, false)); // Don't decrement falling counter


        // Check for matches after the swap animation duration
        Invoke(nameof(CheckMatchesAfterSwap), swapSpeed);
    }

    // Renamed CheckMatches to clarify when it's called
    private void CheckMatchesAfterSwap()
    {
        List<Block> matchingBlocks = FindMatches();
        if (matchingBlocks.Count >= 3)
        {
            // Valid swap, increment counter, process match
            currentSwaps++;
            if (matchCounterText != null)
            {
                matchCounterText.text = (swapLimit - currentSwaps).ToString();
            }
            Debug.Log($"Valid swap performed (Match Found). Current Swaps: {currentSwaps}/{swapLimit}");

            // ProcessMatch is now a coroutine
            StartCoroutine(ProcessMatch(matchingBlocks));

            // Reset swap state immediately after confirming a match
             _isSwapping = false;
            _block1SwappedWith = null;
            _block2SwappedWith = null;

            // Start falling/filling process
            StartCoroutine(ProcessFallingAndCheckMatches());
        }
        else if (_block1SwappedWith != null && _block2SwappedWith != null)
        {
            // Invalid swap, swap back
            SwapBlocksBack(_block1SwappedWith, _block2SwappedWith);
            // _isSwapping is reset in ResetSwapState after the swap back animation
        }
        else
        {
             // Should not happen if swap was initiated correctly, but reset just in case
             ResetSwapState();
        }
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
        StartCoroutine(AnimateBlockMovement(block1, block1.transform.position, new Vector3(block1.column, block1.row, 0), swapSpeed, false));
        StartCoroutine(AnimateBlockMovement(block2, block2.transform.position, new Vector3(block2.column, block2.row, 0), swapSpeed, false));


        // Reset state after animation completes
        Invoke(nameof(ResetSwapState), swapSpeed);
    }

    private void ResetSwapState()
    {
        _isSwapping = false;
        _block1SwappedWith = null;
        _block2SwappedWith = null;
    }

    // Changed to Coroutine to allow delays
    private IEnumerator ProcessMatch(List<Block> matchingBlocks)
    {
        if (matchingBlocks == null || matchingBlocks.Count == 0) yield break; // Use yield break for coroutines

        // --- Step 1: Count Power-ups by Type ---
        Dictionary<PowerUpInventory.PowerUpType, int> powerUpsToGrant = new Dictionary<PowerUpInventory.PowerUpType, int>();
        foreach (Block block in matchingBlocks)
        {
            // Ensure the block hasn't already been processed (e.g., by overlapping matches)
            if (block != null && Blocks[block.column, block.row] == block)
            {
                PowerUpInventory.PowerUpType type = block.GetPowerUpType();
                // Add logic here if Jokers should grant specific/different powerups
                // For now, assume GetPowerUpType() returns the correct type for any block

                if (powerUpsToGrant.ContainsKey(type))
                {
                    powerUpsToGrant[type]++;
                }
                else
                {
                    powerUpsToGrant.Add(type, 1);
                }
            }
        }

        // --- Step 2: Grant Counted Power-ups ---
        if (PowerUpInventory.Instance != null)
        {
            foreach (var kvp in powerUpsToGrant)
            {
                PowerUpInventory.Instance.AddPowerUps(kvp.Key, kvp.Value);
                Debug.Log($"Match Grant: Awarded {kvp.Value} {kvp.Key} power-ups");
            }
        }
        else
        {
            Debug.LogWarning("PowerUpInventory.Instance is null. Cannot grant power-ups.");
        }

        // --- Step 3: Calculate Grouping Point ---
        Vector3 groupCenter = Vector3.zero;
        int validBlockCount = 0;
        foreach (Block block in matchingBlocks)
        {
             // Only consider blocks that are still in the grid for the center calculation
            if (block != null && Blocks[block.column, block.row] == block)
            {
                groupCenter += block.transform.position;
                validBlockCount++;
            }
        }
        if (validBlockCount > 0)
        {
            groupCenter /= validBlockCount;
        }
        // Bring the group center forward visually as well
        groupCenter.z = -1f;


        // --- Step 4: Start Animation for Each Block (Targeting Correctly with Delay) ---
        int animationIndex = 0; // To apply delay progressively
        foreach (Block block in matchingBlocks)
        {
            // Check again if block exists at its position before animating
            if (block != null && Blocks[block.column, block.row] == block)
            {
                // Apply delay before starting the animation for subsequent blocks
                if (animationIndex > 0 && powerUpAnimationDelay > 0)
                {
                    yield return new WaitForSeconds(powerUpAnimationDelay);
                }

                Blocks[block.column, block.row] = null; // Remove from grid array immediately

                // Get the specific power-up type for *this* block to determine animation target
                PowerUpInventory.PowerUpType blockPowerUpType = block.GetPowerUpType();

                // Pass the calculated groupCenter to the animation coroutine
                StartCoroutine(AnimateBlockToPowerUp(block, blockPowerUpType, groupCenter));
                _activePowerUpAnimations++; // Increment animation counter
                animationIndex++; // Increment for the next block's delay calculation
            }
            else if (block != null && block.gameObject != null) // Check if block exists but is not in the grid array (already processed?)
            {
                // This block might have already been cleared by an overlapping match check, just destroy it without animation
                Debug.LogWarning($"Block {block.type} at ({block.column},{block.row}) was already cleared or missing from grid array. Destroying immediately.");
                Destroy(block.gameObject);
            }
        }
        // Note: The coroutine ends here. The ProcessFallingAndCheckMatches coroutine will wait for _activePowerUpAnimations to be 0.
    }


    // Added groupCenter parameter
    private IEnumerator AnimateBlockToPowerUp(Block block, PowerUpInventory.PowerUpType type, Vector3 groupCenter)
    {
        if (block == null)
        {
            _activePowerUpAnimations--; // Decrement if block is already null
            yield break;
        }

        Vector3 startPos = block.transform.position;
        Transform originalParent = block.transform.parent; // Store original parent

        // Reparent to animation panel and bring forward visually
        if (animationPanelParent != null)
        {
            block.transform.SetParent(animationPanelParent, true); // Reparent to the specified panel, keep world position
        }
        else
        {
             Debug.LogError("Animation Panel Parent is null in AnimateBlockToPowerUp! Block will not be reparented.");
        }
        // Ensure block still exists after potential reparenting error log
        if (block == null) {
             _activePowerUpAnimations--; // Decrement if block somehow got destroyed
             yield break;
        }

        Vector3 animationStartPos = new Vector3(startPos.x, startPos.y, -1f); // Use Z=-1 for rendering on top
        block.transform.position = animationStartPos; // Set initial forward position


        Vector3 liftPos = animationStartPos + Vector3.up * liftHeight; // Lift straight up from the forward position
        Vector3 initialScale = block.transform.localScale;
        Vector3 zeroScale = Vector3.zero;

        // --- Phase 1: Lift Up ---
        float timeElapsed = 0f;
        while (timeElapsed < liftDuration)
        {
            if (block == null) yield break; // Exit if block destroyed prematurely
            block.transform.position = Vector3.Lerp(animationStartPos, liftPos, timeElapsed / liftDuration); // Lerp Z as well
            timeElapsed += Time.deltaTime;
            yield return null;
        }
         if (block == null) yield break;
        block.transform.position = liftPos; // Ensure final lift position

        // --- Phase 2: Move Towards PowerUp Button & Shrink (Grouping during movement) ---
        Transform targetTransform = GetPowerUpTargetTransform(type);
        if (targetTransform == null)
        {
            Debug.LogWarning($"No target transform found for PowerUpType {type}. Block will just shrink and disappear.");
            targetTransform = block.transform; // Fallback to self to avoid null ref
        }

        // Declare variables needed for the final phase *before* the loop
        Vector3 powerUpTargetPos; // Calculated world position based on screen target
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
             Debug.LogError("Main Camera not found! Cannot calculate screen positions for power-up animation.");
             if (block != null) Destroy(block.gameObject); // Clean up block
             _activePowerUpAnimations--;
             yield break;
        }

        timeElapsed = 0f; // Reset timer for this phase
        while (timeElapsed < moveToPowerUpDuration)
        {
            if (block == null) yield break;

            float t = timeElapsed / moveToPowerUpDuration;
            float easedT = moveToPowerUpCurve != null && moveToPowerUpCurve.keys.Length > 0 ? moveToPowerUpCurve.Evaluate(t) : t;

            // --- Calculate World Position from UI Target's Screen Position (Recalculate each frame in case UI moves) ---
            // Note: Removed type declarations (Vector3, Canvas, float) as they are declared outside the loop now.
            Vector3 screenPoint = targetTransform.position;
            Canvas targetCanvas = targetTransform.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                 screenPoint = mainCamera.WorldToScreenPoint(targetTransform.position);
            }
            // Using groupCenter's distance might be more consistent now
            float distance = Vector3.Distance(groupCenter, mainCamera.transform.position);
            Vector3 screenPointWithZ = new Vector3(screenPoint.x, screenPoint.y, distance);
            powerUpTargetPos = mainCamera.ScreenToWorldPoint(screenPointWithZ);
            // --- End Calculation ---


            // Calculate position using nested Lerp for curved path:
            // Lerp from liftPos towards an intermediate point that itself lerps from groupCenter to powerUpTargetPos
            Vector3 intermediateTarget = Vector3.Lerp(groupCenter, powerUpTargetPos, easedT);
            block.transform.position = Vector3.Lerp(liftPos, intermediateTarget, easedT);

            // Scale still lerps from initial to zero over the same duration
            block.transform.localScale = Vector3.Lerp(initialScale, zeroScale, easedT);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // --- Cleanup ---
        // No need to reparent as the object is destroyed
        if (block != null)
        {
            Destroy(block.gameObject);
        }
        _activePowerUpAnimations--; // Decrement counter when animation finishes
        if (_activePowerUpAnimations < 0) _activePowerUpAnimations = 0; // Safety check

        // Signal that this block's animation has finished
        OnBlockAnimationFinished?.Invoke(type);
    }

    // Helper to get the correct target transform based on power-up type
    private Transform GetPowerUpTargetTransform(PowerUpInventory.PowerUpType type)
    {
        // Assuming PowerUpInventory.PowerUpType matches the intended logic
        switch (type)
        {
            case PowerUpInventory.PowerUpType.Steps: // Assuming Move maps to Steps
                return movePowerUpTarget;
            case PowerUpInventory.PowerUpType.Sword: // Assuming Attack maps to Sword
                return attackPowerUpTarget;
            case PowerUpInventory.PowerUpType.Shield: // Assuming Defense maps to Shield
                return defensePowerUpTarget;
            case PowerUpInventory.PowerUpType.Trap: // Added Trap type
                return trapPowerUpTarget;
            // Add cases for other power-up types if they exist in PowerUpInventory.PowerUpType
            // case PowerUpInventory.PowerUpType.Health: return healPowerUpTarget;
            default:
                Debug.LogWarning($"No specific target defined for PowerUpInventory.PowerUpType: {type}.");
                return null; // No appropriate target found
        }
    }


    private IEnumerator ProcessFallingAndCheckMatches()
    {
        if (_isFalling) yield break; // Don't run multiple instances

        // Wait for any ongoing power-up animations to finish before starting to fall
        yield return new WaitUntil(() => _activePowerUpAnimations == 0);

        _isFalling = true;
        _activeFallingAnimations = 0; // Reset fall counter for this pass

        bool blocksMovedOrMatched;
        do
        {
            blocksMovedOrMatched = false;
            List<IEnumerator> fallAnimations = new List<IEnumerator>();

            // --- Step 1: Make existing blocks fall ---
            for (int x = 0; x < gridWidth; x++)
            {
                int fallToY = -1; // Lowest empty spot found so far in this column
                for (int y = 0; y < gridHeight; y++) // Iterate bottom-up
                {
                    if (Blocks[x, y] == null && fallToY == -1)
                    {
                        fallToY = y; // Found the first empty spot
                    }
                    else if (Blocks[x, y] != null && fallToY != -1)
                    {
                        // Found a block above an empty spot
                        Block block = Blocks[x, y];
                        Blocks[x, fallToY] = block; // Move block in the array
                        Blocks[x, y] = null;        // Clear original spot
                        block.row = fallToY;        // Update block's row property

                        // Start animation
                        Vector3 startPos = block.transform.position;
                        Vector3 endPos = new Vector3(block.column, block.row, 0);
                        fallAnimations.Add(AnimateBlockMovement(block, startPos, endPos, fallSpeed, true)); // Decrements counter
                        _activeFallingAnimations++;

                        blocksMovedOrMatched = true;
                        fallToY++; // Next empty spot is one higher
                    }
                }
            }

             // --- Step 2: Spawn new blocks ---
            for (int x = 0; x < gridWidth; x++)
            {
                int spawnedCount = 0; // How many blocks spawned in this column for positioning
                for (int y = 0; y < gridHeight; y++) // Check from bottom up for nulls
                {
                    if (Blocks[x, y] == null)
                    {
                        // Spawn new block above the grid
                        Vector3 spawnPos = new Vector3(x, gridHeight + spawnedCount, 0);
                        GameObject blockObject = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
                        blockObject.transform.parent = transform;

                        Block block = blockObject.GetComponent<Block>();
                        Block.BlockType randomType = GetRandomBlockType(x, y); // Get type for the target slot
                        block.Initialize(randomType, x, y); // Initialize with final grid position
                        Blocks[x, y] = block; // Place in array at the target slot

                        // Start fall animation
                        Vector3 endPos = new Vector3(x, y, 0);
                        fallAnimations.Add(AnimateBlockMovement(block, spawnPos, endPos, fallSpeed, true)); // Decrements counter
                        _activeFallingAnimations++;

                        blocksMovedOrMatched = true;
                        spawnedCount++;
                    }
                }
            }


            // --- Step 3: Wait for fall/spawn animations ---
            if (fallAnimations.Count > 0)
            {
                // Start all coroutines for this pass
                foreach (var animCoroutine in fallAnimations)
                {
                    StartCoroutine(animCoroutine);
                }
                // Wait until all falling/spawning animations for this pass are done
                yield return new WaitUntil(() => _activeFallingAnimations == 0);
            }

            // --- Step 4: Check for new matches after falling/spawning ---
            List<Block> newMatches = FindMatches();
            if (newMatches.Count >= 3)
            {
                 Debug.Log($"Cascade Match Found ({newMatches.Count} blocks).");
                 // ProcessMatch is now a coroutine
                 StartCoroutine(ProcessMatch(newMatches));
                 blocksMovedOrMatched = true; // Indicate that changes happened

                 // Wait for these new powerup animations (started by ProcessMatch) before the next loop iteration
                 yield return new WaitUntil(() => _activePowerUpAnimations == 0);
            }


        } while (blocksMovedOrMatched); // Loop if blocks fell, spawned, or new matches were cleared

        _isFalling = false; // All falling and cascading is complete

        // Final check for game state transition based on swaps
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Matching && currentSwaps >= swapLimit)
        {
            GameManager.Instance.UpdateGameState(GameState.Player);
        }
         else if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null. Cannot update game state.");
        }
    }


    // Modified to optionally decrement the falling animation counter and adjust Z for rendering order
    private IEnumerator AnimateBlockMovement(Block block, Vector3 startPos, Vector3 endPos, float duration, bool decrementCounter)
    {
        if (block != null) block.IsFalling = true;
        float timeElapsed = 0f;

        // Bring block forward visually during animation
        Vector3 animationStartPos = new Vector3(startPos.x, startPos.y, -1f); // Use Z=-1 for rendering on top
        Vector3 animationEndPos = new Vector3(endPos.x, endPos.y, -1f); // Keep it forward until the end
        Vector3 finalEndPos = new Vector3(endPos.x, endPos.y, 0f); // Final position at Z=0

        if (block != null) block.transform.position = animationStartPos; // Set initial forward position


        while (timeElapsed < duration)
        {
            if (block == null)
            {
                 // If block destroyed mid-animation, ensure counter is decremented if needed
                 if (decrementCounter)
                 {
                     _activeFallingAnimations--;
                     if (_activeFallingAnimations < 0) _activeFallingAnimations = 0;
                 }
                 yield break;
            }


            float t = timeElapsed / duration;
            // Use fallEaseCurve only for falling/spawning, not for swaps
            float easedT = (decrementCounter && fallEaseCurve != null && fallEaseCurve.keys.Length > 0) ? fallEaseCurve.Evaluate(t) : t;

            block.transform.position = Vector3.LerpUnclamped(animationStartPos, animationEndPos, easedT); // Animate along the forward Z plane

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and state at Z=0
        if (block != null)
        {
             block.transform.position = finalEndPos; // Set final position with Z=0
             block.SyncTargetPosition(); // Sync target position which should be Z=0
             block.IsFalling = false;
        }

        // Decrement counter if this was a falling/spawning animation
        if (decrementCounter)
        {
            _activeFallingAnimations--;
            if (_activeFallingAnimations < 0) _activeFallingAnimations = 0;
        }
    }


    private List<Block> FindMatches()
    {
        HashSet<Block> matchingBlocks = new HashSet<Block>();

        // Check horizontal matches
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 2; /* x incremented inside */)
            {
                Block block1 = Blocks[x, y];
                if (block1 == null) { x++; continue; } // Skip if starting block is null

                List<Block> currentMatch = new List<Block> { block1 };
                Block.BlockType matchType = Block.BlockType.Blue; // Use a default valid type as placeholder
                bool matchTypeDetermined = !block1.IsJoker;
                if(matchTypeDetermined) matchType = block1.type;

                // Look ahead for matches
                for (int i = x + 1; i < gridWidth; i++)
                {
                    Block nextBlock = Blocks[i, y];
                    if (nextBlock == null) break; // End of potential match

                    bool isPotentialMatch = false;
                    if (nextBlock.IsJoker)
                    {
                        isPotentialMatch = true; // Joker always extends potential match
                    }
                    else // nextBlock is not a joker
                    {
                        if (!matchTypeDetermined) // First non-joker determines match type
                        {
                            matchType = nextBlock.type;
                            matchTypeDetermined = true;
                            isPotentialMatch = true;
                        }
                        else // Match type already set
                        {
                            isPotentialMatch = nextBlock.type == matchType;
                        }
                    }

                    if (isPotentialMatch)
                    {
                        currentMatch.Add(nextBlock);
                    }
                    else
                    {
                        break; // Sequence broken
                    }
                }

                // Check if the found sequence is a valid match (length >= 3 and contains at least one non-joker if jokers are involved)
                bool containsNonJoker = currentMatch.Any(b => !b.IsJoker);
                if (currentMatch.Count >= 3 && (containsNonJoker || currentMatch.All(b => b.IsJoker))) // Allow all-joker matches if desired, otherwise require containsNonJoker
                {
                    foreach (Block b in currentMatch)
                    {
                        matchingBlocks.Add(b);
                    }
                    x += currentMatch.Count; // Skip checked blocks
                }
                else
                {
                    x++; // Move to the next block
                }
            }
        }

        // Check vertical matches (similar logic)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight - 2; /* y incremented inside */)
            {
                 Block block1 = Blocks[x, y];
                if (block1 == null) { y++; continue; }

                List<Block> currentMatch = new List<Block> { block1 };
                Block.BlockType matchType = Block.BlockType.Blue; // Use a default valid type as placeholder
                bool matchTypeDetermined = !block1.IsJoker;
                 if(matchTypeDetermined) matchType = block1.type;

                for (int i = y + 1; i < gridHeight; i++)
                {
                    Block nextBlock = Blocks[x, i];
                    if (nextBlock == null) break;

                    bool isPotentialMatch = false;
                    if (nextBlock.IsJoker)
                    {
                        isPotentialMatch = true;
                    }
                    else
                    {
                        if (!matchTypeDetermined)
                        {
                            matchType = nextBlock.type;
                            matchTypeDetermined = true;
                            isPotentialMatch = true;
                        }
                        else
                        {
                            isPotentialMatch = nextBlock.type == matchType;
                        }
                    }

                    if (isPotentialMatch)
                    {
                        currentMatch.Add(nextBlock);
                    }
                    else
                    {
                        break;
                    }
                }

                bool containsNonJoker = currentMatch.Any(b => !b.IsJoker);
                if (currentMatch.Count >= 3 && (containsNonJoker || currentMatch.All(b => b.IsJoker)))
                {
                    foreach (Block b in currentMatch)
                    {
                        matchingBlocks.Add(b);
                    }
                    y += currentMatch.Count;
                }
                else
                {
                    y++;
                }
            }
        }


        return matchingBlocks.ToList();
    }

    // This specific function might not be needed anymore with the improved FindMatches logic
    // private bool AreBlocksMatching(Block block1, Block block2)
    // {
    //     if (block1 == null || block2 == null) return false;
    //     // If neither block is a joker, just check if they're the same type
    //     if (!block1.IsJoker && !block2.IsJoker)
    //     {
    //         return block1.type == block2.type;
    //     }
    //     // If at least one is a joker, they are considered matching in a sequence context
    //     return true;
    // }

}
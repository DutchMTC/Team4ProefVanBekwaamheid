---

# Match3 System

![Mechanics Flowcharts - Match3 System(](https://github.com/user-attachments/assets/98195fdd-0beb-43dc-a7a8-b64d2eebad99)
[Zoomable Flowchart Image](https://i.imgur.com/i6mDBHR.jpeg)

## 1. System Overview

The Match3 system is a core gameplay mechanic that allows players to swap adjacent blocks to create matches of 3 or more of the same color. When matches are created, blocks are removed, new blocks fall from the top, and special rewards may be granted. The system includes special "joker" blocks that can substitute for any color when creating matches.

## 2. Core Components

### 2.1 Block
- Represents an individual tile in the Match3 grid
- Stores information about type, position, and visual representation
- Handles its own movement and animation
- Implements joker functionality for special power-up blocks

### 2.2 GridManager
- Manages the entire grid of blocks
- Handles user input for selecting and swapping blocks
- Controls game flow (initialization, matching, falling, refilling)
- Implements matching algorithms and special case handling

## 3. Block Types and Properties

### 3.1 Basic Block Types
- Blue
- Red
- Green
- Yellow
- Purple

### 3.2 Special Joker Blocks
- JokerSword
- JokerShield
- JokerSteps
- JokerHealth

### 3.3 Block Properties
- **type**: The visual and functional type of the block
- **column/row**: Position coordinates in the grid
- **rarity**: Value between 1-100 affecting spawn frequency (higher = more rare)
- **isJoker**: Whether the block can substitute for any color in matches
- **powerUpType**: What power-up is provided when joker blocks are matched

## 4. Grid Management

### 4.1 Grid Initialization
- Grid size is configurable (default: 8x8)
- Blocks are created with random types
- Initial block placement prevents automatic matches

### 4.2 Block Creation
```csharp
private void CreateBlock(int x, int y)
{
    GameObject blockObject = Instantiate(blockPrefab, new Vector3(x, y, 0), Quaternion.identity);
    blockObject.transform.parent = transform;

    Block block = blockObject.GetComponent<Block>();
    Block.BlockType randomType = GetRandomBlockType(x, y);
    block.Initialize(randomType, x, y);
    blocks[x, y] = block;
}
```

### 4.3 Rarity-Based Block Selection
- Uses a two-tier selection system:
  - Base chance for all blocks (1-rarityInfluence)
  - Remaining probability distributed based on rarity values
- Configurable rarityInfluence value from 0-1 controls impact of rarity

## 5. Matching Mechanics

### 5.1 Match Detection
- Checks for horizontal and vertical matches of 3 or more blocks
- Supports joker blocks substituting for any color
- Tracks all matching blocks for removal

### 5.2 Joker Matching Rules
- Joker blocks only count as a match when connecting blocks of the same color
- For a match to occur with a joker, the other blocks must match each other
- Examples:
  - Red-Joker-Red: Valid match
  - Red-Joker-Blue: Not a valid match
  - Red-Red-Joker: Valid match

### 5.3 Match Handling
```csharp
private void CheckMatches()
{
    List<Block> matchingBlocks = FindMatches();
    if (matchingBlocks.Count >= 3)
    {
        // Special handling for joker matches
        foreach (Block block in matchingBlocks)
        {
            // Destroy matched blocks
        }
        StartFalling();
    }
    else if (blocks were swapped)
    {
        SwapBlocksBack();
    }
}
```

## 6. User Interaction

### 6.1 Input Handling
- Supports both mouse and touch input
- Block selection and dragging to indicate swap direction
- Direction determination based on angle of swipe

### 6.2 Block Swapping
- Swaps adjacent blocks based on user input
- Animates block movement during swaps
- Reverts invalid swaps that don't create matches
- Prevents interaction during animations

## 7. Block Falling and Refilling\

### 7.1 Gravity System
- After matches are removed, blocks above fall down to fill empty spaces
- Blocks move gradually with animation to their new positions

### 7.2 Grid Refilling
- Empty spaces at the top are filled with newly created blocks
- New blocks fall from above the visible grid
- Continues until the grid is completely filled
- Checks for new matches after falling completes

## 8. Special Features

### 8.1 Joker Block Rewards
- When joker blocks are matched, they provide special power-ups
- The type of power-up depends on the joker type
- Power-up quantity scales with the size of the match

### 8.2 Match Prevention Logic
- Sophisticated algorithm prevents creating automatic matches when:
  - Initializing the grid
  - Creating new blocks
  - Special handling for joker blocks to prevent auto-matching

## 9. Technical Considerations

### 9.1 Performance Optimization
- Uses HashSet for efficient tracking of matched blocks
- Reuses block objects rather than destroying and recreating

### 9.2 Extensibility
- System designed to be easily extended with new block types
- Configuration through Unity Inspector for designer-friendly tweaking
- Serialize fields for block properties like rarity and visual appearance

---
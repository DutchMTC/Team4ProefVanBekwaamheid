---

# PowerUps System

![Placeholder for PowerUps Flowchart](https://via.placeholder.com/600x400?text=PowerUps+Flowchart)
[Zoomable Flowchart Image (Placeholder)](https://i.imgur.com/placeholder.jpeg)

## 1. System Overview

The PowerUps system manages the player's power-up collection, tracks how many of each power-up the player has, and determines when they can be used. It also handles updating the visual display of power-ups in the user interface and triggers their effects when activated by the player. The system supports different states for power-ups (unusable, usable, charged, and supercharged) which affect their visual representation and potentially their effects.

## 2. Core Components

### 2.1 PowerUp Inventory
- Keeps track of the quantity of each type of power-up the player possesses.
- Provides methods for adding, using, and checking the count of power-ups.
- Notifies other parts of the system when power-up counts change.

### 2.2 PowerUp Manager
- Connects the power-up inventory to the user interface.
- Listens for changes in power-up counts and updates the visual state of power-up buttons.
- Handles player interaction with power-up buttons.
- Determines the current state (usable, charged, supercharged) of a power-up based on its count.
- Triggers the specific effects of a power-up when it is used.

### 2.3 Specific Power-up Effects
- Individual scripts or logic that define what happens in the game when a specific power-up is activated.
- Examples include applying damage, granting movement, placing obstacles, or providing defensive buffs.

## 3. Power-up Types and States

### 3.1 Power-up Types
- Sword: Typically associated with offensive actions.
- Shield: Typically associated with defensive actions.
- Steps: Typically associated with movement or positioning.
- Wall: Typically associated with placing obstacles.

### 3.2 Power-up States
- **Unusable**: The player does not have enough of this power-up to use it.
- **Usable**: The player has the minimum required amount to use the basic effect.
- **Charged**: The player has accumulated a significant amount, potentially unlocking a stronger effect.
- **Supercharged**: The player has accumulated a large amount, potentially unlocking the most powerful effect.

### 3.3 State Thresholds
- **Usable**: Requires 1 power-up.
- **Charged**: Requires 15 power-ups.
- **Supercharged**: Requires 25 power-ups.

## 4. User Interaction and Visuals

### 4.1 UI Representation
- Power-ups are represented by buttons in the user interface.
- The appearance of the button (sprite and fill amount) changes to reflect the current state and progress towards the next state.

### 4.2 Using Power-ups
- Players click on a power-up button to attempt to use it.
- If the power-up is in a Usable, Charged, or Supercharged state, its effect is triggered, and the count is reset to zero.
- The UI is instantly updated to reflect the reset count and the Unusable state.

## 5. Power-up Effects

### 5.1 Sword Power-up
- Allows the user to attack a target within a certain range.
- The range and damage dealt depend on the power-up state (Usable, Charged, Supercharged).
- For players, initiates a tile selection process to choose a target.
- For AI, attempts to attack the player directly if in range.

### 5.2 Shield Power-up
- Applies a temporary damage reduction buff to the user.
- The amount of damage reduction depends on the power-up state.

### 5.3 Movement Power-up
- Allows the user to move to a new tile within a certain range.
- The movement range depends on the power-up state.
- For players, initiates a tile selection process to choose a destination tile.
- For AI, attempts to find the best tile to move towards the target.

### 5.4 Wall Power-up
- Allows the user to place a wall obstacle on an empty tile within a certain range.
- The range is currently fixed regardless of the power-up state.
- For players, initiates a tile selection process to choose a tile for the wall.
- For AI, attempts to find the best tile adjacent to the enemy (closest to the player) to place the wall.

## 6. Integration

- The PowerUps system interacts with the Match3 system to receive power-ups when special blocks are matched.
- It interacts with the UI system to display power-up status and handle button clicks.
- Specific power-up effects interact with the grid system, tile selection, and tile occupants to perform actions in the game world.
- A GameManager or similar system can control when power-up visuals are enabled or disabled (e.g., during player vs. enemy turns).

---
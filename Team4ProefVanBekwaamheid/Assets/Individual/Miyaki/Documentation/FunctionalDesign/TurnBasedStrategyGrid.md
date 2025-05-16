---

# Turn Based Strategy Grid System

![Placeholder for Turn Based Strategy Grid Flowchart](https://via.placeholder.com/600x400?text=Turn+Based+Strategy+Grid+Flowchart)
[Zoomable Flowchart Image (Placeholder)](https://i.imgur.com/placeholder.jpeg)

## 1. System Overview

The Turn Based Strategy Grid system is responsible for creating and managing the grid of tiles that serves as the battlefield. It handles the placement and movement of various entities (occupants) on these tiles, such as players, enemies, obstacles, and items. The system also incorporates basic mechanics for managing occupant properties like health and defense.

## 2. Core Components

### 2.1 GridGenerator
- Creates the grid structure based on configurable dimensions and tile properties.
- Manages the instantiation and arrangement of individual tiles in the game scene.
- Provides functionality to regenerate the grid.

### 2.2 TileSettings
- Represents an individual tile within the grid.
- Stores its grid coordinates (X and Y).
- Tracks the type of occupant currently on the tile (Player, Enemy, Obstacle, Item, None).
- Holds a reference to the GameObject occupying the tile.
- Manages the visual appearance of the tile based on its occupation status.

### 2.3 TileOccupants
- Represents any entity that can be placed on a tile (players, enemies, items, etc.).
- Stores its current grid coordinates.
- Manages basic properties like health and defense.
- Handles movement logic, including finding target tiles and updating its position.
- Interacts with `TileSettings` to update tile occupation status.
- Includes methods for taking damage, healing, and handling death.

### 2.4 MovementValidator
- (Intended) Responsible for defining and validating rules for occupant movement.
- Will determine if a move to a specific tile is permissible based on factors like obstacles, other occupants, and movement range.

## 3. Grid Structure and Management

### 3.1 Grid Representation
- The grid is represented by a collection of `TileSettings` GameObjects, typically parented under the `GridGenerator`.
- Each tile's position and occupation status are managed by its `TileSettings` component.

### 3.2 Grid Generation
- The grid is generated programmatically by the `GridGenerator` based on specified width and height.
- Tiles are instantiated from a prefab and positioned to create an isometric view.

### 3.3 Tile Occupation
- Each tile can be occupied by a single entity at a time, defined by the `OccupantType` enum in `TileSettings`.
- The `SetOccupant` method in `TileSettings` is used to update the tile's occupant and trigger visual changes.

## 4. Occupant Mechanics

### 4.1 Movement
- Occupants can move to different tiles on the grid.
- The `MoveToTile` method in `TileOccupants` handles the process of finding the target tile, validating the move (basic validation currently implemented), updating the occupant's world position, and updating the occupation status of both the old and new tiles.
- Includes logic for interacting with items on the destination tile.

### 4.2 Health and Damage
- `TileOccupants` have health and defense properties.
- The `TakeDamage` method applies damage, considering damage reduction.
- The `Heal` method restores health.
- The `Die` method handles the occupant's removal when health reaches zero.

## 5. User Interaction

- Basic input for grid regeneration is handled by the `GridGenerator`.
- Input for moving `TileOccupants` is expected to be handled by an external system that calls the `MoveToTile` method.

## 6. Extensibility

- New occupant types can be added by extending the `OccupantType` enum and updating relevant logic in `TileSettings` and `TileOccupants`.
- New tile properties and behaviors can be added to the `TileSettings` script.
- The `MovementValidator` class is a dedicated point for implementing complex movement rules.

---
# PowerUps System

---

## 1. Architecture Overview

The PowerUps system is responsible for managing the player's power-up inventory, tracking quantities, determining their usage state (unusable, usable, charged, supercharged), updating the corresponding UI visuals, and triggering the activation of power-up effects when used. It is primarily composed of two main scripts:

-   [`PowerUpInventory.cs`](Code/Scripts/Core/PowerUpInventory.cs): Manages the raw counts of each power-up type and notifies listeners when counts change.
-   [`PowerUpManager.cs`](Code/Scripts/Core/PowerUpManager.cs): Acts as the interface between the inventory, the UI, and the power-up effect logic. It listens for inventory changes, updates button visuals, handles button clicks, and delegates effect activation.
-   Specific Power-up Effect Scripts (e.g., [`AttackPowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/AttackPowerUp.cs), [`MovementPowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/MovementPowerUp.cs)): Implement the actual game logic and effects when a power-up is used.

```
+-------------------+     notifies     +-----------------+
|                   |----------------->|                 |
| PowerUpInventory  |                  | PowerUpManager  |
|                   |<-----------------|                 |
+---------+---------+     updates      +--------+--------+
          |                                    |
          |                                    | handles
          |                                    |
          |                                    |
          |                                    v
          |                               +----+-----+
          |                               | UI Buttons|
          |                               +----+-----+
          |                                    |
          |                                    | triggers
          |                                    |
          |                                    v
          |                           +--------+--------+
          |                           | Specific PowerUp|
          |                           | Effect Scripts  |
          |                           +-----------------+
          |
          | used by
          |
          v
+-----------------+
|   GridManager   |
+-----------------+
```

## 2. Class Structure

### 2.1 PowerUpInventory Class

The [`PowerUpInventory`](Code/Scripts/Core/PowerUpInventory.cs) class is a singleton MonoBehaviour that holds the current count for each type of power-up. It provides methods to add, get, use, set, and decrease power-up counts. It also exposes a static event [`OnPowerUpCountChanged`](Code/Scripts/Core/PowerUpInventory.cs:10) that is invoked whenever the count of any power-up type changes, allowing other systems (like `PowerUpManager`) to react.

```csharp
using UnityEngine;
using System; // Added for Action

public class PowerUpInventory : MonoBehaviour
{
    public static PowerUpInventory Instance { get; private set; }

    // Event to notify listeners when a power-up count changes
    // Passes the type and the new count
    public static event Action<PowerUpType, int> OnPowerUpCountChanged;

    public enum PowerUpType
    {
        Sword,
        Shield,
        Steps,
        Wall
    }

    [SerializeField] private int _swordCount = 0;
    [SerializeField] private int _shieldCount = 0;
    [SerializeField] private int _stepsCount = 0;
    [SerializeField] private int _wallCount = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddPowerUps(PowerUpType type, int amount) { /* ... */ }
    public int GetPowerUpCount(PowerUpType type) { /* ... */ }
    public void UsePowerUp(PowerUpType type) { /* ... */ }
    public void SetPowerUpCount(PowerUpType type, int count) { /* ... */ }
    public void DecreasePowerUpCount(PowerUpType type, int amount) { /* ... */ }
    public void ClearAllPowerUps() { /* ... */ }
    private void LogInventory() { /* ... */ }
}
```

### 2.2 PowerUpManager Class

The [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) class is a MonoBehaviour responsible for the visual representation and usage logic of power-ups. It holds references to the UI button images, fill images, and sprite assets for each power-up type. It subscribes to the [`PowerUpInventory.OnPowerUpCountChanged`](Code/Scripts/Core/PowerUpInventory.cs:10) event (indirectly via `GridManager.OnBlockAnimationFinished` in the provided code, which then triggers visual updates) and updates the UI based on the current power-up counts and defined thresholds. It also handles button click events to trigger the `TryUsePowerUp` logic and activate the corresponding power-up effects via dedicated power-up scripts.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps;

[Serializable] // Make it visible in the Inspector
public class PowerUpSprites
{
    public Sprite unusable;
    public Sprite usable;
    public Sprite charged;
    public Sprite supercharged;
}

public enum PowerUpState
{
    Unusable,
    Usable,
    Charged,
    Supercharged
}

// Enum to identify who is using the power-up
public enum PowerUpUser
{
    Player,
    Enemy
}

public class PowerUpManager : MonoBehaviour
{
    public PowerUpInventory powerUpInventory;

    [Header("UI Button Images")]
    public Image swordButtonImage;
    public Image shieldButtonImage;
    public Image wallButtonImage;
    public Image stepsButtonImage;

    [Header("UI Fill Images (Set Type to Filled)")]
    public Image swordFillImage;
    public Image shieldFillImage;
    public Image wallFillImage;
    public Image stepsFillImage;

    [Header("PowerUp Specific Sprites")]
    public PowerUpSprites swordSprites;
    public PowerUpSprites shieldSprites;
    public PowerUpSprites wallSprites;
    public PowerUpSprites stepsSprites;

    [Header("Animation Settings")]
    [SerializeField] private float fillAnimationDuration = 0.25f;
    [SerializeField] private AnimationCurve fillAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // Dictionaries to map PowerUpType to UI elements and state
    private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites> powerUpSpriteMap;
    private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpFillImageMap;
    private Dictionary<PowerUpInventory.PowerUpType, Coroutine> fillCoroutines;
    private Dictionary<PowerUpInventory.PowerUpType, PowerUpState> powerUpPreviousStates;
    private Dictionary<PowerUpInventory.PowerUpType, Button> powerUpButtonMap; // Map type to Button component
    private bool _isUsingPowerUp = false; // Flag to prevent event-driven update during use

    // Constants for State Thresholds
    private const int USABLE_THRESHOLD = 1;
    private const int CHARGED_THRESHOLD = 15;
    private const int SUPERCHARGED_THRESHOLD = 25;

    // Power Up References
    [SerializeField] private MovementPowerUp _movementPowerUp; // Reference to the PowerUpInventory script
    [SerializeField] private AttackPowerUp _attackPowerUp;
    [SerializeField] private WallPowerUp _wallPowerUp;
    [SerializeField] private DefensePowerUp _defensePowerUp; // Reference to the PowerUpInventory script


    private void Awake() { /* ... */ }
    private void OnDestroy() { /* ... */ }
    private void HandleBlockAnimationFinished(PowerUpInventory.PowerUpType type) { /* ... */ } // Subscribed to GridManager event
    private PowerUpState DetermineStateFromCount(int count) { /* ... */ }
    public PowerUpState GetCurrentUsageState(PowerUpInventory.PowerUpType type) { /* ... */ }
    public void UpdatePowerUpVisual(PowerUpInventory.PowerUpType type, bool instantReset = false, bool instantUpdate = false) { /* ... */ }
    private IEnumerator AnimateFillAmount(...) { /* ... */ }
    private IEnumerator AnimateFillFromZero(...) { /* ... */ }
    public void UpdateAllPowerUpVisuals(bool instantReset = false, bool instantUpdate = false) { /* ... */ }
    private Sprite GetSpriteForState(PowerUpSprites sprites, PowerUpState state) { /* ... */ }
    public void SetButtonsInteractable(bool interactable) { /* ... */ }
    public bool TryUsePowerUp(PowerUpInventory.PowerUpType type) { /* ... */ }

    // Public methods for UI Button OnClick events
    public void UseSwordPowerUp() { /* ... */ }
    public void UseShieldPowerUp() { /* ... */ }
    public void UseWallPowerUp() { /* ... */ }
    public void UseStepsPowerUp() { /* ... */ }

    private void ActivateEffect(PowerUpInventory.PowerUpType type, PowerUpState state) { /* ... */ }

    // Consolidated Effect Handlers
    private void HandleSword(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user) { /* ... */ }
    private void HandleShield(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user) { /* ... */ }
    private void HandleWall(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user) { /* ... */ }
    private void HandleSteps(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user) { /* ... */ }

    public void SetVisualsToUnusable() { /* ... */ }
    public void RestoreVisualsFromInventory() { /* ... */ }
}
```

### 2.3 PowerUpSprites Class

A simple serializable class used by [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) to group the different sprite variations (unusable, usable, charged, supercharged) for a single power-up type, allowing them to be configured easily in the Unity Inspector.

```csharp
[Serializable]
public class PowerUpSprites
{
    public Sprite unusable;
    public Sprite usable;
    public Sprite charged;
    public Sprite supercharged;
}
```

### 2.4 Specific Power-up Effect Classes

These scripts implement the actual game logic that occurs when a specific power-up is used. They are called by the [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) via dedicated handler methods.

#### 2.4.1 AttackPowerUp Class

The [`AttackPowerUp`](Code/Scripts/TurnBasedStrategy/PowerUps/AttackPowerUp.cs) script handles the logic for the Sword power-up. It determines the attack range and damage based on the power-up state (Usable, Charged, Supercharged) and initiates a tile selection process for the player to choose a target. For AI users, it attempts to find and attack the player directly.

```csharp
using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using static PowerUpManager;
using UnityEngine.Tilemaps;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class AttackPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range = 1;
        [SerializeField] private int _baseDamage = 10;
        private int _currentDamage;
        private TileSelection _tileSelection;
        private TileOccupants _tileOccupants; // The user of the powerup
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        void Start() { /* ... */ }

        // Added optional targetOccupant parameter for AI
        public void AttackPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    _currentDamage = _baseDamage;
                    break;
                case PowerUpState.Charged:
                    _range = 1;
                    _currentDamage = _baseDamage * 2;
                    break;
                case PowerUpState.Supercharged:
                    _range = 2;
                    _currentDamage = _baseDamage * 3;
                    break;
            }

            if (_isWaitingForSelection) { /* ... cancel selection ... */ return; }

            // Start tile selection process to find valid tiles
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Attack, userType);

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                if (playerTile != null && IsTileInRange(playerTile))
                {
                     Debug.Log($"Enemy AI (Attack): Attacking player at ({playerTile.gridY}, {playerTile.gridX})");
                     Attack(playerTile);
                }
                else { /* ... warning ... */ }
                _tileSelection.CancelTileSelection(); // Clean up selection state
            }
            else // Player waits for input
            {
                _isWaitingForSelection = true;
                _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            }
        }

        private void HandleTileSelected(TileSettings selectedTile) { /* ... player logic ... */ }
        private bool IsTileInRange(TileSettings targetTile) { /* ... range check ... */ }
        private void Attack(TileSettings targetTile) { /* ... apply damage ... */ }
        void OnDestroy() { /* ... remove listener ... */ }
    }
}
```

#### 2.4.2 DefensePowerUp Class

The [`DefensePowerUp`](Code/Scripts/TurnBasedStrategy/PowerUps/DefensePowerUp.cs) script applies a damage reduction buff to the user when the Shield power-up is activated. The amount of damage reduction is determined by the power-up state.

```csharp
using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using static PowerUpManager;

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class DefensePowerUp : MonoBehaviour
    {
        [SerializeField] private float _baseReduction = 0.3f; // 30% damage reduction base
        private float _currentReduction;
        private TileOccupants _tileOccupants;
        private bool _isActive = false;
        private TileSelection.UserType _currentUser;

        void Start() { /* ... */ }

        public void DefensePowerUpSelected(PowerUpState state, TileSelection.UserType userType)
        {
            _currentUser = userType;
            _isActive = true;

            // Set damage reduction based on power up state
            switch (state)
            {
                case PowerUpState.Usable:
                    _currentReduction = _baseReduction; // 30% reduction
                    break;
                case PowerUpState.Charged:
                    _currentReduction = _baseReduction * 1.5f; // 45% reduction
                    break;
                case PowerUpState.Supercharged:
                    _currentReduction = _baseReduction * 2f; // 60% reduction
                    break;
            }

            // Apply the defense buff
            if (_tileOccupants != null)
            {
                _tileOccupants.SetDamageReduction(_currentReduction);
                Debug.Log($"{_currentUser} activated defense power-up with {_currentReduction * 100}% damage reduction!");
            }
        }

        void OnDestroy() { /* ... remove buff ... */ }
    }
}
```

#### 2.4.3 MovementPowerUp Class

The [`MovementPowerUp`](Code/Scripts/TurnBasedStrategy/PowerUps/MovementPowerUp.cs) script allows the user to move to a new tile within a certain range, determined by the power-up state (Usable, Charged, Supercharged). It initiates a tile selection process for the player. For AI users, it finds the best available tile to move towards the target.

```csharp
using UnityEngine;
using Team4ProefVanBekwaakheid.TurnBasedStrategy;
using System.Collections.Generic; // Added for List<>

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class MovementPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // Reference to the TileOccupants script (the user of the powerup)
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        void Start() { /* ... */ }

        // Added optional targetOccupant parameter for AI
        public void MovementPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
            _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            // Movement moet hierin aangeroepen worden en range moet hierin bepaald worden
            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1; // Set range for usable state
                    break;
                case PowerUpState.Charged:
                    _range = 2; // Set range for charged state
                    break;
                case PowerUpState.Supercharged:
                    _range = 3; // Set range for supercharged state
                    break;
            }

            if (_isWaitingForSelection) { /* ... cancel selection ... */ return; }

            // Start tile selection process to find valid tiles
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement, userType);

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                TileSettings bestTile = FindBestMoveTileTowardsTarget(selectableTiles, _targetOccupantForAI);

                if (bestTile != null)
                {
                    Debug.Log($"Enemy AI (Movement): Moving towards player at ({_targetOccupantForAI.gridY}, {_targetOccupantForAI.gridX}). Best tile: ({bestTile.gridY}, {bestTile.gridX})");
                    Move(bestTile);
                }
                else { /* ... warning ... */ }
                _tileSelection.CancelTileSelection(); // Clean up selection state
            }
            else // Player waits for input
            {
                _isWaitingForSelection = true;
                _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            }
        }

        private void HandleTileSelected(TileSettings selectedTile) { /* ... player logic ... */ }
        private void Move(TileSettings targetTile) { /* ... move logic ... */ }
        private TileSettings FindBestMoveTileTowardsTarget(List<TileSettings> selectableTiles, TileOccupants target) { /* ... AI logic ... */ }
        void OnDestroy() { /* ... remove listener ... */ }
    }
}
```

#### 2.4.4 WallPowerUp Class

The [`WallPowerUp`](Code/Scripts/TurnBasedStrategy/PowerUps/WallPowerUp.cs) script allows the user to place a wall on an empty tile within a certain range. The range is currently fixed regardless of the power-up state. It initiates a tile selection for the player. For AI users, it attempts to find the best tile adjacent to the enemy (closest to the player) to place the wall.

```csharp
using UnityEngine;
using Team4ProefVanBekwaamheid.TurnBasedStrategy;
using System.Collections.Generic; // Added for List<>

namespace Team4ProefVanBekwaamheid.TurnBasedStrategy.PowerUps
{
    public class WallPowerUp : MonoBehaviour
    {
        [SerializeField] private int _range; // The range of the power-up
        [SerializeField] private GameObject _wallPrefab; // The prefab to place as a wall
        [SerializeField] private Vector3 _positionOffset = Vector3.zero; // Offset for the wall's spawn position
        private TileSelection _tileSelection; // Reference to the TileSelection script
        private TileOccupants _tileOccupants; // The user of the powerup
        private bool _isWaitingForSelection = false;
        private TileSelection.UserType _currentUserType; // Store the user type
        private TileOccupants _targetOccupantForAI; // Store the target for AI

        void Start() { /* ... */ }

        // Added optional targetOccupant parameter for AI
        public void WallPowerUpSelected(PowerUpState _state, TileSelection.UserType userType, TileOccupants targetOccupant = null)
        {
             _currentUserType = userType; // Store user type
            _targetOccupantForAI = targetOccupant; // Store target for AI

            switch (_state)
            {
                case PowerUpState.Usable:
                    _range = 1;
                    break;
                case PowerUpState.Charged:
                    _range = 1;
                    break;
                case PowerUpState.Supercharged:
                    _range = 1;
                    break;
            }

            if (_isWaitingForSelection) { /* ... cancel selection ... */ return; }

            // Start tile selection process to find valid empty tiles within range
            Vector2Int currentPos = new Vector2Int(_tileOccupants.gridX, _tileOccupants.gridY);
            _tileSelection.StartTileSelection(_range, currentPos, TileSelection.SelectionType.Movement, userType); // Movement type finds empty tiles

            if (userType == TileSelection.UserType.Enemy && _targetOccupantForAI != null)
            {
                // AI executes immediately
                 // ... logging ...
                 TileSettings playerTile = _targetOccupantForAI.GetCurrentTile();
                 List<TileSettings> selectableTiles = _tileSelection.GetSelectableTiles();
                 // ... logging ...

                 if (playerTile == null) { /* ... warning ... */ }

                 TileSettings bestTile = FindBestWallPlacementTile(selectableTiles, playerTile);

                 if (bestTile != null)
                 {
                     // ... logging ...
                     PlaceWall(bestTile);
                 }
                 else { /* ... warning ... */ }
                _tileSelection.CancelTileSelection(); // Clean up selection state
            }
            else // Player waits for input
            {
                _isWaitingForSelection = true;
                _tileSelection.OnTileSelected.AddListener(HandleTileSelected);
            }
        }

        private void HandleTileSelected(TileSettings selectedTile) { /* ... player logic ... */ }
        private void PlaceWall(TileSettings targetTile) { /* ... place wall logic ... */ }
        private TileSettings FindBestWallPlacementTile(List<TileSettings> selectableTiles, TileSettings targetPlayerTile) { /* ... AI logic ... */ }
        private int CalculateManhattanDistance(Vector2Int posA, Vector2Int posB) { /* ... distance calculation ... */ }
        void OnDestroy() { /* ... remove listener ... */ }
    }
}
```

## 3. Data Structures and Algorithms

### 3.1 Power-up Count Storage

[`PowerUpInventory`](Code/Scripts/Core/PowerUpInventory.cs) stores the counts of each power-up type using private integer fields (`_swordCount`, `_shieldCount`, etc.). Access and modification are done through public methods that also trigger the [`OnPowerUpCountChanged`](Code/Scripts/Core/PowerUpInventory.cs:10) event.

### 3.2 State Determination

[`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) determines the `PowerUpState` (Unusable, Usable, Charged, Supercharged) based on the current count of a power-up type using predefined thresholds (`USABLE_THRESHOLD`, `CHARGED_THRESHOLD`, `SUPERCHARGED_THRESHOLD`). The [`DetermineStateFromCount`](Code/Scripts/Core/PowerUpManager.cs:183) and [`GetCurrentUsageState`](Code/Scripts/Core/PowerUpManager.cs:191) methods implement this logic.

```csharp
private PowerUpState DetermineStateFromCount(int count)
{
    if (count >= SUPERCHARGED_THRESHOLD) return PowerUpState.Supercharged;
    if (count >= CHARGED_THRESHOLD) return PowerUpState.Charged;
    if (count >= USABLE_THRESHOLD) return PowerUpState.Usable;
    return PowerUpState.Unusable;
}
```

### 3.3 UI Mapping

[`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) uses several Dictionaries to map `PowerUpInventory.PowerUpType` enum values to their corresponding UI elements (Images, Buttons) and sprite assets (`PowerUpSprites`). This allows for easy lookup and management of the visual components associated with each power-up type.

```csharp
private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpImageMap;
private Dictionary<PowerUpInventory.PowerUpType, PowerUpSprites> powerUpSpriteMap;
private Dictionary<PowerUpInventory.PowerUpType, Image> powerUpFillImageMap;
private Dictionary<PowerUpInventory.PowerUpType, Coroutine> fillCoroutines;
private Dictionary<PowerUpInventory.PowerUpType, PowerUpState> powerUpPreviousStates;
private Dictionary<PowerUpInventory.PowerUpType, Button> powerUpButtonMap;
```

### 3.4 Fill Animation

The visual filling of the power-up buttons is handled by coroutines (`AnimateFillAmount`, `AnimateFillFromZero`) in [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs). These coroutines smoothly interpolate the `fillAmount` property of the UI Image component over a specified duration, using an `AnimationCurve` for easing. When a state threshold is crossed (e.g., Usable to Charged), a two-phase animation can occur: the current fill animates to 1.0, the sprites instantly switch to the next state's visuals, and then the new fill animates from 0.0 to the target amount for that state.

```csharp
private IEnumerator AnimateFillAmount(
    PowerUpInventory.PowerUpType type,
    Image image,
    Image bgImage,
    float targetFillAmount,
    bool showDuringAnimation,
    bool triggerPostAnimationSwitch,
    Sprite postAnimBgSprite,
    Sprite postAnimFillSprite,
    float postAnimFillAmount,
    bool postAnimShowFill)
{
    // ... animation logic ...
    yield return null;
    // ... post animation logic ...
}

private IEnumerator AnimateFillFromZero(PowerUpInventory.PowerUpType type, Image image, float targetFillAmount, float duration, bool finalVisibility)
{
    // ... animation logic ...
    yield return null;
    // ... final state logic ...
}
```

## 4. State Management and Visuals

The [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) keeps track of the previous state of each power-up type in the `powerUpPreviousStates` dictionary. The [`UpdatePowerUpVisual`](Code/Scripts/Core/PowerUpManager.cs:205) method is the core logic for updating the UI. It determines the current state based on the inventory count, compares it to the previous state, and updates the background sprite and fill image accordingly.

-   The background sprite changes to reflect the current state (Unusable, Usable, Charged, Supercharged).
-   The fill image's sprite represents the *next* state that is being charged towards.
-   The fill amount represents the progress towards the next state's threshold.
-   Special flags (`instantReset`, `instantUpdate`) allow forcing the visuals to a specific state immediately, bypassing animation.
-   The `HandleBlockAnimationFinished` method, subscribed to the `GridManager`, triggers `UpdatePowerUpVisual` when a block animation completes, ensuring the UI updates react to collected power-ups.

## 5. Power-up Usage and Effects

Player interaction with power-up buttons triggers methods like [`UseSwordPowerUp`](Code/Scripts/Core/PowerUpManager.cs:679). These methods call the central [`TryUsePowerUp`](Code/Scripts/Core/PowerUpManager.cs:638) method.

[`TryUsePowerUp`](Code/Scripts/Core/PowerUpManager.cs:638) checks the current usage state (`GetCurrentUsageState`). If the state is Usable, Charged, or Supercharged, it proceeds:
1.  Sets an internal flag `_isUsingPowerUp` to prevent event-driven visual updates during the use process.
2.  Calls [`ActivateEffect`](Code/Scripts/Core/PowerUpManager.cs:705), passing the power-up type and the state it was used in.
3.  Resets the count for that power-up type to 0 in the [`PowerUpInventory`](Code/Scripts/Core/PowerUpInventory.cs) using [`SetPowerUpCount`](Code/Scripts/Core/PowerUpInventory.cs:101).
4.  Forces an instant visual reset to the Unusable state using [`UpdatePowerUpVisual(type, instantReset: true)`](Code/Scripts/Core/PowerUpManager.cs:205).
5.  Resets the `_isUsingPowerUp` flag.

The [`ActivateEffect`](Code/Scripts/Core/PowerUpManager.cs:705) method acts as a dispatcher, calling specific handler methods (`HandleSword`, `HandleShield`, etc.) based on the power-up type. These handlers then call the relevant methods on the injected power-up effect scripts (e.g., `_attackPowerUp.AttackPowerUpSelected`).

```csharp
public bool TryUsePowerUp(PowerUpInventory.PowerUpType type)
{
    if (powerUpInventory == null) { /* ... error ... */ return false; }

    PowerUpState usageState = GetCurrentUsageState(type);

    switch (usageState)
    {
        case PowerUpState.Unusable:
            // No change in state, no action needed other than feedback
            // ... logging ...
            UpdatePowerUpVisual(type, instantUpdate: true);
            return false;

        case PowerUpState.Usable:
        case PowerUpState.Charged:
        case PowerUpState.Supercharged:
            // Any successful use resets count to 0 and visuals instantly
            _isUsingPowerUp = true;
            ActivateEffect(type, usageState); // Activate effect based on the state it was used in
            powerUpInventory.SetPowerUpCount(type, 0); // Reset count to 0
            UpdatePowerUpVisual(type, instantReset: true); // Force instant visual reset to Unusable state
            _isUsingPowerUp = false; // Reset flag AFTER instant update
            // ... logging ...
            return true;

        default:
            // This case should ideally not be reached
            // ... warning ...
            return false;
    }
}

private void ActivateEffect(PowerUpInventory.PowerUpType type, PowerUpState state)
{
    // ... logging ...

    // Determine the user (In this context, it's always the player clicking the button)
    PowerUpUser user = PowerUpUser.Player;

    // Call the consolidated handler based on the power-up type
    switch (type)
    {
        case PowerUpInventory.PowerUpType.Sword:
            HandleSword(type, state, user);
            break;
        case PowerUpInventory.PowerUpType.Shield:
            HandleShield(type, state, user);
            break;
        case PowerUpInventory.PowerUpType.Wall:
            HandleWall(type, state, user);
            break;
        case PowerUpInventory.PowerUpType.Steps:
            HandleSteps(type, state, user);
            break;
        default:
             Debug.LogWarning($"Unhandled PowerUpType {type} in ActivateEffect");
             break;
    }
}

// --- Consolidated Effect Handlers ---
// These methods now receive the state and user, and can pass this info
// to another script responsible for the actual game logic.

private void HandleSword(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
{
    _attackPowerUp.AttackPowerUpSelected(state, TileSelection.UserType.Player); // Call the attack power-up selection method
    // ... logging ...
    // TODO: Pass type, state, user to the actual effect execution script/system
}

private void HandleShield(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
{
    _defensePowerUp.DefensePowerUpSelected(state, TileSelection.UserType.Player); // Call the defense power-up selection method
    // ... logging ...
    // TODO: Pass type, state, user to the actual effect execution script/system
}

private void HandleWall(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
{
    _wallPowerUp.WallPowerUpSelected(state, TileSelection.UserType.Player); // Call the wall power-up selection method
    // ... logging ...
    // TODO: Pass type, state, user to the actual effect execution script/system
}

private void HandleSteps(PowerUpInventory.PowerUpType type, PowerUpState state, PowerUpUser user)
{
    _movementPowerUp.MovementPowerUpSelected(state, TileSelection.UserType.Player); // Call the movement power-up selection method
    // ... logging ...
    // TODO: Pass type, state, user to the actual effect execution script/system
}
```

## 6. Integration Points

-   **PowerUpInventory**: [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) relies on [`PowerUpInventory`](Code/Scripts/Core/PowerUpInventory.cs) for the current power-up counts and to modify those counts when power-ups are used.
-   **GridManager**: [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) subscribes to the static event [`GridManager.OnBlockAnimationFinished`](Code/Scripts/Core/PowerUpManager.cs:241) to know when a block has finished its animation towards a power-up button, triggering a visual update.
-   **Specific Power-up Effect Scripts**: [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) holds serialized references to the specific power-up effect scripts ([`AttackPowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/AttackPowerUp.cs), [`DefensePowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/DefensePowerUp.cs), [`MovementPowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/MovementPowerUp.cs), and [`WallPowerUp.cs`](Code/Scripts/TurnBasedStrategy/PowerUps/WallPowerUp.cs)). When a power-up is used, `PowerUpManager` calls the relevant method on the corresponding script to execute the actual game effect logic, often involving interaction with the `TileSelection` and `TileOccupants` systems.
-   **UI Buttons**: [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) is connected to UI Button components via public methods (`UseSwordPowerUp`, etc.) assigned in the Inspector. It also manages the `interactable` state of these buttons.
-   **GameManager**: [`PowerUpManager`](Code/Scripts/Core/PowerUpManager.cs) provides public methods (`SetVisualsToUnusable`, `RestoreVisualsFromInventory`) intended to be called by a `GameManager` or similar system to control the visibility and state of the power-up UI based on the current game phase (e.g., hiding/disabling during the enemy turn).
-   **TileSelection**: The specific power-up effect scripts (Attack, Movement, Wall) interact with the `TileSelection` system to allow the user (player or AI) to select a target tile for the effect.
-   **TileOccupants**: The specific power-up effect scripts interact with the `TileOccupants` system to determine the user of the power-up and to affect other occupants on the grid (e.g., dealing damage, moving, placing obstacles).

## 7. Future Improvements / Technical Debt

-   **Event Handling**: The current implementation in `PowerUpManager` subscribes to `GridManager.OnBlockAnimationFinished` to trigger visual updates. A more direct approach would be for `PowerUpManager` to subscribe directly to `PowerUpInventory.OnPowerUpCountChanged`. The `GridManager` could still trigger the *addition* of power-ups to the inventory, and the inventory event would then notify the manager to update visuals.
-   **Effect Activation**: The `ActivateEffect` method and its handlers (`HandleSword`, etc.) currently hardcode calls to specific power-up effect scripts. This could be made more flexible using an interface or a more data-driven approach if many more power-up types were added.
-   **State Thresholds**: The state thresholds (`USABLE_THRESHOLD`, etc.) are hardcoded constants. These could potentially be made configurable (e.g., via Scriptable Objects) if different power-up types required different thresholds or if they needed to change dynamically.
-   **UI References**: The numerous public `Image` and `PowerUpSprites` fields could be managed more dynamically, perhaps by having a list of a custom serializable class that pairs a `PowerUpType` with its UI elements and sprites.
-   **Animation Management**: The `fillCoroutines` dictionary tracks one animation per power-up type. While it prevents multiple animations of the same type running simultaneously, managing coroutine references in a dictionary can sometimes be prone to subtle bugs if not handled carefully (e.g., ensuring references are cleared correctly).
-   **Wall Power-up Range**: The Wall power-up currently has a fixed range of 1 regardless of the power-up state. This could be made variable based on the state for more interesting gameplay.

---
---

# Enemy AI Controller

## 1. Architecture Overview

The Enemy AI system is implemented within the `EnemyAIController.cs` script, which is a Unity MonoBehaviour component. It operates within the game's state machine, primarily reacting to the `GameState.Matching` and `GameState.Enemy` states managed by the `GameManager`.

The controller interacts with several other components:

-   `GameManager.cs`: Listens for game state changes to trigger AI actions.
-   `TileOccupants.cs`: References the player's and enemy's `TileOccupants` to potentially inform AI decisions (though current logic is simple) and for powerup execution that targets occupants.
-   Various `PowerUp` scripts (`MovementPowerUp`, `AttackPowerUp`, `WallPowerUp`, `DefensePowerUp`): Calls methods on these components to execute chosen powerups.
-   UI Elements (`Image`, `RectTransform`): Displays the selected powerups to the player.

```
+-----------------------+     listens to     +--------------+
|                       |<-------------------|              |
| EnemyAIController     |                    |  GameManager |
|                       |------------------->|              |
|                       |     updates state  +--------------+
+-----------+-----------+
            |
            | interacts with
            v
+-----------+-----------+     references     +-----------------+
|                       |------------------->| TileOccupants   |
|                       |                    | (Player/Enemy)  |
|                       |------------------->+-----------------+
|                       |
|                       | calls methods on
|                       |
|                       |------------------->+-----------------+
|                       |                    | MovementPowerUp |
|                       |------------------->+-----------------+
|                       |
|                       |------------------->+-----------------+
|                       |                    | AttackPowerUp   |
|                       |------------------->+-----------------+
|                       |
|                       |------------------->+-----------------+
|                       |                    | WallPowerUp     |
|                       |------------------->+-----------------+
|                       |
|                       |------------------->+-----------------+
|                       |                    | DefensePowerUp  |
|                       |------------------->+-----------------+
|                       |
|                       | updates UI
|                       |
|                       |------------------->+-----------------+
|                       |                    | UI Images       |
|                       |                    | (Powerup Icons) |
+-----------------------+                    +-----------------+
```

## 2. Class Structure

### 2.1 EnemyAIController Class

```csharp
public class EnemyAIController : MonoBehaviour
{
    // ... fields and properties ...

    void Start() { ... }
    void OnEnable() { ... }
    void OnDisable() { ... }
    private void HandleGameStateChanged(GameState newState) { ... }
    private void SelectPowerups() { ... }
    public void DisplayPowerups() { ... }
    private Sprite GetSpriteForState(PowerUpInventory.PowerUpType type, PowerUpState state) { ... }
    private void PositionPowerupDisplay() { ... }
    private void HidePowerups() { ... }
    private System.Collections.IEnumerator ExecutePowerups() { ... }

    // ... nested struct ...
}
```

This is the main class responsible for the enemy's turn logic, including selecting powerups and executing them.

### 2.2 PowerupSpriteMapping Struct

```csharp
[Serializable]
public struct PowerupSpriteMapping
{
    public PowerUpInventory.PowerUpType type;
    public PowerUpSprites sprites;
}
```

A simple serializable struct used to associate a `PowerUpInventory.PowerUpType` with a `PowerUpSprites` object (presumably another struct or class holding sprites for different states). This is used to configure which sprite to display for each powerup type and state via the Unity Inspector.

## 3. Data Structures and Algorithms

### 3.1 Chosen Powerups Storage

The powerups selected for the enemy's turn are stored in a list of a nested struct:

```csharp
private struct SelectedPowerup
{
    public PowerUpInventory.PowerUpType Type;
    public PowerUpState State;
}

private List<SelectedPowerup> chosenPowerups = new List<SelectedPowerup>();
```

This list holds the type and state (Usable, Charged, Supercharged) for each powerup the AI will attempt to use during its turn.

### 3.2 Powerup Selection Algorithm

The `SelectPowerups` method determines which powerups the enemy will use.

```csharp
private void SelectPowerups()
{
    chosenPowerups.Clear();

    List<PowerUpInventory.PowerUpType> availableTypes =
        Enum.GetValues(typeof(PowerUpInventory.PowerUpType))
            .Cast<PowerUpInventory.PowerUpType>()
            .ToList();

    // ... error handling ...

    int count = (UnityEngine.Random.value < probabilityToSelectThreePowerups) ? 3 : 2;
    count = Mathf.Min(count, availableTypes.Count, powerupDisplayIcons.Length);

    List<PowerUpInventory.PowerUpType> pool = new List<PowerUpInventory.PowerUpType>(availableTypes);

    for (int i = 0; i < count; i++)
    {
        if (pool.Count == 0) break;

        int randomIndex = UnityEngine.Random.Range(0, pool.Count);
        PowerUpInventory.PowerUpType selectedType = pool[randomIndex];
        pool.RemoveAt(randomIndex);

        PowerUpState selectedState = PowerUpState.Usable;
        float randomValue = UnityEngine.Random.value;

        if (randomValue < probabilitySupercharged)
        {
            selectedState = PowerUpState.Supercharged;
        }
        else if (randomValue < probabilitySupercharged + probabilityCharged)
        {
            selectedState = PowerUpState.Charged;
        }

        chosenPowerups.Add(new SelectedPowerup { Type = selectedType, State = selectedState });
    }
}
```

This algorithm:
1. Clears the previously chosen powerups.
2. Gets all possible powerup types from the `PowerUpInventory.PowerUpType` enum.
3. Randomly decides whether to select 2 or 3 powerups based on `probabilityToSelectThreePowerups`. The number is capped by the available types and the number of UI display icons.
4. Creates a temporary pool of available powerup types.
5. Iterates `count` times, each time:
    - Selects a random powerup type from the pool.
    - Removes the selected type from the pool to avoid duplicates.
    - Randomly determines the state (Usable, Charged, Supercharged) based on `probabilitySupercharged` and `probabilityCharged`.
    - Adds the selected powerup type and state to the `chosenPowerups` list.

### 3.3 Powerup Execution Sequence

The `ExecutePowerups` coroutine handles the execution of the selected powerups during the `GameState.Enemy` state.

```csharp
private System.Collections.IEnumerator ExecutePowerups()
{
    // ... initialization and checks ...

    List<PowerUpInventory.PowerUpType> priorityOrder = new List<PowerUpInventory.PowerUpType>
    {
        PowerUpInventory.PowerUpType.Steps,
        PowerUpInventory.PowerUpType.Shield,
        PowerUpInventory.PowerUpType.Sword,
        PowerUpInventory.PowerUpType.Wall
    };

    List<int> executedIconIndices = new List<int>();

    foreach (var priorityType in priorityOrder)
    {
        int chosenIndex = chosenPowerups.FindIndex(p => p.Type == priorityType);

        if (chosenIndex != -1)
        {
            SelectedPowerup powerupToExecute = chosenPowerups[chosenIndex];
            bool executedThisPowerup = false;

            switch (powerupToExecute.Type)
            {
                case PowerUpInventory.PowerUpType.Steps:
                    // ... execute Movement ...
                    break;
                case PowerUpInventory.PowerUpType.Shield:
                    // ... execute Defense ...
                    break;
                case PowerUpInventory.PowerUpType.Sword:
                    // ... execute Attack ...
                    break;
                case PowerUpInventory.PowerUpType.Wall:
                    // ... execute Wall with condition ...
                    break;
            }

            if (executedThisPowerup)
            {
                yield return new WaitForSeconds(1.0f); // Wait after execution
            }
        }
    }

    // ... hide icons and transition state ...
}
```

This coroutine:
1. Defines a fixed `priorityOrder` for powerup execution.
2. Iterates through the `priorityOrder`.
3. For each priority type, it checks if that powerup was selected for the current turn.
4. If found, it executes the corresponding logic based on the powerup type using a `switch` statement. This involves calling methods on the linked PowerUp script components.
5. Includes a specific condition for the `Wall` powerup, preventing its use if `Steps` was chosen but hasn't been executed yet (implying the enemy needs to move first).
6. Waits for 1 second after each successful powerup execution to provide visual pacing.
7. After iterating through all priority types, it hides the icons of the executed powerups and transitions the game state back to `Matching`.

## 4. Performance Considerations

The current implementation uses standard Unity coroutines and component interactions. For performance:

-   The `SelectPowerups` logic is executed once per `Matching` phase and involves simple list operations and random number generation, which is efficient.
-   The `ExecutePowerups` coroutine executes sequentially with fixed delays. The performance impact depends on the complexity of the individual PowerUp script methods being called.
-   UI updates in `DisplayPowerups` and `HidePowerups` are straightforward and should not cause performance issues unless there are a very large number of icons.

No specific advanced optimization techniques like object pooling are used within this controller itself, as its primary role is logic and coordination rather than managing a large number of dynamic objects.

## 5. Extensibility Points

-   **Adding New Powerup Types:** Requires adding the new type to the `PowerUpInventory.PowerUpType` enum, updating the `powerupSpriteMappings` in the Inspector, and adding a case to the `switch` statement in `ExecutePowerups` (and potentially the `priorityOrder`).
-   **Adjusting AI Behavior:** The probabilities (`probabilityToSelectThreePowerups`, `probabilityCharged`, `probabilitySupercharged`) can be easily adjusted in the Inspector to change the frequency of selecting 3 powerups and higher states.
-   **More Complex AI Logic:** The `SelectPowerups` and `ExecutePowerups` methods could be extended to incorporate more sophisticated decision-making based on game state, player position, enemy health, etc., rather than purely random selection and fixed priority.
-   **New Powerup States:** Adding new states would require updating the `PowerUpState` enum, `PowerupSpriteMapping`, `GetSpriteForState`, and the logic in `SelectPowerups` and `ExecutePowerups`.

## 6. Testing Strategy

-   **Unit Tests:**
    -   Test `SelectPowerups` to ensure the correct number of powerups are selected based on probabilities and constraints, and that states are assigned according to probabilities.
    -   Test `GetSpriteForState` to verify it returns the correct sprite based on the provided type and state, and handles missing mappings gracefully.
-   **Integration Tests:**
    -   Test `HandleGameStateChanged` to ensure it correctly triggers `SelectPowerups` and `ExecutePowerups` based on state transitions.
    -   Test `ExecutePowerups` to verify that it calls the appropriate methods on the linked PowerUp scripts in the correct priority order and respects the `Wall` powerup condition.
    -   Verify that `DisplayPowerups` and `HidePowerups` correctly update the UI icons.

## 7. Future Improvements

-   **Sophisticated AI Decision Making:** Implement logic to choose powerups and execution order based on the current game situation (e.g., player health, enemy health, player position, available tiles).
-   **Powerup Combinations:** Add logic to identify and execute beneficial combinations of powerups.
-   **Refactor Execution Logic:** Consider a more data-driven approach for powerup execution rather than a large switch statement, potentially using a dictionary or Scriptable Objects to map powerup types to execution logic.
-   **Animation Feedback:** Add visual or audio feedback when the AI selects and executes powerups.

---
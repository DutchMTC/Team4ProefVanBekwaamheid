---

# Enemy AI System

![Placeholder for Enemy AI Flowchart](https://via.placeholder.com/600x400?text=Enemy+AI+Flowchart)
[Zoomable Flowchart Image (Placeholder)](https://i.imgur.com/placeholder.jpeg)

## 1. System Overview

The Enemy AI system controls the behavior of non-player characters (NPCs) in combat and interaction scenarios. Its primary function is to provide challenging and engaging opponents for the player by making decisions based on the game state, player actions, and defined AI profiles. The system aims to simulate intelligent decision-making for movement, targeting, ability usage, and response to environmental factors.

## 2. Core Components

### 2.1 EnemyAIController
- The main script attached to enemy units responsible for executing AI logic.
- Manages the enemy's current state (e.g., Idle, Patrolling, Attacking, Fleeing).
- Interfaces with other systems (e.g., movement, combat, targeting) to perform actions.
- Evaluates potential actions based on defined AI rules and priorities.

### 2.2 Targeting System
- Determines the most appropriate target for the enemy based on factors like proximity, threat level, and target type.
- Can prioritize specific targets (e.g., lowest health player, closest structure).

### 2.3 Pathfinding/Movement System
- Calculates and executes movement paths for the enemy within the game environment.
- Handles navigation around obstacles and towards desired locations (e.g., player position, patrol points).

### 2.4 Ability System
- Manages the enemy's available abilities (attacks, spells, special actions).
- Determines when and how to use abilities based on cooldowns, resources, and tactical considerations.

## 3. AI Behaviors and Decision Making

### 3.1 States
- **Idle:** Enemy is passive, potentially observing the environment or waiting for triggers.
- **Patrolling:** Enemy moves along a predefined path or within a specific area.
- **Alerted:** Enemy has detected a potential threat and is investigating.
- **Attacking:** Enemy is actively engaging a target using available abilities.
- **Fleeing:** Enemy is attempting to escape from a threat, often when low on health.
- **Seeking Cover:** Enemy is attempting to move to a defensive position.

### 3.2 Decision Logic
- Uses a state machine or behavior tree structure to transition between states.
- Evaluates conditions (e.g., player distance, health, cooldowns) to trigger state changes.
- Prioritizes actions within a state based on defined rules (e.g., use strongest ability when available, prioritize low-health targets).

### 3.3 Targeting Logic
- **Closest Target:** Prioritize the nearest enemy unit.
- **Lowest Health:** Prioritize the enemy unit with the least remaining health.
- **Highest Threat:** Prioritize the enemy unit that has dealt the most damage or is perceived as the biggest threat.
- **Random Target:** Select a target randomly.

## 4. Technical Considerations

### 4.1 Performance Optimization
- AI updates can be staggered or run on a lower frequency for distant enemies.
- Use efficient data structures for target finding and pathfinding.

### 4.2 Extensibility
- Design the system to easily add new enemy types with unique AI profiles and behaviors.
- Parameterize AI settings (e.g., aggression level, patrol speed, targeting preferences) for easy tuning.
- Use scriptable objects or data files to define different enemy AI profiles.

---
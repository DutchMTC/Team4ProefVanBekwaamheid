![T_SentoTilesLogo2_Shadow](https://github.com/user-attachments/assets/1d7ec4a5-1b66-4293-a0a2-1861aa2642d3)
# Sento Tiles


## Pages

- [Asset Naming Conventions](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Asset-Naming-Conventions)
- [Code Conventions](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Code-Conventions)
- [Functional Design](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Functional-Design)
- [Pipeline](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Pipeline)
- [Software](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Software)
- [Technical Design](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/Technical-Design)
- [User Tests](https://github.com/DutchMTC/Team4ProefVanBekwaamheid/wiki/User-Tests)

# What is our game?

Our game "Sento Tiles" consists of two parts. Of course, the core of our game is the match-3 part. But we have added an extra element: a kind of turn-based grid combat part. The player stands on a grid, surrounded by classic Japanese props. Opposite the player on the grid is the enemy. The player's goal is to defeat the enemy by strategically matching tiles.

## The match-3 part:

The game is split into two parts; at the bottom is the match-3 section. Each tile the player can match corresponds to a specific action the player can perform. These are: Attack, Move, Defense Up, and Trap Placement.
At the top of the screen are 4 buttons that correspond to the match-3 icons. Initially, the icons are gray, meaning they are unusable. But by matching the corresponding match-3 icons, you can charge these actions. You can charge each action 3 times in the form of levels. Each level makes your action better.
When the match-3 phase is active, the player cannot move and cannot use their actions yet. This phase of the game is about making your desired matches.
The player is entitled to 10 matches per turn. When these matches are used up, the game switches to the grid part.

## Actions:

**Attack:** With the playable character's katana, the player can attack. The attack can only reach 1 square on the grid.
*   Level 1: One-handed attack. This attack does not do much damage. 10 Damage.
*   Level 2: Two-handed attack. With two hands, this attack does more damage than level 1. 15 Damage.
*   Level 3: The katana is drawn from its sheath in a swift motion and slashes horizontally at lightning speed. This attack does the most damage. 25 Damage.

**Movement:** This action simply serves to move you across the grid.
*   Level 1: The player can take 1 step on the grid.
*   Level 2: The player can take 2 steps on the grid.
*   Level 3: The player can take 3 steps on the grid.

**Defense:** With this action, the player can raise their protection to reduce damage.
*   Level 1: 30% damage reduction.
*   Level 2: 45% damage reduction.
*   Level 3: 60% damage reduction.

**Trap placement:** The trap causes whoever steps on it to end their turn immediately. And the trap always does a fixed amount of damage regardless of your defense. From level 2, the trap is hidden under leaves so the enemy does not know where the trap is.
*   Level 1: 8 Damage, can be placed on 1 adjacent tile, it is a visible trap.
*   Level 2: 12 Damage, can be placed on an adjacent tile up to a distance of 2 tiles, it is an invisible trap covered with leaves, and an additional tile is covered with leaves.
*   Level 3: 22 Damage, can be placed on an adjacent tile up to a distance of 2 tiles, it is an invisible trap covered with leaves, and an additional tile is covered with leaves.

## Levels:

*   Level 1: Silver. To reach silver, you only need to make 1 match to unlock this action.
*   Level 2: Gold. To reach gold, the player must make 4 matches to achieve this level.
*   Level 3: Rainbow. The maximum level. The player will need to make a total of 7 matches to reach this level.

## The grid:

The grid part is the part of the game where the player can use their actions. The player can use all their actions based on what they have charged: attacking, moving across the grid, raising their protection, or placing a trap. If the player has used their actions, it is the enemy's turn.

## Enemy:

The enemy can basically do everything the player can. They can attack, move across the grid, raise their protection, and place a trap. All these actions can be upgraded from silver to rainbow, just like the player's. The difference between the player and the enemy is that the player can always see in advance what the enemy will do in the form of an indicator. There is always a bar with icons of actions and corresponding levels. Based on this, the player can predict the enemy's actions and adjust their strategy accordingly.

When the enemy has completed their turn, it is the player's turn again: match the icons and use them on the grid!

## Win/Lose condition:

*   If the player defeats the enemy, they win the game.
*   If the player loses all their lives, they lose the game and must start over.

## Items:

Two items can also be obtained on the grid: a flask and a samurai armor. These items will appear randomly on the grid; if the player wants to get this item, the player must move to this item to pick it up and use it immediately.
*   Flask: This item restores a portion of the player's health.
*   Samurai armor: This item is guaranteed to block all enemy attacks. No health is lost if the player is attacked. If the player is hit by an attack, the armor breaks, and the player is vulnerable to attacks again.

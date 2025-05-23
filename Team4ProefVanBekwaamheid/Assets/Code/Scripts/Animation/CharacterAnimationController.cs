using UnityEngine;
using System;
// Removed: using System.Collections.Generic; as List is no longer used.

public class CharacterAnimationController : MonoBehaviour
{
    public enum AnimationState
    {
        AttackUsable,
        AttackCharged,
        AttackSupercharged,
        Idle,
        Dash,
        DashStop,
        Death,
        Defense,
        TrapThrow,
        Entrance,
        Stuck,
        Damage,
        Start // Added Start animation state
    }

    [Header("Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private SFXManager sfxManager;

    private const string PlayerAnimationPrefix = "AN_Player_";
    private const string EnemyAnimationPrefix = "AN_Enemy_";

    void Start()
    {
        // Play entrance animation for the player on start
        if (playerAnimator != null) // Ensure playerAnimator is assigned
        {
            PlayerEntrance();
        }
    }
 
    // Method to trigger an animation for a specific animator
    private void TriggerAnimation(Animator animator, AnimationState state, string prefix)
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is not assigned.");
            return;
        }

        string animationName = prefix + state.ToString();
        animator.Play(animationName);

        if (sfxManager != null)
        {
            // Attempt to parse AnimationState to SFXManager.ActionType
            // This assumes SFXManager has an enum ActionType with matching names to AnimationState
            if (Enum.TryParse<SFXManager.ActionType>(state.ToString(), out SFXManager.ActionType soundToPlay))
            {
                sfxManager.PlayActionSFX(soundToPlay);
            }
            else
            {
                Debug.LogWarning($"ActionType for animation state '{state.ToString()}' not found in SFXManager.ActionType enum. Ensure SFXManager has a corresponding action type defined (e.g., public enum ActionType {{ {state.ToString()} }}).");
            }
        }
    }

    // Player Animation Triggers
    public void PlayerAttackUsable() => TriggerAnimation(playerAnimator, AnimationState.AttackUsable, PlayerAnimationPrefix);
    public void PlayerAttackCharged() => TriggerAnimation(playerAnimator, AnimationState.AttackCharged, PlayerAnimationPrefix);
    public void PlayerAttackSupercharged() => TriggerAnimation(playerAnimator, AnimationState.AttackSupercharged, PlayerAnimationPrefix);
    public void PlayerIdle() => TriggerAnimation(playerAnimator, AnimationState.Idle, PlayerAnimationPrefix);
    public void PlayerDash() => TriggerAnimation(playerAnimator, AnimationState.Dash, PlayerAnimationPrefix);
    public void PlayerDashStop() => TriggerAnimation(playerAnimator, AnimationState.DashStop, PlayerAnimationPrefix);
    public void PlayerDeath() => TriggerAnimation(playerAnimator, AnimationState.Death, PlayerAnimationPrefix);
    public void PlayerDefense() => TriggerAnimation(playerAnimator, AnimationState.Defense, PlayerAnimationPrefix);
    public void PlayerTrapThrow() => TriggerAnimation(playerAnimator, AnimationState.TrapThrow, PlayerAnimationPrefix);
    public void PlayerEntrance() => TriggerAnimation(playerAnimator, AnimationState.Entrance, PlayerAnimationPrefix);
    public void PlayerStuck() => TriggerAnimation(playerAnimator, AnimationState.Stuck, PlayerAnimationPrefix);
    public void PlayerDamage() => TriggerAnimation(playerAnimator, AnimationState.Damage, PlayerAnimationPrefix);
    public void PlayerStart() => TriggerAnimation(playerAnimator, AnimationState.Start, PlayerAnimationPrefix);

    // Enemy Animation Triggers
    public void EnemyAttackUsable() => TriggerAnimation(enemyAnimator, AnimationState.AttackUsable, EnemyAnimationPrefix);
    public void EnemyAttackCharged() => TriggerAnimation(enemyAnimator, AnimationState.AttackCharged, EnemyAnimationPrefix);
    public void EnemyAttackSupercharged() => TriggerAnimation(enemyAnimator, AnimationState.AttackSupercharged, EnemyAnimationPrefix);
    public void EnemyIdle() => TriggerAnimation(enemyAnimator, AnimationState.Idle, EnemyAnimationPrefix);
    public void EnemyDash() => TriggerAnimation(enemyAnimator, AnimationState.Dash, EnemyAnimationPrefix);
    public void EnemyDashStop() => TriggerAnimation(enemyAnimator, AnimationState.DashStop, EnemyAnimationPrefix);
    public void EnemyDeath() => TriggerAnimation(enemyAnimator, AnimationState.Death, EnemyAnimationPrefix);
    public void EnemyDefense() => TriggerAnimation(enemyAnimator, AnimationState.Defense, EnemyAnimationPrefix);
    public void EnemyTrapThrow() => TriggerAnimation(enemyAnimator, AnimationState.TrapThrow, EnemyAnimationPrefix);
    public void EnemyEntrance() => TriggerAnimation(enemyAnimator, AnimationState.Entrance, EnemyAnimationPrefix);
    public void EnemyStuck() => TriggerAnimation(enemyAnimator, AnimationState.Stuck, EnemyAnimationPrefix);
    public void EnemyDamage() => TriggerAnimation(enemyAnimator, AnimationState.Damage, EnemyAnimationPrefix);
    public void EnemyStart() => TriggerAnimation(enemyAnimator, AnimationState.Start, EnemyAnimationPrefix);
}
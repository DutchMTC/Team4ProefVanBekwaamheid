using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    public enum AnimationState
    {
        AttackTier1,
        AttackTier2,
        AttackTier3,
        Idle,
        Dash,
        DashWait,
        DashStop,
        Death,
        DefenseUp,
        TrapThrow,
        Entrance,
        Stuck
    }

    [Header("Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;

    // Method to trigger an animation for a specific animator
    private void TriggerAnimation(Animator animator, AnimationState state)
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is not assigned.");
            return;
        }
        // Assuming animation state names match the enum names
        animator.Play(state.ToString());
    }

    // Player Animation Triggers
    public void PlayerAttackTier1() => TriggerAnimation(playerAnimator, AnimationState.AttackTier1);
    public void PlayerAttackTier2() => TriggerAnimation(playerAnimator, AnimationState.AttackTier2);
    public void PlayerAttackTier3() => TriggerAnimation(playerAnimator, AnimationState.AttackTier3);
    public void PlayerIdle() => TriggerAnimation(playerAnimator, AnimationState.Idle);
    public void PlayerDash() => TriggerAnimation(playerAnimator, AnimationState.Dash);
    public void PlayerDashWait() => TriggerAnimation(playerAnimator, AnimationState.DashWait);
    public void PlayerDashStop() => TriggerAnimation(playerAnimator, AnimationState.DashStop);
    public void PlayerDeath() => TriggerAnimation(playerAnimator, AnimationState.Death);
    public void PlayerDefenseUp() => TriggerAnimation(playerAnimator, AnimationState.DefenseUp);
    public void PlayerTrapThrow() => TriggerAnimation(playerAnimator, AnimationState.TrapThrow);
    public void PlayerEntrance() => TriggerAnimation(playerAnimator, AnimationState.Entrance);
    public void PlayerStuck() => TriggerAnimation(playerAnimator, AnimationState.Stuck);

    // Enemy Animation Triggers
    public void EnemyAttackTier1() => TriggerAnimation(enemyAnimator, AnimationState.AttackTier1);
    public void EnemyAttackTier2() => TriggerAnimation(enemyAnimator, AnimationState.AttackTier2);
    public void EnemyAttackTier3() => TriggerAnimation(enemyAnimator, AnimationState.AttackTier3);
    public void EnemyIdle() => TriggerAnimation(enemyAnimator, AnimationState.Idle);
    public void EnemyDash() => TriggerAnimation(enemyAnimator, AnimationState.Dash);
    public void EnemyDashWait() => TriggerAnimation(enemyAnimator, AnimationState.DashWait);
    public void EnemyDashStop() => TriggerAnimation(enemyAnimator, AnimationState.DashStop);
    public void EnemyDeath() => TriggerAnimation(enemyAnimator, AnimationState.Death);
    public void EnemyDefenseUp() => TriggerAnimation(enemyAnimator, AnimationState.DefenseUp);
    public void EnemyTrapThrow() => TriggerAnimation(enemyAnimator, AnimationState.TrapThrow);
    public void EnemyEntrance() => TriggerAnimation(enemyAnimator, AnimationState.Entrance);
    public void EnemyStuck() => TriggerAnimation(enemyAnimator, AnimationState.Stuck);
}
using UnityEngine;

// Drives simple enemy animations from code: Walk, Attack, Die
public class EnemyAnimationController : MonoBehaviour
{
    [Header("Animator")] public Animator animator;

    [Header("State Names")]
    public string walkState = "Walk";
    public string attackState = "attack";
    public string dieState = "Die_1";

    [Header("Crossfade Times")]
    public float walkCrossfade = 0.05f;
    public float attackCrossfade = 0.05f;
    public float dieCrossfade = 0.05f;

    private bool hasAnimator;
    private bool isDead = false;
    private bool warnedWalk = false;
    private bool warnedAttack = false;
    private bool warnedDie = false;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        hasAnimator = animator != null;
    }

    public void PlayWalk()
    {
        if (!hasAnimator || isDead) return;
        if (!TryPlay(walkState, walkCrossfade, "Walk", "walk", "WALK"))
        {
            if (!warnedWalk)
            {
                Debug.LogWarning($"EnemyAnimationController: Walk state not found on {name}. Checked: '{walkState}', 'Walk', 'walk', 'WALK'.");
                warnedWalk = true;
            }
        }
    }

    public void PlayAttack()
    {
        if (!hasAnimator || isDead) return;
        if (!TryPlay(attackState, attackCrossfade, "Attack", "attack", "ATTACK", "Shoot", "shoot", "SHOOT", "Fire", "fire", "FIRE"))
        {
            if (!warnedAttack)
            {
                Debug.LogWarning($"EnemyAnimationController: Attack state not found on {name}. Checked: '{attackState}', 'Attack', 'attack', 'ATTACK', 'Shoot', 'shoot', 'SHOOT'.");
                warnedAttack = true;
            }
        }
    }

    public void PlayDie()
    {
        if (!hasAnimator || isDead) return;
        isDead = true;
        if (!TryPlay(dieState, dieCrossfade, "Die_1", "Die", "die", "Death", "death"))
        {
            if (!warnedDie)
            {
                Debug.LogWarning($"EnemyAnimationController: Die state not found on {name}. Checked: '{dieState}', 'Die_1', 'Die', 'die', 'Death', 'death'.");
                warnedDie = true;
            }
        }
    }

    private bool TryPlay(string state, float crossfade, params string[] fallbacks)
    {
        // Layer 0 assumed
        if (HasState(state)) { animator.CrossFade(state, crossfade, 0); return true; }
        for (int i = 0; i < fallbacks.Length; i++)
        {
            if (HasState(fallbacks[i])) { animator.CrossFade(fallbacks[i], crossfade, 0); return true; }
        }
        return false;
    }

    private bool HasState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return false;
        int hash = Animator.StringToHash(stateName);
        return animator.HasState(0, hash);
    }
}


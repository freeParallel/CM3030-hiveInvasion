using UnityEngine;

// Drives Hero locomotion and actions by setting Animator parameters.
// Attach to the Hero root (it will auto-find the child Animator under the model),
// or assign the Animator reference explicitly.
public class HeroAnimationController : MonoBehaviour
{
    [Header("Animator")] public Animator animator;

    [Header("Parameter Names")] 
    public string speedParam = "Speed";     // float [0..1]
    public string attackTrigger = "Attack"; // trigger
    public string shootTrigger  = "Shoot";  // trigger
    public string aoeTrigger    = "AOE";    // trigger
    public string dieTrigger    = "Die";    // trigger

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    // Expect normalized speed in [0..1]
    public void SetMoveSpeed(float speedNormalized)
    {
        if (animator == null) return;
        animator.SetFloat(speedParam, Mathf.Clamp01(speedNormalized));
    }

    public void PlayAttack() { if (animator != null) animator.SetTrigger(attackTrigger); }
    public void PlayShoot()  { if (animator != null) animator.SetTrigger(shootTrigger); }
    public void PlayAOE()    { if (animator != null) animator.SetTrigger(aoeTrigger); }
    public void PlayDie()    { if (animator != null) animator.SetTrigger(dieTrigger); }
}


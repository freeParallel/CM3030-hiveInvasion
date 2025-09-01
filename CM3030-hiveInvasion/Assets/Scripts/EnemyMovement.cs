using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Navigation Targets (auto-acquired)")]
    private GameObject gateObject;
    private GameObject baseObject;
    private Transform currentTarget;

    [Header("Attack Type")]
    public bool isRangedEnemy = false;
    public float rangedAttackRange = 10f;
    public GameObject projectilePrefab; // reserved for future ranged behavior

    [Header("Ranged Attack Settings")]
    public float projectileSpeed = 5f;

    [Header("Melee Attack Settings")]
    public float meleeAttackRange = 4f;
    public int attackDamage = 10;
    public float attackSpeed = 1f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip attackClip;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private NavMeshAgent agent;
    private float lastAttackTime;
    
    private enum TargetPriority { Gate, Base }
    private TargetPriority currentPriority = TargetPriority.Gate;

    void Awake()
    {
        // Ensure SFX source
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f; // 3D sound for enemies
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }
        // Autoload clip based on enemy name if not assigned
        if (attackClip == null)
        {
            string n = gameObject.name;
            if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - 7);
            string lower = n.ToLower();
            string key = "enemy_attack";
            if (lower.Contains("armored")) key = "armored_attack";
            else if (lower.Contains("ranged")) key = "ranged_attack";
            else if (lower.Contains("swarm")) key = "swarm_attack";
            var clip = Resources.Load<AudioClip>("Audio/" + key);
            if (clip != null) attackClip = clip;
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Prefer tags; fall back to common names. Assign tags in Unity for best reliability.
        var gateGO = SafeFindByTagOrName("Gate", "Gate");
        var baseGO = SafeFindByTagOrNames("Base", new string[] { "PlayerBase", "PlayerTower" });
        gateObject = gateGO;
        baseObject = baseGO;

        // Subscribe to gate destruction
        GateHealth.OnGateDestroyed.AddListener(OnGateDestroyed);
        GateHealth.OnGateOpened.AddListener(OnGateOpened);
        GateHealth.OnGateClosed.AddListener(OnGateClosed);

        // Initial target
        AcquireTarget();
    }

    void OnDestroy()
    {
        GateHealth.OnGateDestroyed.RemoveListener(OnGateDestroyed);
        GateHealth.OnGateOpened.RemoveListener(OnGateOpened);
        GateHealth.OnGateClosed.RemoveListener(OnGateClosed);
    }

    void Update()
    {
        // Ensure target is valid; try to reacquire if not
        if (!HasValidTarget())
        {
            AcquireTarget();
            if (!HasValidTarget())
            {
                // Nothing to do this frame
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float effectiveRange = isRangedEnemy ? rangedAttackRange : meleeAttackRange;

        bool canUseAgent = agent != null && agent.enabled && EnsureOnNavMesh();

        if (distanceToTarget <= effectiveRange)
        {
            if (canUseAgent) agent.isStopped = true;
            AttackCurrentTarget();
        }
        else
        {
            if (canUseAgent)
            {
                agent.isStopped = false;
                if (agent.destination != currentTarget.position)
                {
                    agent.SetDestination(currentTarget.position);
                }
            }
            // Play walk animation when moving
            var anim = GetComponentInChildren<EnemyAnimationController>();
            if (anim != null)
            {
                anim.PlayWalk();
            }
        }
    }

    private bool HasValidTarget()
    {
        return currentTarget != null && currentTarget.gameObject != null;
    }

    private void AcquireTarget()
    {
        // Refresh cached references in case scene objects were created/renamed after Start
        if (gateObject == null)
            gateObject = SafeFindByTagOrName("Gate", "Gate");

        // If gate has been opened globally, never target it
        if (GateHealth.IsGateOpen)
        {
            gateObject = null;
            if (currentPriority == TargetPriority.Gate) currentPriority = TargetPriority.Base;
        }
        if (baseObject == null)
            baseObject = SafeFindByTagOrNames("Base", new string[] { "PlayerBase", "PlayerTower" });

        switch (currentPriority)
        {
            case TargetPriority.Gate:
                if (gateObject != null)
                {
                    currentTarget = gateObject.transform;
                    if (agent != null && agent.enabled && currentTarget != null && EnsureOnNavMesh())
                    {
                        agent.SetDestination(currentTarget.position);
                    }
                    return;
                }
                // If gate gone, switch to base
                currentPriority = TargetPriority.Base;
                goto case TargetPriority.Base;

            case TargetPriority.Base:
                if (baseObject != null)
                {
                    currentTarget = baseObject.transform;
                    if (agent != null && agent.enabled && currentTarget != null && EnsureOnNavMesh())
                    {
                        agent.SetDestination(currentTarget.position);
                    }
                    return;
                }
                // No valid targets found
                currentTarget = null;
                break;
        }
    }

    private void OnGateDestroyed()
    {
        // Clear gate reference and switch priority to base
        gateObject = null;
        currentPriority = TargetPriority.Base;
        currentTarget = null; // force reacquire
        AcquireTarget();
    }

    private void OnGateOpened()
    {
        // Treat as if gate is gone for targeting purposes
        gateObject = null;
        currentPriority = TargetPriority.Base;
        currentTarget = null;
        AcquireTarget();
    }

    private void OnGateClosed()
    {
        // Restore targeting to gate if present
        currentPriority = TargetPriority.Gate;
        currentTarget = null;
        AcquireTarget();
    }

    private void AttackCurrentTarget()
    {
        if (Time.time - lastAttackTime < attackSpeed) return;

        if (isRangedEnemy)
        {
            // Placeholder for projectile logic; directly apply damage for now
            TryDamageTarget();
        }
        else
        {
            // Melee damage application
            TryDamageTarget();
        }

        // Attack animation event
        var anim = GetComponentInChildren<EnemyAnimationController>();
        if (anim != null)
        {
            anim.PlayAttack();
        }

        lastAttackTime = Time.time;
    }

    private void TryDamageTarget()
    {
        if (currentTarget == null) return;

        var gateHealth = currentTarget.GetComponent<GateHealth>();
        if (gateHealth != null)
        {
            gateHealth.TakeDamage(attackDamage);
            PlayAttackSfx();
            return;
        }

        var baseHealth = currentTarget.GetComponent<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.TakeDamage(attackDamage);
            PlayAttackSfx();
            return;
        }

        // Target has no recognized health; reacquire
        AcquireTarget();
    }

    private void PlayAttackSfx()
    {
        if (sfxSource != null && attackClip != null)
        {
            sfxSource.PlayOneShot(attackClip, Mathf.Clamp01(sfxVolume));
        }
    }

    private bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return agent.Warp(hit.position);
        }
        return false;
    }

    // Safe find helpers: using tags if they exist; falling back to names to avoid exceptions
    private GameObject TryFindWithTag(string tag)
    {
        try { return GameObject.FindWithTag(tag); } catch { return null; }
    }

    private GameObject SafeFindByTagOrName(string tag, string name)
    {
        var go = TryFindWithTag(tag);
        if (go != null) return go;
        return GameObject.Find(name);
    }

    private GameObject SafeFindByTagOrNames(string tag, string[] names)
    {
        var go = TryFindWithTag(tag);
        if (go != null) return go;
        foreach (var n in names)
        {
            go = GameObject.Find(n);
            if (go != null) return go;
        }
        return null;
    }
}

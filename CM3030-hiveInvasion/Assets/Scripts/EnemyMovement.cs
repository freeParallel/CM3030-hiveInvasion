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

    private NavMeshAgent agent;
    private float lastAttackTime;

    private enum TargetPriority { Gate, Base }
    private TargetPriority currentPriority = TargetPriority.Gate;

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

        // Initial target
        AcquireTarget();
    }

    void OnDestroy()
    {
        GateHealth.OnGateDestroyed.RemoveListener(OnGateDestroyed);
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

        if (distanceToTarget <= effectiveRange)
        {
            agent.isStopped = true;
            AttackCurrentTarget();
        }
        else
        {
            agent.isStopped = false;
            if (agent.destination != currentTarget.position)
            {
                agent.SetDestination(currentTarget.position);
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
        if (baseObject == null)
            baseObject = SafeFindByTagOrNames("Base", new string[] { "PlayerBase", "PlayerTower" });

        switch (currentPriority)
        {
            case TargetPriority.Gate:
                if (gateObject != null)
                {
                    currentTarget = gateObject.transform;
                    if (agent != null && currentTarget != null)
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
                    if (agent != null && currentTarget != null)
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

        lastAttackTime = Time.time;
    }

    private void TryDamageTarget()
    {
        if (currentTarget == null) return;

        var gateHealth = currentTarget.GetComponent<GateHealth>();
        if (gateHealth != null)
        {
            gateHealth.TakeDamage(attackDamage);
            return;
        }

        var baseHealth = currentTarget.GetComponent<BaseHealth>();
        if (baseHealth != null)
        {
            baseHealth.TakeDamage(attackDamage);
            return;
        }

        // Target has no recognized health; reacquire
        AcquireTarget();
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

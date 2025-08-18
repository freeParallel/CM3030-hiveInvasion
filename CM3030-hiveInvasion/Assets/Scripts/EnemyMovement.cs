using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public Transform secondaryTarget;
    public float attackRange = 4f;
    public int attackDamage = 10;
    public float attackSpeed = 1f;
    
    NavMeshAgent agent;
    float lastAttackTime;
    private bool gateDestroyed = false;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
        
        // subscribe to Gate Destroyed event
        GateHealth.OnGateDestroyed.AddListener(OnGateDestroyed);
    }

    void OnDestroy()
    {
        GateHealth.OnGateDestroyed.RemoveListener(OnGateDestroyed);
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
            agent.isStopped = true;
            AttackTarget();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }

    void AttackTarget()
    {
        if (Time.time - lastAttackTime >= attackSpeed)
        {
            GateHealth gateHealth = target.GetComponent<GateHealth>();
            if (gateHealth != null)
            {
                gateHealth.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
                //Debug.Log("Enemy attacking gate!");
            }
            else
            {
                // attack BASE
                BaseHealth baseHealth = target.GetComponent<BaseHealth>();
                if (baseHealth != null)
                {
                    baseHealth.TakeDamage(attackDamage);
                    lastAttackTime = Time.time;
                    // Debug.Log($"Enemy attacking Base, {attackDamage} done.");
                }
                else
                {
                    // swtich attack to player base. Implementation of HP to come
                    Debug.Log("No HP component found on target");
                    lastAttackTime = Time.time;
                }
            }
        }
    }

    // Event handler, called when gate is destroyed
    void OnGateDestroyed()
    {
        if (secondaryTarget != null && !gateDestroyed)
        {
            target = secondaryTarget;
            agent.SetDestination(target.position);
            gateDestroyed = true;
            Debug.Log("EVENT RECEIVED, enemy swithching attack to player base");
        }
    }
}

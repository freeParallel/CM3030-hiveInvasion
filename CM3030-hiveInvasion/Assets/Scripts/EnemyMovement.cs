using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform target;
    public float attackRange = 2f;
    public int attackDamage = 10;
    public float attackSpeed = 1f;
    
    NavMeshAgent agent;
    float lastAttackTime;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    void Update()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= attackRange)
        {
            // stop when facing an obstacle
            agent.isStopped = true;
            AttackTarget();
        }
        else
        {
            // keep moving towards target
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
                lastAttackTime =  Time.time;
                Debug.Log("Enemy at the gates!");
            }
        }
    }
}

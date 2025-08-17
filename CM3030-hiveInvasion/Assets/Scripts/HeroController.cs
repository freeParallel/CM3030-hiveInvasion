using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    [Header("Hero Movement")]
    NavMeshAgent agent;
    Camera playerCamera;

    [Header("Hero Combat")]
    public float attackRange = 2f;
    public int attackDamage = 15;
    public float attackSpeed = 1.5f;
    
    private GameObject currentTarget;
    private float lastAttackTime;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Right click for movement || combat
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
        
        // auto-attack logic
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distance <= attackRange)
            {
                // if in range, attack continuously
                if (Time.time - lastAttackTime >= 1f / attackSpeed)
                {
                    PerformAttack();
                }
            }
            else
            {
                // too far, needs to be closer
                agent.SetDestination(currentTarget.transform.position);
            }
        }
    }

    void HandleRightClick()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.CompareTag("Enemy"))
            {
                AttackEnemy(hit.collider.gameObject);
            }
            // check if there's walkable ground and nothing on the path
            else if (IsWalkableGround(hit.collider))
            {
                // ensure NavMeshAgent can reach destination
                if (agent.enabled && IsValidDestination(hit.point))
                {
                    agent.SetDestination(hit.point);
                    Debug.Log("Debug going to " + hit.point);
                }
                else
                {
                    Debug.Log("No way to walk");
                }
            }
            else
            {
                Debug.Log("Can't walk to " + hit.collider.name);
            }
        }
    }

    void AttackEnemy(GameObject enemy)
    {
        currentTarget = enemy;
        Debug.Log("HERO targeting " + enemy.name + " for auto-attack.");
    }

    void PerformAttack()
    {
        if (currentTarget != null)
        {
            EnemyHP enemyHP = currentTarget.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
                Debug.Log($"Hero deals {attackDamage} damage to {currentTarget.name}");

                // clear target upon destruction
                if (currentTarget == null)
                {
                    currentTarget = null;
                }
            }
            else
            {
                currentTarget = null;
            }
        }   
    }
    
    // helper functions for hero movement
    bool IsWalkableGround(Collider hitCollider)
    {
        // check layer instead of name
        return hitCollider.gameObject.layer == LayerMask.NameToLayer("Ground");
    }

    bool IsValidDestination(Vector3 destination)
    {
        // check if NavMesh can reach the click point destination
        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
    }
}

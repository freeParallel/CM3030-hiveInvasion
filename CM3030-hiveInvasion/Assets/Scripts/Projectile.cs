using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("Units per second")] public float speed = 20f;
    [Tooltip("Damage applied on hit")] public int damage = 30;
    [Tooltip("Seconds until auto-destroy")] public float maxLifetime = 5f;
    [Tooltip("Distance to target at which we consider a hit")] public float hitRadius = 0.25f;

    private Transform target;

    public void Initialize(Transform target, int damage, float speed)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
    }

    void Start()
    {
        // Auto cleanup
        Destroy(gameObject, maxLifetime);

        // Make primitive sphere small and non-blocking
        transform.localScale = Vector3.one * 0.2f;
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position);
        float distThisFrame = speed * Time.deltaTime;

        // If close enough this frame, register a hit
        if (dir.magnitude <= hitRadius + distThisFrame)
        {
            HitTarget();
            return;
        }

        transform.position += dir.normalized * distThisFrame;
    }

    void HitTarget()
    {
        if (target != null)
        {
            var enemyHP = target.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            }
        }
        Destroy(gameObject);
    }
}


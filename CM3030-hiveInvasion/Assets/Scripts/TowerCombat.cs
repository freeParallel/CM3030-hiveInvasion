using UnityEngine;

public class TowerCombat : MonoBehaviour
{
    public float range = 20f;
    public int damage = 25;
    public float fireRate = 2f;

    private float lastShotTime = 0f;

    [Header("Projectiles")]
    public bool useProjectiles = true;
    public ProjectileStyle projectileStyle = ProjectileStyle.MachineGun;
    public Transform muzzleTransform; // optional; if null, uses this.transform
    [Tooltip("If true and muzzleTransform is not set, spawn from the top of the tower's bounds")] public bool spawnFromTopIfNoMuzzle = true;
    [Tooltip("Extra vertical offset above the computed top (in world units)")] public float topYOffset = 0.1f;
    [Tooltip("Extra forward distance beyond tower radius toward target when spawning from top")] public float topForwardOffset = 0.2f;

    [Tooltip("Machine gun projectile speed (units/s)")] public float machineGunSpeed = 50f;
    [Tooltip("Cannon projectile speed (units/s)")] public float cannonSpeed = 25f;
    [Tooltip("Machine gun projectile scale")] public float machineGunScale = 0.2f;
    [Tooltip("Cannon projectile scale")] public float cannonScale = 0.35f;
    [Tooltip("Machine gun projectile color")] public Color machineGunColor = Color.black;
    [Tooltip("Cannon projectile color")] public Color cannonColor = Color.black;
    
    [Header("Manual Targeting")]
    public GameObject manualTarget;
    public LineRenderer targetingLine;

    [Header("Audio")]
    public AudioSource sfxSource; // optional; auto-added if null
    public AudioClip machineGunShotClip;
    public AudioClip cannonShotClip;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private bool isManualTargeting = false;
    
    void Start()
    {
        // A simple 2 color gradient with a fixed alpha of 1.0f.
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        );
        
        if (targetingLine == null)
        {
            targetingLine = gameObject.AddComponent<LineRenderer>();
            targetingLine.material = new Material(Shader.Find("Sprites/Default"));
            targetingLine.colorGradient = gradient;
            targetingLine.startWidth = 0.1f;
            targetingLine.endWidth = 0.1f;
            targetingLine.positionCount = 2;
            targetingLine.enabled = false;
        }

        // Ensure we have an AudioSource for SFX
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f; // 3D by default
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }

        // Autoload default shot clips from Resources if not assigned
        if (machineGunShotClip == null)
        {
            machineGunShotClip = Resources.Load<AudioClip>("Audio/tower_machinegun");
        }
        if (cannonShotClip == null)
        {
            cannonShotClip = Resources.Load<AudioClip>("Audio/tower_cannon");
        }
    }
    
    void Update()
    {
        // DEBUG: Show current effective range
        TowerData towerData = GetComponent<TowerData>();
        if (towerData != null && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"Tower range: {range * towerData.GetRangeMultiplier()} (base: {range}, multiplier: {towerData.GetRangeMultiplier()})");
            Debug.Log($"Tower damage: {damage * towerData.GetDamageMultiplier()} (base: {damage}, multiplier: {towerData.GetDamageMultiplier()})");
        }
        
        // manual targeting
        if (Input.GetKeyDown(KeyCode.X) && IsSelected())
        {
            ActivateManualTargeting();
        }
        
        // handle manual targeting selection
        if (isManualTargeting && Input.GetMouseButtonDown(0))
        {
            HandleManualTargetSelection();
        }
        
        // cancel manual targeting with escape key or right-click
        if (isManualTargeting && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            CancelManualTargeting();
        }
        
        GameObject enemy = GetCurrentTarget();
        if (enemy != null)
        {
            ShootAtEnemy(enemy);
            UpdateTargetingLine(enemy);
        }
        else
        {
            // clear manual target 
            if (manualTarget != null)
            {
                ClearManualTarget();
            }
            DisableTargetingLine();
        }
    }

    GameObject FindEnemyInRange()
    {
        // get the tower data for upgrade multipliers
        TowerData towerData = GetComponent<TowerData>();
        float finalRange = range;

        if (towerData != null)
        {
            finalRange = range * towerData.GetRangeMultiplier();
        }
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= finalRange)
            {
                return enemy;
            }
        }
        return null;
    }

    void ShootAtEnemy(GameObject enemy)
    {
        if (Time.time - lastShotTime > 1f / fireRate)
        {
            // get tower data for update multipliers
            TowerData towerData = GetComponent<TowerData>();
            float finalDamage = damage;

            if (towerData != null)
            {
                finalDamage = damage * towerData.GetDamageMultiplier();
            }

            if (useProjectiles)
            {
                // Spawn a projectile instead of instant hit
                Color color;
                float speed;
                float scale;
                if (projectileStyle == ProjectileStyle.MachineGun)
                {
                    color = machineGunColor;
                    speed = machineGunSpeed;
                    scale = machineGunScale;
                }
                else // Cannon
                {
                    color = cannonColor;
                    speed = cannonSpeed;
                    scale = cannonScale;
                }

                Vector3 spawnPos = GetProjectileSpawnPosition(enemy.transform);
                GameObject projGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projGO.name = projectileStyle == ProjectileStyle.MachineGun ? "MG_Bullet" : "Cannon_Shell";
                projGO.transform.position = spawnPos;

                var col = projGO.GetComponent<Collider>();
                if (col != null) col.isTrigger = true; // ensure no initial physics bump

                var renderer = projGO.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = color;

                var projectile = projGO.AddComponent<TowerProjectile>();
                projectile.Initialize(enemy.transform, Mathf.RoundToInt(finalDamage), speed, color, scale);

                // VISUAL FEEDBACK
                ShowMuzzleFlash();
                PlayShotSfx();
            }
            else
            {
                // Original instant hit behavior
                EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
                if (enemyHP != null)
                {
                    enemyHP.TakeDamage(Mathf.RoundToInt(finalDamage));
                    ShowMuzzleFlash();
                    PlayShotSfx();
                }
            }
        
            lastShotTime = Time.time;
        }
    }

    void ShowMuzzleFlash()
    {
        Renderer towerRenderer = GetComponent<Renderer>();
        if (towerRenderer != null)
        {
            StartCoroutine(FlashColor(Color.yellow, 0.1f));
        }

    }

    System.Collections.IEnumerator FlashColor(Color flashColor, float duration)
    {
        Renderer towerRenderer = GetComponent<Renderer>();
        Color originalColor = towerRenderer.material.color;
    
        towerRenderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        towerRenderer.material.color = originalColor;
    }

    GameObject GetCurrentTarget()
    {
        // Check if manual target is still valid
        if (manualTarget != null)
        {
            // Check if the manual target object still exists (destroyed enemies become null)
            if (manualTarget == null || !manualTarget.activeInHierarchy)
            {
                Debug.Log("Manual target destroyed - clearing");
                ClearManualTarget();
            }
            else if (!IsEnemyInRange(manualTarget))
            {
                Debug.Log("Manual target out of range - clearing");
                ClearManualTarget();
            }
            else
            {
                // manual target is valid, use it
                return manualTarget;
            }
        }
    
        // fall back to automatic targeting
        return FindEnemyInRange();
    }
    
    bool IsEnemyInRange(GameObject enemy, float checkRange = -1)
    {
        if (enemy == null) return false;
    
        TowerData towerData = GetComponent<TowerData>();
        float finalRange = checkRange > 0 ? checkRange : range;
    
        if (towerData != null && checkRange <= 0)
        {
            finalRange = range * towerData.GetRangeMultiplier();
        }
    
        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        return distance <= finalRange;
    }

    void ActivateManualTargeting()
    {
        isManualTargeting = true;
        Debug.Log("Manual targeting activated. Click on enemy to target, ESC to cancel.");
    }

    void HandleManualTargetSelection()
    {
        // Ignore UI clicks
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
    
        Camera playerCamera = Camera.main;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
    
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
        
            if (clickedObject.CompareTag("Enemy"))
            {
                // Check if enemy is in range
                if (IsEnemyInRange(clickedObject))
                {
                    SetManualTarget(clickedObject);
                }
                else
                {
                    Debug.Log("Enemy out of range!");
                }
            }
        }
    
        // Exit targeting mode regardless of hit success
        CancelManualTargeting();
    }
    
    void SetManualTarget(GameObject enemy)
    {
        manualTarget = enemy;
        Debug.Log($"Manual target set: {enemy.name}");
    
        // Visual feedback
        if (targetingLine != null)
        {
            targetingLine.enabled = true;
        }
    }

    void ClearManualTarget()
    {
        manualTarget = null;
        DisableTargetingLine();
        Debug.Log("Manual target cleared");
    }

    void CancelManualTargeting()
    {
        isManualTargeting = false;
        Debug.Log("Manual targeting cancelled");
    }

    void UpdateTargetingLine(GameObject target)
    {
        if (targetingLine != null && target != null && target == manualTarget)
        {
            targetingLine.enabled = true;
            targetingLine.SetPosition(0, transform.position + Vector3.up * 1f);
            targetingLine.SetPosition(1, target.transform.position + Vector3.up * 1f);
        }
        else if (targetingLine != null)
        {
            // If target is not the manual target, disable the line
            targetingLine.enabled = false;
        }
    }

    void DisableTargetingLine()
    {
        if (targetingLine != null)
        {
            targetingLine.enabled = false;
        }
    }

    Vector3 GetProjectileSpawnPosition(Transform target)
    {
        // If a muzzle is provided, trust it exactly (no forward push)
        if (muzzleTransform != null)
        {
            return muzzleTransform.position;
        }

        // Compute the top center of the tower and push outward horizontally toward the target
        if (spawnFromTopIfNoMuzzle)
        {
            // Try renderers first for accurate visual bounds
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                Vector3 topCenter = new Vector3(b.center.x, b.max.y + topYOffset, b.center.z);
                Vector3 dirXZ = GetHorizontalDir(topCenter, target);
                float radius = Mathf.Max(b.extents.x, b.extents.z);
                return topCenter + dirXZ * (radius + Mathf.Max(0f, topForwardOffset));
            }
            // Fallback to collider bounds
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Bounds b = col.bounds;
                Vector3 topCenter = new Vector3(b.center.x, b.max.y + topYOffset, b.center.z);
                Vector3 dirXZ = GetHorizontalDir(topCenter, target);
                float radius = Mathf.Max(b.extents.x, b.extents.z);
                return topCenter + dirXZ * (radius + Mathf.Max(0f, topForwardOffset));
            }
        }
        // Final fallback: above transform, push toward target horizontally
        Vector3 basePos = transform.position + Vector3.up * 1f;
        Vector3 finalDirXZ = GetHorizontalDir(basePos, target);
        return basePos + finalDirXZ * Mathf.Max(0f, topForwardOffset);
    }

    Vector3 GetHorizontalDir(Vector3 from, Transform target)
    {
        Vector3 dir = Vector3.forward;
        if (target != null)
        {
            dir = (target.position - from);
        }
        // Zero out vertical component to avoid pushing up/down
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            // Fallback to tower's forward projected on XZ
            dir = transform.forward;
            dir.y = 0f;
        }
        return dir.sqrMagnitude > 0 ? dir.normalized : Vector3.forward;
    }

    void PlayShotSfx()
    {
        if (sfxSource == null) return;
        AudioClip clip = projectileStyle == ProjectileStyle.MachineGun ? machineGunShotClip : cannonShotClip;
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
        }
    }

    bool IsSelected()
    {
        // Check if this tower is currently selected
        TowerSelectionManager selectionManager = FindObjectOfType<TowerSelectionManager>();
        return selectionManager != null && selectionManager.GetSelectedTower() == gameObject;
    }
    public enum ProjectileStyle { MachineGun, Cannon }
}

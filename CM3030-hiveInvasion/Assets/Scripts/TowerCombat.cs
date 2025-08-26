using UnityEngine;

public class TowerCombat : MonoBehaviour
{
    public float range = 20f;
    public int damage = 25;
    public float fireRate = 2f;

    private float lastShotTime = 0f;
    
    [Header("Manual Targeting")]
    public GameObject manualTarget;
    public LineRenderer targetingLine;

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
            
            EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(Mathf.RoundToInt(finalDamage));
            
                // VISUAL FEEDBACK
                ShowMuzzleFlash();
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

    bool IsSelected()
    {
        // Check if this tower is currently selected
        TowerSelectionManager selectionManager = FindObjectOfType<TowerSelectionManager>();
        return selectionManager != null && selectionManager.GetSelectedTower() == gameObject;
    }
}

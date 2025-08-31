using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TowerPlacementManager : MonoBehaviour
{
    [Header("Tower Placement")]
    public GameObject towerPrefab;
    public Button placeTowerButton;
    
    [Header("Settings")]
    public float minTowerSpacing = 2f;

    private bool placementMode = false;
    private Camera playerCamera;

    [Header("Preview")]
    [Tooltip("Radius of the preview ring (visual only)")] public float previewRadius = 0.75f;
    public Color previewValidColor = new Color(0.4f, 1f, 0.4f, 0.9f);
    public Color previewInvalidColor = new Color(1f, 0.35f, 0.35f, 0.9f);
    public float previewLineWidth = 0.1f;
    [Range(8,128)] public int previewSegments = 64;

    [Header("Placement Bounds")]
    [Tooltip("Enable restriction that towers must be within this radius of the hero")] public bool usePlacementRadius = true;
    [Tooltip("Maximum distance from the hero where towers can be placed")] public float placementRadius = 25f;
    public Color placementRingColor = new Color(1f, 0.85f, 0.2f, 0.85f);
    public float placementRingWidth = 0.1f;
    [Range(8,128)] public int placementRingSegments = 96;

    private GameObject previewGO;
    private LineRenderer previewLR;

    private Transform hero;
    private GameObject radiusRingGO;
    private LineRenderer radiusRingLR;

    void Start()
    {
        playerCamera = Camera.main;
        
        // UI button (needs to be assigned)
        if (placeTowerButton != null)
        {
            placeTowerButton.onClick.AddListener(TogglePlacementMode);
        }

        // Bottom-of-screen hint
        PlacementHintUI.ShowBottomHint("Press T to place a tower");

        // Cache hero
        var heroCtrl = FindObjectOfType<HeroController>();
        if (heroCtrl != null)
        {
            hero = heroCtrl.transform;
        }
    }

    void Update()
    {
        // T key shortcut for placement mode
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TogglePlacementMode();
        }
        
        // Reacquire hero if needed (e.g., scene loads)
        if (hero == null)
        {
            var hc = FindObjectOfType<HeroController>();
            if (hc != null) hero = hc.transform;
        }
        // Only show/update the placement radius ring while in placement mode
        if (placementMode && usePlacementRadius)
        {
            CreateOrUpdateRadiusRing();
        }
        else
        {
            DestroyRadiusRing();
        }
        
        // Handle tower placement when in placement mode
        if (placementMode)
        {
            UpdatePlacementPreview();
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlaceTower();
            }
        }
        
        // Cancel placement mode wit esc key
        if (placementMode && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacementMode();
        }
        
    }

    void TogglePlacementMode()
    {
        placementMode = !placementMode;

        if (placementMode)
        {
            Debug.Log("Tower placement mode ON - Click to place tower (ESC to cancel)");
            CreatePreview();
            if (usePlacementRadius) CreateOrUpdateRadiusRing();
        }
        else
        {
            Debug.Log("Tower placement mode OFF)");
            DestroyPreview();
            DestroyRadiusRing();
        }
    }

    void CancelPlacementMode()
    {
        placementMode = false;
        DestroyPreview();
        DestroyRadiusRing();
        Debug.Log("Tower placement cancelled");
    }

    public bool IsInPlacementMode()
    {
        return placementMode;
    }
    
    void TryPlaceTower()
    {
        // enforce tower cap first
        if (TowerManager.Instance != null && TowerManager.Instance.IsAtLimit())
        {
            Debug.Log($"Tower limit reached ({TowerManager.Instance.GetMaxTowers()}).");
            ToastUI.Show("Maximum towers reached!", Color.red, 1.75f);
            return;
        }

        int cost = ResourceManager.Instance.GetTowerCost();
        // check if we can afford a tower 
        if (!ResourceManager.Instance.CanAfford(cost))
        {
            Debug.Log($"Can't afford tower! You need {cost} points. You have {ResourceManager.Instance.GetCurrentPoints()}");
            return;
        }
        
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // check for ground
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                // check position
                if (IsPositionClear(hit.point) && IsWithinPlacementRadius(hit.point))
                {
                    // spend points then place tower; refund on failure as a safeguard
                    if (ResourceManager.Instance.SpendPoints(cost))
                    {
                        GameObject newTower = TowerManager.Instance.RegisterTower(towerPrefab, hit.point);
                        if (newTower == null)
                        {
                            // limit may have been reached concurrently; refund
                            ResourceManager.Instance.AddPoints(cost);
                            Debug.LogWarning("Tower placement failed after spending points — refunded.");
                            return;
                        }

                        Debug.Log("Tower placement at " + hit.point);
                        CancelPlacementMode();
                    }
                }
                else
                {
                    if (!IsWithinPlacementRadius(hit.point))
                    {
                        Debug.Log("Cannot place tower here, outside placement radius from hero.");
                        ToastUI.Show("Out of build range", new Color(1f,0.6f,0.2f,1f), 1.2f);
                    }
                    else
                    {
                        Debug.Log("Cannot place tower here, too close to existing tower.");
                    }
                }
            }
            else
            {
                Debug.Log("Cannot place tower here, invalid surface.");
            }
        }
    }

    bool IsPositionClear(Vector3 position)
    {
        // Check for nearby towers within minimum spacing
        Collider[] nearby = Physics.OverlapSphere(position, minTowerSpacing);
        foreach (Collider col in nearby)
        {
            if (col == null) continue;

            // Robust: treat any collider that belongs to a tower root as a tower
            var towerData = col.GetComponentInParent<TowerData>();
            if (towerData != null) return false;

            // Fallback to tag check
            if (col.CompareTag("Tower")) return false;
        }
        return true;
    }

    void CreatePreview()
    {
        if (previewGO != null) return;
        previewGO = new GameObject("TowerPreview", typeof(LineRenderer));
        previewGO.layer = LayerMask.NameToLayer("Ignore Raycast");
        previewLR = previewGO.GetComponent<LineRenderer>();
        BuildRing(previewLR, previewRadius, previewValidColor);
        previewGO.SetActive(true);
    }

    void UpdatePlacementPreview()
    {
        if (previewGO == null) CreatePreview();
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Vector3 pos = hit.point;
                previewGO.transform.position = new Vector3(pos.x, pos.y + 0.05f, pos.z);
                bool clearSpacing = IsPositionClear(pos);
                bool within = IsWithinPlacementRadius(pos);
                bool ok = clearSpacing && within;
                SetLineColor(previewLR, ok ? previewValidColor : previewInvalidColor);
                previewGO.SetActive(true);
                return;
            }
        }
        // Hide if not on ground
        previewGO.SetActive(false);
    }

    void DestroyPreview()
    {
        if (previewGO != null)
        {
            Destroy(previewGO);
            previewGO = null;
            previewLR = null;
        }
    }

    void BuildRing(LineRenderer lr, float radius, Color color)
    {
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = Mathf.Max(0.01f, previewLineWidth);
        lr.positionCount = Mathf.Clamp(previewSegments, 8, 256);
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        SetLineColor(lr, color);
        int count = lr.positionCount;
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    void SetLineColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    bool IsWithinPlacementRadius(Vector3 pos)
    {
        if (!usePlacementRadius) return true;
        if (hero == null) return true; // if no hero found, do not restrict
        float d = Vector3.Distance(hero.position, pos);
        return d <= EffectivePlacementRadius();
    }

    float EffectivePlacementRadius()
    {
        return Mathf.Max(0f, placementRadius * 0.5f);
    }

    void CreateOrUpdateRadiusRing()
    {
        if (!usePlacementRadius) { DestroyRadiusRing(); return; }
        if (hero == null) { DestroyRadiusRing(); return; }
        if (radiusRingGO == null)
        {
            radiusRingGO = new GameObject("PlacementRadiusRing", typeof(LineRenderer));
            radiusRingGO.layer = LayerMask.NameToLayer("Ignore Raycast");
            radiusRingLR = radiusRingGO.GetComponent<LineRenderer>();
            radiusRingLR.useWorldSpace = false;
            radiusRingLR.loop = true;
            radiusRingLR.widthMultiplier = Mathf.Max(0.01f, placementRingWidth);
            radiusRingLR.positionCount = Mathf.Clamp(placementRingSegments, 8, 256);
            var mat = new Material(Shader.Find("Sprites/Default"));
            radiusRingLR.material = mat;
        }
        // Position and color
        radiusRingGO.transform.position = new Vector3(hero.position.x, hero.position.y + 0.05f, hero.position.z);
        radiusRingLR.startColor = placementRingColor;
        radiusRingLR.endColor = placementRingColor;
        // Update circle points (half-distance effective radius)
        float r = EffectivePlacementRadius();
        int count = radiusRingLR.positionCount;
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            radiusRingLR.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
    }

    void DestroyRadiusRing()
    {
        if (radiusRingGO != null)
        {
            Destroy(radiusRingGO);
            radiusRingGO = null;
            radiusRingLR = null;
        }
    }
}

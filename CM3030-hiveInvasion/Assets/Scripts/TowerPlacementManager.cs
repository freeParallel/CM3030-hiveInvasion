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

    private GameObject previewGO;
    private LineRenderer previewLR;

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
    }

    void Update()
    {
        // T key shortcut for placement mode
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TogglePlacementMode();
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
        }
        else
        {
            Debug.Log("Tower placement mode OFF)");
            DestroyPreview();
        }
    }

    void CancelPlacementMode()
    {
        placementMode = false;
        DestroyPreview();
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
                if (IsPositionClear(hit.point))
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
                    Debug.Log("Cannot place tower here, too close to existing tower.");
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
                bool clear = IsPositionClear(pos);
                SetLineColor(previewLR, clear ? previewValidColor : previewInvalidColor);
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
}

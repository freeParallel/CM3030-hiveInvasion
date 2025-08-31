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

    void Start()
    {
        playerCamera = Camera.main;
        
        // UI button (needs to be assigned)
        if (placeTowerButton != null)
        {
            placeTowerButton.onClick.AddListener(TogglePlacementMode);
        }
    }

    void Update()
    {
        // T key shortcut for placement mode
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TogglePlacementMode();
        }
        
        // Handle tower placement when in placement mode
        if (placementMode && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceTower();
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
        }
        else
        {
            Debug.Log("Tower placement mode OFF)");
        }
    }

    void CancelPlacementMode()
    {
        placementMode = false;
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
}
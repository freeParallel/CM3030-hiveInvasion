using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TowerSelectionManager : MonoBehaviour
{
    [Header("Selection Manager")]
    public Material selectionMaterial;
    public Material defaultMaterial;
    
    [Header("Range Indicator")]
    public GameObject rangeIndicator;
    private GameObject privateRangeIndicator;
    
    private GameObject selectedTower;
    private Renderer selectedTowerRenderer;
    private Material originalMaterial;
    private Camera playerCamera;
    
    // events to listen to
    public static System.Action<GameObject> OnTowerSelected;
    public static System.Action OnTowerDeselected;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Reacquire camera if needed (scene changes can destroy cameras)
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // tower selection only when in placement mode
        TowerPlacementManager placementManager = FindObjectOfType<TowerPlacementManager>();
        bool inPlacementMode = placementManager != null && placementManager.IsInPlacementMode();

        if (!inPlacementMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectTower();
        }
        
        // deselect with the esc key or right click
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            DeselectTower();
        }
        
        // Update range indicator if tower is selected (to handle upgrades)
        if (selectedTower != null && privateRangeIndicator != null)
        {
            // Check if we need to update the range circle
            TowerCombat towerCombat = selectedTower.GetComponent<TowerCombat>();
            TowerData towerData = selectedTower.GetComponent<TowerData>();
        
            if (towerCombat != null)
            {
                float currentEffectiveRange = towerCombat.range;
                if (towerData != null)
                {
                    currentEffectiveRange = towerCombat.range * towerData.GetRangeMultiplier();
                }
            
                // Simple way: recreate the circle every frame when tower is selected
                // This ensures it's always accurate after upgrades
                CreateRangeIndicator(selectedTower);
            }
        }
    }

    void TrySelectTower()
    {
        // ignore clicks on UI
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        // Guard against missing camera
        if (playerCamera == null)
        {
            return;
        }
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Prefer robust detection by walking up to a TowerData root
            var towerData = hit.collider.GetComponentInParent<TowerData>();
            if (towerData != null)
            {
                SelectTower(towerData.gameObject);
                return;
            }
            
            // Fallback to tag check on the collider we hit
            if (hitObject.CompareTag("Tower"))
            {
                SelectTower(hitObject);
            }
            else
            {
                // click something else
                DeselectTower();
            }
        }
    }

    void SelectTower(GameObject tower)
    {
        // deselect current tower first
        DeselectTower();
        
        // select tower
        selectedTower = tower;
        // Prefer a renderer from children so we work even when root has no Renderer
        selectedTowerRenderer = tower.GetComponentInChildren<Renderer>();

        if (selectedTowerRenderer != null && selectionMaterial != null)
        {
            originalMaterial = selectedTowerRenderer.material;
            selectedTowerRenderer.material = selectionMaterial;
        }
        
        CreateRangeIndicator(tower);
        
        TowerData towerData = tower.GetComponent<TowerData>();
        if (towerData != null)
        {
            Debug.Log($"Selected Tower ID: {towerData.GetTowerID()}, Level: {towerData.GetLevel()}");
        }
        
        // notify UI that tower is selected
        OnTowerSelected?.Invoke(selectedTower);
    }

    void DeselectTower()
    {
        Debug.Log("DeselectTower() called");

        if(selectedTower != null)
        {
            Debug.Log($"Deselecting Tower ID: {selectedTower.GetComponent<TowerData>().GetTowerID()}");

            if (selectedTower != null && selectedTowerRenderer != null && originalMaterial != null)
            {
                selectedTowerRenderer.material = originalMaterial;
            }

            selectedTower = null;
            selectedTowerRenderer = null;
            originalMaterial = null;
            
            // remove range circumference indicator
            RemoveRangeIndicator();

            // notify UI that tower is no longer selected
            OnTowerDeselected?.Invoke();
        }
    }

    // public method for other scripts fo check selected tower
    public GameObject GetSelectedTower()
    {
        return selectedTower;
    }

    // check if a specific tower is selected
    public bool IsTowerSelected(GameObject tower)
    {
        return selectedTower == tower;
    }

    void CreateRangeIndicator(GameObject tower)
    {
        // remove existing indicator
        if (privateRangeIndicator != null)
        {
            Destroy(privateRangeIndicator);
        }
    
        // get tower range
        TowerCombat towerCombat = tower.GetComponent<TowerCombat>();
        TowerData towerData = tower.GetComponent<TowerData>();

        if (towerCombat == null) return;
    
        // calculate range (Base range * multiplier)
        float effectiveRange = towerCombat.range;
        if (towerData != null)
        {
            effectiveRange = towerCombat.range * towerData.GetRangeMultiplier();
        }
    
        // range indicator
        privateRangeIndicator = CreateRangeCircle(tower.transform.position, effectiveRange);
    }
    
    GameObject CreateRangeCircle(Vector3 center, float radius)
    {
        // Create empty GameObject for the circle
        GameObject circleObj = new GameObject("RangeIndicator");
        circleObj.transform.position = center;

        // Add LineRenderer component
        LineRenderer lineRenderer = circleObj.AddComponent<LineRenderer>();

        // Configure LineRenderer for circle
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;  
        lineRenderer.endColor = Color.white; 
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.useWorldSpace = true;

        // Create circle points
        int segments = 64; // More segments = smoother circle
        lineRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 pos = center + new Vector3(x, 0.1f, z); // Slightly above ground
            lineRenderer.SetPosition(i, pos);
        }

        return circleObj;
    }

    void RemoveRangeIndicator()
    {
        if (privateRangeIndicator != null)
        {
            Destroy(privateRangeIndicator);
            privateRangeIndicator = null;
        }
    }
    
    void UpdateRangeIndicator()
    {
        if (selectedTower != null)
        {
            // Recreate the range indicator with updated range
            CreateRangeIndicator(selectedTower);
        }
    }
}

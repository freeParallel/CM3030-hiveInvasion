using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TowerSelectionManager : MonoBehaviour
{
    [Header("Selection Manager")]
    public Material selectionMaterial;
    public Material defaultMaterial;
    
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
        // tower selection only when in placement mode
        TowerPlacementManager placementManager = FindObjectOfType<TowerPlacementManager>();
        bool inPlacementMode = placementManager != null && placementManager.IsInPlacementMode();

        if (!inPlacementMode && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectTower();
        }
        
        // deselect with the esc key or right click
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            DeselectTower();
        }
    }

    void TrySelectTower()
    {
        // ignore clicks on UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // if tower is select with click
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
        selectedTowerRenderer = tower.GetComponent<Renderer>();

        if (selectedTowerRenderer != null && selectionMaterial != null)
        {
            originalMaterial = selectedTowerRenderer.material;
            selectedTowerRenderer.material = selectionMaterial;
        }
        
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

            // notify UI that tower is no longer selected
            OnTowerDeselected?.Invoke();
        }
    }

    public GameObject GetSelectedTower()
    {
        return selectedTower;
    }
    
}

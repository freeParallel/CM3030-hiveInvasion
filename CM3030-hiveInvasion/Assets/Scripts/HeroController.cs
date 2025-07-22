using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    [Header("Hero Movement")]
    NavMeshAgent agent;
    Camera playerCamera;
    
    [Header("Tower Placement")]
    public GameObject towerPrefab;
    public Material previewMaterial;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Right click
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            MoveHero();
        }
        
        // Left Click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceTower();
        }
    }

    void MoveHero()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.name == "Ground")
            {
                agent.SetDestination(hit.point);
                Debug.Log("Hero moving to: " + hit.point);
            }
        }
    }

    void PlaceTower()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.name == "Ground")
            {
                Instantiate(towerPrefab, hit.point, Quaternion.identity);
                Debug.Log("Tower placed at: " + hit.point);
            }
        }
    }
}

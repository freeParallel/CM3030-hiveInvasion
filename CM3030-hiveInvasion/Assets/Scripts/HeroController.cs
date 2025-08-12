using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    [Header("Hero Movement")]
    NavMeshAgent agent;
    Camera playerCamera;
    
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
    }

    void MoveHero()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // check if there's walkable ground and nothing on path
            if (IsWalkableGround(hit.collider))
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

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class HeroController : MonoBehaviour
{
    UnityEngine.AI.NavMeshAgent agent;
    Camera playerCamera;
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        playerCamera = Camera.main;
    }

    void Update()
    {
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
            if (hit.collider.name == "Ground")
            {
                agent.SetDestination(hit.point);
                Debug.Log("Hero moving to: " + hit.point);
            }
        }
    }
}

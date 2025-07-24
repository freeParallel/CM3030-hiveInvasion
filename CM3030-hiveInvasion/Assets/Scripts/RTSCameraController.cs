using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float edgeScrollSpeed = 15f;
    [SerializeField] private float edgeScrollBoundary = 20f;
    
    [Header("Boundary Settings")]
    [SerializeField] private Vector2 mapBounds = new Vector2(60f, 60f);
    
    // Input System
    private CameraControls cameraControls;
    private Vector2 keyboardInput;
    private Vector2 mousePosition;
    
    // Movement
    private Vector3 targetPosition;
    
    void Awake()
    {
        cameraControls = new CameraControls();
        targetPosition = transform.position;
    }
    
    void OnEnable()
    {
        cameraControls.Enable();
        cameraControls.Camera.Movement.performed += OnMovementInput;
        cameraControls.Camera.Movement.canceled += OnMovementInput;
        cameraControls.Camera.MousePosition.performed += OnMousePositionInput;
    }
    
    void OnDisable()
    {
        cameraControls.Disable();
    }
    
    void Update()
    {
        HandleMovement();
    }
    
    private void OnMovementInput(InputAction.CallbackContext context)
    {
        keyboardInput = context.ReadValue<Vector2>();
    }
    
    private void OnMousePositionInput(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }
    
    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        
        // Keyboard movement (WASD)
        if (keyboardInput != Vector2.zero)
        {
            movement.x = keyboardInput.x;
            movement.z = keyboardInput.y;
            movement = movement.normalized * moveSpeed;
        }
        
        // Edge scrolling
        Vector2 edgeMovement = GetEdgeScrollMovement();
        if (edgeMovement != Vector2.zero)
        {
            movement.x += edgeMovement.x * edgeScrollSpeed;
            movement.z += edgeMovement.y * edgeScrollSpeed;
        }
        
        // Apply movement
        if (movement != Vector3.zero)
        {
            targetPosition += movement * Time.deltaTime;
            targetPosition = ClampToBounds(targetPosition);
            transform.position = targetPosition;
        }
    }
    
    private Vector2 GetEdgeScrollMovement()
    {
        Vector2 movement = Vector2.zero;
        
        // Check screen edges
        if (mousePosition.x <= edgeScrollBoundary)
            movement.x = -1f;
        else if (mousePosition.x >= Screen.width - edgeScrollBoundary)
            movement.x = 1f;
            
        if (mousePosition.y <= edgeScrollBoundary)
            movement.y = -1f;
        else if (mousePosition.y >= Screen.height - edgeScrollBoundary)
            movement.y = 1f;
            
        return movement;
    }
    
    private Vector3 ClampToBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, -mapBounds.x, mapBounds.x);
        position.z = Mathf.Clamp(position.z, -mapBounds.y, mapBounds.y);
        return position;
    }
}
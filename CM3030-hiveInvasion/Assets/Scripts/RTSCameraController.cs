using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float edgeScrollSpeed = 15f;
    
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
        
        // Apply movement
        if (movement != Vector3.zero)
        {
            targetPosition += movement * Time.deltaTime;
            targetPosition = ClampToBounds(targetPosition);
            transform.position = targetPosition;
        }
    }
    
    private Vector3 ClampToBounds(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, -mapBounds.x, mapBounds.x);
        position.z = Mathf.Clamp(position.z, -mapBounds.y, mapBounds.y);
        return position;
    }
}
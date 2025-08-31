using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // support Cinemachine v3 if present

public class RTSCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float edgeScrollSpeed = 15f;
    [SerializeField] private float edgeScrollBoundary = 20f;

    [Header("Boundary Settings")]
    [SerializeField] private Vector2 mapBounds = new Vector2(60f, 60f);

    [Header("Zoom Settings")]
    [Tooltip("How fast the zoom changes (affects FOV or Ortho Size)")] [SerializeField] private float zoomSpeed = 40f;
    [Tooltip("Scale for mouse wheel delta before applying zoomSpeed")] [SerializeField] private float scrollSensitivity = 0.01f;
    [Tooltip("Invert zoom direction (true = wheel up zooms out)")] [SerializeField] private bool invertZoom = false;
    [SerializeField] private float minFieldOfView = 25f;
    [SerializeField] private float maxFieldOfView = 85f;
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 60f;

    // Input System
    private CameraControls cameraControls;
    private Vector2 keyboardInput;
    private Vector2 mousePosition;

    // Movement
    private Vector3 targetPosition;

    // Cached camera or Cinemachine references
    private Camera cachedCamera;
    private CinemachineBrain cineBrain;                 // on Main Camera if using Cinemachine
    private CinemachineCamera cmCamera;                 // v3 virtual camera if present

    void Awake()
    {
        cameraControls = new CameraControls();
        targetPosition = transform.position;

        // Try to cache a Camera on this object, else fall back to main camera at runtime
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        // Cinemachine support (v3): cache brain from the output camera, and check if this object is a CM camera
        if (cachedCamera != null)
        {
            cineBrain = cachedCamera.GetComponent<CinemachineBrain>();
        }
        cmCamera = GetComponent<CinemachineCamera>();
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
        HandleZoom();
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

    private void HandleZoom()
    {
        // Combine inputs first
        float scroll = 0f;
        if (Mouse.current != null)
        {
            // Positive Y usually indicates scrolling up
            scroll = Mouse.current.scroll.ReadValue().y;
        }
        float keyAxis = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.iKey.isPressed) keyAxis += 1f; // Zoom In
            if (Keyboard.current.oKey.isPressed) keyAxis -= 1f; // Zoom Out
        }
        float axis = (scroll * scrollSensitivity) + keyAxis;
        if (invertZoom) axis = -axis;
        if (Mathf.Approximately(axis, 0f)) return;

        // Try Cinemachine v3 first if present
        CinemachineCamera activeCm = cmCamera;
        if (activeCm == null && cineBrain != null)
        {
            activeCm = cineBrain.ActiveVirtualCamera as CinemachineCamera;
        }
        if (activeCm == null)
        {
            // As a last resort, try to find any CM camera in the scene
            activeCm = Object.FindObjectOfType<CinemachineCamera>();
        }

        if (activeCm != null)
        {
            var lens = activeCm.Lens;
            // Apply to both fields; only the active projection mode will matter
            float newFov = Mathf.Clamp(lens.FieldOfView - (axis * zoomSpeed * Time.deltaTime), minFieldOfView, maxFieldOfView);
            float newOrtho = Mathf.Clamp(lens.OrthographicSize - (axis * zoomSpeed * Time.deltaTime), minOrthographicSize, maxOrthographicSize);
            lens.FieldOfView = newFov;
            lens.OrthographicSize = newOrtho;
            activeCm.Lens = lens;
            return;
        }

        // Fallback: regular Camera
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
            if (cachedCamera == null) return;
        }
        if (cachedCamera.orthographic)
        {
            float size = cachedCamera.orthographicSize - (axis * zoomSpeed * Time.deltaTime);
            cachedCamera.orthographicSize = Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
        }
        else
        {
            float fov = cachedCamera.fieldOfView - (axis * zoomSpeed * Time.deltaTime);
            cachedCamera.fieldOfView = Mathf.Clamp(fov, minFieldOfView, maxFieldOfView);
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

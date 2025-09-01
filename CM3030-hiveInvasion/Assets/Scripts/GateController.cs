using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Controls opening animation for the gate and broadcasts a global "opened" signal
// so enemies retarget to the base without destroying the gate.
public class GateController : MonoBehaviour
{
    public enum OpenMode { SlideDown, SlideSide }

    [Header("Open Behavior")] public OpenMode mode = OpenMode.SlideDown;
    [Tooltip("How far to move the gate when opening (world units)")] public float distance = 4f;
    [Tooltip("Seconds to complete the opening movement")] public float duration = 1.0f;
    [Tooltip("If true and mode=SlideSide, use transform.right as slide direction; otherwise use sideDirection")] public bool useTransformRight = true;
    [Tooltip("World direction used when mode=SlideSide and useTransformRight=false")] public Vector3 sideDirection = Vector3.right;
    [Tooltip("Disable colliders on open. Leave OFF to allow clicking while open; NavMeshObstacle is toggled regardless")] public bool disableCollidersOnOpen = false;

    [Header("Hover Highlight")]
    [Tooltip("Ring color shown when hovering the gate")]
    public Color hoverRingColor = new Color(0.9f, 0.95f, 1f, 0.35f);
    [Tooltip("Ring line width")]
    public float hoverRingWidth = 0.08f;
    [Range(8,128)] public int hoverRingSegments = 64;
    [Tooltip("Extra radius added around the gate bounds for the hover ring")] public float hoverRingPadding = 0.5f;

    private GameObject hoverRingGO;
    private LineRenderer hoverRingLR;

    [Header("Debug/Convenience")] public bool openOnStart = false;
    public KeyCode debugOpenKey = KeyCode.G;
    public KeyCode debugCloseKey = KeyCode.H;

    private bool opening = false;
    private bool opened = false;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        if (openOnStart) OpenGate();
    }

    void Update()
    {
        if (debugOpenKey != KeyCode.None && Input.GetKeyDown(debugOpenKey))
        {
            OpenGate();
        }
        if (debugCloseKey != KeyCode.None && Input.GetKeyDown(debugCloseKey))
        {
            CloseGate();
        }
    }

    void OnMouseDown()
    {
        // Ignore clicks coming through UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (!opened) OpenGate(); else CloseGate();
    }

    void OnMouseEnter()
    {
        CreateHoverRing();
    }

    void OnMouseExit()
    {
        DestroyHoverRing();
    }

    public void OpenGate()
    {
        if (opened || opening) return;
        opening = true;
        GateHealth.IsGateOpen = true;
        GateHealth.OnGateOpened.Invoke();
        if (disableCollidersOnOpen)
        {
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }
        // Toggle NavMeshObstacle if present
        var nmo = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (nmo != null) nmo.enabled = false;
        // Changing tag prevents reacquisition by enemies that search by tag/name
        if (CompareTag("Gate")) gameObject.tag = "Untagged";
        StartCoroutine(OpenRoutine());
    }

    public void CloseGate()
    {
        if (!opened || opening) return;
        opening = true;
        GateHealth.IsGateOpen = false;
        GateHealth.OnGateClosed.Invoke();
        // Re-enable colliders
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        // Toggle NavMeshObstacle if present
        var nmo = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (nmo != null) nmo.enabled = true;
        // Restore tag so enemies can target it again
        if (!CompareTag("Gate")) gameObject.tag = "Gate";
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 dir;
        if (mode == OpenMode.SlideDown)
        {
            dir = Vector3.down;
        }
        else
        {
            dir = useTransformRight ? transform.right : (sideDirection.sqrMagnitude > 0.0001f ? sideDirection.normalized : Vector3.right);
        }
        Vector3 endPos = startPos + dir * distance;

        float t = 0f;
        float d = Mathf.Max(0.01f, duration);
        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);
            // Ease out cubic
            float e = 1f - Mathf.Pow(1f - u, 3f);
            transform.position = Vector3.Lerp(startPos, endPos, e);
            yield return null;
        }
        transform.position = endPos;
        opened = true;
        opening = false;
    }

    private IEnumerator CloseRoutine()
    {
        Vector3 endPos = startPos;
        Vector3 from = transform.position;
        float t = 0f;
        float d = Mathf.Max(0.01f, duration);
        while (t < d)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / d);
            float e = 1f - Mathf.Pow(1f - u, 3f);
            transform.position = Vector3.Lerp(from, endPos, e);
            yield return null;
        }
        transform.position = endPos;
        opened = false;
        opening = false;
    }

    void OnDestroy()
    {
        DestroyHoverRing();
    }

    void CreateHoverRing()
    {
        if (hoverRingGO != null) return;
        // Estimate radius from renderer bounds
        var rend = GetComponentInChildren<Renderer>();
        float radius = 1.5f;
        if (rend != null)
        {
            var b = rend.bounds;
            radius = Mathf.Max(b.extents.x, b.extents.z) + Mathf.Max(0f, hoverRingPadding);
        }
        hoverRingGO = new GameObject("GateHoverRing", typeof(LineRenderer));
        hoverRingGO.layer = LayerMask.NameToLayer("Ignore Raycast");
        hoverRingGO.transform.SetParent(transform, false);
        hoverRingGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        hoverRingLR = hoverRingGO.GetComponent<LineRenderer>();
        BuildRing(hoverRingLR, radius, hoverRingColor, hoverRingWidth, hoverRingSegments);
        hoverRingGO.SetActive(true);
    }

    void DestroyHoverRing()
    {
        if (hoverRingGO != null)
        {
            Destroy(hoverRingGO);
            hoverRingGO = null;
            hoverRingLR = null;
        }
    }

    void BuildRing(LineRenderer lr, float radius, Color color, float width, int segments)
    {
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = Mathf.Max(0.01f, width);
        lr.positionCount = Mathf.Clamp(segments, 8, 256);
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        int count = lr.positionCount;
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float a = i * step;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }
}


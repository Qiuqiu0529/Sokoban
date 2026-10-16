using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float zoomSpeed = 20f;
    public float minHeight = 2f;
    public float maxHeight = 60f;
    public float rotateSpeed = 0.25f;
    public float minPitch = 35f;
    public float maxPitch = 90f;
    public float dragThreshold = 4f;

    public static bool RightDragThisGesture { get; private set; }

    private Vector3 orbitPivot;
    private float orbitDistance;
    private float orbitYaw;
    private float orbitPitch;
    private bool orbitInitialized;
    private bool rightGestureStartedOverUI;
    private Vector2 rightPressPosition;
    private Vector2 previousMousePosition;

    public void FocusMap(MapPlane map)
    {
        if (map == null || map.mapData == null || map.startPoint == null) return;

        float cellSize = map.mapData.cellSize;
        float width = map.mapData.gridLength * cellSize;
        float depth = map.mapData.gridWidth * cellSize;
        Vector3 center = map.startPoint.position + new Vector3(
            (map.mapData.gridMin.x + map.mapData.gridLength * 0.5f) * cellSize,
            0f,
            (map.mapData.gridMin.y + map.mapData.gridWidth * 0.5f) * cellSize);

        Camera camera = GetComponent<Camera>();
        float verticalFov = camera != null ? camera.fieldOfView : 60f;
        float aspect = camera != null ? camera.aspect : 16f / 9f;
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f) * aspect) * Mathf.Rad2Deg;
        float heightForDepth = depth * 0.5f / Mathf.Tan(verticalFov * Mathf.Deg2Rad * 0.5f);
        float heightForWidth = width * 0.5f / Mathf.Tan(horizontalFov * Mathf.Deg2Rad * 0.5f);
        float viewHeight = Mathf.Max(heightForDepth, heightForWidth) * 1.15f;

        orbitPivot = center;
        orbitYaw = transform.eulerAngles.y;
        orbitPitch = Mathf.Clamp(transform.eulerAngles.x, minPitch, maxPitch);
        orbitDistance = Mathf.Clamp(viewHeight, minHeight, maxHeight);
        orbitInitialized = true;
        ApplyOrbitPose();
    }

    private void Update()
    {
        HandleRightMouseOrbit();

        if (Input.GetMouseButton(2))
        {
            float moveX = -Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveZ = -Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;
            Vector3 pan = new Vector3(moveX, 0f, moveZ);
            transform.position += pan;
            if (orbitInitialized) orbitPivot += pan;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            if (orbitInitialized)
            {
                orbitDistance = Mathf.Clamp(orbitDistance - scroll * zoomSpeed, minHeight, maxHeight);
                ApplyOrbitPose();
            }
            else
            {
                Vector3 position = transform.position;
                position.y = Mathf.Clamp(position.y - scroll * zoomSpeed, minHeight, maxHeight);
                transform.position = position;
            }
        }
    }

    private void HandleRightMouseOrbit()
    {
        Vector2 mousePosition = Input.mousePosition;
        if (Input.GetMouseButtonDown(1))
        {
            RightDragThisGesture = false;
            rightPressPosition = mousePosition;
            previousMousePosition = mousePosition;
            rightGestureStartedOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        if (Input.GetMouseButton(1) && !rightGestureStartedOverUI)
        {
            if (!RightDragThisGesture && Vector2.Distance(mousePosition, rightPressPosition) >= dragThreshold)
            {
                RightDragThisGesture = true;
                EnsureOrbitInitialized();
            }

            if (RightDragThisGesture)
            {
                Vector2 delta = mousePosition - previousMousePosition;
                orbitYaw += delta.x * rotateSpeed;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * rotateSpeed, minPitch, maxPitch);
                ApplyOrbitPose();
            }
        }

        previousMousePosition = mousePosition;
    }

    private void EnsureOrbitInitialized()
    {
        if (orbitInitialized) return;

        orbitPitch = Mathf.Clamp(transform.eulerAngles.x, minPitch, maxPitch);
        orbitYaw = transform.eulerAngles.y;
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        Ray ray = new Ray(transform.position, transform.forward);
        orbitPivot = ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : new Vector3(transform.position.x, 0f, transform.position.z);
        orbitDistance = Mathf.Clamp(Vector3.Distance(transform.position, orbitPivot), minHeight, maxHeight);
        orbitInitialized = true;
    }

    private void ApplyOrbitPose()
    {
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        transform.position = orbitPivot + orbitRotation * (Vector3.back * orbitDistance);
        transform.rotation = orbitRotation;
    }
}

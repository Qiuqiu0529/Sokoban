using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float zoomSpeed = 20f;
    public float minHeight = 2f;
    public float maxHeight = 60f;

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

        transform.position = new Vector3(center.x, Mathf.Clamp(viewHeight, minHeight, maxHeight), center.z);
    }

    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            float moveX = -Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveZ = -Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;
            
            transform.position += new Vector3(moveX, 0, moveZ);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 position = transform.position;
            position.y = Mathf.Clamp(position.y - scroll * zoomSpeed, minHeight, maxHeight);
            transform.position = position;
        }
    }
}

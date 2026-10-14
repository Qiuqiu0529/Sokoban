using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            float moveX = -Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveZ = -Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;
            
            transform.position += new Vector3(moveX, 0, moveZ);
        }
    }
}

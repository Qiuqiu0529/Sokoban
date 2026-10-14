using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 10f;  // Rotation speed around the Y-axis
    [SerializeField]
    private float fixedXAngle = -30f;   // Fixed X rotation angle

    void Update()
    {
        Vector3 currentRotation = transform.rotation.eulerAngles;

        float newYRotation = currentRotation.y + rotationSpeed * Time.deltaTime;

        transform.rotation = Quaternion.Euler(fixedXAngle, newYRotation, 0);
    }
}

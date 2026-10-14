using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private Camera defaultCamera;
    [SerializeField] private Camera otherCamera;
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            SwitchCameras(useOther: true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            SwitchCameras(useOther: false);
        }
    }

    private void SwitchCameras(bool useOther)
    {
        if (defaultCamera != null)
            defaultCamera.gameObject.SetActive(!useOther);

        if (otherCamera != null)
            otherCamera.gameObject.SetActive(useOther);
    }
}

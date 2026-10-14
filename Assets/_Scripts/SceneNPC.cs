using UnityEngine;

public class SceneNPC : MonoBehaviour
{
    [SerializeField]
    private GameObject sceneMenu;
    private bool isWithinCollider = false;

    void Update()
    {
        if (isWithinCollider && Input.GetKeyDown(KeyCode.Space))
        {
            if (sceneMenu != null)
            {
                bool isActive = sceneMenu.activeSelf;
                sceneMenu.SetActive(!isActive);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isWithinCollider = true;
            Debug.Log("Player entered collider");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isWithinCollider = false;
            Debug.Log("Player exited collider");
        }
    }
}

using MoreMountains.FeedbacksForThirdParty;
using UnityEngine;


/* This script is a placeholder for the HubWorld raycasting functionality.
 * Add this to the player character in hub world scene */

public class HubWorldRaycaster : MonoBehaviour
{

    public delegate void SceneChanger(string scenee);
    public static event SceneChanger OnSceneChange;
    public float radius = 1.0f;
    public float maxDist = 2.0f;
    public float height = 1.0f;
    private Vector3 origin;

    public LayerMask layerMask;

    private Collider collid;

    private Vector3 dir;

    void Start()
    {
        collid = GetComponent<Collider>();
       
        

    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.E))
        {
            dir = transform.forward;
            origin = transform.position + new Vector3(0, height, 0);
            Debug.Log("E key pressed");
            // cast 4 rayst in a circular manner
            // cast a ray in the forward direction
            Ray ray = new Ray(origin, transform.forward);

            //Physics.Raycast(ray, out RaycastHit hit)
            //ray, spread, out hit, range
            //ray, radius, out RaycastHit hit, range)

            if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, maxDist, layerMask))
            {
                //Debug.Log("Hit " + hit.transform.name);
                if (hit.transform.tag == "SceneChanger")
                {
                    Debug.Log("Hit a scene changer!");
                    string nextScene = hit.transform.GetComponent<SceneChangeTrigger>().nextScene;
                    OnSceneChange?.Invoke(nextScene);
                }
            }
        }
        
    }

    private void OnDrawGizmos()
    {
        dir = transform.forward;
        origin = transform.position + new Vector3(0, height, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin + dir * maxDist, radius);
    }
}

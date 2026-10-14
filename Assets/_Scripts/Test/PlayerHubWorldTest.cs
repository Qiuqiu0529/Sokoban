using Unity.VisualScripting;
using UnityEngine;

public class PlayerHubWorldTest : MonoBehaviour
{
    public float step;
    private Vector3 currentDir;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentDir = Vector3.forward;

        HubWorldManagerTest.Instance.ReloadPlayerPos();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            this.transform.position += Vector3.forward * step;
            currentDir = Vector3.forward;
            AnimatePlayer();
        }
        else if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            this.transform.position += Vector3.back * step;
            currentDir = Vector3.back;
            AnimatePlayer();
        }
        else if(Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            this.transform.position += Vector3.left * step;
            currentDir = Vector3.left;
            AnimatePlayer();
        }
        else if(Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            this.transform.position += Vector3.right * step;
            currentDir = Vector3.right;
            AnimatePlayer();
        }

        if(Input.GetKeyDown(KeyCode.X))
        {
            foreach(var script in HubWorldManagerTest.Instance.levelButtonScripts)
            {
                Vector2 playerXZ = new Vector2(this.transform.position.x, this.transform.position.z);
                Vector2 scriptXZ = new Vector2(script.transform.position.x, script.transform.position.z);

                if((playerXZ - scriptXZ).magnitude < 0.25f)
                {
                        script.LoadScene();
                        return;
                }
            }
        }
    }

    void AnimatePlayer()
    {
        this.transform.LookAt(this.transform.position + currentDir);
    }
}

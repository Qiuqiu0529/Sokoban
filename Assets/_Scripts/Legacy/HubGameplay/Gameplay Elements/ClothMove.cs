using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothMove : MonoBehaviour
{
    public PlayerGridController playerGridController;
    public Vector3 realmOffset;
    void Start()
    {
        playerGridController = GameObject.FindWithTag("Player").GetComponent<PlayerGridController>();
        realmOffset = -playerGridController.realmOffset;

        this.transform.GetChild(0).localPosition = realmOffset;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Move(Vector3 dir)
    {
        GameManager.SaveGameState();

        if (!CheckObstacle(dir))
        {
            this.transform.position += dir;
        }
    }

    bool CheckObstacle(Vector3 direction)
    {
        Ray ray = new Ray(this.transform.position, direction);
        if (Physics.Raycast(ray, 1))
        {
            Debug.Log("Cloth block");
            return true;
        }

        Ray ray1 = new Ray(this.transform.position + direction, Vector3.down);
        if (Physics.Raycast(ray1, out RaycastHit hitInfo, 1))
        {
            if (!hitInfo.collider.CompareTag("Ground"))
            {
                Debug.Log("No ground");
                return true;
            }
        }

        Debug.Log("Can't move");
        return false;
    }
}


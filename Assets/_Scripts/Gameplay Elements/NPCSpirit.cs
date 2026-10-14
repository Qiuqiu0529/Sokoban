using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpirit : MonoBehaviour
{
    public PlayerGridController playerGridController;
    public List<Transform> possessables;
    public int index;

    public bool isMoving;
    public Material checkMat;
    void Start()
    {
        playerGridController = GameObject.FindWithTag("Player").GetComponent<PlayerGridController>(); ;

        possessables = new List<Transform>();
        GetTrail();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GetTrail()
    {
        possessables.Clear();
        foreach (var possessable in GameObject.FindGameObjectsWithTag("Possessable"))
        {
            possessables.Add(possessable.transform);
        }
    }

    public void CheckBark()
    {
        GetTrail();
        if (Mathf.Abs(playerGridController.transform.position.x - this.transform.position.x) < 0.1f
            || Mathf.Abs(playerGridController.transform.position.z - this.transform.position.z) < 0.1f)
        {
            Debug.Log("NPC hear you bark");
            Vector3 dir = playerGridController.transform.position - this.transform.position;
            dir = new Vector3(Mathf.Clamp(dir.x, -1, 1), 0, Mathf.Clamp(dir.z, -1, 1));
            dir = new Vector3((int)Mathf.Round(dir.x), 0, (int)Mathf.Round(dir.z));
            Walk(dir);
        }
    }

    public void Walk(Vector3 dir)
    {
        isMoving = true;
        StartCoroutine(MoveToTarget(dir));
    }

    IEnumerator MoveToTarget(Vector3 dir)
    {
        while (true)
        {
            if (CheckMovable(dir))
            {
                yield return new WaitForSeconds(0.01f);
                this.transform.position += dir;
            }
            else
            {
                isMoving = false;
                yield break; // 退出协程
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    bool CheckMovable(Vector3 direction)
    {
        Ray ray = new Ray(this.transform.position, direction);
        if (Physics.Raycast(ray, out RaycastHit hitInfo1, 1))
        {
            Debug.Log(hitInfo1.transform.name);
            if (hitInfo1.collider.CompareTag("Pushable"))
            {
                if (hitInfo1.transform.GetComponent<Pushable>().CheckPush(direction))
                {
                    this.transform.position += direction;
                    hitInfo1.transform.position += direction;
                }
            }
            else if(hitInfo1.collider.CompareTag("NPCSpirit"))
            {
                Debug.Log("Double NPC");
                if(hitInfo1.collider.GetComponent<NPCSpirit>().CheckMovable(direction)) return true;
            }
            else
            {
                Debug.Log("NPC Block");
            }
            return false;
        }

        Ray groundRay = new Ray(this.transform.position + direction, Vector3.down);
        if (Physics.Raycast(groundRay, out RaycastHit hitInfo, 1))
        {
            if (hitInfo.collider.CompareTag("Ground"))
            {
                foreach (var possessable in possessables)
                {
                    List<Transform> trailGrounds = new List<Transform>();

                    if (possessable.GetComponent<PumpkinMoveOld>())
                        trailGrounds = possessable.GetComponent<PumpkinMoveOld>().trailGrounds;
                    else if (possessable.GetComponent<BarrelMoveOld>())
                        trailGrounds = possessable.GetComponent<BarrelMoveOld>().trailGrounds;
                    else if (possessable.GetComponent<LogMove>())
                        trailGrounds = possessable.GetComponent<LogMove>().trailGrounds;
                    else if(possessable.GetComponent<ScareCrow>())
                        trailGrounds = possessable.GetComponent<ScareCrow>().trailGrounds;

                    if (trailGrounds.Contains(hitInfo.transform))
                    {
                        Debug.Log("LightArea");
                        return true;
                    }
                }

                    if(LightAreaManager.Instance.initialLightAreas.Contains(hitInfo.transform))
                    {
                        Debug.Log("InitialLightArea");
                        return true;
                    }
            }
        }

        Debug.Log("No Light Area");
        return false;
    }

    public bool CheckPush(Vector3 dir)
    {
        Ray ray = new Ray(this.transform.position, dir);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 1))
        {
            Debug.Log("Can't Push me because something behind me");
            return false;
        }
        return CheckMovable(dir);
    }

}

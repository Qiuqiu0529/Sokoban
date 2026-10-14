using System.Collections.Generic;
using UnityEngine;

public class Pushable : MonoBehaviour
{
    private PlayerGridController player;
    private Vector3 realmOffset;
    public GameObject pushable;

    public List<Transform> trailGrounds;
    public int trailLength;
    public bool trailWithHead;
    public Material trailMat;
    public Material defaultMat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PlayerGridController>();
        realmOffset = player.realmOffset;
        // if(isInBothRealms)
        // {
        //     Ray ray = new Ray(this.transform.position + realmOffset + Vector3.up, Vector3.down);
        //     if(Physics.Raycast(ray, out RaycastHit hitInfo, 1))
        //     {
        //         isInBothRealms = false;
        //         Debug.Log("Can't Generate in Both Realms");
        //     }
        //     else
        //     {
        //         mirrorPushable.SetActive(true);
        //         mirrorPushable.transform.position = this.transform.position + realmOffset;
        //     }
        // }
        if (trailWithHead) CastTrail();

    }

    public bool CheckPush(Vector3 dir)
    {
        Ray ray = new Ray(this.transform.position, dir);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 1))
        {
            Debug.Log("Can't Push me because something behind me");
            return false;
        }

        // if(isInBothRealms)
        // {
        //     Ray ray1 = new Ray(mirrorPushable.transform.position, dir);
        //     if(Physics.Raycast(ray1, out RaycastHit hitInfo1, 1))
        //     {
        //         Debug.Log("Can't Push me because something in other realm behind me");
        //         return false;
        //     }
        // }

        //CastTrail();
        return true;
    }

    public void CastTrail()
    {
        if (trailLength == 0) return;

        Ray ray = new Ray(this.transform.position + realmOffset, Vector3.down);
        RaycastHit hitInfo;
        Physics.Raycast(ray, out hitInfo, 1);
        if (hitInfo.collider.CompareTag("Ground"))
        {

            if (trailGrounds.Contains(hitInfo.transform))
            {
                trailGrounds.Remove(hitInfo.transform);
                trailGrounds.Add(hitInfo.transform);
                Debug.Log("Sort");
            }
            else
            {
                Debug.Log("Add");
                trailGrounds.Add(hitInfo.transform);
            }

            if (trailGrounds.Count > trailLength)
            {
                RemoveTrail();
            }
            hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().material = trailMat;
        }
    }

    void RemoveTrail()
    {
        trailGrounds[0].GetChild(0).GetComponent<MeshRenderer>().material = defaultMat;

        trailGrounds.RemoveAt(0);
    }

    public void ResetTrailGrounds(List<Transform> trails)
    {
        trailGrounds.Clear();
        trailGrounds.AddRange(trails);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PumpkinMoveOld : MonoBehaviour
{
    // Start is called before the first frame update
    public PlayerGridController playerGridController;
    public Vector3 realmOffset;
    public bool isMoving;
    public int trailLength;
    public List<Transform> trailGrounds = new List<Transform>();

    private Material defaultMat;
    public Material trailMat;
    public Material BlockMat;

    private Coroutine currentCoroutine;
    void Start()
    {
        playerGridController = GameObject.FindWithTag("Player").GetComponent<PlayerGridController>(); ;
        realmOffset = -playerGridController.realmOffset;

        Ray ray = new Ray(this.transform.position + realmOffset, Vector3.down);
        RaycastHit hitInfo;
        Physics.Raycast(ray, out hitInfo, 1);
        if (hitInfo.collider.CompareTag("Ground"))
        {
            // Debug.Log("Add");
            trailGrounds.Add(hitInfo.transform);
            defaultMat = hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().material;
            hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().material = trailMat;
        }

        this.transform.GetChild(1).localPosition = realmOffset; 
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Rolling(Vector3 dir)
    {
        GameManager.SaveGameState();
        isMoving = true;
        currentCoroutine = StartCoroutine(MoveToTarget(dir));
    }

    IEnumerator MoveToTarget(Vector3 dir)
    {
        while (true)
        {
            if (!CheckObstacle(dir))
            {
                this.transform.position += dir;
                CastTrail();
                AnimateObject(dir);
            }
            else
            {
                isMoving = false;
                yield break; // 退出协程
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    bool CheckObstacle(Vector3 direction)
    {
        Ray ray = new Ray(this.transform.position, direction);
        if (Physics.Raycast(ray, 1))
        {
            return true;
        }

        Ray ray1 = new Ray(this.transform.position + direction, Vector3.down);
        if (Physics.Raycast(ray1, out RaycastHit hitInfo, 1))
        {
            if (hitInfo.collider.CompareTag("Ground"))
            {
                if (hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial == BlockMat)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void AnimateObject(Vector3 dir)
    {
        this.transform.GetChild(0).RotateAround(this.transform.position, Vector3.Cross(dir, Vector3.up), 90);
    }

    void CastTrail()
    {
        Ray ray = new Ray(this.transform.position + realmOffset, Vector3.down);
        RaycastHit hitInfo;
        Physics.Raycast(ray, out hitInfo, 1);
        if (hitInfo.collider.CompareTag("Ground"))
        {

            if (trailGrounds.Contains(hitInfo.transform))
            {
                trailGrounds.Remove(hitInfo.transform);
                trailGrounds.Add(hitInfo.transform);
                // Debug.Log("Sort");
            }
            else
            {
                // Debug.Log("Add");
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

    public void StopRolling()
    {
        if (isMoving) StopCoroutine(currentCoroutine);
        currentCoroutine = null;
        isMoving = false;
    }

    public void ResetTrailGrounds(List<Transform> trails)
    {
        trailGrounds.Clear();
        trailGrounds.AddRange(trails);
    }
}

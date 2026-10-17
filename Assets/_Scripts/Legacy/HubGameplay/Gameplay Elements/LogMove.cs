using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogMove : MonoBehaviour
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
    public bool isStanding = true;
    public List<Vector3> possibleDir = new List<Vector3>();
    public int logLength = 2;
    public List<Transform> checkPoints = new List<Transform>();
    public Transform logModel;
    public int playerIndex = -1;
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

        isStanding = true;

        // BoxCollider boxCollider = this.GetComponent<BoxCollider>();
        // boxCollider.size = new Vector3(1, logLength, 1);
        // boxCollider.center = new Vector3(0, 0.5f + (logLength-2)/2, 0);

        for (int i = 0; i < logLength; i++)
        {
            GameObject checkPoint = new GameObject("checkPoint_" + i);
            checkPoint.AddComponent<BoxCollider>();
            checkPoint.tag = "Possessable";
            checkPoint.transform.parent = this.transform;
            checkPoint.transform.position = this.transform.position + Vector3.up * i;
            checkPoints.Add(checkPoint.transform);
        }

        logModel.localScale = new Vector3(1, 1 + (logLength - 2) / 2, 1);
        logModel.localPosition = new Vector3(0, 0.5f + (logLength - 2) / 2, 0);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Coroutine currentCoroutine;
    public void Rolling(Vector3 dir)
    {
        if (!isStanding)
        {
            if (!possibleDir.Contains(dir)) return;

            isMoving = true;
            GameManager.SaveGameState();
            currentCoroutine = StartCoroutine(RollToTarget(dir));
        }
        else
        {
            FallDown(dir);
        }
    }
    void FallDown(Vector3 dir)
    {
        if (!CheckObstacle(dir))
        {
            GameManager.SaveGameState();
            this.transform.position += dir;
            isStanding = false;
            AnimateObject(dir);
            CastTrail();
            GetPossibleDir(dir);
        }
    }

    void GetPossibleDir(Vector3 dir)
    {
        Vector3 dir1 = Vector3.Cross(dir, Vector3.up);
        possibleDir.Clear();
        possibleDir.Add(dir1);
        possibleDir.Add(-dir1);
    }

    IEnumerator RollToTarget(Vector3 dir)
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
        if (isStanding)
        {
            Ray ray = new Ray(this.transform.position, direction);
            if (Physics.Raycast(ray, logLength))
            {
                return true;
            }

            for (int i = 1; i < logLength + 1; i++)
            {
                Ray rayTmp = new Ray(this.transform.position + direction * i, Vector3.down);
                if (Physics.Raycast(rayTmp, out RaycastHit hitInfo, 1))
                {
                    if (hitInfo.collider.CompareTag("Ground"))
                    {
                        if (hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial == BlockMat)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        for (int i = 0; i < logLength; i++)
        {
            Ray ray = new Ray(checkPoints[i].position, direction);
            if (Physics.Raycast(ray, 1))
            {
                return true;
            }

            Ray ray1 = new Ray(checkPoints[i].position + direction, Vector3.down);
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
            continue;
        }
        return false;
    }

    void AnimateObject(Vector3 dir)
    {
        logModel.RotateAround(this.transform.position, Vector3.Cross(dir, Vector3.down), 90);
        for (int i = 0; i < logLength; i++)
        {
            checkPoints[i].RotateAround(this.transform.position, Vector3.Cross(dir, Vector3.down), 90);
        }
    }

    void CastTrail()
    {
        for (int i = 0; i < logLength; i++)
        {
            Ray ray = new Ray(checkPoints[i].position + realmOffset, Vector3.down);
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

    public void Reset()
    {
        if (!isStanding) return;
        logModel.localEulerAngles = new Vector3(90,0,0);
        logModel.localScale = new Vector3(1, 1 + (logLength - 2) / 2, 1);
        logModel.localPosition = new Vector3(0, 0.5f + (logLength - 2) / 2, 0);

        for (int i = 0; i < logLength; i++)
        {
            checkPoints[i].localEulerAngles = Vector3.zero;
            checkPoints[i].transform.position = this.transform.position + Vector3.up * i;
        }
    }
}

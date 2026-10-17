using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelMoveOld : MonoBehaviour
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
            CastTrail();
            AnimateObject(dir);
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

    public void Reset()
    {
        if (!isStanding) return;
        this.transform.GetChild(0).localEulerAngles = Vector3.zero;
        this.transform.GetChild(0).localPosition = Vector3.down * 0.5F;
    }
}

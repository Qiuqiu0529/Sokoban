using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScareCrow: MonoBehaviour
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
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Jumping(Vector3 dir)
    {
        GameManager.SaveGameState();
        isMoving = true;
        currentCoroutine = StartCoroutine(JumpToTarget(dir));
    }

    IEnumerator JumpToTarget(Vector3 dir)
    {
        Vector3 startPos = transform.position;

        Vector3 targetPos = startPos + dir * 2;
        // if(CheckObstacle(dir, Vector3.zero)) targetPos = startPos + dir;
        // else targetPos = startPos + dir * 2;
        float jumpDuration = 0.3f;
        float timer = 0f;

        if (CheckObstacle(dir, dir))
        {
            isMoving = false;
            CastTrail();
            yield break;
        }

        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float t = timer / jumpDuration;

            // 平滑插值
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // 加入跳跃的弧线效果 (使用抛物线公式 y = -4(x-0.5)^2 + 1)
            currentPos.y += 1.0f * (-4 * (t - 0.5f) * (t - 0.5f) + 1);

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPos; // 确保最终到达目标位置
        CastTrail();
        isMoving = false;
    }


    bool CheckObstacle(Vector3 direction, Vector3 offsetPos)
    {       
        Ray ray = new Ray(this.transform.position + offsetPos + direction + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, 1))
        {
            Debug.Log("something block scarecrow");
            return true;
        }

        // Ray ray1 = new Ray(this.transform.position + offsetPos + direction, Vector3.down);
        // if (Physics.Raycast(ray1, out RaycastHit hitInfo, 1))
        // {
        //     if (hitInfo.collider.CompareTag("Ground"))
        //     {
        //         if (hitInfo.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial == BlockMat)
        //         {
        //             return true;
        //         }
        //     }
        // }
        // else
        // {
        //     Debug.Log("No Ground");
        //     return true;
        // }

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

using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class PumpkinMove : MovableObj
{
    protected List<Vector3> targetPos = new List<Vector3>();
    protected List<Vector3> targetRot = new List<Vector3>();

    protected List<Vector3> targetIndicatorPos = new List<Vector3>();
    protected Vector3 currentRotationAngles = Vector3.zero;

    public MMF_Player stopSound;

    public void MovePumpkin(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1, float adjustTime=0)
    { 
        pos = targetGrid;

        Vector3 direction = (targetPosition - transform.position).normalized;


        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            currentRotationAngles.z += direction.x > 0 ? -90f : 90f;
        }
        else
        {
            currentRotationAngles.x += direction.z > 0 ? 90f : -90f;
        }
        targetPos.Add(targetPosition);
        if (useIndicator)
        {
            if (indicateInside)
            {
                targetIndicatorPos.Add(GameMgr.Instance.InsidePos(targetGrid));
            }
            else
            {
                targetIndicatorPos.Add(GameMgr.Instance.OutsidePos(targetGrid));
            }
        }

        targetRot.Add(currentRotationAngles);

        if (GameMgr.Instance.CanCheckEnd())
        {
            GameMgr.Instance.EnterPlayMoveAnim();
            StartCoroutine(PlayMoveQueue(adjustTime));
        }
    }


    public override void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        MovePumpkin(targetGrid,targetPosition,targetQuaternion,grid);

    }

    public override void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
       
        MovePumpkin(targetGrid,targetPosition,targetQuaternion,grid,-timePerGrid);
    }



    protected IEnumerator PlayMoveQueue(float basewait=0)
    {
        while (targetPos.Count > 0)
        {
            mMF_Position.DestinationPosition = targetPos[0];
            mMF_Position.AnimatePositionDuration= timePerGrid + basewait+0.01f;
            mMF_Rotation.DestinationAngles = targetRot[0];
            mMF_Rotation.AnimateRotationDuration= timePerGrid + basewait+0.01f;
            
            targetPos.RemoveAt(0);
            targetRot.RemoveAt(0);
            moveFB.PlayFeedbacks();
            if (useIndicator)
            {
                indicatePos.DestinationPosition = targetIndicatorPos[0];
                indicatePos.AnimatePositionDuration = timePerGrid + basewait+0.01f;
                targetIndicatorPos.RemoveAt(0);
                indicateMoveFB.PlayFeedbacks();
            }
            //indicateMoveFB.PlayFeedbacks();
            yield return new WaitForSeconds(timePerGrid + basewait + 0.02f);
        }

        Resume();
        stopSound?.PlayFeedbacks();
        yield return 0;
    }
}

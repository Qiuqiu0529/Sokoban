using System;
using UnityEngine;

public class NPCMove : MovableObj
{
    bool pair;
    public PairColor pairColor;
    EndingMgr endingMgr;
    void OnEnable()
    {
        endingMgr=EndingMgr.Instance;
        endingMgr.AddNeedPair();
    }
    void OnDisable()
    {
        endingMgr.DeleteNeedPair();
    }
    
    public override void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        int tempgrid = (int)MathF.Max(grid, 1);

        mMF_Position.AnimatePositionDuration = 0.1f + (1f - Mathf.Exp(-tempgrid * 0.1f)) * timePerGrid;
        mMF_Position.DestinationPosition = targetPosition;

        Vector3 dir = (targetPosition - transform.position).normalized;
        float yRotation = 0f;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
        {
            yRotation = dir.x > 0 ? 90f : -90f;
        }
        else
        {
            yRotation = dir.z > 0 ? 0f :180f;
        }
        Vector3 targetRot = new Vector3(0f, yRotation, 0f);
        if (mMF_Rotation.DestinationAngles != targetRot)
        {
            mMF_Rotation.DestinationAngles = targetRot;
            mMF_Rotation.Active = true;
        }
        else
        {
            mMF_Rotation.Active = false;
        }

        mMF_Rotation.AnimateRotationDuration= mMF_Position.AnimatePositionDuration;
        GameMgr.Instance.EnterPlayMoveAnim();
        StartCoroutine(ResumeMove());
        MoveIndicator(targetGrid, mMF_Position.AnimatePositionDuration);
        moveFB.PlayFeedbacks();
    }
    

    public override void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPostion, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        int tempgrid = (int)MathF.Max(grid, 1);

        mMF_Position.AnimatePositionDuration = 0.01f;
        mMF_Position.DestinationPosition = targetPostion;

        // mMF_Rotation.AnimateRotationDuration= mMF_Position.AnimatePositionDuration;
        // mMF_Rotation.DestinationAngles = targetQuaternion.eulerAngles;
        mMF_Rotation.Active = false;
        GameMgr.Instance.EnterPlayMoveAnim();
        StartCoroutine(ResumeUndoMove());
        MoveIndicator(targetGrid,0.01f);
        moveFB.PlayFeedbacks();
    }
    public override void Resume()
    {
        base.Resume();
        string goal = currentMap.mapData.GetGoalIDAtGrid(new Vector3Int(pos.x, pos.y, 0));
        if (goal != null)
        {
            if (pair)
            {
                return;
            }

            GameObject goalobj = currentMap.GetObjectByInstanceID(goal);
            if (goalobj)
            {
                GoalObj pairGoal = goalobj.GetComponent<GoalObj>();
                if (pairGoal.pairColor == pairColor)
                {
                    EndingMgr.Instance.AddCorrectPair();
                    pair = true;
                }
            }

        }
        else
        {
            if (pair)
            {
                pair = false;
                EndingMgr.Instance.DeletePair();
            }
        }
    }
}

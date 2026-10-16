using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

public class DogMove : MovableObj
{
    public Animator dogAnim;
    MMF_Pause mMF_Pause;
    int moveCount = 0;
    public override void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        // int tempGrid = Mathf.Max(grid, 1);

        float duration = timePerGrid;
        mMF_Position.AnimatePositionDuration = duration;
        mMF_Position.DestinationPosition = targetPosition;

        Vector3 dir = (targetPosition - transform.position).normalized;
        float yRotation = 0f;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
        {
            yRotation = dir.x > 0 ? 0f : 180f;
        }
        else
        {
            yRotation = dir.z > 0 ? -90f : 90f;
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
        moveCount++;
        if (moveCount == 1)
        { 
            dogAnim.SetFloat("walkSpeed", 0.2f);
        }
        
        StartCoroutine(ResumeDogMove());
        GameMgr.Instance.EnterPlayMoveAnim();
        
        MoveIndicator(targetGrid, timePerGrid);
        
        mMF_Pause = moveFB.GetFeedbackOfType<MMF_Pause>();
        mMF_Pause.Active = true;
        moveFB.PlayFeedbacks();
    }
    protected IEnumerator ResumeDogMove()
    {
        yield return new WaitForSeconds(timePerGrid + 0.02f);
        moveCount--;
        Resume();
        yield return new WaitForSeconds(0.3f);
        if (moveCount <= 0)
            dogAnim.SetFloat("walkSpeed", 0);
    }

    public override void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        // int tempGrid = Mathf.Max(grid, 1);

        float duration = 0.01f;
        
        MoveIndicator(targetGrid, 0.01f);

        mMF_Position.AnimatePositionDuration = duration;
        mMF_Position.DestinationPosition = targetPosition;
        mMF_Rotation.Active = false;
        mMF_Pause = moveFB.GetFeedbackOfType<MMF_Pause>();
        mMF_Pause.Active = false;
        //GameMgr.Instance.EnterPlayMoveAnim();
        //StartCoroutine(ResumeMove());
        moveFB.PlayFeedbacks();
    }
    

}

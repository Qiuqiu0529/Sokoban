using UnityEngine;
using MoreMountains.Feedbacks;
using System;
using Commands;
using ICommand = Commands.ICommand;
using System.Collections;

public class MovableObj : MonoBehaviour
{
    public Vector2Int pos;
    public float timePerGrid = 0.2f;
    public MMF_Player moveFB;
    public GameObject model;

    public MMF_Position mMF_Position;
    public MMF_Rotation mMF_Rotation;
    public MapPlane currentMap;

    public bool useIndicator=false;

    public MMF_Player indicateMoveFB;
    public MMF_Position indicatePos;

    public MMF_Player barkFB;
    public MMF_Player cantMoveFB;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        mMF_Position = moveFB.GetFeedbackOfType<MMF_Position>();
        mMF_Rotation = moveFB.GetFeedbackOfType<MMF_Rotation>();
        if (useIndicator)
        {
            indicatePos = indicateMoveFB.GetFeedbackOfType<MMF_Position>();
        }

    }

    public virtual void Resume()
    {
        GameMgr.Instance.ResetPlayMoveAnim();
    }

    public void SetMap(MapPlane mapPlane)
    {
        currentMap = mapPlane;
    }

    public virtual void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPostion, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        int tempgrid = (int)MathF.Max(grid, 1);

        mMF_Position.AnimatePositionDuration = 0.1f + (1f - Mathf.Exp(-tempgrid * 0.1f)) * timePerGrid;
        mMF_Position.DestinationPosition = targetPostion;

        mMF_Rotation.AnimateRotationDuration= mMF_Position.AnimatePositionDuration;
        mMF_Rotation.DestinationAngles = targetQuaternion.eulerAngles;
        GameMgr.Instance.EnterPlayMoveAnim();
        StartCoroutine(ResumeMove());
        MoveIndicator(targetGrid, mMF_Position.AnimatePositionDuration);
        moveFB.PlayFeedbacks();
    }
    protected IEnumerator ResumeUndoMove()
    {
        yield return new WaitForSeconds(0.02f);
        Resume();
        yield return 0;
    }
    protected IEnumerator ResumeMove()
    {
        yield return new WaitForSeconds(timePerGrid + 0.02f);
        Resume();
        yield return 0;
    }

    public virtual void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPostion, Quaternion targetQuaternion, int grid = 1)
    {
        pos = targetGrid;
        int tempgrid = (int)MathF.Max(grid, 1);

        mMF_Position.AnimatePositionDuration = 0.01f;
        mMF_Position.DestinationPosition = targetPostion;

        mMF_Rotation.AnimateRotationDuration= mMF_Position.AnimatePositionDuration;
        mMF_Rotation.DestinationAngles = targetQuaternion.eulerAngles;
        GameMgr.Instance.EnterPlayMoveAnim();
        StartCoroutine(ResumeUndoMove());
        MoveIndicator(targetGrid,0.01f);
        moveFB.PlayFeedbacks();
    }

    public virtual void PlayBarkFB()
    {
        barkFB?.PlayFeedbacks();
    }

    public virtual void CantMoveFB()
    {
        cantMoveFB?.PlayFeedbacks();
    }

    public virtual bool CanMove(Vector2Int moveDir)
    {
        return true;
    }

    public virtual ICommand AnyCommand(Vector2Int moveDir)
    {
        return new EmptyCommand();
    }

    public void MoveIndicator(Vector2Int targetGrid,float time)
    {
        if (useIndicator)
        {
            indicateMoveFB.StopFeedbacks();
            indicatePos.DestinationPosition = GameMgr.Instance.MapPos(targetGrid);
            indicatePos.AnimatePositionDuration = time;
            //Debug.Log(indicatePos.DestinationPosition);
            indicateMoveFB.PlayFeedbacks();
        }
    }

}

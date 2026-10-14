using UnityEngine;
using Commands;
using ICommand = Commands.ICommand;
using Movement;
using MoreMountains.Feedbacks;

public class BarrelMove : PumpkinMove
{
    public bool hasMoved = false;
    public bool hori;

    public MMF_Player oldFB;
    public MMF_Player newFB;
    protected Vector3 originalRotationAngles = Vector3.zero;
    protected Vector3 rollingRotationAngles = Vector3.zero;

    public override bool CanMove(Vector2Int moveDir)
    {
        if (moveDir.x != 0 && moveDir.y != 0)
            return false;

        if (!hasMoved)
        {
            return true;
        }
        else
        {
            if (hori)
            {
                return moveDir.x == 0;
            }
            else
            {
                return moveDir.y == 0;
            }

        }

    }

    public void SetHasMove(Vector2Int moveDir)
    {
        hasMoved = true;
        moveFB = newFB;
        mMF_Position = moveFB.GetFeedbackOfType<MMF_Position>();
        mMF_Rotation = moveFB.GetFeedbackOfType<MMF_Rotation>();
        hori = moveDir.x != 0;
        rollingRotationAngles = Vector3.zero;
    }
    public void ResetMove()
    {
        moveFB = oldFB;
        mMF_Position = moveFB.GetFeedbackOfType<MMF_Position>();
        mMF_Rotation = moveFB.GetFeedbackOfType<MMF_Rotation>();
        hasMoved = false;
        //originalRotationAngles = Vector3.zero;
        rollingRotationAngles = Vector3.zero;
        //mMF_Rotation.Play(this.transform.position);
    }
    public void MoveBarrel(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1, float adjustTime = 0)
    {
        pos = targetGrid;
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (!hasMoved)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                originalRotationAngles.z += direction.x > 0 ? -90f : 90f;
            }
            else
            {
                originalRotationAngles.x += direction.z > 0 ? 90f : -90f;
            }
            targetRot.Add(originalRotationAngles);
        }
        else
        {
            // rollingRotationAngles.z += direction.x != 0 ? (direction.x > 0 ? -90f : 90f) :
            //                             (direction.z > 0 ? 90f : -90f);
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                rollingRotationAngles.z += direction.x > 0 ? -90f : 90f;
            }
            else
            {
                rollingRotationAngles.x += direction.z > 0 ? 90f : -90f;
            }
            targetRot.Add(rollingRotationAngles);
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

        if (GameMgr.Instance.CanCheckEnd())
        {
            GameMgr.Instance.EnterPlayMoveAnim();
            StartCoroutine(PlayMoveQueue(adjustTime));
        }
    }

    public override void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        MoveBarrel(targetGrid,targetPosition, targetQuaternion, grid);
    }
    public override void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
       
        MoveBarrel(targetGrid,targetPosition, targetQuaternion, grid ,-timePerGrid);
    }


    public IMovementStrategy CurrentMovementStartegy()
    {
        if (hasMoved)
        {
            return new RollingMovementStrategy();
        }
        return new SimpleMovementStrategy();
    }

    public override ICommand AnyCommand(Vector2Int moveDir)
    {
        if (!hasMoved)
        {
            ICommand command = new ChangeMovementStrategy(GameMgr.Instance.movementStrategy, new RollingMovementStrategy());
            command.Execute();
            CompositeCommand compositeCommand = new();
            compositeCommand.AddCommand(command);
            ICommand command1 = new ChangeBarrelMoveCommand(this, moveDir);
            command1.Execute();
            compositeCommand.AddCommand(command1);
            return compositeCommand;
        }


        return new EmptyCommand();
    }


}

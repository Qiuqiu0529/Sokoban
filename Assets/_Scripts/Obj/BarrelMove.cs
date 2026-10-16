using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>Grid movement with MMF movement/audio and DOTween model rotation.</summary>
public class BarrelMove : PumpkinMove
{
    // Kept for existing prefab serialization; barrel motion now uses DOTween.
    public bool hasMoved;
    public bool hori;
    public MMF_Player oldFB;
    public MMF_Player newFB;

    private struct State
    {
        public bool standing;
        public Vector2Int longAxis;
        public Quaternion modelRotation;
    }

    private readonly Stack<State> undoStates = new Stack<State>();
    private readonly Queue<(Vector3 position, Quaternion modelRotation, float duration, MMF_Player feedback)> moves
        = new Queue<(Vector3, Quaternion, float, MMF_Player)>();
    private bool standing = true;
    private Vector2Int longAxis = Vector2Int.right;
    private Quaternion modelRotation = Quaternion.identity;
    private bool queueRunning;

    public bool IsStanding => standing;
    public Vector2Int LongAxis => longAxis;

    public void InitializeOrientation(Quaternion rotation)
    {
        bool placedLying = Mathf.Abs(Mathf.DeltaAngle(rotation.eulerAngles.y, 90f)) < 45f;
        // Yaw encodes the editor's two placement choices: upright (0) or lying (90).
        // The actual visible barrel mesh is under the `model` child, so tilt that child
        // around the board plane instead of relying on the root's yaw to show lying.
        longAxis = placedLying ? Vector2Int.up : Vector2Int.right;
        standing = !placedLying;
        hasMoved = placedLying;
        hori = longAxis.x != 0;
        modelRotation = placedLying ? Quaternion.Euler(0f, 0f, -90f) : Quaternion.identity;
        if (model != null) model.transform.localRotation = modelRotation;
        if (mMF_Rotation == null && moveFB != null) mMF_Rotation = moveFB.GetFeedbackOfType<MMF_Rotation>();
        if (mMF_Position == null && moveFB != null) mMF_Position = moveFB.GetFeedbackOfType<MMF_Position>();
    }

    public void SetPlacementPreviewOrientation(Quaternion rotation)
    {
        bool lying = Mathf.Abs(Mathf.DeltaAngle(rotation.eulerAngles.y, 90f)) < 45f;
        if (model != null)
            model.transform.localRotation = lying
                ? Quaternion.Euler(0f, 0f, -90f)
                : Quaternion.identity;
    }

    public void SetHasMove(Vector2Int moveDir)
    {
        standing = false;
        longAxis = moveDir;
        hasMoved = true;
        hori = longAxis.x != 0;
    }

    public void SetOrientation(Quaternion rotation) => InitializeOrientation(rotation);
    public void ResetMove() => InitializeOrientation(transform.rotation);

    public override void PlayMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        Vector2Int direction = targetGrid - pos;
        if (direction == Vector2Int.zero) direction = new Vector2Int(1, 0);
        bool wasStanding = standing;
        undoStates.Push(new State { standing = standing, longAxis = longAxis, modelRotation = modelRotation });

        if (standing)
        {
            standing = false;
            longAxis = direction;
            Vector3 localDirection = transform.InverseTransformDirection(
                new Vector3(direction.x, 0f, direction.y)).normalized;
            modelRotation = Quaternion.FromToRotation(Vector3.up,
                localDirection);
        }
        else if (Mathf.Abs(direction.x * longAxis.x + direction.y * longAxis.y) == 1)
        {
            standing = true;
            modelRotation = Quaternion.identity;
        }
        else
        {
            int turnSign = longAxis.x * direction.y - longAxis.y * direction.x;
            Vector3 localLongAxis = transform.InverseTransformDirection(
                new Vector3(longAxis.x, 0f, longAxis.y)).normalized;
            modelRotation = Quaternion.AngleAxis(turnSign * 90f, localLongAxis) * modelRotation;
        }
        hasMoved = !standing;
        hori = longAxis.x != 0;
        EnqueueMove(targetGrid, targetPosition, modelRotation, timePerGrid,
            wasStanding ? oldFB : newFB);
    }

    public override void PlayUndoMoveFB(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetQuaternion, int grid = 1)
    {
        bool wasStanding = standing;
        if (undoStates.Count > 0)
        {
            State previous = undoStates.Pop();
            standing = previous.standing;
            longAxis = previous.longAxis;
            modelRotation = previous.modelRotation;
            hasMoved = !standing;
            hori = longAxis.x != 0;
        }
        EnqueueMove(targetGrid, targetPosition, modelRotation, 0.08f,
            wasStanding ? oldFB : newFB);
    }

    private void EnqueueMove(Vector2Int targetGrid, Vector3 targetPosition, Quaternion targetModelRotation,
        float duration, MMF_Player feedback)
    {
        pos = targetGrid;
        if (model == null && feedback == null)
        {
            transform.position = targetPosition;
            return;
        }

        moves.Enqueue((targetPosition, targetModelRotation, duration, feedback));
        GameMgr.Instance.EnterPlayMoveAnim();
        GameMgr.Instance.EnterBlockMove();
        if (!queueRunning) StartCoroutine(PlayMoveQueue());
    }

    private IEnumerator PlayMoveQueue()
    {
        queueRunning = true;
        while (moves.Count > 0)
        {
            var step = moves.Dequeue();
            Sequence animation = DOTween.Sequence();
            if (model != null)
                animation.Join(model.transform.DOLocalRotateQuaternion(step.modelRotation, step.duration));
            if (step.feedback != null)
            {
                MMF_Position positionFeedback = step.feedback.GetFeedbackOfType<MMF_Position>();
                MMF_Rotation rotationFeedback = step.feedback.GetFeedbackOfType<MMF_Rotation>();
                if (positionFeedback != null)
                {
                    positionFeedback.DestinationPosition = step.position;
                    positionFeedback.AnimatePositionDuration = step.duration;
                }
                if (rotationFeedback != null) rotationFeedback.Active = false;
                step.feedback.PlayFeedbacks();
            }
            else
            {
                animation.Join(transform.DOMove(step.position, step.duration));
            }
            animation.SetEase(Ease.InOutSine);
            yield return animation.WaitForCompletion();
            yield return new WaitForSeconds(0.03f);
        }
        queueRunning = false;
        stopSound?.PlayFeedbacks();
        GameMgr.Instance.ResetPlayMoveAnim();
        GameMgr.Instance.ResetBlockMove();
    }
}

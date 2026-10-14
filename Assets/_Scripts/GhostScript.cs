using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using MoreMountains.Feedbacks;
using System.Collections;


public class GhostScript : MonoBehaviour
{
    private Animator Anim;
    private CharacterController Ctrl;
    private Vector3 MoveDirection = Vector3.zero;

    // Cache hash values for animations
    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");

    // Movement speed and push power
    [SerializeField] private float Speed = 4f;
    [SerializeField] private float pushPower = 2f;

    // Reference to the camera
    private Camera mainCamera;

    // Track the last rotation
    private Quaternion lastRotation;

    private Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float rotationSpeed = 10f;
    public Animator dogAnim;

    void Start()
    {
        Anim = this.GetComponent<Animator>();
        Ctrl = this.GetComponent<CharacterController>();
        mainCamera = Camera.main;  // Get the main camera in the scene

        lastRotation = transform.rotation; // Initialize lastRotation
        InitPlayerInput();
    }

    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction moveAction;
    private InputAction barkAction;
    private InputAction possessAction;
    private InputAction undoAction;
    private float inputCooldown = 0.2f;
    private float inputTimer = 0f;

    private Vector2 navInput => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    private bool barkPressed => barkAction?.triggered ?? false;
    private bool possessPressed => possessAction?.triggered ?? false;
    private bool undoPressed => undoAction?.IsPressed() ?? false;
    bool waitPossess;
    public void InitPlayerInput()
    {
        var playerActionMap = inputActionsAsset.FindActionMap("Player");

        moveAction = playerActionMap.FindAction("Move");
        barkAction = playerActionMap.FindAction("Bark");
        possessAction = playerActionMap.FindAction("Possess");
        undoAction = playerActionMap.FindAction("Undo");

        moveAction.Enable();
        barkAction.Enable();
        possessAction.Enable();
        undoAction.Enable();
    }

    void Update()
    {
        GRAVITY();

        MOVE();

        CheckEnterLevelButton();
    }

    private void GRAVITY()
    {
        if (Ctrl.enabled)
        {
            if (CheckGrounded())
            {
                if (MoveDirection.y < -0.1f)
                {
                    MoveDirection.y = -0.1f;
                }
            }
            MoveDirection.y -= 0.1f;
            Ctrl.Move(MoveDirection * Time.deltaTime);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) { return; }

        if (hit.moveDirection.y < -0.3) { return; }
        var pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * pushPower;
    }


    void CheckEnterLevelButton()
    {
        if (!possessPressed) return;

        LevelButtonScript tmp = null;

        foreach (var button in HubWorldManagerTest.Instance.levelButtonScripts)
        {
            if (GetXZVector2(button.transform.position, this.transform.position) > 0.5f) continue;

            if (tmp == null)
            {
                tmp = button;
                continue;
            }

            if (GetXZVector2(tmp.transform.position, this.transform.position)
                > GetXZVector2(button.transform.position, this.transform.position))
                tmp = button;
        }

        if (tmp == null)
        {
            Debug.Log("No Near Level Button");
            return;
        }
        if (!waitPossess)
        {
            dogAnim.SetTrigger("possess");
            StartCoroutine(WaitPossessAnim(tmp));
            waitPossess = true;
        }


    }
    IEnumerator WaitPossessAnim(LevelButtonScript tmp)
    {
        yield return new WaitForSeconds(0.65f);
        Debug.Log("near " + tmp.name);
        tmp.LoadScene();
    }

    private bool CheckGrounded()
    {
        if (Ctrl.isGrounded && Ctrl.enabled)
        {
            return true;
        }
        Ray ray = new Ray(this.transform.position + Vector3.up * 0.1f, Vector3.down);
        float range = 0.2f;
        return Physics.Raycast(ray, range);
    }

    private void MOVE()
    {
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector2 input = navInput;
        Vector3 targetDir = cameraForward * input.y + cameraRight * input.x;
        targetDir.Normalize();

        Vector3 targetVelocity = targetDir * Speed;
        float lerpSpeed = (targetDir.magnitude > 0.1f) ? acceleration : deceleration;
        
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.deltaTime);

        if (targetDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        MOVE_Velocity(currentVelocity);
    }

    // private void MOVE()
    // {
    //     // Calculate movement direction relative to the camera
    //     Vector3 cameraForward = mainCamera.transform.forward;
    //     Vector3 cameraRight = mainCamera.transform.right;

    //     // We want to ignore the vertical component of the camera's forward vector (Y-axis)
    //     cameraForward.y = 0;
    //     cameraRight.y = 0;

    //     // Normalize the vectors
    //     cameraForward.Normalize();
    //     cameraRight.Normalize();

    //     float horizontal = 0f;
    //     float vertical = 0f;


    //     Vector2 input = navInput;
    //     if (input.magnitude > 0.1f)
    //     {
    //         if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
    //         {
    //             horizontal = input.x > 0 ? 1 : -1;
    //         }
    //         else
    //         {
    //             vertical = input.y > 0 ? 1 : -1;
    //         }
    //     }

    //     // Combine the inputs to create a movement direction
    //     Vector3 moveDirection = cameraForward * vertical + cameraRight * horizontal;
    //     moveDirection.Normalize();

    //     if (moveDirection != Vector3.zero)
    //     {
    //         // Update the rotation to face the direction of movement
    //         lastRotation = Quaternion.LookRotation(moveDirection);
    //     }

    //     // Move the player
    //     MOVE_Velocity(moveDirection * Speed);
    // }

    // private void MOVE_Velocity(Vector3 velocity)
    // {
    //     MoveDirection = new Vector3(velocity.x, MoveDirection.y, velocity.z);
    //     if (Ctrl.enabled)
    //     {
    //         Ctrl.Move(MoveDirection * Time.deltaTime);
    //     }

    //     // Apply the last known rotation, whether the character is moving or not
    //     transform.rotation = lastRotation;

    //     // Trigger the move animation when moving
    //     if (velocity != Vector3.zero)
    //     {
    //         Anim.CrossFade(MoveState, 0.1f, 0, 0);
    //     }
    //     else
    //     {
    //         Anim.CrossFade(IdleState, 0.1f, 0, 0);
    //     }
    // }

    private void MOVE_Velocity(Vector3 velocity)
    {
        MoveDirection = new Vector3(velocity.x, MoveDirection.y, velocity.z);

        if (Ctrl.enabled)
            Ctrl.Move(MoveDirection * Time.deltaTime);

        float movementMagnitude = new Vector2(velocity.x, velocity.z).magnitude;
        dogAnim.SetFloat("walkSpeed",movementMagnitude);

        // if (movementMagnitude > 0.1f)
        // {
        //     Anim.CrossFade(MoveState, 0.1f, 0, 0);
        //     // if (!walkSoundFB.IsPlaying)
        //     // {
        //     //     walkSoundFB.PlayFeedbacks();
        //     // }

        // }

        // else
        //     Anim.CrossFade(IdleState, 0.1f, 0, 0);
    }


    float GetXZVector2(Vector3 input1, Vector3 input2)
    {
        Vector2 outcome = new Vector2(input1.x - input2.x, input1.z - input2.z);

        return outcome.magnitude;
    }

}

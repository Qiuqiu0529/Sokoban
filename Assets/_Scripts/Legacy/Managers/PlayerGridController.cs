using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lofelt.NiceVibrations;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerGridController : MonoBehaviour
{
    private Vector3 currentDir = Vector3.forward;
    
    private Vector3 inputDir;

    public Vector3 realmOffset;

    public Transform controllingObject;
    public bool isPossessing;

    public List<NPCSpirit> npcSpirits;

    public delegate void ChangeCamera();
    public static event ChangeCamera OnChangeCamera;

    // Start is called before the first frame update
    void Start()
    {
        controllingObject = this.transform;
        npcSpirits = new List<NPCSpirit>();
        GetSpirits();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        CheckPossess();
        if(Input.GetKeyDown(KeyCode.C) && !isPossessing)
        {
            Bark();
        }
    }

    void CheckInput()
    {
        
        if(Input.GetKeyDown(KeyCode.W))
        {
            inputDir = Vector3.forward;
        }
        else if(Input.GetKeyDown(KeyCode.S))
        {
            inputDir = -Vector3.forward;
        }
        else if(Input.GetKeyDown(KeyCode.A))
        {
            inputDir = -Vector3.right;
        }
        else if(Input.GetKeyDown(KeyCode.D))
        {
            inputDir = Vector3.right;
        }
        else
        {
            inputDir = Vector3.zero;
        }

        if(inputDir == Vector3.zero) return;

        currentDir = inputDir;
        AnimatePlayer();

        if(!isPossessing)
        {
            if(!CheckObstacle(inputDir))
            {
                PlayerMove(inputDir);
            }
        }
        else
        {
            if(controllingObject.GetComponent<PumpkinMoveOld>())
            {
                PumpkinMoveOld pumpkinMove = controllingObject.GetComponent<PumpkinMoveOld>();
                if(!pumpkinMove.isMoving)
                {
                    pumpkinMove.Rolling(inputDir);
                }
                return;
            }
            if(controllingObject.GetComponent<BarrelMoveOld>())
            {
                BarrelMoveOld barrelMove = controllingObject.GetComponent<BarrelMoveOld>();
                if(!barrelMove.isMoving)
                {
                    barrelMove.Rolling(inputDir);
                }
                return;
            }
            if(controllingObject.GetComponent<LogMove>())
            {
                LogMove logMove = controllingObject.GetComponent<LogMove>();
                if(!logMove.isMoving)
                {
                    logMove.Rolling(inputDir);
                }
                return;
            }
            if(controllingObject.GetComponent<ScareCrow>())
            {
                ScareCrow scareCrow = controllingObject.GetComponent<ScareCrow>();
                if(!scareCrow.isMoving)
                {
                    scareCrow.Jumping(inputDir);
                }
                return;
            }
            if(controllingObject.GetComponent<ClothMove>())
            {
                ClothMove clothMove = controllingObject.GetComponent<ClothMove>();
                clothMove.Move(inputDir);
                
                return;
            }
        }

    }

    void AnimatePlayer()
    {
        this.transform.LookAt(this.transform.position + currentDir);
    }

    bool CheckObstacle(Vector3 direction)
    {
        Ray ray = new Ray(this.transform.position, direction);
        if(Physics.Raycast(ray, out RaycastHit hitInfo, 1))
        {
            if(hitInfo.collider.CompareTag("Pushable"))
            {
                Pushable pushable = hitInfo.transform.GetComponent<Pushable>();
                if(pushable.CheckPush(direction))
                {
                    if(!pushable.trailWithHead) hitInfo.transform.GetComponent<Pushable>().CastTrail();
                    PlayerMove(direction);
                    hitInfo.transform.position += direction;
                    if(pushable.trailWithHead) hitInfo.transform.GetComponent<Pushable>().CastTrail();
                }
            }
            else if(hitInfo.collider.CompareTag("NPCSpirit"))
            {
                if(hitInfo.transform.GetComponent<NPCSpirit>().CheckPush(direction))
                {
                    // this.transform.position += direction;
                    // hitInfo.transform.position += direction;
                }
            }            
            return true;
        }
        return false;
    }

    public void PlayerMove(Vector3 input)
    {
        GameManager.SaveGameState();
        this.transform.position += input;
        inputDir = Vector3.zero;
    }

    void CheckPossess()
    {
        if(Input.GetKeyDown(KeyCode.X))
        {
            if(!isPossessing)
            {
                Ray ray = new Ray(this.transform.position + realmOffset + Vector3.up, Vector3.down);
                RaycastHit hitInfo;
                Physics.Raycast(ray,out hitInfo, 1);
                
                if( hitInfo.collider != null)
                {
                    if(hitInfo.collider.CompareTag("Possessable"))
                    {
                        GameManager.SaveGameState();
                        if(hitInfo.transform.parent && hitInfo.transform.parent.GetComponent<LogMove>())
                        {
                            LogMove logMove = hitInfo.transform.parent.GetComponent<LogMove>();
                            logMove.playerIndex = logMove.checkPoints.IndexOf(hitInfo.transform);
                            
                            Possess(hitInfo.transform.parent);
                        }

                        else Possess(hitInfo.transform);
                    }
                    else
                    {
                        Debug.Log("Can't Possess");
                    }
                }
                else
                {
                    Debug.Log("Nothing To Possess");
                }
            }
            else
            {
                if(controllingObject.GetComponent<LogMove>())
                {
                    CheckDePossessLog();
                    return;
                }
                
                Ray ray = new Ray(controllingObject.position - realmOffset + Vector3.up, Vector3.down);
                RaycastHit hitInfo;
                Physics.Raycast(ray,out hitInfo, 1);
                if(hitInfo.collider == null)
                {
                    GameManager.SaveGameState();
                    DePossess();
                }
                else
                {
                    Debug.Log("Can't Go back to Spirit Realm Here");
                }
            }

        }
    }

    void CheckDePossessLog()
    {
        LogMove controllingLog = controllingObject.GetComponent<LogMove>();
        Ray rayLog = new Ray(controllingLog.checkPoints[controllingLog.playerIndex].position - realmOffset + Vector3.up * 5, Vector3.down);
        RaycastHit hitInfoLog;
        Physics.Raycast(rayLog, out hitInfoLog, 5);
        if(hitInfoLog.collider == null)
        {
            GameManager.SaveGameState();
            DePossess();
            return;
        }

        Debug.Log("Can't Go back to Spirit Realm Here");
    }

    public void Possess(Transform hit)
    {
        this.transform.position -= Vector3.up * 2;
        isPossessing = true;
        controllingObject = hit;
        Debug.Log("Possess");
        OnChangeCamera?.Invoke();
    }

    public void DePossess()
    {
        this.transform.position = controllingObject.position - realmOffset;
        
        if(controllingObject.GetComponent<LogMove>())
        {
            LogMove logMove = controllingObject.GetComponent<LogMove>();
            this.transform.position = logMove.checkPoints[logMove.playerIndex].position - realmOffset;
            logMove.playerIndex = -1;
        }

        isPossessing = false;
        controllingObject = this.transform;
        Debug.Log("DePossess");
        OnChangeCamera?.Invoke();
    }

    void Bark()
    {
        Debug.Log("Bark");
        foreach(var npcSpirit in npcSpirits)
        {
            npcSpirit.CheckBark();
        }
    }

    void GetSpirits()
    {
        GameObject[] spirits = GameObject.FindGameObjectsWithTag("NPCSpirit");

        foreach(var spirit in spirits)
        {
            if(npcSpirits.Contains(spirit.GetComponent<NPCSpirit>())) continue;
            npcSpirits.Add(spirit.GetComponent<NPCSpirit>());
        }
    }
}

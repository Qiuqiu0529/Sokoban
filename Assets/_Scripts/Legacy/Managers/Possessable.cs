using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Possessable : MonoBehaviour
{
    //ASSIGN THIS SCRIPT TO AN EMPTY GAME OBJECT NOT TO THE PLAYER OR THE OBJECTS !! 


    public GameObject Player;
    [SerializeField]
    public float range = 1.0f;
    private GameObject currentPossessedObject; // The object currently possessed
    private bool possessionBool = false;
    //private CameraScript camScript;
    private Vector3 spawnOffset;                        // Where player respawns after stop posession


   

    // Start is called before the first frame update
    void Start()
    {

        spawnOffset = new Vector3(0, 0, -1.5f);
    }

    // Update is called once per frame
    void Update()
    {

        if (possessionBool)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                StopPossessObject();
            }
            return; // Exit update early
        }

        GameObject[] possessableObjects = GameObject.FindGameObjectsWithTag("Possessable");
        foreach (GameObject obj in possessableObjects)
        {
            float distance = Vector3.Distance(Player.transform.position, obj.transform.position); // check distance to player

            if (distance <= range) // compare current distance with the set range
            {
                if (Input.GetKeyDown(KeyCode.P) && !possessionBool)
                {
                    Debug.Log("Attempting to possess object");
                    PossessObject(obj);
                    return; // Exit the loop once possession starts
                }


            }
        }
    }

    public void PossessObject(GameObject obj)
    {
        Debug.Log("Player swapping engaged. Starting possession");
        // somehow disable player movement
        obj.GetComponent<PossessedController>().enabled = true; // activates a script on the possessable item <-- it needs to be there but unactive first
        possessionBool = true;
        currentPossessedObject = obj;           // Marks what we are possessing
        //camScript.player = currentPossessedObject.transform;

        Player.SetActive(false);                //Hides player
        Debug.Log($"Possessed {obj.name}.");
    }

    public void StopPossessObject()
    {
        Debug.Log("Player swapping engaged. Releasing possession");
        // somehow reenable player movement
        currentPossessedObject.GetComponent<PossessedController>().enabled = false;     // disbables movement script on possessed object
        possessionBool = false;
        Player.transform.position = currentPossessedObject.GetComponent<Transform>().position + spawnOffset;
        currentPossessedObject = null;                               // clear possession
        Player.SetActive(true);
        Debug.Log("Possession released.");

        //camScript.player = Player.transform;
        Debug.Log("Freed self from possession.");
    }

}     //ASSIGN THIS SCRIPT TO AN EMPTY GAME OBJECT NOT TO THE PLAYER OR THE OBJECTS !! 



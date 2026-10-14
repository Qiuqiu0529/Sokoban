using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class PlayerScript : MonoBehaviour
{
    public NavMeshAgent navAgent;
    //public NavMeshAgent trailAgent;
    public Transform portal;
    public Light dirLight;
    public GameObject spirit;
    private bool trailBool;
    private float lastTrailVel;
    private float trailAcc;
    private bool normalWorld;
    private GameObject[] posessables;

    // Start is called before the first frame update
    void Start()
    {
        trailBool = false;
        trailAcc = 1f;
        lastTrailVel = 0f;
        //Physics.IgnoreLayerCollision(6, 7);
        Physics.IgnoreLayerCollision(6, 8);
        normalWorld = true;
        posessables = GameObject.FindGameObjectsWithTag("Possessable");
        spirit = GameObject.FindGameObjectWithTag("NavAgent");

        foreach (var pos in posessables)
        {
            if (pos.layer == 8)
            {
                pos.gameObject.GetComponent<Renderer>().enabled = false;
            }
            else
            {
                pos.gameObject.GetComponent<Renderer>().enabled = true;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        // handle sending the navmesh agent to the portal
        if (Input.GetKeyDown(KeyCode.O))
        {
            trailBool = true;
            Debug.Log("Sent NavMesh agent towords portal");
            navAgent.SetDestination(portal.position);

        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("switching dimension");
            normalWorld = !normalWorld;
            

            if (normalWorld)
            {
                navAgent.autoRepath = true;
                spirit.GetComponent<Renderer>().enabled = true;
                dirLight.color = Color.HSVToRGB(212.78f, 56.07f, 67.84f);
                foreach (var pos in posessables)
                {
                    if (pos.layer == 8)
                    {
                        pos.gameObject.GetComponent<Renderer>().enabled = false;
                    } else
                    {
                        pos.gameObject.GetComponent<Renderer>().enabled = true;
                    }
                }
            } else
            {
                navAgent.autoRepath = false;
                spirit.GetComponent<Renderer>().enabled = false;
                dirLight.color = Color.cyan;
                foreach (var pos in posessables)
                {
                    

                    if (pos.layer == 8)
                    {
                        pos.gameObject.GetComponent<Renderer>().enabled = true;
                    }
                    else
                    {
                        pos.gameObject.GetComponent<Renderer>().enabled = false;
                    }
                }

            }

        }

        if (navAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            trailAcc = (navAgent.velocity.sqrMagnitude - lastTrailVel) / Time.deltaTime;

            if (navAgent.velocity.sqrMagnitude < 0.2)
            {
                if (trailAcc < 0)
                {
                    //Debug.Log("Path partial");

                }


            }

            lastTrailVel = navAgent.velocity.sqrMagnitude;
        }



        if (trailBool && navAgent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            if (navAgent.pathPending == false) {
                Debug.Log("Path ready!");
                trailBool = false;
                //.spirit.GetComponent<Material>().color = Color.yellow;
            }

            
        }


    }
}

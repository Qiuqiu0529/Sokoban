using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Target : MonoBehaviour
{
    public int index;
    public bool isTouched;
    private List<NPCSpirit> npcSpirits = new List<NPCSpirit>(); 
    // Start is called before the first frame update
    void Start()
    {
        foreach(var npc in GameObject.FindGameObjectsWithTag("NPCSpirit"))
        {
            npcSpirits.Add(npc.GetComponent<NPCSpirit>());
        }
    }

    private object FindObjectsByType<T>()
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.RotateAround(this.transform.position,Vector3.up, 45*Time.deltaTime);

        CheckTouch();
    }

    void CheckTouch()
    {
        Vector2 npcPos = Vector2.one * -100;
        Vector2 thisPos = new Vector2(this.transform.position.x, this.transform.position.z);
        foreach(var npc in npcSpirits)
        {
            if(npc.index == this.index) npcPos = new Vector2(npc.transform.position.x, npc.transform.position.z);
        }

        if(npcPos == Vector2.one * -100) Debug.Log("npc or target index is not configured properly");

                //Debug.Log((npcPos - thisPos).magnitude);
        if((npcPos - thisPos).magnitude < 0.1f) isTouched = true;
        else isTouched = false;
    }
}

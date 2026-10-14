using System.Collections.Generic;
using UnityEngine;

public class WinCheck : MonoBehaviour
{
    public List<Target> targets = new List<Target>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(var target in GameObject.FindGameObjectsWithTag("Target"))
        {
            targets.Add(target.GetComponent<Target>());
        }
    }

    
    private float wincheckTimer;
    public float wincheckTime = 1.5f;
    public bool willWin;
    // Update is called once per frame
    void Update()
    {
        if(targets.Count == 0) return;
        
        
        if(willWin)
        {
            //LoadSceneHere
            Debug.Log("Win!!!");
            return;
        }
        
        
        if(CheckWin())
        {
            if(wincheckTimer > wincheckTime)
            {
                willWin = true;
            }
            wincheckTimer += Time.deltaTime;
        }
        else
        {
            wincheckTimer = 0;
        }
    }

    bool CheckWin()
    {
        if(targets.Count == 0)
        {
            return false;
        }

        foreach(var target in targets)
        {
            if(!target.isTouched) return false;
        }


        return true;
    }
}

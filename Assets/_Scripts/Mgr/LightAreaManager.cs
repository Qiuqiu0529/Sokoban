using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightAreaManager : Singleton<LightAreaManager>
{
    public List<Transform> initialLightAreas = new List<Transform>();
    public Material lightMat;
    public Material deadGroundMat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<GameObject> grounds = GameObject.FindGameObjectsWithTag("Ground").ToList();
        foreach(var ground in grounds)
        {
            if(ground.transform.GetComponent<MeshRenderer>().sharedMaterial == deadGroundMat)
                if(ground.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial == lightMat)
                    initialLightAreas.Add(ground.transform);
        }

        Debug.Log("initial Light Area: " + initialLightAreas.Count);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
public class FBItem : MonoBehaviour
{
    bool enterScene;
    public string levelName;
    public MMF_Player mMF_Player;
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(!enterScene&&Input.GetKey(KeyCode.E))
            {
                enterScene=true;
                LevelMgr.Instance.TryLoadLevel(levelName);
            }
            
            //mMF_Player?.PlayFeedbacks();
        }
            
    }
    
}

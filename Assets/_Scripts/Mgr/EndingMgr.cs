using System.Collections;
using UnityEngine;

public class EndingMgr : Singleton<EndingMgr>
{
    int pairCount=0;
    int currentCount=0;

    void Start()
    {
        currentCount=0;
    }

    public void AddNeedPair()
    {
        ++pairCount;
    }
    public void DeleteNeedPair()
    {
        --pairCount;
    }

    public void AddCorrectPair()
    {
        ++currentCount;
        CheckEnd();
    }
    public void DeletePair()
    {
        --currentCount;
        CheckEnd();
    }

    public void CheckEnd()
    {
        if(currentCount>=pairCount)
        {
            Debug.Log("All clear!");
            StartCoroutine(Check());
        }
        else
        {
            Debug.Log("Not finish");
        }
    }
    
    public IEnumerator Check()
    {
        yield return new WaitForSeconds(0.3f);
        while(currentCount>=pairCount)
        {
            if(GameMgr.Instance.CanCheckEnd())
            {
                ProgressMgr.Instance.MarkLevelCleared(GameMgr.Instance.levelName);
                LevelMgr.Instance.AfterLevelClear(GameMgr.Instance.levelName);
                break;
            }
            yield return new WaitForSeconds(0.1f);

        }
        yield return 0;

    }
}

using UnityEngine;

public class MiniMapMgr : Singleton<MiniMapMgr>
{
    public GameObject[] minimaps;

    public void ActiveMiniMap()
    {
        foreach (var minimap in minimaps)
        {
            if(minimap != null) minimap.SetActive(true);
        }
    }

    public void DeActiveMiniMap()
    { 
         foreach (var minimap in minimaps)
        {
            if (minimap != null) minimap.SetActive(false);
        }
    }
}

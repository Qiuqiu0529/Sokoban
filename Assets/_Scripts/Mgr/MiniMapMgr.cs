using UnityEngine;

public class MiniMapMgr : Singleton<MiniMapMgr>
{
    public GameObject[] minimaps;

    public void ActiveMiniMap()
    {
        if (minimaps == null) return;
        foreach (var minimap in minimaps)
        {
            if(minimap != null) minimap.SetActive(true);
        }
    }

    public void DeActiveMiniMap()
    { 
         if (minimaps == null) return;
         foreach (var minimap in minimaps)
        {
            if (minimap != null) minimap.SetActive(false);
        }
    }
}

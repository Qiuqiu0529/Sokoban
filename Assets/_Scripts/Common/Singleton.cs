using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> :MonoBehaviour where T:Component
{
    private static T instance;
    public static T Instance
    {
        get 
        {
            if(instance==null)
            {
                instance=(T)FindObjectOfType(typeof(T));
                if(instance==null)
                {
                    SetUpInstance();
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        RemoveOther();
    }

    private static void SetUpInstance()
    {
        GameObject gameObject=new GameObject();
        gameObject.name=typeof(T).Name;
        instance=gameObject.AddComponent<T>();
    }

    private void RemoveOther()
    {
        if(instance==null)
        {
            instance=this as T;
        }
        else
        {
            Destroy(gameObject);
        }

    }
}

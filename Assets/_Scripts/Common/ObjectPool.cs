using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class ObjectPool<T> where T : MonoBehaviour
{
    private Stack<T> pool = new Stack<T>();
    private Func<T> factoryMethod;
    private int maxSize;
    private Transform poolParent;

    public ObjectPool(Func<T> factoryMethod, int maxSize, Transform poolParent = null)
    {
        this.factoryMethod = factoryMethod;
        this.maxSize = maxSize;
        this.poolParent = poolParent;
    }

    public T Get()
    {
        T obj;
        if (pool.Count > 0)
        {
            obj = pool.Pop();
            obj.gameObject.SetActive(true);
        }
        else
        {
            obj = factoryMethod();
        }

        return obj;
    }

    public void Release(T obj)
    {
        obj.gameObject.SetActive(false);
        if (poolParent != null)
        {
            obj.transform.SetParent(poolParent);
        }
        pool.Push(obj);

        if (pool.Count > maxSize)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            T randomObj = pool.ToArray()[randomIndex];

            pool = new Stack<T>(new Stack<T>(pool).Where(o => !o.Equals(randomObj)));
            GameObject.Destroy(randomObj.gameObject);
        }

    }


    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Pop();
            GameObject.Destroy(obj.gameObject);
        }
    }

    public int Count()
    {
        return pool.Count;
    }

}

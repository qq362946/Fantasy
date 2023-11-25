using System;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;

public class Pool
{
    private static Pool _instance = null;
    public static Pool Instance
    {
        get{return _instance;}
    }
    static Pool(){
        _instance = new Pool();
    }

    /// <summary>
    /// 获取，从对象池中取出
    /// </summary>
    public GameObject Get(PoolType type, string prefabName, string name = null)
    {
        switch (type)
        {
            case PoolType.UIPanel:
                return UIPanelPool.Instance.Get(prefabName,name);
            case PoolType.Game:
                return GamePool.Instance.Get(prefabName,name);
            case PoolType.UI:
                return UIPool.Instance.Get(prefabName,name);
            case PoolType.Unit:
                return UnitPool.Instance.Get(prefabName,name);
            default:
                throw new ArgumentOutOfRangeException("type", type, null);
        }
    }
    public GameObject Get(PoolType type, string name, GameObject prefab)
    {
        switch (type)
        {
            case PoolType.UIPanel:
                return UIPanelPool.Instance.Get(name,prefab);
            case PoolType.Game:
                return GamePool.Instance.Get(name,prefab);
            case PoolType.UI:
                return UIPool.Instance.Get(name,prefab);
            case PoolType.Unit:
                return UnitPool.Instance.Get(name,prefab);
            default:
                throw new ArgumentOutOfRangeException("type", type, null);
        }
    }

    /// <summary>
    /// 回收，放入对象池
    /// </summary>
    public void Push(PoolType type, string itemName,GameObject item)
    {
        switch (type)
        {
            case PoolType.UIPanel:
                UIPanelPool.Instance.Push(itemName,item);
                break;
            case PoolType.Game:
                GamePool.Instance.Push(itemName,item);
                break;
            case PoolType.UI:
                UIPool.Instance.Push(itemName,item);
                break;
            case PoolType.Unit:
                UnitPool.Instance.Push(itemName,item);
                break;
            default:
                throw new ArgumentOutOfRangeException("type", type, null);
        }
    }

}
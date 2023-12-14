using System;
using System.Collections.Generic;
using UnityEngine;

public class BasePool
{
    protected Dictionary<string, Stack<GameObject>> objectPoolDict = new Dictionary<string, Stack<GameObject>>(); // 对象池字典
    protected Dictionary<string, GameObject> factoryDict = new Dictionary<string, GameObject>(); // 游戏物体资源(预制体)的字典
    public string loadPath = "Prefabs/"; // 加载路径
    public GameObject itemPool; // 对象池的父物体


    /// <summary>
    /// 获取，从对象池中取出
    /// </summary>
    public GameObject TryGet(string prefabName,string name = null)
    {
        GameObject item = TryGetFromPool(prefabName,name);
        if (item != null) item.SetActiveX(true);
        return item;
    }
    public GameObject TryGet(string name,GameObject prefab)
    {
        GameObject item = TryGetFromPool(name,prefab);
        if (item != null) item.SetActiveX(true);
        return item;
    }

    /// <summary>
    /// 回收，放入对象池
    /// </summary>
    public void Push(string name,GameObject item)
    {
        item.SetActiveX(false); // 将要放入对象池的游戏物体失效
        item.transform.SetParent(itemPool.transform);

        if (objectPoolDict.ContainsKey(name)) // 如果存在该对象池才放入(安全校验)
        {
            if (objectPoolDict[name].Count > 0) return;
            objectPoolDict[name].Push(item);
        }
        else // 异常处理(警告)
        {
            Debug.LogWarning(string.Format("对象池Push异常: 不存在{0}对象池", name));
        }
    }

    /// <summary>
    /// 尝试从对象池中取得实例或创建实例
    /// </summary>
    private GameObject TryGetFromPool(string prefabName,string name = null)
    {
        if (name == null) name = prefabName;

        // 如果不存在该对象池则创建
        if (!objectPoolDict.ContainsKey(name)) 
            objectPoolDict.Add(name, new Stack<GameObject>());
        
        // 对象池为空则创建实例，否则取出对象池中的实例
        if (objectPoolDict[name].Count == 0) 
        {
            return GetResource(prefabName,name);
        }

        return objectPoolDict[name].Pop();
    }

    private GameObject TryGetFromPool(string name,GameObject prefab)
    {
        GameObject item = null;

        // 如果不存在该对象池则创建
        if (!objectPoolDict.ContainsKey(name)) 
            objectPoolDict.Add(name, new Stack<GameObject>());
        
        // 对象池为空则创建实例，否则取出对象池中的实例
        if (objectPoolDict[name].Count == 0) 
        {
            if (prefab != null)
            {
                prefab.name = name;
                item = GameObject.Instantiate(prefab);
                item.name = name;
                return item;
            }
        }

        return objectPoolDict[name].Pop();
    }

    /// <summary>
    /// 从Resources加载资源的方法
    /// </summary>
    public GameObject GetResource(string prefabName,string name)
    {
        string path = loadPath + prefabName;

        if (factoryDict.ContainsKey(name)) // 如果工厂中有该资源
            return factoryDict[name];
        
        var prefab = Resources.Load<GameObject>(path); // 从文件夹中加载该资源
        prefab.name = name;

        GameObject go = GameObject.Instantiate(prefab);
        go.name = name;
        factoryDict.Add(name, go); // 将得到的资源放入资源字典当中
        
        if (go == null) //异常处理
            Debug.LogError(string.Format("资源加载异常: {0}资源加载失败,加载路径为{1}", name, path));
        
        return go;
    }
}
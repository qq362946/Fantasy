using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameFacade : Singleton<GameFacade>
{
    public void CamLocation(Transform location){
        Camera.main.transform.position = location.position;
        Camera.main.transform.rotation = location.rotation;
    }

    public void SetMMOCamera(Transform target){
        CameraMMO cameraMMO = Camera.main.GetComponent<CameraMMO>();
        cameraMMO.enabled = true;
        cameraMMO.target = target;
    }

    public void ReSetMMOCamera(){
        CameraMMO cameraMMO = Camera.main.GetComponent<CameraMMO>();
        cameraMMO.enabled = false;
        cameraMMO.target = null;
    }

    // 从对象池获取Unit
    // 没创建时从Resources加载
    public GameObject TryGet(PoolType type,string prefabName,string name = null)
    {
        return Pool.Instance.TryGet(type,prefabName,name);
    }

    // 将Unit放回对象池
    public void Push(PoolType type,string name, GameObject item)
    {
        Pool.Instance.Push(type,name, item);
    }
}
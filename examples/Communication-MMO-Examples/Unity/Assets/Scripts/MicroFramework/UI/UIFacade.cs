using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFacade : MonoBehaviour
{
    // UI面板GameObject容器
    public Dictionary<string, GameObject> currentScenePanelGoDict;
    //UIPanel对象容器(当前场景状态下的UIPanel脚本对象)
    public Dictionary<string, BasePanel> currentScenePanelDict;
    public Transform canvas; // UIPanel放置的容器

    // 场景状态
    public UIScene currentScene;
    public UIScene lastScene;
    public BasePanel lastPanel = null;

    private static UIFacade _instance = null;
    public static UIFacade Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIFacade");
                _instance = go.AddComponent<UIFacade>();
            }

            return _instance;
        }
    } 

    private void Awake()
    {
        // 初始UI容器
        currentScenePanelGoDict = new Dictionary<string, GameObject>();
        currentScenePanelDict = new Dictionary<string, BasePanel>();

        // 获得Canvas
        canvas = GameObject.Find("Canvas").transform;
    }

    // 改变当前场景的状态
    public void EnterScene(UIScene scene)
    {
        scene.EnterScene();
        currentScene = scene;
    }
    
    // 获取UIPanel
    public BasePanel GetUIPanel(string pName){
        return currentScenePanelDict[pName];
    }

    // 实例化当前场景下的UIPanel并存入字典
    public void InitUIPanelDict()
    {
        
        foreach (var item in currentScenePanelGoDict)
        {
            item.Value.transform.SetParent(canvas);
            item.Value.transform.localPosition = Vector3.zero;
            item.Value.transform.localScale = Vector3.one;
            item.Value.SetActive(false); // 初始时先取消Panel的激活状态

            BasePanel basePanel = item.Value.GetComponent<BasePanel>();
            if (basePanel == null)
                Debug.LogWarning(string.Format("{0}上的IBasePanel脚本丢失!", item.Key));
            
            basePanel.InitPanel();  // UIPanel初始化UI
            currentScenePanelDict.Add(item.Key, basePanel); // 将该场景下的UIPanel身上的Panel脚本添加进字典中
            //Debug.Log(string.Format("UIPanel字典添加{0}成功", item.Key));
        }
    }

    // 将UIPanel添加进UIManager字典
    public void AddPanelToDict(string uIPanelName)
    {
        currentScenePanelGoDict.Add(uIPanelName, GetPanel(uIPanelName));
        //Debug.Log(mUIManager.currentScenePanelDict.Count);
    }

    // 清空UIPanel字典,并将所有UIPanel放回对象池
    public void ClearUIPanelDict()
    {
        currentScenePanelDict.Clear();

        foreach (var item in currentScenePanelGoDict)
        {
            PushPanel(item.Key, item.Value);
        }
        currentScenePanelGoDict.Clear();
    }

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

    // 获取Panel
    public GameObject GetPanel(string prefabName,string name = null)
    {
        return Pool.Instance.Get(PoolType.UIPanel,prefabName,name);
    }

    // 将Panel放回对象池
    public void PushPanel(string name, GameObject item)
    {
        Pool.Instance.Push(PoolType.UIPanel,name, item);
    }

    // 获取Panel
    public GameObject GetUI(string prefabName,string name = null)
    {
        return Pool.Instance.Get(PoolType.UI,prefabName,name);
    }
    public GameObject GetUI(string name, GameObject prefab)
    {
        return Pool.Instance.Get(PoolType.UI,name,prefab);
    }
    

    // 将Panel放回对象池
    public void PushUI(string name, GameObject item)
    {
        Pool.Instance.Push(PoolType.UI,name, item);
    }

}

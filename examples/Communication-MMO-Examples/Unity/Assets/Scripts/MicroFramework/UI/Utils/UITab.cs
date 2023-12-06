using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UITab : MonoBehaviour
{
    [HideInInspector]public string tabName;
    public Transform content; 
    public GameObject tabPrefab;
    
    public Color downColor;

    public Action<string> action;
    // 我们熟悉定义一个delegate来写委托
    // 其实也可以用Action,Func来写。无返回就用Action，有返回就用Func

    // CreateTabList方法用于创建tab按钮列表
    public GameObject[] CreateTabList(int count,Vector2 dir,float offset,
                                    string[] titles=null,Action<string> action=null)
    {
        var list = new List<GameObject>();
        this.action = action;
        for(int i=0;i<count;i++){
            string title;
            if(titles!=null) title = titles[i];
            else title = tabName+i;

            GameObject go = UIFacade.Ins.GetUI(title,tabPrefab.gameObject);
            go.transform.SetParent(content);
            go.name = title;
            // pos位置
            go.GetComponent<RectTransform>().anchoredPosition = i*dir*offset;
            list.Add(go);
        }
        RefreshTab();
        return list.ToArray();
    }

    // 刷新tab按钮事件状态
    // 是因为有的tab按钮是动态生成的，存在用完清除的情况，这样当再次创建时需要调用。
    public void RefreshTab(){
        // 第一个tab为默认状态
        if(content.childCount>0) {
            content.GetChild(0).GetComponent<Button>().image.color = downColor;
        }
        // 按钮按下时状态
        foreach(Transform go in content)
        {
            Button btn = go.gameObject.GetComponent<Button>();
            btn.onClick.SetListener(() => {
                ResetButton();
                btn.image.color = downColor;

                //btn.Select();
                if(action!=null) action(btn.name);
            });
        }
    }

    public void ClearTablist(){
        ResetButton();
        for (int i = content.childCount - 1; i >= 0; i--) {
            GameObject go = content.GetChild(i).gameObject;
            UIFacade.Ins.PushUI(go.name,go);
        }
    }

    void ResetButton(){
        foreach(var btn in content.GetComponentsInChildren<Button>())
        {
           btn.image.color = Color.white;
        }
    }
}
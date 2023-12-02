using UnityEngine;

/// <summary>
/// Panel基类
/// </summary>
public class BasePanel : BehaviourNonAlloc
{
    protected UIFacade mUIFacade;
    protected string backToPanel;
    protected bool isPart = false;

    protected virtual void Awake()
    {
        // 获得UIFacade
        mUIFacade = UIFacade.Ins;
    }

    public virtual void InitPanel(){}
    public virtual void UpdatePanel(){}

    public virtual void EnterPanel()
    {
        GetComponent<RectTransform>().offsetMin = new Vector2(0.0f, 0.0f);
        GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, 0.0f);
        gameObject.SetActive(true);
        
        if(mUIFacade.lastPanel!=null && !isPart) mUIFacade.lastPanel.ExitPanel();
        if(!isPart)mUIFacade.lastPanel = this;
    }

    public virtual void ExitPanel()
    {
        ResetPanel();
        gameObject.SetActive(false);
    }

    public virtual void ResetPanel()
    {
        
    }

    public virtual void BackToPanel()
    {
        if(!isPart)mUIFacade.GetUIPanel(backToPanel).EnterPanel();
        
        ExitPanel();
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
public class UIScene
{
    string sceneName;
    protected UIFacade mUIFacade;
    
    public UIScene()
    {
        mUIFacade = UIFacade.Instance;
        sceneName = SceneManager.GetActiveScene().name;

        if(mUIFacade.lastScene!=null)mUIFacade.lastScene.ExitScene();
        mUIFacade.lastScene = this;
    }

    public virtual void EnterScene()
    {
        mUIFacade.currentScene = this;
        mUIFacade.InitUIPanelDict();
    }

    public virtual void ExitScene()
    {
        mUIFacade.ClearUIPanelDict();
    }
}
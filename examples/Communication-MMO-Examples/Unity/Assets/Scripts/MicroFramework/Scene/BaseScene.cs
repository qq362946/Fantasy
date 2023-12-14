using UnityEngine;
using UnityEngine.SceneManagement;
public class BaseScene
{
    string sceneName;
    protected UIFacade mUIFacade;
    
    public BaseScene()
    {
        mUIFacade = UIFacade.Ins;
        sceneName = SceneManager.GetActiveScene().name;

        if(mUIFacade.lastScene!=null) mUIFacade.lastScene.ExitScene();
        mUIFacade.lastScene = this;
    }

    public virtual void EnterScene()
    {
        mUIFacade.currentScene = this;
    }

    public virtual void ExitScene()
    {
        mUIFacade.ClearUIPanelDict();
    }
}
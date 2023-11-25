using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapScene : UIScene
{
    public MapScene(){ 

    }

    public override void EnterScene()
    {
        mUIFacade.AddPanelToDict(StringManager.MapUIFramePanel);
        mUIFacade.AddPanelToDict(StringManager.SettingPanel);
        base.EnterScene();

        mUIFacade.GetUIPanel(StringManager.MapUIFramePanel).EnterPanel();
        mUIFacade.lastPanel = mUIFacade.GetUIPanel(StringManager.MapUIFramePanel);  
    }

}

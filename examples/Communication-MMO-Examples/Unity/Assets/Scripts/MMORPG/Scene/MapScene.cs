using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapScene : BaseScene
{
    public MapScene(){ 

    }

    public override void EnterScene()
    {
        // 添加UIPanel
        mUIFacade.AddPanelToDict(StringManager.MapUIFramePanel);
        mUIFacade.AddPanelToDict(StringManager.SettingPanel);
        base.EnterScene();

        // 打开MapUIFramePanel
        mUIFacade.GetUIPanel(StringManager.MapUIFramePanel).EnterPanel();
        mUIFacade.lastPanel = mUIFacade.GetUIPanel(StringManager.MapUIFramePanel);

        // playerUnits加入场景
        GameFacade.playerUnits.EnterScene();
        // NpcUnits加入场景
        GameFacade.NpcUnits.EnterScene();
        // MonsterUnits加入场景
        GameFacade.MonsterUnits.EnterScene();
    }

    public override void ExitScene()
    {
        base.ExitScene();

        // playerUnits退出场景
        GameFacade.playerUnits.ExitScene();
        // NpcUnits退出场景
        GameFacade.NpcUnits.ExitScene();
        // MonsterUnits退出场景
        GameFacade.MonsterUnits.ExitScene();
    }

}

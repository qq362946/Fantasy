using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fantasy;
/// <summary>
/// 登录面板
/// </summary>
public class ServerListPanel : BasePanel
{
    public ZoneSlot zoneSlot;
    public Transform content;
    public Button btn_submit;
    public Button btn_back;
    public Transform zoneTab;

    public int selectZoneId = 0;
    public int selectRegionId = 1;

    protected override void Awake()
    {
        base.Awake();
        isPart = true;
    }

    void Start()
    {
        foreach(var tab in zoneTab.GetComponentsInChildren<ZoneTabSlot>())
        {
            tab.btn.onClick.SetListener(() => {
                selectRegionId = tab.regionId;
                ResetButton();
                tab.btn.image.color = tab.selectedColor;
                ClearList();
                CreateZoneSlot();
            });
        }

        btn_submit.onClick.SetListener(() => {
            base.BackToPanel(); 
        });

        btn_back.onClick.SetListener(() => {
            base.BackToPanel(); 
        });
    }

    public override void EnterPanel()
    {
        base.EnterPanel();
        CreateZoneSlot();
        CreateZoneTab();
    }

    public override void ExitPanel()
    {
        ClearList();
        base.ExitPanel();
    }

    void CreateZoneTab()
    {
        int regionId = 0;
        foreach(var zone in Sender.Ins.zoneInfo)
        {
            if(regionId!=zone.RegionId){
                regionId++;
                var go = UIFacade.Ins.GetUI("zoneTabSlot","zoneTabSlot"+regionId);
                go.name = "zoneTabSlot"+regionId;
                go.transform.SetParent(zoneTab);
                go.GetComponent<RectTransform>().anchoredPosition = (regionId-1)*Vector2.right*82;

                var slot = go.GetComponent<ZoneTabSlot>();
                slot.regionId = regionId;
                var regionName = StringHelper.OneBitNumberToChinese(regionId.ToString())+"区";
                slot.regionName.text = regionName;
                slot.btn.image.color = regionId==selectRegionId?slot.selectedColor:Color.white;
            } 
        }
    }

    void CreateZoneSlot()
    {
        foreach(var zone in Sender.Ins.zoneInfo)
        {
            if(zone.RegionId != selectRegionId) continue;
            var go = UIFacade.Ins.GetUI("zoneSlot","zoneSlot"+zone.ZoneId);
            go.name = "zoneSlot"+zone.ZoneId;
            go.transform.SetParent(content);

            var slot = go.GetComponent<ZoneSlot>();
            slot.text.text = zone.ZoneName;
            slot.zoneId = zone.ZoneId;

            if(zone.ZoneId == (selectZoneId==0?Sender.Ins.defaultZone.ZoneId:selectZoneId)) 
                slot.btn.Select();
            
            slot.btn.onClick.SetListener(() => {
                selectZoneId = slot.zoneId;
                Sender.Ins.selectZone = zone;

                var loginPanel = mUIFacade.GetUIPanel(StringManager.LoginPanel) as LoginPanel;
                loginPanel.zoneInfo.text = zone.ZoneName;
            });
        }
    }

    void ResetButton()
    {
        foreach(var slot in zoneTab.GetComponentsInChildren<ZoneTabSlot>())
        {
           slot.btn.image.color = Color.white;
        }
    }
    void ClearList()
    {
        foreach(var slot in content.GetComponentsInChildren<ZoneSlot>())
        {
            UIFacade.Ins.PushUI("zoneSlot"+slot.zoneId,slot.gameObject);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using PlatformCharacterController;
public partial class Player : UnitBase
{
    [Header("Player Property")]
    public long RoleId;
    
    /// 绑定经验组件
    // [NonSerialized]
    // public Experience experience;

    // /// 获取基类绑定的背包组件
    // public PlayerInventory inventory => baseInventory as PlayerInventory;

    // /// 获取基类绑定的装备组件
    // public PlayerEquipment equipment => baseEquipment as PlayerEquipment;

    // /// 获取绑定的技能工具条组件
    // public PlayerSkillbar skillbar;


    // /// 玩家目标选择指示器
    // public PlayerIndicator indicator;

    /// 角色职业图标
    public Sprite classIcon; 

    /// 角色头像
    public Sprite portraitIcon;

    /// 全局本地玩
    public static string localName = "";
    public static Player localPlayer;
    public bool isLocalPlayer => localPlayer == this;
    [TextArea(1, 30)] public string toolTip; 

    new void Awake()
    {
        base.Awake();

        // 绑定经验组件
    }

    public override void OnInit()
    {
        base.OnInit();
        if(cached) return;
        
        if (SuperIsLocal()) 
        {
            gameObject.AddComponent<PlayerInput>();
            return;
        }
            
        // if(movement != null) Destroy(movement);
        // movement = gameObject.AddComponent<NetCharaMovement>();
        cached = true;
    }

    public override void OnShow()
    {
        base.OnShow();
        if(SuperIsLocal())
        {
            localPlayer = this;
            CharacterMovement localMovement = movement as CharacterMovement;
            GetComponent<PlayerInput>().enabled = true;
            localMovement.CanControl = true;
            localMovement.PlayerInputs = GetComponent<PlayerInput>();
        }
    }

    public override void OnHide()
    {
        base.OnHide();
        if(SuperIsLocal())
        {
            localPlayer = null;
            GetComponent<PlayerInput>().enabled = false;
        }
    }

    public override void UpdateStateLogic()
    {
        if (!isLocalPlayer) return;

        if (state == "IDLE" || state == "MOVING")
        {
            // 释放命令时TryUse技能，如果还有些距离没有CmdUse,
            // 当角色靠近并进入攻击范围时,仍然可以通过CmdUse攻击目标
            // ... 
        }
        else if (state == "CASTING")
        {
            // 保持面向目标
            // if (target && movement.DoCombatLookAt())
            //     movement.LookAtY(target.transform.position);

            // 如果escape键被按下，取消操作
            // ...
        }
        else if (state == "STUNNED")
        {
            // 如果escape键被按下，取消操作
            // ...
        }
        else if (state == "TRADING") {}
        else if (state == "CRAFTING") {}
        else if (state == "DEAD") {}
        else Debug.LogError("invalid state:" + state);
    }

    // 超前到,可在角色预制体创建实例前判断是否是本地角色
    bool SuperIsLocal()
    {
        string gameObjectName = gameObject.name;
        return string.Equals(Player.localName, gameObjectName, StringComparison.OrdinalIgnoreCase)
            || string.Equals($"{Player.localName}(Clone)", gameObjectName, StringComparison.OrdinalIgnoreCase);
    }
}
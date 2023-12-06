using System;
using System.Collections.Generic;
using UnityEngine;
using MicroCharacterController;
public partial class Player : GameEntity
{
    [Header("Player Property")]
    
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

    /// 全局本地玩家
    public static Player localPlayer;
    public bool isLocalPlayer => IsLocal();
    [TextArea(1, 30)] public string toolTip; 

    new void Awake()
    {
        base.Awake();

        // 绑定经验组件
    }

    public override void OnShow()
    {
        base.OnShow();
        if(IsLocal()) localPlayer = this;
    }

    public override void OnHide()
    {
        base.OnHide();
        localPlayer = null;

        RmovePlayerComponent();
        EnableController(false);
        CanControlMove(false);
    }

    /// 进入地图场景时，设置角色组件
    public override void SetPlayerComponent(){
        if(IsLocal()) {
            if (!TryGetComponent(out PlayerInput playerInput))
                gameObject.AddComponent<PlayerInput>();
            
            GetInputs();

            if (!TryGetComponent(out PlayerMoveSender moveSender))
                gameObject.AddComponent<PlayerMoveSender>();
        }else{
            if (TryGetComponent(out CharacterMovement cMovement))
                Destroy(cMovement);

            if (!TryGetComponent(out NetCharaMovement nMovement))
                gameObject.AddComponent<NetCharaMovement>();
        }

        CanControlMove();
    }
    
    /// 离开地图场景或隐藏缓存起来时，移除角色组件
    public override void RmovePlayerComponent()
    {
        if (TryGetComponent(out NetCharaMovement nMovement))
            Destroy(nMovement);

        if (TryGetComponent(out PlayerInput playerInput))
            Destroy(playerInput);

        if (TryGetComponent(out PlayerMoveSender moveSender))
            Destroy(moveSender);
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
    bool IsLocal()
    {
        if (GameManager.Ins == null) return false;
        string gameObjectName = gameObject.name;
        string localRoleId = GameManager.Ins.RoleId.ToString();
        return string.Equals(localRoleId, gameObjectName);
    }
}
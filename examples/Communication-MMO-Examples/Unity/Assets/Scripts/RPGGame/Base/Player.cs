using System;
using System.Collections.Generic;
using UnityEngine;

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
    [TextArea(1, 30)] public string toolTip; 

    new void Awake()
    {
        base.Awake();

        // 绑定经验组件
    }
}
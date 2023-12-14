using System;
using System.Collections.Generic;
using UnityEngine;
using MicroCharacterController;
public partial class Player : GameEntity
{
    [Header("Player Property")]
    /// 角色职业图标
    public Sprite classIcon; 

    /// 角色头像
    public Sprite portraitIcon;

    /// 全局本地玩家
    public static Player localPlayer;
    public bool isLocalPlayer = false;
    [TextArea(1, 30)] public string toolTip; 


    public override void OnShow()
    {
        if (!TryGetComponent(out CharacterMovement cMovement))
            movement = gameObject.AddComponent<CharacterMovement>();

        if(isLocalPlayer) localPlayer = this;
    }

    public override void OnHide()
    {
        localPlayer = null;
        isLocalPlayer = false;

        RmovePlayerComponent();
        EnableController(false);
        CanControlMove(false);
    }

    /// 进入地图场景时，设置角色组件
    public override void SetPlayerComponent(){
        if (TryGetComponent(out CharacterMovement cMovement))
                Destroy(cMovement);

        if(isLocalPlayer) {
            gameObject.AddComponent<PlayerInput>();
            gameObject.AddComponent<PlayerMoveSender>();
            movement = gameObject.AddComponent<CharacterMovement>();
        }else{
            movement = gameObject.AddComponent<NetCharaMovement>();
        }

        CanControlMove();
    }
    
    /// 离开地图场景或隐藏缓存起来时，移除角色组件
    public override void RmovePlayerComponent()
    {
        if (TryGetComponent(out CharacterMovement cMovement))
            Destroy(cMovement);

        if (TryGetComponent(out NetCharaMovement nMovement))
            Destroy(nMovement);

        if (TryGetComponent(out PlayerInput playerInput))
            Destroy(playerInput);

        if (TryGetComponent(out PlayerMoveSender moveSender))
            Destroy(moveSender);
    }
}
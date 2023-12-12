using System;
using System.Collections.Generic;
using UnityEngine;
using MicroCharacterController;

//Player，NPC，Monster等的父对象类
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public abstract class GameEntity : BehaviourNonAlloc
{
    [Header("Entity Property")]
    public int ConfigId;
    public string ClassName;
    public string nickName;

    public Animator animator;
    public Collider controlCollider;
    public AudioSource audioSource;

    [HideInInspector]public Movement movement;

    // // 角色等级
    // [HideInInspector]public Level level;
    // // 角色生命
    // [HideInInspector]public Health health;
    // // SkillAbility
    // [HideInInspector]public BaseAbility skillAbility;

    // 角色战斗或脱战状态  
    public bool inbattle = false; 

    [Header("State")]
    [SerializeField] string _state = "IDLE";
    public string state 
    {
        get { return _state; }
        set { _state = value; }
    }

    [Header("Target")]
    GameObject _target;
    /// 角色目标 
    public GameEntity target
    {
        get { return _target != null  ? _target.GetComponent<GameEntity>() : null; }
        set { _target = value != null ? value.gameObject : null; }
    }

    protected void Awake()
    {
        // 必要组件
        animator = GetComponent<Animator>();
        controlCollider = GetComponent<Collider>();
        // level = GetComponent<Level>();
        // health = GetComponent<Health>();

        // 可选组件
        if (TryGetComponent(out Movement movement))
            this.movement = movement;

        OnInit();
    }

    // 显示与隐藏相关
    void OnEnable()
    {
        OnShow();
    }

    void OnDisable()
    {
        OnHide();
    }

    protected bool cached = false;
    public virtual void OnInit()
    {
    }

    public virtual void OnShow()
    { 
    }

    public virtual void OnHide()
    {  
    }

    public virtual void SetPlayerComponent(){}
    public virtual void RmovePlayerComponent(){}

    public virtual void EnableController(bool enable = true)
    {
        movement.EnableController(enable);
    }

    public virtual void CanControlMove(bool enable = true)
    {
        movement.CanControlMove(enable);
    }
}
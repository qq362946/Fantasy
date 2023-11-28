using System;
using System.Collections.Generic;
using UnityEngine;
using PlatformCharacterController;

//Player，NPC，Monster等的父对象类
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public abstract class UnitBase : BehaviourNonAlloc
{
    [Header("Entity Property")]
    public long UnitId;
    public int configId;
    public string ClassName;

    public string nickName;

    [NonSerialized]
    public Animator animator;
    [NonSerialized]
    new public Collider collider;
    [NonSerialized]
    public AudioSource audioSource;

    [HideInInspector]public Movement movement;

    // // 角色等级
    // [HideInInspector]public Level level;
    // // 角色生命
    // [HideInInspector]public Health health;
    // // SkillAbility
    // [HideInInspector]public BaseAbility skillAbility;

    // /// 角色能力组件
    // [HideInInspector]public BaseAbility[] abilitys;

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
    public UnitBase target
    {
        get { return _target != null  ? _target.GetComponent<UnitBase>() : null; }
        set { _target = value != null ? value.gameObject : null; }
    }

    protected void Awake()
    {
        // 必要组件
        animator = GetComponent<Animator>();
        collider = GetComponent<Collider>();
        // level = GetComponent<Level>();
        // health = GetComponent<Health>();
        // abilitys = GetComponents<BaseAbility>();

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
        EnableCharacter(false);
    }

    public void ActiveController(Vector3 position)
    {
        movement.SetPosition(position);
        EnableCharacter(true);
    }

    public void EnableCharacter(bool enable)
    {
        GetComponent<CharacterController>().enabled = enable;
        movement.enabled = enable;
    }

    void Update()
    {
        if(GameManager.Ins.IsConnect == true){
            // 角色移动状态逻辑更新
            UpdateStateLogic(); 
        }
    }

    // 更新角色状态逻辑
    public virtual void UpdateStateLogic(){}
}
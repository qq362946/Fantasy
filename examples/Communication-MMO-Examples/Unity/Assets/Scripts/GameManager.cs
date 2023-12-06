using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using MicroCharacterController;

public class GameManager : SingletonMono<GameManager>
{
    public Location location;

    public long RoleId = 0;

    // 可选角色类集
    [HideInInspector] public List<Player> playerClasses = new List<Player>(); 
    [HideInInspector] public List<GameEntity> unitClasses = new List<GameEntity>(); 
    
    private new void Awake()
    {
        base.Awake();
        FindUnitPrefabs();
    }

    // 获取unit的角色预制体对象
    public void FindUnitPrefabs()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Unit");
        playerClasses = FindUnitClasses<Player>(prefabs);
        unitClasses = FindUnitClasses<GameEntity>(prefabs);
    }

    public List<T> FindUnitClasses<T>(GameObject[] prefabs)
    {
        return prefabs.Select(prefab => prefab.GetComponent<T>())
            .Where(entity => entity != null)
            .ToList();
    }
}
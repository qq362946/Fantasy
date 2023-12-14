using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class BaseUnits : MonoBehaviour
{
    [HideInInspector]public UnitViewer unitViewer;
    private Dictionary<long,GameObject> units = new Dictionary<long,GameObject>();

    protected virtual void Awake()
    {
        unitViewer = GetComponent<UnitViewer>() ?? gameObject.AddComponent<UnitViewer>();
    }

    public virtual void AddUnit(long id,GameObject go)
    {
        if(units.ContainsKey(id)) return;
        units.Add(id,go);
    }

    public virtual void RemoveUnit(long id)
    {
        if(!units.ContainsKey(id)) return;
        units.Remove(id);
    }

    public virtual GameObject GetUnit(long id)
    {
        if(!units.ContainsKey(id)) return null;
        return units[id];
    }

    public virtual void EnterScene()
    {
    }

    public virtual void ExitScene()
    { 
    }

}
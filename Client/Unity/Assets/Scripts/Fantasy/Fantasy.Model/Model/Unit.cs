using System.Collections.Generic;
using UnityEngine;

namespace Fantasy.Model
{
    public sealed class UnitManageComponent : Entity
    {
        public readonly Dictionary<long, UnitA> UnitAs = new Dictionary<long, UnitA>();
    }

    public enum UnitType
    {
        None = 0,
        NPC = 1,
        Monster = 2,
        Player = 3,
        Self = 4
    }
    
    public sealed class UnitA : Entity
    {
        public GameObject Obj;
        public UnitType UnitType;
        public int Age;
        public string Name;
    }

    public sealed class MoveComponent : Entity
    {
        
    }

    public sealed class UnitB : Entity
    {
        public int Age;
        public string Name;
    }
}
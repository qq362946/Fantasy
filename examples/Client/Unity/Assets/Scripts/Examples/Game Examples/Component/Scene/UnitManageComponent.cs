using System.Collections.Generic;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    public sealed class UnitManageComponentDestroySystem : DestroySystem<UnitManageComponent>
    {
        protected override void Destroy(UnitManageComponent self)
        {
            foreach (var (_, unit) in self.Units)
            {
                unit.Dispose();
            }

            self.Units.Clear();
        }
    }

    public sealed class UnitManageComponent : Entity
    {
        public readonly Dictionary<long, Unit> Units = new Dictionary<long, Unit>();

        public void Add(Unit unit)
        {
            if (Units.TryAdd(unit.Id, unit))
            {
                return;
            }
            Log.Error($"Unit {unit.Id} is already in use");
        }

        public bool Contains(long id)
        {
            return Units.ContainsKey(id);
        }

        public Unit Get(long id)
        {
            return Units.GetValueOrDefault(id);
        }

        public bool TryGet(long id, out Unit unit)
        {
            return Units.TryGetValue(id, out unit);
        }

        public void Remove(long id, bool isDispose = true)
        {
            if (!Units.Remove(id, out var unit))
            {
                return;
            }

            if (isDispose)
            {
                unit.Dispose();
            }
        }
    }
}
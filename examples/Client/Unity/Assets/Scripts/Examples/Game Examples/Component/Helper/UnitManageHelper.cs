using System.Runtime.CompilerServices;

namespace Fantasy
{
    public static class UnitManageHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(Unit unit)
        {
            unit.Scene.GetComponent<UnitManageComponent>().Add(unit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(Scene scene, long id)
        {
            return scene.GetComponent<UnitManageComponent>().Contains(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Unit Get(Scene scene,long id)
        {
            return scene.GetComponent<UnitManageComponent>().Get(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(Scene scene, long id, out Unit unit)
        {
            return scene.GetComponent<UnitManageComponent>().TryGet(id, out unit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove(Scene scene,long unitId, bool isDispose = true)
        {
            scene.GetComponent<UnitManageComponent>().Remove(unitId, isDispose);
        }
    }
}
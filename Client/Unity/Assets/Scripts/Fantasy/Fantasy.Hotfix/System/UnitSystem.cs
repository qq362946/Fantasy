using Fantasy.Model;

namespace Fantasy.Hotfix.System
{
    public static class UnitSystem
    {
        public static void ShowA(this UnitA unitA)
        {
            Log.Debug($"Age:{unitA.Age} Name1:{unitA.Name}");
        }
        
        public static void ShowB(this UnitB unitB)
        {
            Log.Debug($"Age:{unitB.Age} Name:{unitB.Name}");
        }
    }
}
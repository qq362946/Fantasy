
namespace Fantasy;

public sealed class CellDestroySystem : DestroySystem<Cell>
{
    protected override void Destroy(Cell self)
    {
        self.Units.Clear();
        self.SubsEnterEntities.Clear();
        self.SubsLeaveEntities.Clear();
    }
}

public static class CellSystem
{
    public static int GetCellPos(this float x)
    {
        // 实际坐标 * 缩放比 / 单位格子尺寸
        return (int) (x * AOIComponent.CellUnitLen) / AOIComponent.CellSize;
    }
        
    public static void Add(this Cell self, AOIEntity aoiEntity)
    {
        self.Units.Add(aoiEntity.Id, aoiEntity);
    }

    public static void Remove(this Cell self, AOIEntity aoiEntity)
    {
        self.Units.Remove(aoiEntity.Id);
    }

    public static string CellIdToString(this long cellId)
    {
        var y = (int) (cellId & 0xffffffff);
        var x = (int) ((ulong) cellId >> 32);
        return $"{x}:{y}";
    }
}
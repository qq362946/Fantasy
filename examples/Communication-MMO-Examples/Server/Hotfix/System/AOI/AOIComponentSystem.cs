namespace Fantasy;

public sealed class AOIComponentDestroySystem : DestroySystem<AOIComponent>
{
    protected override void Destroy(AOIComponent self)
    {
        foreach (var (_, cell) in self.Cells)
        {
            cell.Dispose();
        }
            
        self.Cells.Clear();
    }
}

public static class AOIComponentSystem
{
    public static void Add(this AOIComponent self, float x, float y, AOIEntity aoiEntity)
    {
        var cellX = x.GetCellPos();
        var cellY = y.GetCellPos();

        if (aoiEntity.ViewDistance == 0)
        {
            aoiEntity.ViewDistance = 1;
        }

        // 计算订阅进入和离开格子
        AOIHelper.EnterAndLeaveCell(aoiEntity, cellX, cellY, aoiEntity.SubEnterCells, aoiEntity.SubLeaveCells);
        // 将进入格子中的Unit添加到视野中
        self.EnterSightAndNoticeClient(aoiEntity, aoiEntity.SubEnterCells, false);
        // 订阅离开格子事件
        foreach (var cellId in aoiEntity.SubLeaveCells)
        {
            var cell = self.GetCell(cellId);
            aoiEntity.SubLeave(cell);
        }
        // 把自己添加到格子中
        var selfCell = self.GetCell(AOIHelper.ToCellId(cellX, cellY));
        aoiEntity.Cell = selfCell;
        selfCell.Add(aoiEntity);
        // 通知订阅该Cell Enter的Unit
        self.EnterSightAndNoticeClient(selfCell.SubsEnterEntities.Values, aoiEntity, 0, false);
    }

    public static void Remove(this AOIComponent self, AOIEntity aoiEntity)
    {
        // 销毁场景时AOIEntity会触发Remove，这个时候Scene获取不到Server
        if (aoiEntity.Cell == null || self.IsDisposed)
        {
            return;
        }
        // 通知订阅该Cell Leave的Unit
        aoiEntity.Cell.Remove(aoiEntity);
        // 通知格子内Unit aoiEntity离开视野
        self.LeaveSightAndNoticeClient(aoiEntity.Cell.SubsLeaveEntities.Values, aoiEntity, 0, false);
        // 通知自己订阅的Cell
        foreach (var cellId in aoiEntity.SubEnterCells)
        {
            var cell = self.GetCell(cellId);
            aoiEntity.UnSubEnter(cell);
        }
        // 通知aoiEntity 格子内玩家离开视野
        self.LeaveSightAndNoticeClient(aoiEntity, aoiEntity.SubLeaveCells);
        // 检查是否有错误
        if (aoiEntity.SeeUnits.Count > 1)
        {
            Log.Error($"aoiEntity has see units: {aoiEntity.SeeUnits.Count}");
        }
        if (aoiEntity.BeSeeUnits.Count > 1)
        {
            Log.Error($"aoiEntity has beSee units: {aoiEntity.BeSeeUnits.Count}");
        }
    }

    private static void EnterSightAndNoticeClient(this AOIComponent self, AOIEntity aoiEntity, IEnumerable<long> cells, bool checkEnterCell)
    {
        using var list = ListPool<AOIEntity>.Create();
        foreach (var cellId in cells)
        {
            if (checkEnterCell && aoiEntity.SubEnterCells.Contains(cellId))
            {
                continue;
            }

            var cell = self.GetCell(cellId);
            aoiEntity.SubEnter(cell, list);
        }

        var unit = aoiEntity.Unit;
        // 不是玩家不下发
        if (unit.UnitType != UnitType.Player || list.Count <= 0)
        {
            return;
        }

        var m2cUnitCreate = new M2C_UnitCreate();

        foreach (var enterAoi in list)
        {
            var enter = enterAoi.Unit;
            var unitInfo = enter.CreateUnitInfo();
            m2cUnitCreate.UnitInfos.Add(unitInfo);
        }

        MessageHelper.SendInnerRoute(unit.Scene, unit.SessionRuntimeId, m2cUnitCreate);
    }

    public static void EnterSightAndNoticeClient(this AOIComponent self, IEnumerable<AOIEntity> aoiEntities, AOIEntity enter, long exceptCell, bool checkExcept)
    {
        var scene = self.Scene;
        using var list = ListPool<Unit>.Create();
        foreach (var subEnterAoiEntity in aoiEntities)
        {
            if (checkExcept && subEnterAoiEntity.SubEnterCells.Contains(exceptCell))
            {
                continue;
            }

            var b = subEnterAoiEntity.EnterSight(enter);
            var unit = subEnterAoiEntity.Unit;

            if (unit.UnitType != UnitType.Player || !b)
            {
                continue;
            }

            list.Add(unit);
        }

        var enterUnit = enter.Unit;

        if (list.Count <= 0)
        {
            return;
        }

        // 广播给客户端
        var m2cUnitCreate = new M2C_UnitCreate();
        var unitInfo = enterUnit.CreateUnitInfo();
        m2cUnitCreate.UnitInfos.Add(unitInfo);

        foreach (var unit in list)
        {
            MessageHelper.SendInnerRoute(scene, unit.SessionRuntimeId, m2cUnitCreate);
        }
    }

    private static void LeaveSightAndNoticeClient(this AOIComponent self, AOIEntity aoiEntity, IEnumerable<long> cells)
    {
        using var list = ListPool<AOIEntity>.Create();
        foreach (var cellId in cells)
        {
            var cell = self.GetCell(cellId);
            aoiEntity.UnSubLeave(cell, list);
        }

        var unit = aoiEntity.Unit;

        // 不是玩家不下发
        if (unit.UnitType != UnitType.Player || list.Count <= 0)
        {
            return;
        }

        var m2cUnitRemove = new M2C_UnitRemove();

        foreach (var leaveAoi in list)
        {
            var leave = leaveAoi.Unit;
            m2cUnitRemove.UnitIds.Add(leave.Id);
        }

        MessageHelper.SendInnerRoute(self.Scene, unit.SessionRuntimeId, m2cUnitRemove);
    }

    public static void LeaveSightAndNoticeClient(this AOIComponent self, IEnumerable<AOIEntity> aoiEntities, AOIEntity leave, long exceptCell, bool checkExcept)
    {
        using var list = ListPool<Unit>.Create();
        foreach (var subLeaveAoiEntity in aoiEntities)
        {
            if (checkExcept && subLeaveAoiEntity.SubLeaveCells.Contains(exceptCell))
            {
                continue;
            }

            var b = subLeaveAoiEntity.LeaveSight(leave);
            var unit = subLeaveAoiEntity.Unit;
            // 不是玩家不通知
            if (unit.UnitType != UnitType.Player || !b)
            {
                continue;
            }

            list.Add(unit);
        }

        var leaveUnit = leave.Unit;

        if (list.Count <= 0)
        {
            return;
        }

        // 广播给客户端
        var m2cUnitRemove = new M2C_UnitRemove();
        m2cUnitRemove.UnitIds.Add(leaveUnit.Id);

        foreach (var unit in list)
        {
            MessageHelper.SendInnerRoute(self.Scene, unit.SessionRuntimeId, m2cUnitRemove);
        }
    }

    private static Cell GetCell(this AOIComponent self, long cellId)
    {
        if (self.Cells.TryGetValue(cellId, out var cell))
        {
            return cell;
        }

        cell = Entity.Create<Cell>(self.Scene, cellId);
        self.Cells.Add(cellId, cell);
        return cell;
    }
}
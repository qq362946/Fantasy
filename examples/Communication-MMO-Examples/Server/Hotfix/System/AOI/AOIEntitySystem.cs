namespace Fantasy;

public sealed class AOIEntityAwakeSystem : AwakeSystem<AOIEntity>
{
    protected override void Awake(AOIEntity self)
    {
        self.ViewDistance = 1;
    }
}
    
public sealed class AOIEntityDestroySystem : DestroySystem<AOIEntity>
{
    protected override void Destroy(AOIEntity self)
    {
        self.Scene.GetComponent<AOIComponent>().Remove(self);
        self.ViewDistance = 0;
        self.SeeUnits.Clear();
        self.SeePlayers.Clear();
        self.BeSeePlayers.Clear();
        self.BeSeeUnits.Clear();
        self.SubEnterCells.Clear();
        self.SubLeaveCells.Clear();
        self.Cell = null;
    }
}

public static class AOIEntitySystem
{
    /// <summary>
    /// unit进入self
    /// </summary>
    /// <param name="self"></param>
    /// <param name="cell"></param>
    /// <param name="list"></param>
    public static void SubEnter(this AOIEntity self, Cell cell, List<AOIEntity> list)
    {
        cell.SubsEnterEntities.Add(self.Id, self);

        foreach (var (key, aoiEntity) in cell.Units)
        {
            if (key == self.Id || aoiEntity.RuntimeId == 0)
            {
                continue;
            }

            var b = self.EnterSight(aoiEntity);

            if (b)
            {
                list.Add(aoiEntity);
            }
        }
    }

    public static void UnSubEnter(this AOIEntity self, Cell cell)
    {
        cell.SubsEnterEntities.Remove(self.Id);
    }

    public static void SubLeave(this AOIEntity self, Cell cell)
    {
        cell.SubsLeaveEntities.Add(self.Id, self);
    }

    /// <summary>
    /// unit离开self视野
    /// </summary>
    /// <param name="self"></param>
    /// <param name="cell"></param>
    /// <param name="list"></param>
    public static void UnSubLeave(this AOIEntity self, Cell cell, List<AOIEntity> list)
    {
        foreach (var (key, aoiEntity) in cell.Units)
        {
            if (key == self.Id)
            {
                continue;
            }

            bool b = self.LeaveSight(aoiEntity);

            if (b)
            {
                list.Add(aoiEntity);
            }
        }

        cell.SubsLeaveEntities.Remove(self.Id);
    }

    /// <summary>
    /// unit进入self视野
    /// </summary>
    /// <param name="self"></param>
    /// <param name="enter"></param>
    public static bool EnterSight(this AOIEntity self, AOIEntity enter)
    {
        // 有可能之前在Enter，后来出了Enter还在LeaveCell，这样仍然没有删除，继续进来Enter，这种情况不需要处理

        if (self.Id == enter.Id || self.SeeUnits.ContainsKey(enter.Id))
        {
            return false;
        }

        if (self.Unit.UnitType == UnitType.Player)
        {
            self.SeeUnits.Add(enter.Id, enter);
            enter.BeSeeUnits.Add(self.Id, self);
            enter.BeSeePlayers.Add(self.Id, self);

            if (enter.Unit.UnitType == UnitType.Player)
            {
                self.AddToSeePlayer(enter);
            }
        }
        else
        {
            self.SeeUnits.Add(enter.Id, enter);
            enter.BeSeeUnits.Add(self.Id, self);

            if (enter.Unit.UnitType == UnitType.Player)
            {
                self.AddToSeePlayer(enter);
            }
        }

        EventSystem.Instance.Publish(new UnitEnterSightRange
        {
            Unit = self,
            Enter = enter
        });

        return true;
    }

    /// <summary>
    /// unit离开self视野
    /// </summary>
    /// <param name="self"></param>
    /// <param name="leave"></param>
    public static bool LeaveSight(this AOIEntity self, AOIEntity leave)
    {
        if (self.Id == leave.Id || !self.SeeUnits.ContainsKey(leave.Id))
        {
            return false;
        }

        self.SeeUnits.Remove(leave.Id);

        if (leave.Unit.UnitType == UnitType.Player)
        {
            self.RemoveSeePlayer(leave.Id);
        }

        leave.BeSeeUnits.Remove(self.Id);

        if (self.Unit.UnitType == UnitType.Player)
        {
            leave.BeSeePlayers.Remove(self.Id);

            EventSystem.Instance.Publish(new UnitBeSeeByPlayerRemove
            {
                Unit = self,
                Leave = leave
            });
        }

        EventSystem.Instance.Publish(new UnitLeaveSightRange
        {
            Unit = self,
            Leave = leave
        });

        return true;
    }

    public static void RemoveSeePlayer(this AOIEntity self, long unitId)
    {
        if (self == null)
        {
            return;
        }

        self.SeePlayers.Remove(unitId);
    }

    public static void AddToSeePlayer(this AOIEntity self, AOIEntity aoiEntity)
    {
        var unitId = aoiEntity.Id;
        var unit = (Unit)self.Parent;
        self.SeePlayers[unitId] = aoiEntity;
    }
}
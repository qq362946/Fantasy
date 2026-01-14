namespace Fantasy
{
    public enum UnitType
    {
        None = 0,
        Player = 1,
        Monster = 2,
        Boss = 3,
        Npc = 4,
    }

    public enum UnitMoveState
    {
        None = 0,
        /// <summary>
        /// 开始移动
        /// </summary>
        StartMove = 1,
        /// <summary>
        /// 移动中
        /// </summary>
        Moving = 2,
        /// <summary>
        /// 停止移动
        /// </summary>
        Stop = 3,
        /// <summary>
        /// 到达目标点
        /// </summary>
        TargetPoint = 4,
    }
}
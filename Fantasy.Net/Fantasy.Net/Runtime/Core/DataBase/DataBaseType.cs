// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
#if FANTASY_NET

namespace Fantasy.Database
{
    /// <summary>
    /// 数据库类型（通过位与计算, 支持多选）
    /// </summary>
    [Flags]
    public enum DatabaseType: byte
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,
        /// <summary>
        /// 任意
        /// </summary>
        Any = PostgreSQL | MongoDB,
        /// <summary>
        /// PostgreSQL, 适用于强关系型数据的存储、复杂跨表聚合、强事务性操作
        /// </summary>
        PostgreSQL = 1 << 0,
        /// <summary>
        /// MongoDB, 适用于弱关系型、非结构化的、文档型数据存储和查询
        /// </summary>
        MongoDB = 1 << 1,
    }
}
#endif
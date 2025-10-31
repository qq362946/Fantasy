using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// [FantasyDbSet] 是 Fantasy 对原生 [Table]标签的扩展, 设计用于加强对数据库的分表、表迁移等情况的管理。
    /// （在任意类上打上这个标签, 框架会根据其中设置的信息为该类建立 ORM 映射模型，）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class FantasyDbSetAttribute : TableAttribute
    {
        /// <summary>
        /// 数据库选择, 默认为任意, 即所有数据库均可以操作这个类
        /// </summary>
        public DatabaseType DbSelection = DatabaseType.Any;
        /// <summary>
        /// 表描述
        /// </summary>
        public string? Description { get; set; }

        #region TODO-LIST 这些特性以后再做
        ///// <summary>
        ///// 禁止整表更新
        ///// </summary>
        //public bool NoBulkUpdate { get; set; } = false;

        ///// <summary>
        ///// 禁止整表删除
        ///// </summary>
        //public bool NoBulkDelete { get; set; } = false;

        ///// <summary>
        ///// 是否启用分表
        ///// </summary>
        //public bool EnableSharding { get; set; } = false;
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        public FantasyDbSetAttribute(string? name = null) : base(name ?? "")
        {
            
        }

        /// <summary>
        /// 检查标签中的数据库选项是否包含数据库
        /// </summary>
        /// <returns></returns>
        public bool IfSelectionContainsDbType(DatabaseType dbType)
        {
            return (DbSelection & DatabaseType.MongoDB) == DatabaseType.MongoDB;
        }
    }
}

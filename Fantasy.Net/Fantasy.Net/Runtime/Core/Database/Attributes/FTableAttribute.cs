using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// [FTable] 是 Fantasy 对原生 [Table]标签的扩展, 设计用于加强对SQL数据库的分表、表迁移等情况的管理。
    /// 打上这个标签, 框架会为该类建立"Entity-Table"映射。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class FTableAttribute : TableAttribute
    {
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
        public FTableAttribute(string? name = null) : base(name ?? "")
        {
            
        }
    }
}
